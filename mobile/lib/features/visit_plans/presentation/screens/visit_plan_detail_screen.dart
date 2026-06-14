import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/visit_plans_repository.dart';
import '../../domain/visit_plan_models.dart';
import 'visit_plans_list_screen.dart' show formatVisitDate, visitPlanStatusChip;

/// Visit plan detail (M11). Approved plans let the Leader fill the report form for
/// each store, linking the submission (AC-30) — once all are linked the plan is executed.
class VisitPlanDetailScreen extends ConsumerStatefulWidget {
  const VisitPlanDetailScreen({super.key, required this.id});
  final String id;

  @override
  ConsumerState<VisitPlanDetailScreen> createState() => _VisitPlanDetailScreenState();
}

class _VisitPlanDetailScreenState extends ConsumerState<VisitPlanDetailScreen> {
  String? _busyItemId;

  Future<void> _report(VisitPlan plan, VisitPlanItem item) async {
    final l = AppLocalizations.of(context);
    // Fill the report form (M10). It pops with the new submission id on success.
    final submissionId = await context.push<String>('${AppRoutes.forms}/${item.formId}');
    if (submissionId == null || !mounted) return;

    setState(() => _busyItemId = item.id);
    try {
      await ref.read(visitPlansRepositoryProvider).executeItem(
            planId: plan.id,
            itemId: item.id,
            formSubmissionId: submissionId,
          );
      ref.invalidate(visitPlanProvider(plan.id));
      ref.invalidate(myVisitPlansProvider);
      if (mounted) showAppToast(context, message: l.visitPlanExecuted, kind: AppToastKind.success);
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _busyItemId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(visitPlanProvider(widget.id));

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.visitPlanDetailTitle),
            Expanded(
              child: async.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => Center(child: Text(l.commonError, style: const TextStyle(color: AppPalette.muted))),
                data: (plan) => RefreshIndicator(
                  onRefresh: () async => ref.invalidate(visitPlanProvider(widget.id)),
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                    children: [
                      _Header(plan: plan),
                      const SizedBox(height: 16),
                      Text(l.visitPlanItems,
                          style: const TextStyle(color: AppPalette.ink, fontSize: 15, fontWeight: FontWeight.w800)),
                      const SizedBox(height: 10),
                      ...plan.items.map((item) => Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: _ItemTile(
                              item: item,
                              canReport: plan.isApproved,
                              busy: _busyItemId == item.id,
                              onReport: () => _report(plan, item),
                            ),
                          )),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.plan});
  final VisitPlan plan;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final chip = visitPlanStatusChip(l, plan.status);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.event_rounded, color: AppPalette.indigo),
              const SizedBox(width: 10),
              Text(formatVisitDate(plan.visitDate),
                  style: const TextStyle(color: AppPalette.ink, fontSize: 18, fontWeight: FontWeight.w800)),
              const Spacer(),
              AppChip(label: chip.label, tone: chip.tone),
            ],
          ),
          const SizedBox(height: 10),
          Text(l.visitPlanStoresProgress(plan.doneCount, plan.items.length),
              style: const TextStyle(color: AppPalette.muted, fontSize: 13)),
          if (plan.notes != null && plan.notes!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(plan.notes!, style: const TextStyle(color: AppPalette.ink, fontSize: 14)),
          ],
          if (plan.status == 'pending') ...[
            const SizedBox(height: 10),
            _Banner(text: l.visitPlanAwaitingApproval, tone: AppTone.amber),
          ] else if (plan.status == 'rejected') ...[
            const SizedBox(height: 10),
            _Banner(text: l.visitPlanRejectedBanner, tone: AppTone.rose),
          ],
        ],
      ),
    );
  }
}

class _Banner extends StatelessWidget {
  const _Banner({required this.text, required this.tone});
  final String text;
  final AppTone tone;

  @override
  Widget build(BuildContext context) {
    final c = chipColors(tone);
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: c.bg, borderRadius: BorderRadius.circular(12)),
      child: Text(text, style: TextStyle(color: c.fg, fontSize: 13, fontWeight: FontWeight.w600)),
    );
  }
}

class _ItemTile extends StatelessWidget {
  const _ItemTile({required this.item, required this.canReport, required this.busy, required this.onReport});
  final VisitPlanItem item;
  final bool canReport;
  final bool busy;
  final VoidCallback onReport;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              AppIconTile(
                icon: item.isDone ? Icons.check_circle_rounded : Icons.store_rounded,
                tone: item.isDone ? AppTone.emerald : AppTone.sky,
                size: 40,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(item.storeName ?? item.storeId,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: AppPalette.ink, fontWeight: FontWeight.w700)),
                    const SizedBox(height: 2),
                    Text(item.formName ?? item.formId,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: AppPalette.muted, fontSize: 13)),
                  ],
                ),
              ),
              if (item.isDone) AppChip(label: l.visitPlanReported, tone: AppTone.emerald, icon: Icons.check_rounded),
            ],
          ),
          if (canReport && !item.isDone) ...[
            const SizedBox(height: 12),
            AppButton.soft(label: l.visitPlanReport, icon: Icons.edit_note_rounded, loading: busy, onPressed: onReport),
          ],
        ],
      ),
    );
  }
}
