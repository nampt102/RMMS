import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/deeplink/deep_link_service.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'l10n/generated/app_localizations.dart';

/// Root widget. Wires:
///  - GoRouter (declarative routing)
///  - rmms:// deep-link handling (email verify + password reset)
///  - Locale handling (vi default, en supported)
///  - Material 3 theme
class RmmsApp extends ConsumerStatefulWidget {
  const RmmsApp({super.key});

  @override
  ConsumerState<RmmsApp> createState() => _RmmsAppState();
}

class _RmmsAppState extends ConsumerState<RmmsApp> {
  @override
  void initState() {
    super.initState();
    // Start listening for deep links once the first frame is scheduled, so the
    // router is mounted before we attempt to navigate from a cold-start link.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(deepLinkServiceProvider).init();
    });
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(appRouterProvider);

    return MaterialApp.router(
      title: 'RMMS',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.light,
      routerConfig: router,
      // i18n
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      locale: const Locale('vi'),
    );
  }
}
