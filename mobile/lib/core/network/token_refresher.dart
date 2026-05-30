import 'package:dio/dio.dart';

import '../storage/secure_storage.dart';

/// Performs refresh-token rotation against `POST /auth/refresh`, persisting the
/// rotated pair. Uses a single-flight guard so a burst of concurrent 401s
/// triggers exactly one refresh call (sprint-01 R-3: avoid rotation races that
/// would trip server-side reuse detection and revoke every token).
class TokenRefresher {
  TokenRefresher(this._storage, this._dio);

  final SecureStorage _storage;

  /// A token-less Dio (no [AuthInterceptor]) to avoid recursive refreshes.
  final Dio _dio;

  Future<bool>? _inflight;

  /// Returns true if a valid new access/refresh pair was stored.
  Future<bool> refresh() {
    return _inflight ??= _doRefresh().whenComplete(() => _inflight = null);
  }

  Future<bool> _doRefresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) return false;

    try {
      final res = await _dio.post<Map<String, dynamic>>(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );
      final data = res.data?['data'];
      if (data is! Map<String, dynamic>) return false;

      final access = data['accessToken'] as String?;
      final refresh = data['refreshToken'] as String?;
      if (access == null || refresh == null) return false;

      await _storage.writeTokens(access: access, refresh: refresh);
      return true;
    } on DioException {
      // Refresh token invalid/expired/reused → caller forces re-login.
      return false;
    }
  }
}
