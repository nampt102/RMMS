import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/deeplink/deep_link_service.dart';
import 'core/notifications/fcm_coordinator.dart';
import 'core/notifications/in_app_notification.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'l10n/generated/app_localizations.dart';

/// Root widget. Wires:
///  - GoRouter (declarative routing)
///  - rmms:// deep-link handling (email verify + password reset)
///  - FCM (foreground banner + device-change handling)
///  - Locale handling (vi default, en supported)
///  - Material 3 theme
class RmmsApp extends ConsumerStatefulWidget {
  const RmmsApp({super.key});

  @override
  ConsumerState<RmmsApp> createState() => _RmmsAppState();
}

class _RmmsAppState extends ConsumerState<RmmsApp> {
  final GlobalKey<ScaffoldMessengerState> _messengerKey =
      GlobalKey<ScaffoldMessengerState>();

  @override
  void initState() {
    super.initState();
    // Defer until the first frame so the router/messenger are mounted before we
    // navigate or show a banner from a cold-start link or notification.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(deepLinkServiceProvider).init();
      ref.read(fcmCoordinatorProvider).start();
    });
  }

  void _showBanner(InAppNotification n) {
    final messenger = _messengerKey.currentState;
    if (messenger == null) return;

    final (Color bg, Color fg, IconData icon) = switch (n.level) {
      InAppNotificationLevel.success => (
          const Color(0xFFE7F4EA),
          const Color(0xFF1B5E20),
          Icons.check_circle_outline,
        ),
      InAppNotificationLevel.warning => (
          const Color(0xFFFDECEA),
          const Color(0xFFB3261E),
          Icons.warning_amber_rounded,
        ),
      InAppNotificationLevel.info => (
          const Color(0xFFE8F0FE),
          const Color(0xFF174EA6),
          Icons.notifications_none,
        ),
    };

    messenger
      ..clearMaterialBanners()
      ..showMaterialBanner(
        MaterialBanner(
          backgroundColor: bg,
          leading: Icon(icon, color: fg),
          content: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              if (n.title.isNotEmpty)
                Text(
                  n.title,
                  style: TextStyle(fontWeight: FontWeight.w600, color: fg),
                ),
              if (n.body.isNotEmpty)
                Text(n.body, style: TextStyle(color: fg)),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () {
                messenger.clearMaterialBanners();
                ref.read(inAppNotificationProvider.notifier).clear();
              },
              child: Text(
                AppLocalizations.of(context).commonDismiss,
                style: TextStyle(color: fg),
              ),
            ),
          ],
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(appRouterProvider);

    // Surface foreground FCM banners as they arrive.
    ref.listen<InAppNotification?>(inAppNotificationProvider, (_, next) {
      if (next != null) _showBanner(next);
    });

    return MaterialApp.router(
      title: 'RMMS',
      debugShowCheckedModeBanner: false,
      scaffoldMessengerKey: _messengerKey,
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
