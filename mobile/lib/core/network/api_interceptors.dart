import 'package:dio/dio.dart';

import '../device/device_info_service.dart';
import '../storage/secure_storage.dart';
import 'token_refresher.dart';

/// Marks a request that has already been replayed after a token refresh, so we
/// never loop on a second 401.
const _kRetriedFlag = '__rmms_retried';

/// Adds `Authorization: Bearer <accessToken>` and, on 401, transparently
/// rotates the refresh token once and replays the original request
/// (sprint-01 Day 6 auto-refresh interceptor).
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required SecureStorage storage,
    required TokenRefresher refresher,
    required Dio retryDio,
  })  : _storage = storage,
        _refresher = refresher,
        _retryDio = retryDio;

  final SecureStorage _storage;
  final TokenRefresher _refresher;

  /// Token-less Dio used to replay the original request after refresh, so the
  /// replay does not pass through this interceptor again.
  final Dio _retryDio;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await _storage.readAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final request = err.requestOptions;
    final alreadyRetried = request.extra[_kRetriedFlag] == true;

    if (err.response?.statusCode != 401 ||
        alreadyRetried ||
        _isAuthFreeEndpoint(request.path)) {
      handler.next(err);
      return;
    }

    final refreshed = await _refresher.refresh();
    if (!refreshed) {
      // Session is dead — wipe tokens so the router guard sends the user to login.
      await _storage.clear();
      handler.next(err);
      return;
    }

    try {
      final newToken = await _storage.readAccessToken();
      final retried = await _retryDio.fetch<dynamic>(
        request.copyWith(
          extra: {...request.extra, _kRetriedFlag: true},
          headers: {
            ...request.headers,
            if (newToken != null) 'Authorization': 'Bearer $newToken',
          },
        ),
      );
      handler.resolve(retried);
    } on DioException catch (e) {
      handler.next(e);
    }
  }

  /// Endpoints that must never trigger a refresh-on-401 (they ARE the auth
  /// surface, or a 401 there is a real credential failure).
  bool _isAuthFreeEndpoint(String path) {
    return path.contains('/auth/login') ||
        path.contains('/auth/refresh') ||
        path.contains('/auth/register') ||
        path.contains('/auth/forgot-password') ||
        path.contains('/auth/reset-password') ||
        path.contains('/auth/verify-email');
  }
}

/// Adds `X-Device-Id` and `X-App-Version` per 05-api-conventions.md, sourced
/// from the shared [DeviceInfoService] so the header id matches the login body.
class DeviceHeadersInterceptor extends Interceptor {
  DeviceHeadersInterceptor(this._deviceInfo);

  final DeviceInfoService _deviceInfo;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final device = await _deviceInfo.resolve();
    options.headers['X-Device-Id'] = device.deviceId;
    options.headers['X-App-Version'] = device.appVersion;
    handler.next(options);
  }
}
