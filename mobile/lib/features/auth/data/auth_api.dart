import 'package:dio/dio.dart';

/// Manually-written client (no retrofit codegen for now) for simplicity.
/// Once the scaffold builds, M01 will switch to a retrofit-generated client.
class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  Future<LoginEnvelope> login(
      {required String email, required String password}) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/auth/login',
      data: {'email': email, 'password': password},
    );
    final body = response.data!;
    return LoginEnvelope.fromJson(body['data'] as Map<String, dynamic>);
  }
}

class LoginEnvelope {
  LoginEnvelope({
    required this.accessToken,
    required this.refreshToken,
  });

  factory LoginEnvelope.fromJson(Map<String, dynamic> json) => LoginEnvelope(
        accessToken: json['accessToken'] as String,
        refreshToken: json['refreshToken'] as String,
      );

  final String accessToken;
  final String refreshToken;
}
