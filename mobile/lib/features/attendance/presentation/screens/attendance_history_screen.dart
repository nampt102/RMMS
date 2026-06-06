import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/attendance_repository.dart';
import '../../domain/attendance.dart';
import '../widgets/attendance_status_chip.dart';

/// The caller's attendance history for the last 30 days (M05).
class AttendanceHistoryScreen extends ConsumerWidget {
  const AttendanceHistoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(attendanceHistoryProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l.attendanceHistory)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(attendanceHistoryProvider),
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => ListView(children: [
              const SizedBox(height: 120),
              Icon(Icons.error_outline, size: 64, color: Theme.of(context).colorScheme.error),
              const SizedBox(height: 16),
              Text(e is ApiException ? e.message : l.commonRetry, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              Center(
                child: FilledButton.tonal(
                  onPressed: () => ref.invalidate(attendanceHistoryProvider),
                  child: Text(l.commonRetry),
                ),
              ),
            ]),
            data: (records) {
              if (records.isEmpty) {
                return ListView(children: [
                  const SizedBox(height: 120),
                  Icon(Icons.history, size: 64, color: Theme.of(context).colorScheme.outline),
                  const SizedBox(height: 16),
                  Text(l.attendanceHistoryEmpty, textAlign: TextAlign.center),
                ]);
              }
              return ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                itemCount: records.length,
                itemBuilder: (context, i) =>
                    _HistoryCard(key: ValueKey(records[i].id), record: records[i]),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _HistoryCard extends StatelessWidget {
  const _HistoryCard({super.key, required this.record});

  final AttendanceRecord record;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = Theme.of(context).colorScheme;
    final lang = Localizations.localeOf(context).languageCode;
    final dayFmt = DateFormat.yMMMEd(lang);
    final timeFmt = DateFormat.Hm(lang);

    final outText = record.checkOutAt == null
        ? l.attendanceOpen
        : timeFmt.format(record.checkOutAt!.toLocal());

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
                  child: Text(dayFmt.format(record.checkInAt.toLocal()),
                      style: Theme.of(context).textTheme.titleSmall),
                ),
                AttendanceStatusChip(status: record.status),
              ],
            ),
            const SizedBox(height: 8),
            Row(children: [
              Icon(Icons.store_outlined, size: 16, color: scheme.outline),
              const SizedBox(width: 6),
              Expanded(child: Text('${record.storeCode} · ${record.storeName}')),
            ]),
            const SizedBox(height: 4),
            Row(children: [
              Icon(Icons.login, size: 16, color: scheme.outline),
              const SizedBox(width: 6),
              Text(timeFmt.format(record.checkInAt.toLocal())),
              const SizedBox(width: 16),
              Icon(Icons.logout, size: 16, color: scheme.outline),
              const SizedBox(width: 6),
              Text(outText),
              const Spacer(),
              Text('${record.checkInDistanceMeters.round()} m',
                  style: TextStyle(color: scheme.outline)),
            ]),
          ],
        ),
      ),
    );
  }
}
