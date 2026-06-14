import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/visit_plans_repository.dart';
import '../../domain/visit_plan_models.dart';

/// (AppTone, label) for a visit-plan status — color + text, never color alone.
({AppTone tone, String label}) visitPlanStatusChip(AppLocalizations l, String status) => switch (status) {
      'approved' => (tone: AppTone.sky, label: l.visitPlanStatusApproved),
      'rejected' => (tone: AppTone.rose, label: l.visitPlanStatusRejected),
      'executed' => (tone: AppTone.emerald, label: l.visitPlanStatusExecuted),
      _ => (tone: AppTone.amber, label: l.visitPlanStatusPending),
    };

String formatVisitDate(DateTime d) =>
    '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

class VisitPlansListScreen extends ConsumerWidget {
  const VisitPlansListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final plans = ref.watch(myVisitPlansProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(
              title: l.visitPlansTitle,
              trailing: [
                PressScale(
                  onTap: () => context.push(AppRoutes.visitPlanNew),
                  child: Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: AppPalette.surface,
                      borderRadius: BorderRadius.circular(13),
                    ),
                    alignment: Alignment.center,
                    child: const Icon(Icons.add_rounded, color: AppPalette.indigo, size: 24),
                  ),
                ),
              ],
            ),
            Expanded(
              child: plans.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, __) => _Message(text: l.commonError),
                data: (items) {
                  if (items.isEmpty) return _Message(text: l.visitPlansEmpty);
                  return RefreshIndicator(
                    onRefresh: () async => ref.invalidate(myVisitPlansProvider),
                    child: ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 12),
                      itemBuilder: (context, i) => _PlanCard(plan: items[i]),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PlanCard extends StatelessWidget {
  const _PlanCard({required this.plan});
  final VisitPlan plan;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final chip = visitPlanStatusChip(l, plan.status);
    return AppCard(
      onTap: () => context.push('${AppRoutes.visitPlans}/${plan.id}'),
      child: Row(
        children: [
          const AppIconTile(icon: Icons.pin_drop_rounded, tone: AppTone.sky),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  formatVisitDate(plan.visitDate),
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.2,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  l.visitPlanStoresProgress(plan.doneCount, plan.items.length),
                  style: const TextStyle(color: AppPalette.muted, fontSize: 13),
                ),
              ],
            ),
          ),
          AppChip(label: chip.label, tone: chip.tone),
          const SizedBox(width: 6),
          const Icon(Icons.chevron_right_rounded, color: AppPalette.faint),
        ],
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Text(text, textAlign: TextAlign.center, style: const TextStyle(color: AppPalette.muted)),
        ),
      );
}
