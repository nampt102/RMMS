import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/attendance_repository.dart';
import '../../domain/attendance.dart';
import 'check_in_screen.dart';

/// Redesign 2026 — Chấm công (pushed; also the centre bottom-nav FAB).
///
/// AppTopBar + context chips (store + emerald "Trong khu vực"); large face
/// target with a soft gradient halo + rotating dashed ring + 150 circular
/// avatar (switches to grad-emerald + check when checked in); big Space
/// Grotesk 44/700 clock + localised date; "VÀO CA" / "RA CA" cards with
/// times or "--:--"; sticky primary CTA.
class AttendanceTodayScreen extends ConsumerWidget {
  const AttendanceTodayScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(todayShiftsProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(
              title: l.attendanceShort,
              trailing: [
                PressScale(
                  onTap: () => context.push(AppRoutes.attendanceHistory),
                  child: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: AppPalette.surface,
                      borderRadius: BorderRadius.circular(13),
                      boxShadow: context.semantics.shadowSm,
                    ),
                    alignment: Alignment.center,
                    child: const Icon(Icons.history_rounded,
                        color: AppPalette.ink, size: 20),
                  ),
                ),
              ],
            ),
            Expanded(
              child: async.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => _Error(
                  message: e is ApiException ? e.message : l.commonRetry,
                  onRetry: () => ref.invalidate(todayShiftsProvider),
                ),
                data: (shifts) {
                  if (shifts.isEmpty) {
                    return _Empty(message: l.attendanceNoShifts);
                  }
                  return _Body(shift: shifts.first);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Body extends StatefulWidget {
  const _Body({required this.shift});
  final TodayShift shift;

  @override
  State<_Body> createState() => _BodyState();
}

class _BodyState extends State<_Body> with TickerProviderStateMixin {
  Timer? _clock;
  late final AnimationController _ring = AnimationController(
    vsync: this,
    duration: const Duration(seconds: 12),
  )..repeat();

  @override
  void initState() {
    super.initState();
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _clock?.cancel();
    _ring.dispose();
    super.dispose();
  }

  String _clockText() {
    final n = DateTime.now();
    return '${n.hour.toString().padLeft(2, '0')}:'
        '${n.minute.toString().padLeft(2, '0')}:'
        '${n.second.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final reduce = MediaQuery.disableAnimationsOf(context);
    final shift = widget.shift;
    final checkedIn = shift.isCheckedIn;
    final timeFmt = DateFormat.Hm(lang);

    final storeLabel = '${shift.storeCode} · ${shift.storeName}';

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        // Context chips.
        Row(
          children: [
            Expanded(
              child: AppChip(
                icon: Icons.place_rounded,
                label: storeLabel,
                tone: AppTone.indigo,
              ),
            ),
            const SizedBox(width: 8),
            AppChip(
              icon: Icons.location_on_rounded,
              label: l.attendanceInArea,
              tone: AppTone.emerald,
            ),
          ],
        ),
        const SizedBox(height: 18),
        // Face target.
        Center(
          child: _FaceTarget(
            checkedIn: checkedIn,
            ring: _ring,
            reduce: reduce,
          ),
        ),
        const SizedBox(height: 22),
        // Big clock + date.
        Center(
          child: Text(
            _clockText(),
            style: GoogleFonts.spaceGrotesk(
              color: AppPalette.ink,
              fontSize: 44,
              fontWeight: FontWeight.w700,
              letterSpacing: -1.0,
              height: 1.0,
              fontFeatures: const [FontFeature.tabularFigures()],
            ),
          ),
        ),
        const SizedBox(height: 6),
        Center(
          child: Text(
            DateFormat.yMMMEd(lang).format(DateTime.now()),
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 13.5,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.2,
            ),
          ),
        ),
        const SizedBox(height: 22),
        // In/out cards.
        Row(
          children: [
            Expanded(
              child: _IoCard(
                title: l.attendanceInLabel,
                value: shift.checkInAt != null
                    ? timeFmt.format(shift.checkInAt!.toLocal())
                    : l.attendancePlaceholder,
                tone: AppTone.emerald,
                icon: Icons.login_rounded,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _IoCard(
                title: l.attendanceOutLabel,
                value: shift.checkOutAt != null
                    ? timeFmt.format(shift.checkOutAt!.toLocal())
                    : l.attendancePlaceholder,
                tone: AppTone.rose,
                icon: Icons.logout_rounded,
              ),
            ),
          ],
        ),
        const SizedBox(height: 22),
        // Primary CTA.
        if (shift.isDone)
          AppCard(
            padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 16),
            child: Center(
              child: Text(
                l.attendanceDone,
                style: GoogleFonts.plusJakartaSans(
                  color: AppPalette.emeraldDeep,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          )
        else if (checkedIn)
          AppButton.emerald(
            label: l.attendanceCheckOutCta,
            icon: Icons.logout_rounded,
            onPressed: () => context.push(
              AppRoutes.attendanceCapture,
              extra: CheckCaptureArgs(
                mode: CheckMode.checkOut,
                storeLabel: storeLabel,
                attendanceId: shift.attendanceId,
              ),
            ),
          )
        else
          AppButton.primary(
            label: l.attendanceCheckInCta,
            icon: Icons.sentiment_satisfied_alt_rounded,
            onPressed: () => context.push(
              AppRoutes.attendanceCapture,
              extra: CheckCaptureArgs(
                mode: CheckMode.checkIn,
                storeLabel: storeLabel,
                storeId: shift.storeId,
              ),
            ),
          ),
      ],
    );
  }
}

// ─── Face target ──────────────────────────────────────────────────────────

class _FaceTarget extends StatelessWidget {
  const _FaceTarget({
    required this.checkedIn,
    required this.ring,
    required this.reduce,
  });
  final bool checkedIn;
  final AnimationController ring;
  final bool reduce;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return SizedBox(
      width: 230,
      height: 230,
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Soft halo.
          Container(
            width: 220,
            height: 220,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: RadialGradient(
                colors: [
                  AppPalette.indigo.withValues(alpha: 0.18),
                  Colors.transparent,
                ],
              ),
            ),
          ),
          // Rotating dashed ring.
          if (!reduce)
            AnimatedBuilder(
              animation: ring,
              builder: (_, __) {
                return Transform.rotate(
                  angle: ring.value * 2 * math.pi,
                  child: CustomPaint(
                    size: const Size(200, 200),
                    painter: _DashedRingPainter(
                      color: checkedIn
                          ? AppPalette.emerald
                          : AppPalette.indigo,
                    ),
                  ),
                );
              },
            )
          else
            CustomPaint(
              size: const Size(200, 200),
              painter: _DashedRingPainter(
                color: checkedIn ? AppPalette.emerald : AppPalette.indigo,
              ),
            ),
          // Avatar.
          Container(
            width: 150,
            height: 150,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: checkedIn ? s.emeraldGradient : s.brandGradient,
              boxShadow: checkedIn
                  ? const [
                      BoxShadow(
                          color: Color(0x6610B981),
                          blurRadius: 36,
                          offset: Offset(0, 14))
                    ]
                  : s.shadowBrand,
            ),
            alignment: Alignment.center,
            child: Icon(
              checkedIn
                  ? Icons.check_rounded
                  : Icons.sentiment_satisfied_alt_rounded,
              color: Colors.white,
              size: 72,
            ),
          ),
        ],
      ),
    );
  }
}

