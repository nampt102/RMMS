import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/notifications/in_app_notification.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../application/auth_controller.dart';

/// Shown after a valid login from a device pending Leader/Admin approval
/// (BR-105/BR-106). Reacts live to the FCM device-change push:
///  - approved → success state + "sign in again" CTA
///  - rejected → rejected state + back-to-login CTA
///  - pending  → the original waiting state
class DevicePendingScreen extends ConsumerWidget {
  const DevicePendingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final scheme = Theme.of(context).colorScheme;
    final outcome = ref.watch(deviceApprovalProvider);

    // Clears the pushed outcome then returns to the login screen.
    Future<void> backToLogin() async {
      ref.read(deviceApprovalProvider.notifier).clear();
      await ref.read(authControllerProvider.notifier).cancelPendingDevice();
    }

    final (IconData icon, Color iconColor, String title, String body) = switch (outcome) {
      DeviceApprovalOutcome.approved => (
          Icons.verified_user_outlined,
          scheme.primary,
          l.devicePendingApprovedTitle,
          l.devicePendingApprovedBody,
        ),
      DeviceApprovalOutcome.rejected => (
          Icons.gpp_bad_outlined,
          scheme.error,
          l.devicePendingRejectedTitle,
          l.devicePendingRejectedBody,
        ),
      null => (
          Icons.phonelink_lock_outlined,
          scheme.primary,
          l.devicePendingTitle,
          l.devicePendingBody,
        ),
    };

    return Scaffold(
      appBar: AppBar(title: Text(l.devicePendingTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Icon(icon, size: 72, color: iconColor),
              const SizedBox(height: 24),
              Text(
                title,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 12),
              Text(body, textAlign: TextAlign.center),
              const SizedBox(height: 32),
              if (outcome == DeviceApprovalOutcome.approved)
                FilledButton(
                  style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(48), // ≥44pt touch target
                  ),
                  onPressed: backToLogin,
                  child: Text(l.devicePendingLoginAgain),
                )
              else
                OutlinedButton(
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                  ),
                  onPressed: backToLogin,
                  child: Text(l.devicePendingBackToLogin),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
