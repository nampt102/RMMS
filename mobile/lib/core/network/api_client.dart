import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

import '../config/app_config.dart';
import '../device/device_info_service.dart';
import '../storage/secure_storage.dart';
import 'api_interceptors.dart';
import 'token_refresher.dart';

bool get _isDev => const bool.fromEnvironment('dart.vm.product') == false;

BaseOptions _baseOptions() => BaseOptions(
      baseUrl: '${AppConfig.apiBaseUrl}/api/v1',
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 30),
      sendTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json', 'Accept-Language': 'vi'},
    );

PrettyDioLogger _logger() => PrettyDioLogger(
      requestHeader: false,
      requestBody: false,
      responseBody: false,
      compact: true,
    );

/// A token-less Dio: carries device headers but NOT [AuthInterceptor]. Used for
/// refresh calls and for replaying requests after a refresh, so neither can
/// recurse back into the auth-refresh path.
final _bareDioProvider = Provider<Dio>((ref) {
  final deviceInfo = ref.watch(deviceInfoServiceProvider);
  final dio = Dio(_baseOptions());
  dio.interceptors.add(DeviceHeadersInterceptor(deviceInfo));
  if (_isDev) dio.interceptors.add(_logger());
  return dio;
});

final tokenRefresherProvider = Provider<TokenRefresher>((ref) {
  return TokenRefresher(
    ref.watch(secureStorageProvider),
    ref.watch(_bareDioProvider),
  );
});

/// The primary authenticated Dio instance — single shared client per session.
final dioProvider = Provider<Dio>((ref) {
  final storage = ref.watch(secureStorageProvider);
  final deviceInfo = ref.watch(deviceInfoServiceProvider);
  final refresher = ref.watch(tokenRefresherProvider);
  final bareDio = ref.watch(_bareDioProvider);

  final dio = Dio(_baseOptions());
  dio.interceptors.addAll([
    AuthInterceptor(storage: storage, refresher: refresher, retryDio: bareDio),
    DeviceHeadersInterceptor(deviceInfo),
    if (_isDev) _logger(),
  ]);
  return dio;
});
