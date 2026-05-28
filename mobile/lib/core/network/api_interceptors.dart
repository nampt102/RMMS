import 'package:device_info_plus/device_info_plus.dart';
import 'package:dio/dio.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../storage/secure_storage.dart';

/// Adds `Authorization: Bearer <accessToken>` from secure storage.
/// On 401, will trigger refresh flow (TODO: implement in M01).
class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._storage);

  final SecureStorage _storage;

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
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.response?.statusCode == 401) {
      // TODO(M01): rotate refresh token, retry the original request once.
    }
    handler.next(err);
  }
}

/// Adds `X-Device-Id` and `X-App-Version` per 05-api-conventions.md.
class DeviceHeadersInterceptor extends Interceptor {
  String? _deviceId;
  String? _appVersion;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    _deviceId ??= await _resolveDeviceId();
    _appVersion ??= await _resolveAppVersion();

    if (_deviceId != null) {
      options.headers['X-Device-Id'] = _deviceId;
    }
    if (_appVersion != null) {
      options.headers['X-App-Version'] = _appVersion;
    }
    handler.next(options);
  }

  Future<String?> _resolveDeviceId() async {
    final info = DeviceInfoPlugin();
    try {
      final android = await info.androidInfo;
      return android.id;
    } catch (_) {
      /* iOS */
    }
    try {
      final ios = await info.iosInfo;
      return ios.identifierForVendor;
    } catch (_) {}
    return null;
  }

  Future<String> _resolveAppVersion() async {
    final pkg = await PackageInfo.fromPlatform();
    return '${pkg.version}+${pkg.buildNumber}';
  }
}
