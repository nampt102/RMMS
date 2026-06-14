import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/face_repository.dart';

/// One face angle to capture during enrollment (M06, ADR-011 CompreFace).
enum _Angle { front, left, right }

/// Redesign 2026 — Khuôn mặt (pushed).
///
/// Status header (gradient face circle + check badge when enrolled + chips)
/// then the 3-angle capture flow. Sticky bottom = primary CTA ("Đăng ký
/// khuôn mặt" / "Đăng ký lại") + destructive-soft "Xóa khuôn mặt" when
/// enrolled. The capture logic is preserved from the prior implementation.
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
  bool _deleting = false;

  int get _captured => _paths.length;
  bool get _allCaptured => _captured == _Angle.values.length;

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
      final paths = _Angle.values.map((a) => _paths[a]!).toList();
      await ref.read(faceRepositoryProvider).enroll(paths);
      ref.invalidate(faceStatusProvider);
      if (!mounted) return;
      showAppToast(context,
          message: l.faceEnrollSuccess, kind: AppToastKind.success);
      context.pop();
    } on ApiException catch (e) {
      if (mounted) {
        showAppToast(context, message: e.message, kind: AppToastKind.error);
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Future<void> _delete() async {
    final l = AppLocalizations.of(context);
    setState(() => _deleting = true);
    try {
      await ref.read(faceRepositoryProvider).remove();
      ref.invalidate(faceStatusProvider);
      if (!mounted) return;
      showAppToast(context, message: l.faceDeleted, kind: AppToastKind.success);
    } on ApiException catch (e) {
      if (mounted) {
        showAppToast(context, message: e.message, kind: AppToastKind.error);
      }
    } finally {
      if (mounted) setState(() => _deleting = false);
    }
  }

  String _angleLabel(AppLocalizations l, _Angle a) => switch (a) {
        _Angle.front => l.faceAngleFront,
        _Angle.left => l.faceAngleLeft,
        _Angle.right => l.faceAngleRight,
      };

  IconData _angleIcon(_Angle a) => switch (a) {
        _Angle.front => Icons.face_rounded,
        _Angle.left => Icons.turn_left_rounded,
        _Angle.right => Icons.turn_right_rounded,
      };

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final statusAsync = ref.watch(faceStatusProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.faceTitle),
            Expanded(
              child: statusAsync.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => _Error(
                  message: e is ApiException ? e.message : l.commonRetry,
                  onRetry: () => ref.invalidate(faceStatusProvider),
                ),
                data: (status) => ListView(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                  children: [
                    _StatusHeader(
                      enrolled: status.enrolled,
                      enrolledAt: status.enrolledAt,
                      lang: lang,
                    ),
                    const SizedBox(height: 22),
                    Text(
                      l.faceEnrollIntro,
                      style: GoogleFonts.plusJakartaSans(
                        color: AppPalette.muted,
                        fontSize: 14.5,
                        fontWeight: FontWeight.w600,
                        height: 1.45,
                      ),
                    ),
                    const SizedBox(height: 14),
                    for (final angle in _Angle.values) ...[
                      _AngleTile(
                        label: _angleLabel(l, angle),
                        icon: _angleIcon(angle),
                        path: _paths[angle],
                        onTap: () => _capture(angle),
                      ),
                      const SizedBox(height: 10),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: statusAsync.when(
          loading: () => const SizedBox.shrink(),
          error: (_, __) => const SizedBox.shrink(),
          data: (status) => Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                AppButton.primary(
                  label: status.enrolled ? l.faceReenroll : l.faceEnrollCta,
                  icon: Icons.face_rounded,
                  loading: _submitting,
                  onPressed: _allCaptured && !_deleting ? _submit : null,
                ),
                if (status.enrolled) ...[
                  const SizedBox(height: 10),
                  AppButton.destructiveSoft(
                    label: l.faceDelete,
                    icon: Icons.delete_outline_rounded,
                    loading: _deleting,
                    onPressed: _submitting ? null : _delete,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ─── Status header ────────────────────────────────────────────────────────

class _StatusHeader extends StatelessWidget {
  const _StatusHeader({
    required this.enrolled,
    required this.enrolledAt,
    required this.lang,
  });
  final bool enrolled;
  final DateTime? enrolledAt;
  final String lang;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;

    return Column(
      children: [
        // Big gradient circle with optional emerald check badge.
        SizedBox(
          width: 180,
          height: 180,
          child: Stack(
            alignment: Alignment.center,
            children: [
              Container(
                width: 160,
                height: 160,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: s.brandGradient,
                  boxShadow: s.shadowBrand,
                ),
                alignment: Alignment.center,
                child: const Icon(Icons.face_rounded,
                    color: Colors.white, size: 84),
              ),
              if (enrolled)
                Positioned(
                  right: 8,
                  bottom: 8,
                  child: Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      gradient: s.emeraldGradient,
                      shape: BoxShape.circle,
                      border:
                          Border.all(color: AppPalette.bg, width: 4),
                    ),
                    alignment: Alignment.center,
                    child: const Icon(Icons.check_rounded,
                        color: Colors.white, size: 22),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        Text(
          enrolled ? l.faceRegistered : l.faceNotRegistered,
          textAlign: TextAlign.center,
          style: GoogleFonts.spaceGrotesk(
            color: AppPalette.ink,
            fontSize: 22,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.5,
          ),
        ),
        const SizedBox(height: 6),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Text(
            enrolled ? l.faceHelperRegistered : l.faceHelperNotRegistered,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 13.5,
              fontWeight: FontWeight.w600,
              height: 1.45,
            ),
          ),
        ),
        if (enrolled) ...[
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            alignment: WrapAlignment.center,
            children: [
              AppChip(
                  icon: Icons.verified_rounded,
                  label: l.faceChipVerified,
                  tone: AppTone.emerald),
              if (enrolledAt != null)
                AppChip(
                  icon: Icons.event_rounded,
                  label: l.faceChipUpdated(
                      DateFormat.MMMd(lang).format(enrolledAt!.toLocal())),
                  tone: AppTone.indigo,
                ),
            ],
          ),
        ],
      ],
    );
  }
}

// ─── Angle tile ───────────────────────────────────────────────────────────

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
    final done = path != null;

    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.fromLTRB(12, 12, 14, 12),
      child: Row(
        children: [
          Container(
            width: 56,
            height: 56,
            clipBehavior: Clip.antiAlias,
            decoration: BoxDecoration(
              color: AppPalette.surface2,
              borderRadius: BorderRadius.circular(14),
            ),
            child: done
                ? Image.file(File(path!), fit: BoxFit.cover)
                : Icon(icon, color: AppPalette.muted, size: 26),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  label,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.ink,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                done
                    ? AppChip(
                        label: l.faceCaptured,
                        icon: Icons.check_circle_rounded,
                        tone: AppTone.emerald,
                      )
                    : Text(
                        l.faceTapToCapture,
                        style: GoogleFonts.plusJakartaSans(
                          color: AppPalette.muted,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
              ],
            ),
          ),
          Icon(
            done ? Icons.refresh_rounded : Icons.photo_camera_rounded,
            color: done ? AppPalette.muted : AppPalette.indigo,
            size: 22,
          ),
        ],
      ),
    );
  }
}

// ─── Error ────────────────────────────────────────────────────────────────

class _Error extends StatelessWidget {
  const _Error({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return ListView(
      children: [
        const SizedBox(height: 60),
        const AppIconTile(
          icon: Icons.error_outline_rounded,
          tone: AppTone.rose,
          size: 64,
          radius: 22,
        ),
        const SizedBox(height: 16),
        Text(message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
                color: AppPalette.ink,
                fontSize: 14.5,
                fontWeight: FontWeight.w600)),
        const SizedBox(height: 16),
        Center(
          child: AppButton.soft(
            label: l.commonRetry,
            icon: Icons.refresh_rounded,
            expand: false,
            onPressed: onRetry,
          ),
        ),
      ],
    );
  }
}
