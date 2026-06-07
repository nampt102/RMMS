import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/requests_repository.dart';
import '../../domain/leave_request.dart';
import '../../domain/ot_request.dart';

/// PG/Leader history of their leave + OT requests with status (M08). A "+" opens
/// the leave or OT form. Pending leave can be withdrawn.
class RequestsHistoryScreen extends StatelessWidget {
  const RequestsHistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l.requestsTitle),
          bottom: TabBar(tabs: [Tab(text: l.requestsTabLeave), Tab(text: l.requestsTabOt)]),
        ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => _showAdd(context, l),
          icon: const Icon(Icons.add),
          label: Text(l.requestsAdd),
        ),
        body: const SafeArea(child: TabBarView(children: [_LeaveList(), _OtList()])),
      ),
    );
  }

  void _showAdd(BuildContext context, AppLocalizations l) {
    showModalBottomSheet<void>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.beach_access_outlined),
              title: Text(l.requestsAddLeave),
              onTap: () {
                Navigator.pop(ctx);
                context.push(AppRoutes.leaveRequest);
              },
            ),
            ListTile(
              leading: const Icon(Icons.more_time_outlined),
              title: Text(l.requestsAddOt),
              onTap: () {
                Navigator.pop(ctx);
                context.push(AppRoutes.otRequest);
              },
            ),
          ],
        ),
      ),
    );
  }
}

({BrandTone tone, IconData icon, String label}) _statusMeta(AppLocalizations l, String status) => switch (status) {
      'approved' => (tone: BrandTone.success, icon: Icons.check_circle_outline, label: l.requestStatusApproved),
      'rejected' => (tone: BrandTone.danger, icon: Icons.cancel_outlined, label: l.requestStatusRejected),
      _ => (tone: BrandTone.info, icon: Icons.hourglass_empty, label: l.requestStatusPending),
    };

class _LeaveList extends ConsumerWidget {
  const _LeaveList();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myLeaveProvider);
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(myLeaveProvider),
      child: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => _Empty(message: e is ApiException ? e.message : l.commonRetry, icon: Icons.error_outline),
        data: (items) => items.isEmpty
            ? _Empty(message: l.requestsLeaveEmpty, icon: Icons.beach_access_outlined)
            : ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
                itemCount: items.length,
                itemBuilder: (_, i) => _LeaveCard(item: items[i]),
              ),
      ),
    );
  }
}

class _LeaveCard extends ConsumerWidget {
  const _LeaveCard({required this.item});
  final LeaveRequest item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final m = _statusMeta(l, item.status);
    final lang = Localizations.localeOf(context).languageCode;
    String fmt(String d) {
      final parsed = DateTime.tryParse(d);
      return parsed == null ? d : DateFormat.yMMMEd(lang).format(parsed);
    }

    final range = item.startDate == item.endDate ? fmt(item.startDate) : '${fmt(item.startDate)} → ${fmt(item.endDate)}';
    final typeLabel = item.leaveType == 'emergency' ? l.requestLeaveEmergency : l.requestLeaveRegular;

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: SoftCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(range, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700))),
                StatusPill(label: m.label, icon: m.icon, tone: m.tone),
              ],
            ),
            const SizedBox(height: 6),
            Row(children: [
              StatusPill(
                  label: typeLabel,
                  icon: Icons.label_outline,
                  tone: item.leaveType == 'emergency' ? BrandTone.warning : BrandTone.neutral),
            ]),
            if ((item.reason ?? '').isNotEmpty) ...[
              const SizedBox(height: 8),
              Text(item.reason!, style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13)),
            ],
            if (item.isPending) ...[
              const SizedBox(height: 4),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () async {
                    final ok = await _confirm(context, l.requestWithdrawConfirm, l.requestWithdraw, l.commonCancel);
                    if (!ok) return;
                    try {
                      await ref.read(requestsRepositoryProvider).withdrawLeave(item.id);
                      ref.invalidate(myLeaveProvider);
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(l.requestWithdrawn)));
                      }
                    } on ApiException catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
                      }
                    }
                  },
                  child: Text(l.requestWithdraw),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _OtList extends ConsumerWidget {
  const _OtList();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myOtProvider);
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(myOtProvider),
      child: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => _Empty(message: e is ApiException ? e.message : l.commonRetry, icon: Icons.error_outline),
        data: (items) => items.isEmpty
            ? _Empty(message: l.requestsOtEmpty, icon: Icons.more_time_outlined)
            : ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
                itemCount: items.length,
                itemBuilder: (_, i) => _OtCard(item: items[i]),
              ),
      ),
    );
  }
}

class _OtCard extends StatelessWidget {
  const _OtCard({required this.item});
  final OtRequest item;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final m = _statusMeta(l, item.status);
    final lang = Localizations.localeOf(context).languageCode;
    final parsed = DateTime.tryParse(item.otDate);
    final dateText = parsed == null ? item.otDate : DateFormat.yMMMEd(lang).format(parsed);
    String hhmm(String t) => t.length >= 5 ? t.substring(0, 5) : t;

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: SoftCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(dateText, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700))),
                StatusPill(label: m.label, icon: m.icon, tone: m.tone),
              ],
            ),
            const SizedBox(height: 6),
            Text('${hhmm(item.startTime)}–${hhmm(item.endTime)}',
                style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13)),
            if ((item.reason ?? '').isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(item.reason!, style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13)),
            ],
          ],
        ),
      ),
    );
  }
}

Future<bool> _confirm(BuildContext context, String title, String ok, String cancel) async {
  final r = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      content: Text(title),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(cancel)),
        FilledButton(onPressed: () => Navigator.pop(ctx, true), child: Text(ok)),
      ],
    ),
  );
  return r ?? false;
}

class _Empty extends StatelessWidget {
  const _Empty({required this.message, required this.icon});
  final String message;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return ListView(children: [
      const SizedBox(height: 120),
      Icon(icon, size: 64, color: Theme.of(context).colorScheme.outline),
      const SizedBox(height: 16),
      Text(message, textAlign: TextAlign.center),
    ]);
  }
}
