import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../l10n/generated/app_localizations.dart';
import '../../data/organization_repository.dart';
import '../../domain/assigned_leader.dart';
import '../../domain/assigned_store.dart';

/// Read-only view of the signed-in user's M03 assignments: their managing
/// Leader (PG only) and the stores they are assigned to. Backed by
/// `GET /users/me/leader` and `GET /users/me/stores`.
class MyAssignmentsScreen extends ConsumerWidget {
  const MyAssignmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final stores = ref.watch(myStoresProvider);
    final leader = ref.watch(myLeaderProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l.assignmentsTitle)),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(myStoresProvider);
          ref.invalidate(myLeaderProvider);
          await Future.wait([
            ref.read(myStoresProvider.future),
            ref.read(myLeaderProvider.future),
          ]);
        },
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _SectionTitle(l.assignmentsLeader),
            leader.when(
              loading: () => const _Loading(),
              error: (_, __) => _ErrorTile(
                message: l.assignmentsError,
                onTap: () => ref.invalidate(myLeaderProvider),
              ),
              data: (data) => _LeaderTile(leader: data, emptyText: l.assignmentsNoLeader),
            ),
            const SizedBox(height: 24),
            _SectionTitle(l.assignmentsStores),
            stores.when(
              loading: () => const _Loading(),
              error: (_, __) => _ErrorTile(
                message: l.assignmentsError,
                onTap: () => ref.invalidate(myStoresProvider),
              ),
              data: (data) => data.isEmpty
                  ? _EmptyText(l.assignmentsNoStores)
                  : Column(
                      children: data.map((s) => _StoreTile(store: s)).toList(growable: false),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(text, style: Theme.of(context).textTheme.titleMedium),
      );
}

class _LeaderTile extends StatelessWidget {
  const _LeaderTile({required this.leader, required this.emptyText});
  final AssignedLeader? leader;
  final String emptyText;

  @override
  Widget build(BuildContext context) {
    if (leader == null) return _EmptyText(emptyText);
    return Card(
      child: ListTile(
        leading: const Icon(Icons.supervisor_account_outlined),
        title: Text(leader!.fullName),
        subtitle: Text(leader!.phone == null ? leader!.email : '${leader!.email}\n${leader!.phone}'),
        isThreeLine: leader!.phone != null,
      ),
    );
  }
}

class _StoreTile extends StatelessWidget {
  const _StoreTile({required this.store});
  final AssignedStore store;

  @override
  Widget build(BuildContext context) {
    final inactive = store.status != 'active';
    return Card(
      child: ListTile(
        leading: const Icon(Icons.storefront_outlined),
        title: Text('${store.code} — ${store.name}'),
        subtitle: store.address == null ? null : Text(store.address!),
        trailing: inactive
            ? const Icon(Icons.block, size: 18, color: Colors.grey)
            : null,
      ),
    );
  }
}

class _EmptyText extends StatelessWidget {
  const _EmptyText(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Text(text, style: TextStyle(color: Theme.of(context).hintColor)),
      );
}

class _Loading extends StatelessWidget {
  const _Loading();

  @override
  Widget build(BuildContext context) => const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: Center(child: CircularProgressIndicator()),
      );
}

class _ErrorTile extends StatelessWidget {
  const _ErrorTile({required this.message, required this.onTap});
  final String message;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Card(
        color: Theme.of(context).colorScheme.errorContainer,
        child: ListTile(
          leading: const Icon(Icons.error_outline),
          title: Text(message),
          onTap: onTap,
        ),
      );
}
