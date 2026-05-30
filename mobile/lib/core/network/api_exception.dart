import 'package:dio/dio.dart';

/// Stable backend error codes (mirror `Rmms.Shared/Errors/ErrorCodes.cs`).
/// Use these constants for control flow — never compare against raw strings.
class ApiErrorCodes {
  ApiErrorCodes._();

  static const validationFailed = 'VALIDATION_FAILED';
  static const rateLimitExceeded = 'RATE_LIMIT_EXCEEDED';
  static const invalidCredentials = 'INVALID_CREDENTIALS';
  static const emailNotVerified = 'EMAIL_NOT_VERIFIED';
  static const emailAlreadyRegistered = 'EMAIL_ALREADY_REGISTERED';
  static const accountInactive = 'ACCOUNT_INACTIVE';
  static const accountLocked = 'ACCOUNT_LOCKED';
  static const tokenExpired = 'TOKEN_EXPIRED';
  static const tokenInvalid = 'TOKEN_INVALID';
  static const refreshTokenRevoked = 'REFRESH_TOKEN_REVOKED';
  static const refreshTokenReused = 'REFRESH_TOKEN_REUSED';
  static const deviceNotAuthorized = 'DEVICE_NOT_AUTHORIZED';
  static const passwordTooWeak = 'PASSWORD_TOO_WEAK';
  static const emailTokenExpired = 'EMAIL_TOKEN_EXPIRED';
  static const emailTokenUsed = 'EMAIL_TOKEN_USED';

  /// Client-side synthetic code for connectivity / timeout failures.
  static const network = 'NETWORK';

  /// Client-side synthetic fallback when no envelope could be parsed.
  static const unknown = 'UNKNOWN';
}

/// A single field-level validation detail from the error envelope.
class ApiErrorDetail {
  const ApiErrorDetail({required this.field, required this.code, required this.message});

  factory ApiErrorDetail.fromJson(Map<String, dynamic> json) => ApiErrorDetail(
    field: json['field'] as String? ?? '',
    code: json['code'] as String? ?? '',
    message: json['message'] as String? ?? '',
  );

  final String field;
  final String code;
  final String message;
}

/// Normalized API error decoded from the backend `{ error: {...} }` envelope
/// (see `05-api-conventions.md`). Carries the machine-readable [code] so the UI
/// can map it to a localized message and decide on navigation.
class ApiException implements Exception {
  const ApiException({
    required this.code,
    required this.message,
    this.statusCode,
    this.traceId,
    this.details = const [],
  });

  final String code;
  final String message;
  final int? statusCode;
  final String? traceId;
  final List<ApiErrorDetail> details;

  bool get isNetwork => code == ApiErrorCodes.network;

  /// Builds an [ApiException] from a Dio failure, parsing the error envelope
  /// when present and falling back to a synthetic network/unknown code.
  factory ApiException.fromDio(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const ApiException(
          code: ApiErrorCodes.network,
          message: 'Network error',
        );
      default:
        break;
    }

    final status = e.response?.statusCode;
    final data = e.response?.data;
    if (data is Map<String, dynamic> && data['error'] is Map<String, dynamic>) {
      final error = data['error'] as Map<String, dynamic>;
      final rawDetails = error['details'];
      return ApiException(
        code: error['code'] as String? ?? ApiErrorCodes.unknown,
        message: error['message'] as String? ?? 'Unexpected error',
        statusCode: status,
        traceId: error['traceId'] as String?,
        details: rawDetails is List
            ? rawDetails
                .whereType<Map<String, dynamic>>()
                .map(ApiErrorDetail.fromJson)
                .toList()
            : const [],
      );
    }

    return ApiException(
      code: ApiErrorCodes.unknown,
      message: e.message ?? 'Unexpected error',
      statusCode: status,
    );
  }

  @override
  String toString() => 'ApiException($code, status=$statusCode): $message';
}
