import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/device/device_info_service.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/notifications/fcm_service.dart';
import '../../../core/storage/secure_storage.dart';
import '../../notifications/data/notifications_repository.dart';
import '../domain/auth_user.dart';
import 'auth_api.dart';
import 'dtos/auth_dtos.dart';

final authApiProvider = Provider<AuthApi>((ref) {
  return AuthApi(ref.watch(dioProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    api: ref.watch(authApiProvider),
    storage: ref.watch(secureStorageProvider),
    deviceInfo: ref.watch(deviceInfoServiceProvider),
    fcmService: ref.watch(fcmServiceProvider),
    notifications: ref.watch(notificationsRepositoryProvider),
  );
});

/// Coordinates the auth API with secure-storage token persistence and maps
/// transport DTOs to the [AuthUser] domain model. All Dio failures are
/// normalized to [ApiException] so callers branch on a stable error code.
class AuthRepository {
  AuthRepository({
    required AuthApi api,
    required SecureStorage storage,
    required DeviceInfoService deviceInfo,
    required FcmService fcmService,
    required NotificationsRepository notifications,
  })  : _api = api,
        _storage = storage,
        _deviceInfo = deviceInfo,
        _fcmService = fcmService,
        _notifications = notifications;

  final AuthApi _api;
  final SecureStorage _storage;
  final DeviceInfoService _deviceInfo;
  final FcmService _fcmService;
  final NotificationsRepository _notifications;

  Future<bool> hasSession() => _storage.hasTokens();

  /// PG self-registration (BR-101). No tokens are issued; the user must verify
  /// their email then log in. Throws [ApiException] (e.g. `EMAIL_ALREADY_REGISTERED`).
  Future<void> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
    required String preferredLanguage,
  }) =>
      _guard(() => _api.register(
            email: email,
            password: password,
            fullName: fullName,
            phone: phone,
            preferredLanguage: preferredLanguage,
          ));

  /// Confirms the registration email. Throws [ApiException]
  /// (e.g. `EMAIL_TOKEN_EXPIRED`, `EMAIL_TOKEN_USED`).
  Future<void> verifyEmail(String token) =>
      _guard(() => _api.verifyEmail(token));

  Future<void> clearSession() => _storage.clear();

  /// Authenticates, persists the issued tokens, and returns the profile.
  /// Throws [ApiException] (e.g. `DEVICE_NOT_AUTHORIZED`, `INVALID_CREDENTIALS`).
  Future<AuthUser> login({
    required String email,
    required String password,
  }) async {
    return _guard(() async {
      final device = await _deviceInfo.resolve();
      // Best-effort: a null token (Firebase unconfigured / permission denied)
      // just omits it — the server treats it as optional (BR-105 device flow).
      final fcmToken = await _fcmService.token();
      debugPrint(
        'AuthRepository.login: fcmToken ${fcmToken == null ? 'NULL (không gửi lên server)' : 'present (${fcmToken.length} chars)'}',
      );
      final res = await _api.login(
        email: email,
        password: password,
        device: device,
        fcmToken: fcmToken,
      );
      await _storage.writeTokens(
        access: res.accessToken,
        refresh: res.refreshToken,
      );
      // Login body may omit fcmToken if permission/APNs were not ready yet.
      final refreshedToken = await _fcmService.token();
      if (refreshedToken != null && refreshedToken.isNotEmpty) {
        try {
          await _notifications.registerFcmToken(refreshedToken);
          debugPrint('AuthRepository.login: PUT /users/me/fcm-token OK');
        } catch (e) {
          debugPrint('AuthRepository.login: PUT /users/me/fcm-token failed ($e)');
        }
      } else {
        debugPrint('AuthRepository.login: skip PUT /users/me/fcm-token (no token)');
      }
      return _fromLoginUser(res.user);
    });
  }

  Future<AuthUser> me() => _guard(() async => _fromMe(await _api.me()));

  /// Best-effort server-side logout; always clears local tokens afterwards.
  Future<void> logout() async {
    try {
      final refresh = await _storage.readRefreshToken();
      if (refresh != null && refresh.isNotEmpty) {
        await _api.logout(refresh);
      }
    } on DioException {
      // Ignore — local sign-out must still proceed.
    } finally {
      await _storage.clear();
    }
  }

  Future<void> forgotPassword(String email) =>
      _guard(() => _api.forgotPassword(email));

  Future<void> resetPassword({
    required String token,
    required String newPassword,
  }) =>
      _guard(() => _api.resetPassword(token: token, newPassword: newPassword));

  // ── mapping ──────────────────────────────────────────────────────────────

  AuthUser _fromLoginUser(LoginUserDto u) => AuthUser(
        id: u.userId,
        email: u.email,
        fullName: u.fullName,
        role: UserRole.fromValue(u.role),
        status: u.status,
        preferredLanguage: u.preferredLanguage,
      );

  AuthUser _fromMe(MeDto m) => AuthUser(
        id: m.id,
        email: m.email,
        fullName: m.fullName,
        phone: m.phone,
        role: UserRole.fromValue(m.role),
        status: m.status,
        preferredLanguage: m.preferredLanguage,
      );

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
