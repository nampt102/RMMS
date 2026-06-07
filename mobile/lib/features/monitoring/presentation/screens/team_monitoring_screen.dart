import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/monitoring_repository.dart';
import '../../domain/team_today.dart';

/// Leader's team-today view (M12, AC-26): PG status list + summary. Manual refresh.
class TeamMonitoringScreen extends ConsumerWidget {
  const TeamMonitoringScreen({super.key});

  static ({BrandTone tone, String label}) meta(AppLocalizations l, String status) => switch (status) {
        'working' => (tone: BrandTone.success, label: l.monStatusWorking),
        'checked_out' => (tone: BrandTone.info, label: l.monStatusCheckedOut),
        'not_checked_in' => (tone: BrandTone.warning, label: l.monStatusNotCheckedIn),
        'on_leave' => (tone: BrandTone.brand, label: l.monStatusOnLeave),
        'pending_review' => (tone: BrandTone.danger, label: l.monStatusPendingReview),
        _ => (tone: BrandTone.neutral, label: l.monStatusNoSchedule),
      };

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(teamTodayProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l.monTitle)),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(teamTodayProvider),
          child: async.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => _Center(message: e is ApiException ? e.message : l.commonRetry),
            data: (data) => _Body(data: data),
          ),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.data});
  final TeamToday data;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    const order = ['working', 'not_checked_in', 'pending_review', 'checked_out', 'on_leave', 'no_schedule_today'];

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final s in order)
              if ((data.summary[s] ?? 0) > 0)
                StatusPill(
                  label: '${TeamMonitoringScreen.meta(l, s).label}: ${data.summary[s]}',
                  tone: TeamMonitoringScreen.meta(l, s).tone,
                ),
          ],
        ),
        const SizedBox(height: 16),
        if (data.members.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 48),
            child: Center(child: Text(l.monEmpty)),
          )
        else
          ...data.members.map((m) => Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _MemberCard(member: m),
              )),
        const SizedBox(height: 8),
        Center(
          child: Text(
            '${l.monAsOf}: ${DateFormat.Hm(Localizations.localeOf(context).languageCode).format(data.asOf.toLocal())}',
            style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 12),
          ),
        ),
      ],
    );
  }
}

class _MemberCard extends StatelessWidget {
  const _MemberCard({required this.member});
  final TeamMember member;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final scheme = context.scheme;
    final m = TeamMonitoringScreen.meta(l, member.status);
    final sub = <String>[
      if (member.checkInAt != null)
        DateFormat.Hm(Localizations.localeOf(context).languageCode).format(member.checkInAt!.toLocal()),
      if ((member.storeName ?? '').isNotEmpty) member.storeName!,
    ].join(' · ');

    return SoftCard(
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(member.fullName, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                if (sub.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(sub, style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 12.5)),
                ],
              ],
            ),
          ),
          StatusPill(label: m.label, tone: m.tone),
        ],
      ),
    );
  }
}

class _Center extends StatelessWidget {
  const _Center({required this.message});
  final String message;

  @override
  Widget build(BuildContext context) {
    return ListView(children: [
      const SizedBox(height: 120),
      Icon(Icons.error_outline, size: 56, color: Theme.of(context).colorScheme.outline),
      const SizedBox(height: 12),
      Text(message, textAlign: TextAlign.center),
    ]);
  }
}
