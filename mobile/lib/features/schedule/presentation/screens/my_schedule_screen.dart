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
import '../../data/schedule_repository.dart';
import '../../domain/work_schedule.dart';

/// Redesign 2026 — Lịch làm việc (tab).
///
/// Layout: page-title header + compact gradient "+ Đăng ký" CTA; 3 summary
/// stat cards (Ca tuần này · Tổng giờ · Chờ duyệt); shift cards with a 5px
/// left status accent, time/store chips, and Sửa / Gửi duyệt / Thu hồi
/// actions depending on status.
class MyScheduleScreen extends ConsumerWidget {
  const MyScheduleScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myScheduleProvider);

    return SafeArea(
      bottom: false,
      child: RefreshIndicator(
        onRefresh: () async => ref.invalidate(myScheduleProvider),
        child: async.when(
          loading: () => const _LoadingView(),
          error: (e, _) => _ErrorView(
            message: e is ApiException ? e.message : l.commonRetry,
            onRetry: () => ref.invalidate(myScheduleProvider),
          ),
          data: (schedules) {
            final stats = _computeStats(schedules);
            return ListView(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 140),
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                _Header(
                  onRegister: () => context.push(AppRoutes.scheduleRegister),
                ),
                const SizedBox(height: 16),
                _SummaryRow(stats: stats),
                const SizedBox(height: 18),
                if (schedules.isEmpty)
                  _EmptyState(message: l.scheduleEmpty)
                else
                  ...schedules.map(
                    (s) => Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: _ScheduleCard(schedule: s),
                    ),
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

// ─── Header ───────────────────────────────────────────────────────────────

class _Header extends StatelessWidget {
  const _Header({required this.onRegister});
  final VoidCallback onRegister;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;

    return Row(
      children: [
        Expanded(
          child: Text(
            l.scheduleTitle,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.spaceGrotesk(
              color: AppPalette.ink,
              fontSize: 25,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.6,
            ),
          ),
        ),
        const SizedBox(width: 12),
        PressScale(
          onTap: onRegister,
          child: Container(
            height: 40,
            padding: const EdgeInsets.symmetric(horizontal: 14),
            decoration: BoxDecoration(
              gradient: s.brandGradient,
              borderRadius: BorderRadius.circular(14),
              boxShadow: s.shadowBrand,
            ),
            alignment: Alignment.center,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.add_rounded, color: Colors.white, size: 18),
                const SizedBox(width: 4),
                Text(
                  l.scheduleRegisterCompact,
                  style: GoogleFonts.plusJakartaSans(
                    color: Colors.white,
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

// ─── Summary stats ────────────────────────────────────────────────────────

class _Stats {
  const _Stats({
    required this.weekShifts,
    required this.totalHours,
    required this.pending,
  });
  final int weekShifts;
  final double totalHours;
  final int pending;
}

_Stats _computeStats(List<WorkSchedule> list) {
  final now = DateTime.now();
  final monday = DateTime(now.year, now.month, now.day)
      .subtract(Duration(days: now.weekday - 1));
  final sunday = monday.add(const Duration(days: 7));

  var shifts = 0;
  var hours = 0.0;
  var pending = 0;
  for (final ws in list) {
    final d = DateTime.tryParse(ws.scheduleDate);
    if (d != null && !d.isBefore(monday) && d.isBefore(sunday)) {
      shifts += ws.shifts.length;
      for (final s in ws.shifts) {
        hours += _diffHours(s.startTime, s.endTime);
      }
    }
    if (ws.status == 'pending' || ws.status == 'edit_pending') pending++;
  }
  return _Stats(weekShifts: shifts, totalHours: hours, pending: pending);
}

double _diffHours(String startHHmm, String endHHmm) {
  int m(String t) {
    final p = t.split(':');
    return (int.tryParse(p[0]) ?? 0) * 60 + (int.tryParse(p[1]) ?? 0);
  }
  final d = m(endHHmm) - m(startHHmm);
  return d <= 0 ? 0 : d / 60.0;
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({required this.stats});
  final _Stats stats;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final hoursLabel = stats.totalHours == stats.totalHours.truncateToDouble()
        ? '${stats.totalHours.toInt()}h'
        : '${stats.totalHours.toStringAsFixed(1)}h';
    return Row(
      children: [
        Expanded(
          child: _StatCard(
            value: '${stats.weekShifts}',
            label: l.scheduleStatWeek,
            tone: AppTone.indigo,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            value: hoursLabel,
            label: l.scheduleStatHours,
            tone: AppTone.emerald,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _StatCard(
            value: '${stats.pending}',
            label: l.scheduleStatPending,
            tone: AppTone.amber,
          ),
        ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.value,
    required this.label,
    required this.tone,
  });
  final String value;
  final String label;
  final AppTone tone;

  @override
  Widget build(BuildContext context) {
    final c = tileColors(tone);
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 12, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.spaceGrotesk(
              color: c.fg,
              fontSize: 22,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.4,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            label,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 12.5,
              fontWeight: FontWeight.w600,
              height: 1.2,
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Shift card ───────────────────────────────────────────────────────────

({AppTone tone, String label}) _statusMeta(AppLocalizations l, WorkSchedule s) {
  if (s.isDraft) return (tone: AppTone.neutral, label: l.scheduleStatusDraft);
  return switch (s.status) {
    'approved' => (tone: AppTone.emerald, label: l.scheduleStatusApproved),
    'rejected' => (tone: AppTone.rose, label: l.scheduleStatusRejected),
    'edit_pending' => (tone: AppTone.amber, label: l.scheduleStatusEditPending),
    'superseded' => (tone: AppTone.neutral, label: l.scheduleStatusSuperseded),
    _ => (tone: AppTone.amber, label: l.scheduleStatusPending),
  };
}

class _ScheduleCard extends ConsumerWidget {
  const _ScheduleCard({required this.schedule});
  final WorkSchedule schedule;

  Future<void> _run(
    BuildContext context,
    WidgetRef ref,
    Future<void> Function() action,
    String okMessage,
  ) async {
    try {
      await action();
      ref.invalidate(myScheduleProvider);
      if (context.mounted) {
        showAppToast(context, message: okMessage, kind: AppToastKind.success);
      }
    } on ApiException catch (e) {
      if (context.mounted) {
        showAppToast(context, message: e.message, kind: AppToastKind.error);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final date = DateTime.tryParse(schedule.scheduleDate);
    final dateText = date == null
        ? schedule.scheduleDate
        : DateFormat.yMMMEd(lang).format(date);

    final meta = _statusMeta(l, schedule);
    final c = chipColors(meta.tone);
    final showActions = schedule.isEditable || schedule.isWithdrawable;

    return Container(
      decoration: BoxDecoration(
        color: AppPalette.surface,
        borderRadius: BorderRadius.circular(28),
        boxShadow: context.semantics.shadowSm,
      ),
      clipBehavior: Clip.antiAlias,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // 5px left accent bar.
          Container(width: 5, color: c.fg),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          dateText,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          softWrap: false,
                          style: GoogleFonts.plusJakartaSans(
                            color: AppPalette.ink,
                            fontSize: 14.5,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      AppChip(label: meta.label, tone: meta.tone),
                    ],
                  ),
                  if (schedule.shifts.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    Wrap(
                      spacing: 6,
                      runSpacing: 6,
                      children: [
                        for (final s in schedule.shifts) ...[
                          AppChip(
                            icon: Icons.schedule_rounded,
                            label: '${s.startTime} – ${s.endTime}',
                            tone: AppTone.neutral,
                          ),
                          AppChip(
                            icon: Icons.place_rounded,
                            label: s.storeCode,
                            tone: AppTone.neutral,
                          ),
                        ],
                      ],
                    ),
                  ],
                  if (schedule.status == 'rejected' &&
                      (schedule.rejectReason?.isNotEmpty ?? false)) ...[
                    const SizedBox(height: 8),
                    Text(
                      l.scheduleRejectedReason(schedule.rejectReason!),
                      style: GoogleFonts.plusJakartaSans(
                        color: AppPalette.rose,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                  if (showActions) ...[
                    const SizedBox(height: 12),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        if (schedule.isEditable) ...[
                          AppButton.soft(
                            label: l.scheduleEdit,
                            icon: Icons.edit_rounded,
                            expand: false,
                            onPressed: () => context.push(
                              AppRoutes.scheduleRegister,
                              extra: schedule,
                            ),
                          ),
                          const SizedBox(width: 8),
                        ],
                        if (schedule.isDraft)
                          AppButton.primary(
                            label: l.scheduleSubmit,
                            icon: Icons.send_rounded,
                            expand: false,
                            onPressed: () => _run(
                              context,
                              ref,
                              () => ref
                                  .read(scheduleRepositoryProvider)
                                  .submit(schedule.id),
                              l.scheduleSubmitted,
                            ),
                          )
                        else if (schedule.isWithdrawable)
                          AppButton.destructiveSoft(
                            label: l.scheduleWithdraw,
                            icon: Icons.undo_rounded,
                            expand: false,
                            onPressed: () => _run(
                              context,
                              ref,
                              () => ref
                                  .read(scheduleRepositoryProvider)
                                  .withdraw(schedule.id),
                              l.scheduleWithdrawn,
                            ),
                          ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Empty / loading / error ──────────────────────────────────────────────

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator());
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 80),
      child: Column(
        children: [
          const AppIconTile(
            icon: Icons.event_busy_rounded,
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

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return ListView(
      children: [
        const SizedBox(height: 80),
        const AppIconTile(
          icon: Icons.error_outline_rounded,
          tone: AppTone.rose,
          size: 64,
          radius: 22,
        ),
        const SizedBox(height: 16),
        Text(
          message,
          textAlign: TextAlign.center,
          style: GoogleFonts.plusJakartaSans(
            color: AppPalette.ink,
            fontSize: 14.5,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 18),
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
