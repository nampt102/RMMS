import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/deeplink/deep_link_service.dart';
import 'core/notifications/fcm_coordinator.dart';
import 'core/notifications/in_app_notification.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'core/widgets/app_widgets.dart';
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
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(deepLinkServiceProvider).init();
      ref.read(fcmCoordinatorProvider).start();
    });
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(appRouterProvider);
    final banner = ref.watch(inAppNotificationProvider);

    return MaterialApp.router(
      title: 'RMMS',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.light,
      routerConfig: router,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      locale: const Locale('vi'),
      // Host foreground push banner in the widget tree (Overlay via
      // ScaffoldMessenger context was unreliable with MaterialApp.router).
      builder: (context, child) {
        final l10n = AppLocalizations.of(context);
        return Stack(
          clipBehavior: Clip.none,
          children: [
            child ?? const SizedBox.shrink(),
            if (banner != null)
              AppPushBanner(
                key: ValueKey('${banner.title}|${banner.body}'),
                title: banner.title,
                body: banner.body,
                kind: switch (banner.level) {
                  InAppNotificationLevel.success => AppPushBannerKind.success,
                  InAppNotificationLevel.warning => AppPushBannerKind.warning,
                  InAppNotificationLevel.info => AppPushBannerKind.info,
                },
                dismissLabel: l10n.commonDismiss,
                onDismiss: () =>
                    ref.read(inAppNotificationProvider.notifier).clear(),
              ),
          ],
        );
      },
    );
  }
}
