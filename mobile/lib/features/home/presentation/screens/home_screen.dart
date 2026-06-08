import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../attendance/data/attendance_repository.dart';
import '../../../attendance/domain/attendance.dart';
import '../../../auth/application/auth_controller.dart';
import '../../../auth/application/auth_state.dart';

/// Redesign 2026 — Home (Trang chủ tab).
///
/// Layout (per `design_handoff_rmms_redesign/README.md` §Screens/Home):
///   - Mesh-gradient hero (avatar tile + greeting + name + chips + logout).
///   - "Ca hôm nay" card with pulsing ring when not checked in, live clock.
///   - Quick-access 3-grid: Phân công · Lịch sử · Khuôn mặt.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final auth = ref.watch(authControllerProvider);
    final user = auth is AuthAuthenticated ? auth.user : null;
    final todayAsync = ref.watch(todayShiftsProvider);

    final activeShift = todayAsync.maybeWhen<TodayShift?>(
      data: (s) => s.isEmpty ? null : s.first,
      orElse: () => null,
    );
    final checkedIn = activeShift?.isCheckedIn ?? false;

    return SafeArea(
      bottom: false,
      child: RefreshIndicator(
        onRefresh: () async => ref.invalidate(todayShiftsProvider),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 140),
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            AppRiseIn(
              child: _Hero(
                name: user?.fullName ?? 'PG',
                role: user?.role.name,
                storeCode: activeShift?.storeCode,
                onLogout: () =>
                    ref.read(authControllerProvider.notifier).logout(),
              ),
            ),
            const SizedBox(height: 14),
            AppRiseIn(
              delay: const Duration(milliseconds: 60),
              child: _TodayShiftCard(
                shift: activeShift,
                checkedIn: checkedIn,
                onTap: () => context.push(AppRoutes.attendance),
              ),
            ),
            const SizedBox(height: 22),
            AppRiseIn(
              delay: const Duration(milliseconds: 120),
              child: const SectionEyebrow(
                'Truy cập nhanh',
                trailing: Icon(Icons.auto_awesome_rounded,
                    color: AppPalette.violet, size: 16),
              ),
            ),
            const SizedBox(height: 12),
            AppRiseIn(
              delay: const Duration(milliseconds: 180),
              child: _QuickGrid(
                items: [
                  _QuickItem(
                    icon: Icons.groups_rounded,
                    label: l.homeQuickAssignment,
                    tone: AppTone.indigo,
                    onTap: () => context.push(AppRoutes.myAssignments),
                  ),
                  _QuickItem(
                    icon: Icons.history_rounded,
                    label: l.homeQuickHistory,
                    tone: AppTone.emerald,
                    onTap: () => context.push(AppRoutes.attendanceHistory),
                  ),
                  _QuickItem(
                    icon: Icons.sentiment_satisfied_alt_rounded,
                    label: l.homeQuickFace,
                    tone: AppTone.amber,
                    onTap: () => context.push(AppRoutes.faceEnroll),
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

// ─── Hero ─────────────────────────────────────────────────────────────────

String _greetingKey(AppLocalizations l) {
  final h = DateTime.now().hour;
  if (h < 12) return l.homeGreetingMorning;
  if (h < 18) return l.homeGreetingAfternoon;
  return l.homeGreetingEvening;
}

class _Hero extends StatelessWidget {
  const _Hero({
    required this.name,
    required this.role,
    required this.storeCode,
    required this.onLogout,
  });

  final String name;
  final String? role;
  final String? storeCode;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;
    final initial =
        name.trim().isEmpty ? '?' : name.trim().substring(0, 1).toUpperCase();

    return ClipRRect(
      borderRadius: BorderRadius.circular(30),
      child: Stack(
        children: [
          Container(
            decoration: BoxDecoration(
              gradient: s.meshGradient,
              borderRadius: BorderRadius.circular(30),
              boxShadow: s.shadowLg,
            ),
          ),
          // Decorative circles.
          Positioned(
            top: -30,
            right: -20,
            child: _circle(120, Colors.white.withValues(alpha: 0.10)),
          ),
          Positioned(
            top: 20,
            right: 60,
            child: _circle(60, Colors.white.withValues(alpha: 0.08)),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 18, 14, 18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    // Avatar tile.
                    Container(
                      width: 56,
                      height: 56,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.22),
                        borderRadius: BorderRadius.circular(18),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        initial,
                        style: GoogleFonts.spaceGrotesk(
                          color: Colors.white,
                          fontSize: 24,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            _greetingKey(l),
                            style: GoogleFonts.plusJakartaSans(
                              color: Colors.white.withValues(alpha: 0.88),
                              fontSize: 13.5,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            name,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: GoogleFonts.spaceGrotesk(
                              color: Colors.white,
                              fontSize: 22,
                              fontWeight: FontWeight.w800,
                              letterSpacing: -0.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                    PressScale(
                      onTap: onLogout,
                      child: Container(
                        width: 40,
                        height: 40,
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.22),
                          borderRadius: BorderRadius.circular(13),
                        ),
                        alignment: Alignment.center,
                        child: const Icon(Icons.logout_rounded,
                            color: Colors.white, size: 20),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    if (role != null)
                      _glassChip(
                        icon: Icons.workspace_premium_rounded,
                        text: 'Vai trò · $role',
                      ),
                    if (storeCode != null)
                      _glassChip(
                        icon: Icons.place_rounded,
                        text: storeCode!,
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  static Widget _circle(double size, Color color) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(shape: BoxShape.circle, color: color),
      );

  static Widget _glassChip({required IconData icon, required String text}) =>
      Container(
        padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 6),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.22),
          borderRadius: BorderRadius.circular(999),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: Colors.white, size: 14),
            const SizedBox(width: 6),
            Text(
              text,
              style: GoogleFonts.plusJakartaSans(
                color: Colors.white,
                fontSize: 12.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      );
}

// ─── Ca hôm nay card ──────────────────────────────────────────────────────

class _TodayShiftCard extends StatefulWidget {
  const _TodayShiftCard({
    required this.shift,
    required this.checkedIn,
    required this.onTap,
  });

  final TodayShift? shift;
  final bool checkedIn;
  final VoidCallback onTap;

  @override
  State<_TodayShiftCard> createState() => _TodayShiftCardState();
}

class _TodayShiftCardState extends State<_TodayShiftCard>
    with SingleTickerProviderStateMixin {
  Timer? _clock;
  late AnimationController _pulse;

  @override
  void initState() {
    super.initState();
    _pulse = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1800),
    )..repeat();
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _clock?.cancel();
    _pulse.dispose();
    super.dispose();
  }

  String get _clockText {
    final now = DateTime.now();
    final hh = now.hour.toString().padLeft(2, '0');
    final mm = now.minute.toString().padLeft(2, '0');
    final ss = now.second.toString().padLeft(2, '0');
    return '$hh:$mm:$ss';
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final reduce = MediaQuery.disableAnimationsOf(context);
    final shift = widget.shift;
    final timeRange = shift == null
        ? '08:00–17:00'
        : '${shift.startTime}–${shift.endTime}';

    return AppCard(
      onTap: widget.onTap,
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          _AvatarRing(
            checkedIn: widget.checkedIn,
            pulse: _pulse,
            reduce: reduce,
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  l.homeShiftEyebrow(timeRange),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 11.5,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 0.8,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  widget.checkedIn
                      ? l.homeShiftActive
                      : l.homeShiftNotChecked,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.4,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  _clockText,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.indigoDeep,
                    fontSize: 21,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.2,
                    fontFeatures: const [FontFeature.tabularFigures()],
                  ),
                ),
              ],
            ),
          ),
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: AppPalette.surface2,
              borderRadius: BorderRadius.circular(13),
            ),
            alignment: Alignment.center,
            child: const Icon(Icons.chevron_right_rounded,
                color: AppPalette.muted, size: 22),
          ),
        ],
      ),
    );
  }
}

class _AvatarRing extends StatelessWidget {
  const _AvatarRing({
    required this.checkedIn,
    required this.pulse,
    required this.reduce,
  });

  final bool checkedIn;
  final AnimationController pulse;
  final bool reduce;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    final gradient = checkedIn ? s.emeraldGradient : s.brandGradient;
    final icon = checkedIn ? Icons.check_rounded : Icons.sentiment_satisfied_alt_rounded;

    final avatar = Container(
      width: 62,
      height: 62,
      decoration: BoxDecoration(
        gradient: gradient,
        shape: BoxShape.circle,
        boxShadow: checkedIn
            ? const [
                BoxShadow(
                    color: Color(0x4D10B981),
                    blurRadius: 24,
                    offset: Offset(0, 8))
              ]
            : s.shadowBrand,
      ),
      alignment: Alignment.center,
      child: Icon(icon, color: Colors.white, size: 30),
    );

    if (checkedIn || reduce) return SizedBox(width: 72, height: 72, child: Center(child: avatar));

    return SizedBox(
      width: 72,
      height: 72,
      child: Stack(
        alignment: Alignment.center,
        children: [
          AnimatedBuilder(
            animation: pulse,
            builder: (_, __) {
              final t = pulse.value;
              return Opacity(
                opacity: (1 - t) * 0.55,
                child: Container(
                  width: 50 + t * 26,
                  height: 50 + t * 26,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: AppPalette.indigo,
                      width: 2,
                    ),
                  ),
                ),
              );
            },
          ),
          avatar,
        ],
      ),
    );
  }
}

// ─── Quick-access grid ────────────────────────────────────────────────────

class _QuickItem {
  const _QuickItem({
    required this.icon,
    required this.label,
    required this.tone,
    required this.onTap,
  });
  final IconData icon;
  final String label;
  final AppTone tone;
  final VoidCallback onTap;
}

class _QuickGrid extends StatelessWidget {
  const _QuickGrid({required this.items});
  final List<_QuickItem> items;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        for (var i = 0; i < items.length; i++) ...[
          Expanded(child: _QuickCard(item: items[i])),
          if (i < items.length - 1) const SizedBox(width: 12),
        ],
      ],
    );
  }
}

class _QuickCard extends StatelessWidget {
  const _QuickCard({required this.item});
  final _QuickItem item;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: item.onTap,
      borderRadius: BorderRadius.circular(22),
      padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 10),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          AppIconTile(icon: item.icon, tone: item.tone, size: 44, radius: 14),
          const SizedBox(height: 12),
          Text(
            item.label,
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.ink,
              fontSize: 13.5,
              fontWeight: FontWeight.w700,
              height: 1.2,
            ),
          ),
        ],
      ),
    );
  }
}
