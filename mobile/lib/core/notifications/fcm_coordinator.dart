import 'dart:async';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/notifications/data/notifications_repository.dart';
import '../router/app_router.dart';
import 'fcm_service.dart';
import 'in_app_notification.dart';

/// Message-contract constants shared with the (M14) server push payload.
class FcmMessageKeys {
  FcmMessageKeys._();

  static const String type = 'type';
  static const String status = 'status';

  /// Device-change approval/rejection (BR-105 / BR-106).
  static const String typeDeviceChanged = 'device_changed';
  static const String statusApproved = 'approved';
  static const String statusRejected = 'rejected';

  /// M14 push payloads carry a deep link (e.g. rmms://approvals/123).
  static const String deepLink = 'deepLink';
}

/// Wires [FcmService] streams into app state:
///  - device-change messages → [deviceApprovalProvider] (drives the pending screen)
///  - foreground/opened messages with a notification block → [inAppNotificationProvider] banner
///
/// `start()` is idempotent and safe to call when Firebase is unavailable
/// (every stream is empty in that case).
class FcmCoordinator {
  FcmCoordinator(this._ref);

  final Ref _ref;
  bool _started = false;
  final List<StreamSubscription<Object?>> _subs = [];

  FcmService get _service => _ref.read(fcmServiceProvider);

  Future<void> start() async {
    if (_started) return;
    _started = true;

    await _service.initialize();
    await _service.requestPermission();

    // Foreground: show an in-app banner, do not auto-navigate (no user tap).
    _subs.add(_service.onForegroundMessage
        .listen((m) => _handle(m, showBanner: true, navigate: false)));
    // Tapped from tray (warm start): follow the deep link.
    _subs.add(_service.onMessageOpenedApp
        .listen((m) => _handle(m, showBanner: false, navigate: true)));
    // M14: re-register a rotated FCM token with the active device (best-effort).
    _subs.add(_service.onTokenRefresh.listen(_registerToken));

    // A notification tap that cold-started the app: follow the deep link.
    final initial = await _service.initialMessage();
    if (initial != null) _handle(initial, showBanner: false, navigate: true);

    _ref.onDispose(() {
      for (final s in _subs) {
        s.cancel();
      }
    });
  }

  Future<void> _registerToken(String token) async {
    if (token.isEmpty) return;
    try {
      await _ref.read(notificationsRepositoryProvider).registerFcmToken(token);
    } catch (e) {
      // Not signed in / no active device yet — ignore; login re-sends the token.
      debugPrint('FcmCoordinator: fcm-token register skipped ($e)');
    }
  }

  /// Map a server deep link (rmms://host/id) to an in-app route. Unknown → null.
  static String? _routeForDeepLink(String? deepLink) {
    if (deepLink == null || deepLink.isEmpty) return null;
    final uri = Uri.tryParse(deepLink);
    if (uri == null) return null;
    return switch (uri.host) {
      'approvals' => AppRoutes.approvals,
      'requests' => AppRoutes.requests,
      'schedules' => AppRoutes.schedule,
      'attendance' => AppRoutes.attendance,
      _ => null,
    };
  }

  void _handle(RemoteMessage message, {required bool showBanner, required bool navigate}) {
    final data = message.data;

    if (data[FcmMessageKeys.type] == FcmMessageKeys.typeDeviceChanged) {
      final status = data[FcmMessageKeys.status];
      final approval = _ref.read(deviceApprovalProvider.notifier);
      if (status == FcmMessageKeys.statusApproved) {
        approval.set(DeviceApprovalOutcome.approved);
      } else if (status == FcmMessageKeys.statusRejected) {
        approval.set(DeviceApprovalOutcome.rejected);
      }
    }

    // A notification tap (opened/initial) with a deep link → navigate.
    if (navigate) {
      final route = _routeForDeepLink(data[FcmMessageKeys.deepLink] as String?);
      if (route != null) {
        _ref.read(appRouterProvider).push(route);
      }
    }

    // Foreground messages get no OS tray entry on Android — surface a banner
    // using the server-localized notification copy (M14 sends vi/en per user).
    if (showBanner) {
      final n = message.notification;
      if (n != null && (n.title != null || n.body != null)) {
        final rejected = data[FcmMessageKeys.type] == FcmMessageKeys.typeDeviceChanged &&
            data[FcmMessageKeys.status] == FcmMessageKeys.statusRejected;
        _ref.read(inAppNotificationProvider.notifier).show(
              InAppNotification(
                title: n.title ?? '',
                body: n.body ?? '',
                level: rejected
                    ? InAppNotificationLevel.warning
                    : InAppNotificationLevel.info,
              ),
            );
      }
    }
  }
}

final fcmCoordinatorProvider =
    Provider<FcmCoordinator>((ref) => FcmCoordinator(ref));
