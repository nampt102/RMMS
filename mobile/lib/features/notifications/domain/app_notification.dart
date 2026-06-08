import 'package:freezed_annotation/freezed_annotation.dart';

part 'app_notification.freezed.dart';
part 'app_notification.g.dart';

/// One in-app notification — mirrors the server `NotificationDto` (M14).
@freezed
sealed class AppNotification with _$AppNotification {
  const factory AppNotification({
    required String id,
    required String type,
    required String title,
    @Default('') String body,
    Map<String, String>? data,
    @Default(false) bool isRead,
    DateTime? readAt,
    required DateTime createdAt,
  }) = _AppNotification;

  factory AppNotification.fromJson(Map<String, dynamic> json) =>
      _$AppNotificationFromJson(json);
}

/// A page of notifications + the unread badge count — mirrors `NotificationListDto`.
@freezed
sealed class NotificationPage with _$NotificationPage {
  const factory NotificationPage({
    @Default(<AppNotification>[]) List<AppNotification> items,
    @Default(0) int unreadCount,
    @Default(1) int page,
    @Default(20) int pageSize,
    @Default(0) int total,
  }) = _NotificationPage;

  factory NotificationPage.fromJson(Map<String, dynamic> json) =>
      _$NotificationPageFromJson(json);
}
