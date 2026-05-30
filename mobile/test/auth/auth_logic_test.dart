import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rmms/core/network/api_exception.dart';
import 'package:rmms/core/utils/app_uuid.dart';
import 'package:rmms/features/auth/domain/auth_user.dart';

void main() {
  group('ApiException.fromDio', () {
    final req = RequestOptions(path: '/auth/login');

    test('parses the error envelope into code/message/traceId', () {
      final e = DioException(
        requestOptions: req,
        response: Response<Map<String, dynamic>>(
          requestOptions: req,
          statusCode: 401,
          data: {
            'error': {
              'code': 'INVALID_CREDENTIALS',
              'message': 'Wrong email or password.',
              'traceId': 'abc-123',
              'details': [
                {'field': 'email', 'code': 'X', 'message': 'bad'},
              ],
            },
          },
        ),
      );

      final ex = ApiException.fromDio(e);
      expect(ex.code, ApiErrorCodes.invalidCredentials);
      expect(ex.statusCode, 401);
      expect(ex.traceId, 'abc-123');
      expect(ex.details, hasLength(1));
      expect(ex.details.first.field, 'email');
    });

    test('maps timeouts/connection failures to the synthetic NETWORK code', () {
      final e = DioException(
        requestOptions: req,
        type: DioExceptionType.connectionTimeout,
      );
      final ex = ApiException.fromDio(e);
      expect(ex.code, ApiErrorCodes.network);
      expect(ex.isNetwork, isTrue);
    });

    test('falls back to UNKNOWN when there is no envelope', () {
      final e = DioException(
        requestOptions: req,
        response: Response<dynamic>(
          requestOptions: req,
          statusCode: 500,
          data: 'gateway error',
        ),
      );
      expect(ApiException.fromDio(e).code, ApiErrorCodes.unknown);
    });
  });

  group('UserRole.fromValue', () {
    test('maps known lowercase roles', () {
      expect(UserRole.fromValue('pg'), UserRole.pg);
      expect(UserRole.fromValue('leader'), UserRole.leader);
      expect(UserRole.fromValue('buh'), UserRole.buh);
      expect(UserRole.fromValue('admin'), UserRole.admin);
    });

    test('is case-insensitive and defaults unknown roles to pg', () {
      expect(UserRole.fromValue('ADMIN'), UserRole.admin);
      expect(UserRole.fromValue('superuser'), UserRole.pg);
    });
  });

  group('generateUuidV4', () {
    test('produces a well-formed v4 UUID', () {
      final id = generateUuidV4();
      final re = RegExp(
        r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
      );
      expect(re.hasMatch(id), isTrue, reason: id);
    });

    test('is effectively unique across calls', () {
      final ids = List.generate(1000, (_) => generateUuidV4()).toSet();
      expect(ids, hasLength(1000));
    });
  });
}
