import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

import '../config/app_config.dart';
import '../storage/secure_storage.dart';
import 'api_interceptors.dart';

/// Provides a configured Dio instance. Single shared client per app session.
final dioProvider = Provider<Dio>((ref) {
  final storage = ref.watch(secureStorageProvider);

  final dio = Dio(
    BaseOptions(
      baseUrl: '${AppConfig.apiBaseUrl}/api/v1',
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 30),
      sendTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json', 'Accept-Language': 'vi'},
    ),
  );

  dio.interceptors.addAll([
    AuthInterceptor(storage),
    DeviceHeadersInterceptor(),
    if (const bool.fromEnvironment('dart.vm.product') == false)
      PrettyDioLogger(
        requestHeader: false,
        requestBody: false,
        responseBody: false,
        compact: true,
      ),
  ]);

  return dio;
});
