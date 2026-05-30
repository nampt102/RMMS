import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Wraps `flutter_secure_storage` so we can mock it in tests.
class SecureStorage {
  SecureStorage(this._storage);

  static const _kAccessToken = 'rmms.access_token';
  static const _kRefreshToken = 'rmms.refresh_token';
  static const _kDeviceId = 'rmms.device_id';

  final FlutterSecureStorage _storage;

  Future<String?> readAccessToken() => _storage.read(key: _kAccessToken);
  Future<String?> readRefreshToken() => _storage.read(key: _kRefreshToken);

  Future<void> writeTokens({
    required String access,
    required String refresh,
  }) async {
    await _storage.write(key: _kAccessToken, value: access);
    await _storage.write(key: _kRefreshToken, value: refresh);
  }

  /// True when both tokens are present — used to decide whether to attempt a
  /// silent session restore on app launch.
  Future<bool> hasTokens() async {
    final access = await readAccessToken();
    final refresh = await readRefreshToken();
    return (access != null && access.isNotEmpty) &&
        (refresh != null && refresh.isNotEmpty);
  }

  /// Install-scoped device fingerprint (sprint-01 R-7). Persists across app
  /// updates but is wiped on uninstall, so a reinstall counts as a new device.
  Future<String?> readDeviceId() => _storage.read(key: _kDeviceId);

  Future<void> writeDeviceId(String deviceId) =>
      _storage.write(key: _kDeviceId, value: deviceId);

  /// Clears the auth session. Keeps the device id (it must survive logout so the
  /// same physical device stays recognized by BR-105 on the next login).
  Future<void> clear() async {
    await _storage.delete(key: _kAccessToken);
    await _storage.delete(key: _kRefreshToken);
  }
}

final secureStorageProvider = Provider<SecureStorage>((ref) {
  const options = AndroidOptions(encryptedSharedPreferences: true);
  return SecureStorage(const FlutterSecureStorage(aOptions: options));
});
