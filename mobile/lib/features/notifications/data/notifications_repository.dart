import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/app_notification.dart';

final notificationsRepositoryProvider = Provider<NotificationsRepository>((ref) {
  return NotificationsRepository(ref.watch(dioProvider));
});

/// In-app notifications (M14): inbox, read-state, FCM token rotation.
class NotificationsRepository {
  NotificationsRepository(this._dio);

  final Dio _dio;

  Future<NotificationPage> page({int page = 1, int pageSize = 20}) async {
    try {
      final res = await _dio.get<Map<String, dynamic>>(
        '/notifications/me',
        queryParameters: {'page': page, 'pageSize': pageSize},
      );
      final data = res.data?['data'];
      if (data is! Map<String, dynamic>) {
        throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
      }
      return NotificationPage.fromJson(data);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<void> markRead(String id) async {
    try {
      await _dio.post<void>('/notifications/$id/read');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<void> markAllRead() async {
    try {
      await _dio.post<void>('/notifications/read-all');
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  /// Refresh the FCM token for the active device (token rotation, M14).
  /// Best-effort; callers ignore failures.
  Future<void> registerFcmToken(String token) async {
    try {
      await _dio.put<void>('/users/me/fcm-token', data: {'token': token});
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Inbox page (first page) — also the source for the unread badge count.
final notificationsPageProvider =
    FutureProvider.autoDispose<NotificationPage>((ref) {
  return ref.watch(notificationsRepositoryProvider).page();
});

/// Unread badge count. Derives from the inbox page so a single fetch feeds both;
/// returns 0 while loading or on error (badge simply hides).
final unreadCountProvider = Provider.autoDispose<int>((ref) {
  return ref.watch(notificationsPageProvider).maybeWhen(
        data: (p) => p.unreadCount,
        orElse: () => 0,
      );
});
