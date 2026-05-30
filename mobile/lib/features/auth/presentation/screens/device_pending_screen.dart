import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../l10n/generated/app_localizations.dart';
import '../../application/auth_controller.dart';

/// Shown after a valid login from a device that is not the user's active device
/// (BR-105). A pending approval row exists server-side; the Leader/Admin
/// approval workflow + live polling land in Sprint 02.
class DevicePendingScreen extends ConsumerWidget {
  const DevicePendingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l.devicePendingTitle)),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Icon(
                Icons.phonelink_lock_outlined,
                size: 72,
                color: Theme.of(context).colorScheme.primary,
              ),
              const SizedBox(height: 24),
              Text(
                l.devicePendingTitle,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 12),
              Text(l.devicePendingBody, textAlign: TextAlign.center),
              const SizedBox(height: 32),
              OutlinedButton(
                onPressed: () =>
                    ref.read(authControllerProvider.notifier).cancelPendingDevice(),
                child: Text(l.devicePendingBackToLogin),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
