import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/approval_repository.dart';
import '../../domain/approval.dart';

/// Leader's pending-approval queue (M09, AC-17): inline approve + reject-with-reason.
class ApprovalsScreen extends ConsumerWidget {
  const ApprovalsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(pendingApprovalsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l.approvalsTitle)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(pendingApprovalsProvider),
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => _CenteredList(
              icon: Icons.error_outline,
              message: e is ApiException ? e.message : l.commonRetry,
            ),
            data: (items) {
              if (items.isEmpty) {
                return _CenteredList(icon: Icons.inbox_outlined, message: l.approvalsEmpty);
              }
              return ListView.builder(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                itemCount: items.length,
                itemBuilder: (context, i) => _ApprovalCard(approval: items[i]),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _ApprovalCard extends ConsumerStatefulWidget {
  const _ApprovalCard({required this.approval});

  final Approval approval;

  @override
  ConsumerState<_ApprovalCard> createState() => _ApprovalCardState();
}

class _ApprovalCardState extends ConsumerState<_ApprovalCard> {
  bool _busy = false;

  String _entityText(AppLocalizations l, String raw) =>
      raw == 'work_schedule' ? l.approvalEntityWorkSchedule : raw;

  Future<void> _approve() async {
    final l = AppLocalizations.of(context);
    await _run(() => ref.read(approvalRepositoryProvider).approve(widget.approval.id), l.approvalApproved);
  }

  Future<void> _reject() async {
    final l = AppLocalizations.of(context);
    final reason = await _askReason(l);
    if (reason == null) return;
    await _run(() => ref.read(approvalRepositoryProvider).reject(widget.approval.id, reason), l.approvalRejected);
  }

  Future<void> _run(Future<void> Function() action, String okMessage) async {
    setState(() => _busy = true);
    try {
      await action();
      ref.invalidate(pendingApprovalsProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(okMessage)));
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<String?> _askReason(AppLocalizations l) async {
    final controller = TextEditingController();
    final result = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l.approvalRejectTitle),
        content: TextField(
          controller: controller,
          maxLength: 500,
          maxLines: 3,
          autofocus: true,
          decoration: InputDecoration(hintText: l.approvalReasonHint),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: Text(l.commonCancel)),
          FilledButton(
            onPressed: () {
              final v = controller.text.trim();
              if (v.isEmpty) return;
              Navigator.pop(ctx, v);
            },
            child: Text(l.approvalReject),
          ),
        ],
      ),
    );
    controller.dispose();
    return result;
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final a = widget.approval;
    final created = DateFormat.yMMMEd(Localizations.localeOf(context).languageCode).format(a.createdAt);

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: SoftCard(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(a.requesterName,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                ),
                StatusPill(label: l.approvalStatusPending, icon: Icons.hourglass_empty, tone: BrandTone.info),
              ],
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Icon(Icons.assignment_outlined, size: 16, color: scheme.onSurfaceVariant),
                const SizedBox(width: 6),
                Text(_entityText(l, a.entityType),
                    style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13)),
                const Spacer(),
                Text(created, style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 12.5)),
              ],
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _reject,
                    icon: const Icon(Icons.close, size: 18),
                    label: Text(l.approvalReject),
                    style: OutlinedButton.styleFrom(foregroundColor: scheme.error),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: _busy ? null : _approve,
                    icon: _busy
                        ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                        : const Icon(Icons.check, size: 18),
                    label: Text(l.approvalApprove),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CenteredList extends StatelessWidget {
  const _CenteredList({required this.icon, required this.message});

  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ListView(
      children: [
        const SizedBox(height: 120),
        Icon(icon, size: 64, color: scheme.outline),
        const SizedBox(height: 16),
        Text(message, textAlign: TextAlign.center),
      ],
    );
  }
}
