import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Wraps `flutter_secure_storage` so we can mock it in tests.
class SecureStorage {
  SecureStorage(this._storage);

  static const _kAccessToken = 'rmms.access_token';
  static const _kRefreshToken = 'rmms.refresh_token';

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

  Future<void> clear() async {
    await _storage.delete(key: _kAccessToken);
    await _storage.delete(key: _kRefreshToken);
  }
}

final secureStorageProvider = Provider<SecureStorage>((ref) {
  const options = AndroidOptions(encryptedSharedPreferences: true);
  return SecureStorage(const FlutterSecureStorage(aOptions: options));
});
