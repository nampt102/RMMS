import 'package:dio/dio.dart';

import '../../../core/device/device_info_service.dart';
import '../../../core/utils/app_uuid.dart';
import 'dtos/auth_dtos.dart';

/// Hand-written Dio client for the M01 auth surface. Kept hand-written (rather
/// than retrofit-generated) so the envelope unwrapping (`data` / `error`) stays
/// explicit. Failures surface as raw [DioException]; the repository converts
/// them to `ApiException`.
class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  Future<RegisterResponseDto> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
    required String preferredLanguage,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/register',
      data: {
        'email': email,
        'password': password,
        'fullName': fullName,
        if (phone != null && phone.isNotEmpty) 'phone': phone,
        'preferredLanguage': preferredLanguage,
      },
      options: _idempotent(),
    );
    return RegisterResponseDto.fromJson(_data(response));
  }

  Future<VerifyEmailResponseDto> verifyEmail(String token) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/verify-email',
      data: {'token': token},
      options: _idempotent(),
    );
    return VerifyEmailResponseDto.fromJson(_data(response));
  }

  Future<LoginResponseDto> login({
    required String email,
    required String password,
    required DeviceDescriptor device,
    String? fcmToken,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: {
        'email': email,
        'password': password,
        'device': {
          'deviceId': device.deviceId,
          'deviceName': device.deviceName,
          'os': device.os,
          'osVersion': device.osVersion,
          'appVersion': device.appVersion,
          if (fcmToken != null) 'fcmToken': fcmToken,
        },
      },
    );
    return LoginResponseDto.fromJson(_data(response));
  }

  Future<void> logout(String refreshToken) async {
    await _dio.post<void>('/auth/logout', data: {'refreshToken': refreshToken});
  }

  /// Always succeeds server-side (204) regardless of whether the email exists
  /// — the UI must show the same neutral confirmation either way.
  Future<void> forgotPassword(String email) async {
    await _dio.post<void>(
      '/auth/forgot-password',
      data: {'email': email},
      options: _idempotent(),
    );
  }

  Future<void> resetPassword({
    required String token,
    required String newPassword,
  }) async {
    await _dio.post<void>(
      '/auth/reset-password',
      data: {'token': token, 'newPassword': newPassword},
      options: _idempotent(),
    );
  }

  Future<MeDto> me() async {
    final response = await _dio.get<Map<String, dynamic>>('/auth/me');
    return MeDto.fromJson(_data(response));
  }

  /// Unwraps the `{ "data": {...} }` success envelope.
  Map<String, dynamic> _data(Response<Map<String, dynamic>> response) {
    final body = response.data;
    if (body == null || body['data'] is! Map<String, dynamic>) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data").',
      );
    }
    return body['data'] as Map<String, dynamic>;
  }

  /// Adds a per-request `X-Idempotency-Key` recommended for mutation endpoints
  /// (`05-api-conventions.md`).
  Options _idempotent() =>
      Options(headers: {'X-Idempotency-Key': generateUuidV4()});
}
