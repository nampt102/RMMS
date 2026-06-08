import 'dart:async';
import 'dart:io';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';

/// The randomised liveness action a user must perform (ADR-013). A static photo
/// cannot satisfy any of these, which is what rejects photo-of-photo / screen replay.
enum LivenessChallenge { blink, smile, turnHead }

/// Full-screen front-camera capture gated by an active-liveness challenge.
///
/// Flow: open the front camera → stream frames to ML Kit face detection → once the
/// (randomly chosen) challenge is satisfied, stop the stream, take a still photo and
/// pop with its file path. Pops `null` on cancel / permission denied / timeout.
///
/// Push it and await the result:
/// ```dart
/// final path = await Navigator.of(context).push<String>(
///   MaterialPageRoute(builder: (_) => const LivenessCaptureScreen()));
/// ```
class LivenessCaptureScreen extends StatefulWidget {
  const LivenessCaptureScreen({super.key, this.timeout = const Duration(seconds: 30)});

  final Duration timeout;

  @override
  State<LivenessCaptureScreen> createState() => _LivenessCaptureScreenState();
}

class _LivenessCaptureScreenState extends State<LivenessCaptureScreen> with WidgetsBindingObserver {
  CameraController? _controller;
  FaceDetector? _detector;
  Timer? _timeoutTimer;

  // Pick the challenge once per attempt. Index derives from the millisecond clock so it
  // varies between attempts without needing Random (kept deterministic-friendly).
  late LivenessChallenge _challenge;

