import 'dart:async';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

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

    _subs.add(_service.onForegroundMessage
        .listen((m) => _handle(m, showBanner: true)));
    _subs.add(_service.onMessageOpenedApp
        .listen((m) => _handle(m, showBanner: true)));
    _subs.add(_service.onTokenRefresh.listen(
      (t) => debugPrint('FcmCoordinator: token refreshed (re-register in M14)'),
    ));

    // A notification tap that cold-started the app.
    final initial = await _service.initialMessage();
    if (initial != null) _handle(initial, showBanner: false);

    _ref.onDispose(() {
      for (final s in _subs) {
        s.cancel();
      }
    });
  }

  void _handle(RemoteMessage message, {required bool showBanner}) {
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
