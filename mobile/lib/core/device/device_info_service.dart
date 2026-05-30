import 'dart:io';

import 'package:device_info_plus/device_info_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../storage/secure_storage.dart';
import '../utils/app_uuid.dart';

/// Immutable description of the current device, sent both as request headers
/// (`X-Device-Id` / `X-App-Version`) and in the `/auth/login` body so the
/// backend device check (BR-105) sees a single, consistent fingerprint.
class DeviceDescriptor {
  const DeviceDescriptor({
    required this.deviceId,
    required this.deviceName,
    required this.os,
    required this.osVersion,
    required this.appVersion,
  });

  /// Install-scoped UUID v4 (sprint-01 R-7) — NOT the hardware id.
  final String deviceId;
  final String deviceName;

  /// `ios` or `android` — matches the backend `^(ios|android)$` contract.
  final String os;
  final String osVersion;

  /// e.g. `1.0.3+12`.
  final String appVersion;
}

/// Resolves and caches the [DeviceDescriptor] for the running install.
///
/// The device id is generated once and persisted in secure storage so it is
/// stable across launches and app updates (see [SecureStorage.readDeviceId]).
class DeviceInfoService {
  DeviceInfoService(this._storage);

  final SecureStorage _storage;
  DeviceDescriptor? _cached;

  Future<DeviceDescriptor> resolve() async {
    final cached = _cached;
    if (cached != null) return cached;

    final descriptor = DeviceDescriptor(
      deviceId: await _resolveDeviceId(),
      deviceName: await _resolveDeviceName(),
      os: _resolveOs(),
      osVersion: await _resolveOsVersion(),
      appVersion: await _resolveAppVersion(),
    );
    _cached = descriptor;
    return descriptor;
  }

  /// Convenience for interceptors that only need the id.
  Future<String> deviceId() async => (await resolve()).deviceId;

  Future<String> _resolveDeviceId() async {
    final existing = await _storage.readDeviceId();
    if (existing != null && existing.isNotEmpty) return existing;

    final generated = generateUuidV4();
    await _storage.writeDeviceId(generated);
    return generated;
  }

  String _resolveOs() {
    if (Platform.isIOS) return 'ios';
    if (Platform.isAndroid) return 'android';
    // Desktop/web are not target platforms for the PG/Leader app; default to
    // android so dev runs still satisfy the backend contract.
    return 'android';
  }

  Future<String> _resolveDeviceName() async {
    final info = DeviceInfoPlugin();
    try {
      if (Platform.isAndroid) {
        final a = await info.androidInfo;
        return '${a.manufacturer} ${a.model}'.trim();
      }
      if (Platform.isIOS) {
        final i = await info.iosInfo;
        return i.name;
      }
    } catch (_) {
      // Fall through to the generic name below.
    }
    return 'Unknown device';
  }

  Future<String> _resolveOsVersion() async {
    final info = DeviceInfoPlugin();
    try {
      if (Platform.isAndroid) {
        final a = await info.androidInfo;
        return 'Android ${a.version.release}';
      }
      if (Platform.isIOS) {
        final i = await info.iosInfo;
        return '${i.systemName} ${i.systemVersion}';
      }
    } catch (_) {
      // Fall through.
    }
    return Platform.operatingSystemVersion;
  }

  Future<String> _resolveAppVersion() async {
    final pkg = await PackageInfo.fromPlatform();
    return '${pkg.version}+${pkg.buildNumber}';
  }
}

final deviceInfoServiceProvider = Provider<DeviceInfoService>((ref) {
  return DeviceInfoService(ref.watch(secureStorageProvider));
});
