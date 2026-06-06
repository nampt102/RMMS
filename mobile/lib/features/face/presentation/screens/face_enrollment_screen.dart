import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/face_repository.dart';

/// One face angle to capture during enrollment (M06, ADR-011 CompreFace).
enum _Angle { front, left, right }

/// 3-angle face enrollment wizard. Captures front/left/right selfies with the
/// front camera and submits them to `POST /face/enroll`. Replaces any prior
/// enrollment server-side, so this same screen is reused for re-enrollment.
class FaceEnrollmentScreen extends ConsumerStatefulWidget {
  const FaceEnrollmentScreen({super.key});

  @override
  ConsumerState<FaceEnrollmentScreen> createState() =>
      _FaceEnrollmentScreenState();
}

class _FaceEnrollmentScreenState extends ConsumerState<FaceEnrollmentScreen> {
  final _picker = ImagePicker();
  final Map<_Angle, String> _paths = {};
  bool _submitting = false;

  int get _captured => _paths.length;
  bool get _canSubmit => _captured == _Angle.values.length && !_submitting;

  Future<void> _capture(_Angle angle) async {
    final file = await _picker.pickImage(
      source: ImageSource.camera,
      preferredCameraDevice: CameraDevice.front,
      imageQuality: 80,
      maxWidth: 720,
    );
    if (file == null || !mounted) return;
    setState(() => _paths[angle] = file.path);
  }

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    setState(() => _submitting = true);
    try {
      // Deterministic order: front, left, right.
      final paths = _Angle.values.map((a) => _paths[a]!).toList(growable: false);
      await ref.read(faceRepositoryProvider).enroll(paths);
      ref.invalidate(faceStatusProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(l.faceEnrollSuccess)));
      context.pop();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  String _angleLabel(AppLocalizations l, _Angle a) => switch (a) {
        _Angle.front => l.faceAngleFront,
        _Angle.left => l.faceAngleLeft,
        _Angle.right => l.faceAngleRight,
      };

  IconData _angleIcon(_Angle a) => switch (a) {
        _Angle.front => Icons.face_outlined,
        _Angle.left => Icons.turn_left_outlined,
        _Angle.right => Icons.turn_right_outlined,
      };

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final total = _Angle.values.length;

    return Scaffold(
      appBar: AppBar(title: Text(l.faceEnrollTitle)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 120),
          children: [
            // Hero / progress ------------------------------------------------
            SoftCard(
              gradient: context.semantics.brandGradient,
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        width: 48,
                        height: 48,
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.18),
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: const Icon(Icons.face_retouching_natural,
                            color: AppPalette.white, size: 26),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Text(
                          l.faceEnrollIntro,
                          style: const TextStyle(
                            color: AppPalette.white,
                            height: 1.4,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(999),
                    child: LinearProgressIndicator(
                      value: _captured / total,
                      minHeight: 8,
                      backgroundColor: Colors.white.withValues(alpha: 0.25),
                      valueColor:
                          const AlwaysStoppedAnimation(AppPalette.white),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    l.faceProgress(_captured, total),
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.9),
                      fontWeight: FontWeight.w600,
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Angle capture tiles -------------------------------------------
            for (final angle in _Angle.values) ...[
              _AngleTile(
                label: _angleLabel(l, angle),
                icon: _angleIcon(angle),
                path: _paths[angle],
                onTap: () => _capture(angle),
              ),
              const SizedBox(height: 12),
            ],

            const SizedBox(height: 8),
            _GuidelinesCard(),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.all(16),
        child: FilledButton.icon(
          onPressed: _canSubmit ? _submit : null,
          icon: _submitting
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: AppPalette.white),
                )
              : const Icon(Icons.verified_user_outlined),
          label: Text(l.faceEnrollSubmit),
          style: FilledButton.styleFrom(backgroundColor: scheme.primary),
        ),
      ),
    );
  }
}

class _AngleTile extends StatelessWidget {
  const _AngleTile({
    required this.label,
    required this.icon,
    required this.path,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final String? path;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final done = path != null;
    final radius = BorderRadius.circular(20);

    return DecoratedBox(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: radius,
        border: Border.all(
          color: done ? context.semantics.success : scheme.outlineVariant,
          width: done ? 1.5 : 1,
        ),
      ),
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                // Thumbnail / icon
                Container(
                  width: 56,
                  height: 56,
                  clipBehavior: Clip.antiAlias,
                  decoration: BoxDecoration(
                    color: scheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: done
                      ? Image.file(File(path!), fit: BoxFit.cover)
                      : Icon(icon, color: scheme.onSurfaceVariant, size: 26),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(label,
                          style: const TextStyle(
                              fontSize: 15, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 4),
                      done
                          ? StatusPill(
                              label: l.faceCaptured,
                              icon: Icons.check_circle,
                              tone: BrandTone.success,
                            )
                          : Text(l.faceTapToCapture,
                              style: TextStyle(
                                  fontSize: 13,
                                  color: scheme.onSurfaceVariant)),
                    ],
                  ),
                ),
                Icon(
                  done ? Icons.refresh : Icons.photo_camera_outlined,
                  color: done ? scheme.onSurfaceVariant : scheme.primary,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _GuidelinesCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final items = [l.faceGuideline1, l.faceGuideline2, l.faceGuideline3];

    return DecoratedBox(
      decoration: BoxDecoration(
        color: scheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.lightbulb_outline,
                    size: 18, color: scheme.onSurfaceVariant),
                const SizedBox(width: 8),
                Text(l.faceGuidelinesTitle,
                    style: const TextStyle(fontWeight: FontWeight.w700)),
              ],
            ),
            const SizedBox(height: 10),
            for (final item in items)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 3),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.check, size: 16, color: context.semantics.success),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(item,
                          style: TextStyle(
                              fontSize: 13.5,
                              height: 1.35,
                              color: scheme.onSurfaceVariant)),
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