class _DashedRingPainter extends CustomPainter {
  _DashedRingPainter({required this.color});
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color.withValues(alpha: 0.55)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.5
      ..strokeCap = StrokeCap.round;

    final rect = Offset.zero & size;
    final path = Path()..addOval(rect);
    final metric = path.computeMetrics().first;
    const dash = 9.0;
    const gap = 7.0;
    var distance = 0.0;
    while (distance < metric.length) {
      final next = math.min(distance + dash, metric.length);
      canvas.drawPath(metric.extractPath(distance, next), paint);
      distance = next + gap;
    }
  }

  @override
  bool shouldRepaint(covariant _DashedRingPainter oldDelegate) =>
      oldDelegate.color != color;
}

// ─── In/out cards ─────────────────────────────────────────────────────────

class _IoCard extends StatelessWidget {
  const _IoCard({
    required this.title,
    required this.value,
    required this.tone,
    required this.icon,
  });
  final String title;
  final String value;
  final AppTone tone;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final c = tileColors(tone);
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          AppIconTile(icon: icon, tone: tone, size: 40, radius: 12),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 0.8,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: c.fg,
                    fontSize: 22,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.4,
                    fontFeatures: const [FontFeature.tabularFigures()],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Empty / error ────────────────────────────────────────────────────────

class _Empty extends StatelessWidget {
  const _Empty({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: [
        const SizedBox(height: 80),
        const Center(
          child: AppIconTile(
            icon: Icons.event_available_rounded,
            tone: AppTone.indigo,
            size: 64,
            radius: 22,
          ),
        ),
        const SizedBox(height: 16),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 32),
          child: Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 14.5,
              fontWeight: FontWeight.w600,
              height: 1.4,
            ),
          ),
        ),
      ],
    );
  }
}

class _Error extends StatelessWidget {
  const _Error({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return ListView(
      children: [
        const SizedBox(height: 80),
        const Center(
          child: AppIconTile(
            icon: Icons.error_outline_rounded,
            tone: AppTone.rose,
            size: 64,
            radius: 22,
          ),
        ),
        const SizedBox(height: 16),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 32),
          child: Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.ink,
              fontSize: 14.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
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
