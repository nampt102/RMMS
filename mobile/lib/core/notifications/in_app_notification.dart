import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Severity of a foreground in-app banner — drives its color/icon.
enum InAppNotificationLevel { info, success, warning }

/// A transient foreground banner shown when an FCM message arrives while the
/// app is open. Plain immutable class (no codegen) so the build needs no
/// `build_runner` pass.
class InAppNotification {
  const InAppNotification({
    required this.title,
    required this.body,
    this.level = InAppNotificationLevel.info,
  });

  final String title;
  final String body;
  final InAppNotificationLevel level;
}

/// Holds the currently visible in-app banner (or `null`). The root widget
/// watches this and renders a [MaterialBanner].
class InAppNotificationController extends Notifier<InAppNotification?> {
  @override
  InAppNotification? build() => null;

  void show(InAppNotification notification) => state = notification;
  void clear() => state = null;
}

final inAppNotificationProvider =
    NotifierProvider<InAppNotificationController, InAppNotification?>(
  InAppNotificationController.new,
);

/// Outcome of a device-change approval (BR-105/BR-106) delivered via FCM.
enum DeviceApprovalOutcome { approved, rejected }

/// Latest device-change outcome pushed to this install, consumed by the
/// device-pending screen to switch its call-to-action.
class DeviceApprovalController extends Notifier<DeviceApprovalOutcome?> {
  @override
  DeviceApprovalOutcome? build() => null;

  void set(DeviceApprovalOutcome outcome) => state = outcome;
  void clear() => state = null;
}

final deviceApprovalProvider =
    NotifierProvider<DeviceApprovalController, DeviceApprovalOutcome?>(
  DeviceApprovalController.new,
);
