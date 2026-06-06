import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_router.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../auth/application/auth_controller.dart';
import '../../../auth/application/auth_state.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final auth = ref.watch(authControllerProvider);
    final user = auth is AuthAuthenticated ? auth.user : null;

    return Scaffold(
      appBar: AppBar(
        title: Text(l.appName),
        actions: [
          IconButton(
            tooltip: l.homeLogout,
            icon: const Icon(Icons.logout),
            onPressed: () =>
                ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                user == null ? l.appName : l.homeWelcome(user.fullName),
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleLarge,
              ),
              if (user != null) ...[
                const SizedBox(height: 8),
                Text(l.homeRoleLabel(user.role.name)),
              ],
              const SizedBox(height: 24),
              FilledButton.tonalIcon(
                icon: const Icon(Icons.assignment_outlined),
                label: Text(l.homeViewAssignments),
                onPressed: () => context.push(AppRoutes.myAssignments),
              ),
              const SizedBox(height: 12),
              FilledButton.tonalIcon(
                icon: const Icon(Icons.calendar_month_outlined),
                label: Text(l.homeViewSchedule),
                onPressed: () => context.push(AppRoutes.schedule),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                icon: const Icon(Icons.how_to_reg_outlined),
                label: Text(l.homeViewAttendance),
                onPressed: () => context.push(AppRoutes.attendance),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
