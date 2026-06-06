import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/attendance_api.dart';
import '../../data/attendance_repository.dart';

enum CheckMode { checkIn, checkOut }

/// Arguments for the capture screen (passed via GoRouter `extra`).
class CheckCaptureArgs {
  const CheckCaptureArgs({
    required this.mode,
    required this.storeLabel,
    this.storeId,
    this.attendanceId,
  });

  final CheckMode mode;
  final String storeLabel;
  final String? storeId; // required for check-in
  final String? attendanceId; // required for check-out
}

/// Capture screen for both check-in and check-out (M05). Acquires GPS (with a
/// fake-GPS flag — BR-205), captures a selfie + store photo, then submits a
/// multipart request. No offline support (BR-210): submit requires connectivity.
class CheckInScreen extends ConsumerStatefulWidget {
  const CheckInScreen({super.key, required this.args});

  final CheckCaptureArgs args;

  @override
  ConsumerState<CheckInScreen> createState() => _CheckInScreenState();
}

class _CheckInScreenState extends ConsumerState<CheckInScreen> {
  final _picker = ImagePicker();
  final _noteController = TextEditingController();

  Position? _position;
  bool _locating = true;
  _LocErr? _locationError;
  String? _selfiePath;
  String? _storePhotoPath;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _acquireLocation();
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _acquireLocation() async {
    setState(() {
      _locating = true;
      _locationError = null;
    });
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        throw _LocationDenied(_LocErr.disabled);
      }
      var perm = await Geolocator.checkPermission();
      if (perm == LocationPermission.denied) {
        perm = await Geolocator.requestPermission();
      }
      if (perm == LocationPermission.denied || perm == LocationPermission.deniedForever) {
        throw _LocationDenied(_LocErr.permission);
      }
      final pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high),
      );
      if (!mounted) return;
      setState(() => _position = pos);
    } on _LocationDenied catch (e) {
      if (mounted) setState(() => _locationError = e.kind);
    } catch (_) {
      if (mounted) setState(() => _locationError = _LocErr.generic);
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  Future<void> _capture({required bool selfie}) async {
    final file = await _picker.pickImage(
      source: ImageSource.camera,
      preferredCameraDevice: selfie ? CameraDevice.front : CameraDevice.rear,
      imageQuality: 70,
      maxWidth: 1280,
    );
    if (file == null || !mounted) return;
    setState(() {
      if (selfie) {
        _selfiePath = file.path;
      } else {
        _storePhotoPath = file.path;
      }
    });
  }

  bool get _canSubmit =>
      _position != null && _selfiePath != null && _storePhotoPath != null && !_submitting;

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    final pos = _position;
    if (pos == null) return;
    setState(() => _submitting = true);

    final submission = AttendanceSubmission(
      latitude: pos.latitude,
      longitude: pos.longitude,
      accuracyMeters: pos.accuracy,
      fakeGpsDetected: pos.isMocked,
      selfiePath: _selfiePath,
      storePhotoPath: _storePhotoPath,
      note: _noteController.text,
    );

    try {
      final repo = ref.read(attendanceRepositoryProvider);
      if (widget.args.mode == CheckMode.checkIn) {
        await repo.checkIn(widget.args.storeId!, submission);
      } else {
        await repo.checkOut(widget.args.attendanceId!, submission);
      }
      ref.invalidate(todayShiftsProvider);
      ref.invalidate(attendanceHistoryProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(widget.args.mode == CheckMode.checkIn
            ? l.attendanceCheckInSuccess
            : l.attendanceCheckOutSuccess)),
      );
      context.pop();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final isCheckIn = widget.args.mode == CheckMode.checkIn;

    return Scaffold(
      appBar: AppBar(
        title: Text(isCheckIn ? l.attendanceCheckIn : l.attendanceCheckOut),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
          children: [
            Text(widget.args.storeLabel, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 16),
            _LocationCard(
              locating: _locating,
              error: _locationError,
              position: _position,
              onRetry: _acquireLocation,
            ),
            const SizedBox(height: 16),
            _PhotoTile(
              label: l.attendanceSelfie,
              path: _selfiePath,
              icon: Icons.face_outlined,
              onTap: () => _capture(selfie: true),
            ),
            const SizedBox(height: 12),
            _PhotoTile(
              label: l.attendanceStorePhoto,
              path: _storePhotoPath,
              icon: Icons.storefront_outlined,
              onTap: () => _capture(selfie: false),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _noteController,
              maxLength: 1000,
              maxLines: 2,
              decoration: InputDecoration(
                labelText: l.attendanceNoteOptional,
                border: const OutlineInputBorder(),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.all(16),
        child: FilledButton.icon(
          onPressed: _canSubmit ? _submit : null,
          icon: _submitting
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Icon(Icons.check),
          style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
          label: Text(isCheckIn ? l.attendanceCheckIn : l.attendanceCheckOut),
        ),
      ),
    );
  }
}

class _LocationCard extends StatelessWidget {
  const _LocationCard({
    required this.locating,
    required this.error,
    required this.position,
    required this.onRetry,
  });

  final bool locating;
  final _LocErr? error;
  final Position? position;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = Theme.of(context).colorScheme;

    Widget body;
    if (locating) {
      body = Row(children: [
        const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)),
        const SizedBox(width: 12),
        Text(l.attendanceLocating),
      ]);
    } else if (error != null) {
      final msg = switch (error!) {
        _LocErr.disabled => l.attendanceGpsDisabled,
        _LocErr.permission => l.attendanceGpsPermission,
        _LocErr.generic => l.attendanceGpsError,
      };
      body = Row(children: [
        Icon(Icons.location_off_outlined, color: scheme.error),
        const SizedBox(width: 12),
        Expanded(child: Text(msg, style: TextStyle(color: scheme.error))),
        TextButton(onPressed: onRetry, child: Text(l.commonRetry)),
      ]);
    } else if (position != null) {
      final p = position!;
      body = Row(children: [
        Icon(p.isMocked ? Icons.gpp_bad_outlined : Icons.gps_fixed, color: p.isMocked ? scheme.error : scheme.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Text(p.isMocked
              ? l.attendanceFakeGpsWarning
              : '${p.latitude.toStringAsFixed(5)}, ${p.longitude.toStringAsFixed(5)} · ±${p.accuracy.toStringAsFixed(0)}m'),
        ),
      ]);
    } else {
      body = Text(l.attendanceGpsError);
    }

    return Card(child: Padding(padding: const EdgeInsets.all(16), child: body));
  }
}

class _PhotoTile extends StatelessWidget {
  const _PhotoTile({
    required this.label,
    required this.path,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final String? path;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final done = path != null;
    return Card(
      child: ListTile(
        onTap: onTap,
        leading: Icon(icon, color: done ? scheme.primary : scheme.outline),
        title: Text(label),
        subtitle: done
            ? Text(AppLocalizations.of(context).attendancePhotoCaptured)
            : Text(AppLocalizations.of(context).attendanceTapToCapture),
        trailing: Icon(done ? Icons.check_circle : Icons.photo_camera_outlined,
            color: done ? Colors.green : scheme.outline),
      ),
    );
  }
}

enum _LocErr { disabled, permission, generic }

class _LocationDenied implements Exception {
  _LocationDenied(this.kind);
  final _LocErr kind;
}