  bool _initializing = true;
  bool _busy = false; // a frame is being analysed
  bool _capturing = false; // taking the still / popping
  bool _sawOpenEyes = false; // blink baseline: eyes were open before closing
  String? _error;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _challenge = LivenessChallenge
        .values[DateTime.now().millisecondsSinceEpoch % LivenessChallenge.values.length];
    _start();
  }

  Future<void> _start() async {
    try {
      final cameras = await availableCameras();
      final front = cameras.firstWhere(
        (c) => c.lensDirection == CameraLensDirection.front,
        orElse: () => cameras.first,
      );

      final controller = CameraController(
        front,
        ResolutionPreset.medium,
        enableAudio: false,
        imageFormatGroup:
            Platform.isAndroid ? ImageFormatGroup.nv21 : ImageFormatGroup.bgra8888,
      );
      await controller.initialize();

      _detector = FaceDetector(
        options: FaceDetectorOptions(
          enableClassification: true, // smiling + eye-open probabilities
          performanceMode: FaceDetectorMode.fast,
        ),
      );

      _controller = controller;
      if (!mounted) return;
      setState(() => _initializing = false);

      await controller.startImageStream(_onFrame);
      _timeoutTimer = Timer(widget.timeout, _onTimeout);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    }
  }

  void _onTimeout() {
    if (!mounted || _capturing) return;
    setState(() => _error = 'timeout');
  }

  Future<void> _onFrame(CameraImage image) async {
    if (_busy || _capturing || _detector == null || _controller == null) return;
    _busy = true;
    try {
      final input = _toInputImage(image, _controller!.description);
      if (input == null) return;
      final faces = await _detector!.processImage(input);
      if (faces.isEmpty) return;
      if (_isChallengeMet(faces.first)) {
        await _captureAndReturn();
      }
    } catch (_) {
      // Drop this frame; the next one will be analysed.
    } finally {
      _busy = false;
    }
  }

  bool _isChallengeMet(Face face) {
    switch (_challenge) {
      case LivenessChallenge.smile:
        return (face.smilingProbability ?? 0) > 0.75;
      case LivenessChallenge.turnHead:
        return (face.headEulerAngleY ?? 0).abs() > 25;
      case LivenessChallenge.blink:
        final l = face.leftEyeOpenProbability;
        final r = face.rightEyeOpenProbability;
        if (l == null || r == null) return false;
        if (l > 0.7 && r > 0.7) _sawOpenEyes = true; // baseline
        return _sawOpenEyes && l < 0.2 && r < 0.2; // then a blink
    }
  }

  Future<void> _captureAndReturn() async {
    if (_capturing) return;
    _capturing = true;
    _timeoutTimer?.cancel();
    try {
      await _controller!.stopImageStream();
      final file = await _controller!.takePicture(); // still cannot be taken while streaming
      if (mounted) Navigator.of(context).pop(file.path);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
      _capturing = false;
    }
  }

  /// Converts a [CameraImage] to an ML Kit [InputImage] (single-plane nv21 / bgra8888).
  InputImage? _toInputImage(CameraImage image, CameraDescription camera) {
    final rotation = InputImageRotationValue.fromRawValue(camera.sensorOrientation);
    final format = InputImageFormatValue.fromRawValue(image.format.raw);
    if (rotation == null || format == null || image.planes.isEmpty) return null;

    return InputImage.fromBytes(
      bytes: image.planes.first.bytes,
      metadata: InputImageMetadata(
        size: Size(image.width.toDouble(), image.height.toDouble()),
        rotation: rotation,
        format: format,
        bytesPerRow: image.planes.first.bytesPerRow,
      ),
    );
  }

  void _retry() {
    setState(() {
      _error = null;
      _initializing = true;
      _busy = false;
      _capturing = false;
      _sawOpenEyes = false;
      _challenge = LivenessChallenge
          .values[DateTime.now().millisecondsSinceEpoch % LivenessChallenge.values.length];
    });
    _disposeCamera();
    _start();
  }

  Future<void> _disposeCamera() async {
    _timeoutTimer?.cancel();
    try {
      if (_controller?.value.isStreamingImages ?? false) {
        await _controller!.stopImageStream();
      }
    } catch (_) {}
    await _controller?.dispose();
    _controller = null;
    await _detector?.close();
    _detector = null;
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _disposeCamera();
    super.dispose();
  }

  String _promptFor(AppLocalizations l) => switch (_challenge) {
        LivenessChallenge.blink => l.livenessBlink,
        LivenessChallenge.smile => l.livenessSmile,
        LivenessChallenge.turnHead => l.livenessTurn,
      };

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);

    return Scaffold(
      backgroundColor: Colors.black,
      body: SafeArea(
        child: _error != null
            ? _ErrorView(
                message: _error == 'timeout' ? l.livenessTimeout : l.livenessCameraError,
                onRetry: _retry,
                onCancel: () => Navigator.of(context).pop(),
              )
            : _initializing || _controller == null
                ? const Center(child: CircularProgressIndicator(color: Colors.white))
                : Stack(
                    fit: StackFit.expand,
                    children: [
                      Center(child: CameraPreview(_controller!)),
                      // Top bar.
                      Positioned(
                        top: 8,
                        left: 8,
                        child: IconButton(
                          icon: const Icon(Icons.close_rounded, color: Colors.white),
                          onPressed: () => Navigator.of(context).pop(),
                        ),
                      ),
                      // Prompt.
                      Positioned(
                        left: 24,
                        right: 24,
                        bottom: 48,
                        child: Column(
                          children: [
                            Text(
                              l.livenessTitle,
                              style: GoogleFonts.plusJakartaSans(
                                color: Colors.white70,
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              _promptFor(l),
                              textAlign: TextAlign.center,
                              style: GoogleFonts.spaceGrotesk(
                                color: Colors.white,
                                fontSize: 24,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.4,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry, required this.onCancel});
  final String message;
  final VoidCallback onRetry;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.no_photography_rounded, color: Colors.white70, size: 56),
          const SizedBox(height: 16),
          Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: Colors.white,
              fontSize: 15,
              fontWeight: FontWeight.w600,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 24),
          AppButton.primary(label: l.commonRetry, icon: Icons.refresh_rounded, onPressed: onRetry),
          const SizedBox(height: 10),
          TextButton(
            onPressed: onCancel,
            child: Text(l.commonCancel, style: const TextStyle(color: Colors.white70)),
          ),
        ],
      ),
    );
  }
}
