import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/schedule_repository.dart';
import '../../domain/work_schedule.dart';

/// PG/Leader view of their own work schedule for the next 30 days (M07).
/// Lists each day's shifts + approval status, with submit/withdraw actions on
/// drafts and a FAB to register new days.
class MyScheduleScreen extends ConsumerWidget {
  const MyScheduleScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myScheduleProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l.scheduleTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.scheduleRegister),
        icon: const Icon(Icons.add),
        label: Text(l.scheduleRegister),
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(myScheduleProvider),
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => _ErrorView(
              message: e is ApiException ? e.message : l.commonRetry,
              onRetry: () => ref.invalidate(myScheduleProvider),
              retryLabel: l.commonRetry,
            ),
            data: (schedules) {
              if (schedules.isEmpty) {
                return _EmptyView(message: l.scheduleEmpty);
              }
              return ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
                itemCount: schedules.length,
                itemBuilder: (context, i) => _ScheduleCard(schedule: schedules[i]),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ScheduleCard extends ConsumerWidget {
  const _ScheduleCard({required this.schedule});

  final WorkSchedule schedule;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final scheme = Theme.of(context).colorScheme;
    final date = DateTime.tryParse(schedule.scheduleDate);
    final dateText = date == null
        ? schedule.scheduleDate
        : DateFormat.yMMMEd(Localizations.localeOf(context).languageCode).format(date);

    Future<void> run(Future<void> Function() action, String okMessage) async {
      try {
        await action();
        ref.invalidate(myScheduleProvider);
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(okMessage)));
        }
      } on ApiException catch (e) {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
        }
      }
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(dateText, style: Theme.of(context).textTheme.titleMedium),
                ),
                _StatusChip(status: schedule.status),
              ],
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: schedule.shifts
                  .map((s) => Chip(
                        visualDensity: VisualDensity.compact,
                        avatar: Icon(Icons.store_outlined, size: 18, color: scheme.primary),
                        label: Text('${s.startTime}–${s.endTime} · ${s.storeCode}'),
                      ))
                  .toList(),
            ),
            if (schedule.status == 'rejected' && (schedule.rejectReason?.isNotEmpty ?? false)) ...[
              const SizedBox(height: 8),
              Text(
                l.scheduleRejectedReason(schedule.rejectReason!),
                style: TextStyle(color: scheme.error),
              ),
            ],
            if (schedule.isEditable || schedule.isWithdrawable) ...[
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  if (schedule.isEditable)
                    TextButton.icon(
                      onPressed: () =>
                          context.push(AppRoutes.scheduleRegister, extra: schedule),
                      icon: const Icon(Icons.edit_outlined, size: 18),
                      label: Text(l.scheduleEdit),
                    ),
                  if (schedule.isEditable && (schedule.isDraft || schedule.isWithdrawable))
                    const SizedBox(width: 8),
                  if (schedule.isDraft)
                    FilledButton.tonal(
                      onPressed: () => run(
                        () => ref.read(scheduleRepositoryProvider).submit(schedule.id),
                        l.scheduleSubmitted,
                      ),
                      child: Text(l.scheduleSubmit),
                    ),
                  if (schedule.isWithdrawable) ...[
                    const SizedBox(width: 8),
                    TextButton(
                      onPressed: () async {
                        final ok = await _confirm(context, l.scheduleWithdrawConfirm, l.scheduleWithdraw, l.commonCancel);
                        if (ok) {
                          await run(
                            () => ref.read(scheduleRepositoryProvider).withdraw(schedule.id),
                            l.scheduleWithdrawn,
                          );
                        }
                      },
                      child: Text(l.scheduleWithdraw),
                    ),
                  ],
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<bool> _confirm(BuildContext context, String title, String okLabel, String cancelLabel) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        content: Text(title),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(cancelLabel)),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text(okLabel)),
        ],
      ),
    );
    return result ?? false;
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final (BrandTone tone, IconData icon, String label) = switch (status) {
      'approved' => (BrandTone.success, Icons.check_circle_outline, l.scheduleStatusApproved),
      'rejected' => (BrandTone.danger, Icons.cancel_outlined, l.scheduleStatusRejected),
      'edit_pending' => (BrandTone.warning, Icons.edit_calendar_outlined, l.scheduleStatusEditPending),
      'superseded' => (BrandTone.neutral, Icons.history, l.scheduleStatusSuperseded),
      _ => (BrandTone.info, Icons.hourglass_empty, l.scheduleStatusPending),
    };
    return StatusPill(label: label, icon: icon, tone: tone);
  }
}

class _EmptyView extends StatelessWidget {
  const _EmptyView({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: [
        const SizedBox(height: 120),
        Icon(Icons.event_busy_outlined, size: 64, color: Theme.of(context).colorScheme.outline),
        const SizedBox(height: 16),
        Text(message, textAlign: TextAlign.center),
      ],
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry, required this.retryLabel});

  final String message;
  final VoidCallback onRetry;
  final String retryLabel;

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: [
        const SizedBox(height: 120),
        Icon(Icons.error_outline, size: 64, color: Theme.of(context).colorScheme.error),
        const SizedBox(height: 16),
        Text(message, textAlign: TextAlign.center),
        const SizedBox(height: 16),
        Center(
          child: FilledButton.tonal(onPressed: onRetry, child: Text(retryLabel)),
        ),
      ],
    );
  }
}
