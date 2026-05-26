import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../l10n/generated/app_localizations.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l.appName)),
      body: const Center(
        child: Text(
          'Scaffold ok. M01 sẽ thay màn hình này.',
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}
