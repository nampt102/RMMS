import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/attendance_repository.dart';
import '../../domain/attendance.dart';

/// Redesign 2026 — Lịch sử chấm công (pushed).
///
/// Mesh-gradient summary card (month + hours + on-time %) + day rows
/// (IconTile + date + "07:58 → 17:03" + right-aligned hours + on-time tag).
class AttendanceHistoryScreen extends ConsumerWidget {
  const AttendanceHistoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(attendanceHistoryProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.historyTitle),
            Expanded(
              child: RefreshIndicator(
                onRefresh: () async =>
                    ref.invalidate(attendanceHistoryProvider),
                child: async.when(
                  loading: () =>
                      const Center(child: CircularProgressIndicator()),
                  error: (e, _) => _Error(
                    message: e is ApiException ? e.message : l.commonRetry,
                    onRetry: () => ref.invalidate(attendanceHistoryProvider),
                  ),
                  data: (records) {
                    final summary = _computeSummary(records);
                    return ListView(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: [
                        _SummaryCard(summary: summary),
                        const SizedBox(height: 16),
                        if (records.isEmpty)
                          _Empty(message: l.attendanceHistoryEmpty)
                        else
                          ...records.map(
                            (r) => Padding(
                              padding: const EdgeInsets.only(bottom: 10),
                              child: _DayRow(record: r),
                            ),
                          ),
                      ],
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─── Summary ──────────────────────────────────────────────────────────────

class _Summary {
  const _Summary({
    required this.month,
    required this.hoursText,
    required this.onTimePct,
  });
  final int month;
  final String hoursText;
  final int onTimePct;
}

_Summary _computeSummary(List<AttendanceRecord> records) {
  final now = DateTime.now();
  var hours = 0.0;
  var monthCount = 0;
  var onTime = 0;
  for (final r in records) {
    final start = r.checkInAt.toLocal();
    if (start.month == now.month && start.year == now.year) {
      monthCount++;
      if (r.checkOutAt != null) {
        hours += r.checkOutAt!
                .toLocal()
                .difference(start)
                .inMinutes /
            60.0;
      }
      if (r.status == 'valid' && !r.isLate) onTime++;
    }
  }
  final pct = monthCount == 0 ? 0 : ((onTime / monthCount) * 100).round();
  final hoursText = hours == hours.truncateToDouble()
      ? '${hours.toInt()}h'
      : '${hours.toStringAsFixed(1)}h';
  return _Summary(month: now.month, hoursText: hoursText, onTimePct: pct);
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.summary});
  final _Summary summary;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;
    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: DecoratedBox(
        decoration: BoxDecoration(gradient: s.meshGradient),
        child: Stack(
          children: [
            const MeshRadialOverlay(),
            Positioned(
              top: -24,
              right: -16,
              child: Container(
                width: 110,
                height: 110,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.10),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 20, 20, 22),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                Text(
                  l.historyMonthSummary(summary.month, summary.hoursText),
                  style: GoogleFonts.plusJakartaSans(
                    color: Colors.white.withValues(alpha: 0.92),
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.4,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  summary.hoursText,
                  style: GoogleFonts.spaceGrotesk(
                    color: Colors.white,
                    fontSize: 38,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -1,
                  ),
                ),
                const SizedBox(height: 4),
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.22),
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.bolt_rounded,
                          color: Colors.white, size: 14),
                      const SizedBox(width: 6),
                      Text(
                        l.historyOnTimePct(summary.onTimePct),
                        style: GoogleFonts.plusJakartaSans(
                          color: Colors.white,
                          fontSize: 12.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
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

// ─── Day row ──────────────────────────────────────────────────────────────

class _DayRow extends StatelessWidget {
  const _DayRow({required this.record});
  final AttendanceRecord record;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final timeFmt = DateFormat.Hm(lang);
    final dayFmt = DateFormat.MMMEd(lang);
    final start = record.checkInAt.toLocal();
    final end = record.checkOutAt?.toLocal();
    final isLate = record.isLate;
    final tag = isLate ? l.historyLate : l.historyOnTime;
    final tone = isLate ? AppTone.amber : AppTone.emerald;
    final icon = isLate ? Icons.schedule_rounded : Icons.check_circle_rounded;

    final hours = end == null
        ? '—'
        : ((end.difference(start).inMinutes) / 60.0).toStringAsFixed(1);

    return AppCard(
      padding: const EdgeInsets.fromLTRB(12, 12, 14, 12),
      child: Row(
        children: [
          AppIconTile(icon: icon, tone: tone, size: 44, radius: 14),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  dayFmt.format(start),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.ink,
                    fontSize: 14.5,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  '${timeFmt.format(start)} → ${end == null ? l.attendanceOpen : timeFmt.format(end)}',
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w600,
                    fontFeatures: const [FontFeature.tabularFigures()],
                  ),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                end == null ? '—' : '${hours}h',
                style: GoogleFonts.spaceGrotesk(
                  color: AppPalette.ink,
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.3,
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
              ),
              const SizedBox(height: 2),
              AppChip(label: tag, tone: tone),
            ],
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
    return Padding(
      padding: const EdgeInsets.only(top: 40),
      child: Column(
        children: [
          const AppIconTile(
            icon: Icons.history_rounded,
            tone: AppTone.indigo,
            size: 64,
            radius: 22,
          ),
          const SizedBox(height: 16),
          Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 14.5,
              fontWeight: FontWeight.w600,
              height: 1.4,
            ),
          ),
        ],
      ),
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
