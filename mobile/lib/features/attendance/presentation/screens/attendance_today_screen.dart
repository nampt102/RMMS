import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/attendance_repository.dart';
import '../../domain/attendance.dart';
import '../widgets/attendance_status_chip.dart';
import 'check_in_screen.dart';

/// Today's shifts with check-in / check-out actions (M05 entry point).
class AttendanceTodayScreen extends ConsumerWidget {
  const AttendanceTodayScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(todayShiftsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(l.attendanceTitle),
        actions: [
          IconButton(
            tooltip: l.attendanceHistory,
            icon: const Icon(Icons.history),
            onPressed: () => context.push(AppRoutes.attendanceHistory),
          ),
        ],
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(todayShiftsProvider),
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => _Error(
              message: e is ApiException ? e.message : l.commonRetry,
              retryLabel: l.commonRetry,
              onRetry: () => ref.invalidate(todayShiftsProvider),
            ),
            data: (shifts) {
              if (shifts.isEmpty) return _Empty(message: l.attendanceNoShifts);
              return ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                itemCount: shifts.length,
                itemBuilder: (context, i) => _ShiftCard(key: ValueKey(shifts[i].workScheduleShiftId), shift: shifts[i]),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ShiftCard extends StatelessWidget {
  const _ShiftCard({super.key, required this.shift});

  final TodayShift shift;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = Theme.of(context).colorScheme;
    final storeLabel = '${shift.storeCode} · ${shift.storeName}';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.store_outlined, size: 18, color: scheme.primary),
                const SizedBox(width: 8),
                Expanded(child: Text(storeLabel, style: Theme.of(context).textTheme.titleSmall)),
                if (shift.attendanceStatus != null)
                  AttendanceStatusChip(status: shift.attendanceStatus!),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(Icons.schedule, size: 16, color: scheme.outline),
                const SizedBox(width: 6),
                Text('${shift.startTime}–${shift.endTime}'),
              ],
            ),
            const SizedBox(height: 12),
            Align(alignment: Alignment.centerRight, child: _action(context, l, storeLabel)),
          ],
        ),
      ),
    );
  }

  Widget _action(BuildContext context, AppLocalizations l, String storeLabel) {
    if (shift.isDone) {
      return Text(l.attendanceDone, style: TextStyle(color: Theme.of(context).colorScheme.outline));
    }
    if (shift.isCheckedIn) {
      return FilledButton.icon(
        icon: const Icon(Icons.logout),
        onPressed: () => context.push(
          AppRoutes.attendanceCapture,
          extra: CheckCaptureArgs(
            mode: CheckMode.checkOut,
            storeLabel: storeLabel,
            attendanceId: shift.attendanceId,
          ),
        ),
        label: Text(l.attendanceCheckOut),
      );
    }
    return FilledButton.icon(
      icon: const Icon(Icons.login),
      onPressed: () => context.push(
        AppRoutes.attendanceCapture,
        extra: CheckCaptureArgs(
          mode: CheckMode.checkIn,
          storeLabel: storeLabel,
          storeId: shift.storeId,
        ),
      ),
      label: Text(l.attendanceCheckIn),
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return ListView(children: [
      const SizedBox(height: 120),
      Icon(Icons.event_available_outlined, size: 64, color: Theme.of(context).colorScheme.outline),
      const SizedBox(height: 16),
      Text(message, textAlign: TextAlign.center),
    ]);
  }
}

class _Error extends StatelessWidget {
  const _Error({required this.message, required this.onRetry, required this.retryLabel});
  final String message;
  final VoidCallback onRetry;
  final String retryLabel;

  @override
  Widget build(BuildContext context) {
    return ListView(children: [
      const SizedBox(height: 120),
      Icon(Icons.error_outline, size: 64, color: Theme.of(context).colorScheme.error),
      const SizedBox(height: 16),
      Text(message, textAlign: TextAlign.center),
      const SizedBox(height: 16),
      Center(child: FilledButton.tonal(onPressed: onRetry, child: Text(retryLabel))),
    ]);
  }
}
