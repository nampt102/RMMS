import 'package:dio/dio.dart';

import '../../../core/utils/app_uuid.dart';
import '../domain/approval.dart';

/// Hand-written Dio client for the M09 approval queue (Leader inline approve/reject).
class ApprovalApi {
  ApprovalApi(this._dio);

  final Dio _dio;

  /// Pending approvals routed to the caller — `GET /approvals/pending`.
  Future<List<Approval>> pending() async {
    final response = await _dio.get<Map<String, dynamic>>('/approvals/pending');
    final list = response.data?['data'];
    if (list is! List) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" array).',
      );
    }
    return list
        .whereType<Map<String, dynamic>>()
        .map(Approval.fromJson)
        .toList(growable: false);
  }

  /// Approve — `POST /approvals/{id}/approve`.
  Future<void> approve(String id) async {
    await _dio.post<void>('/approvals/$id/approve', options: _idempotent());
  }

  /// Reject with a reason — `POST /approvals/{id}/reject`.
  Future<void> reject(String id, String reason) async {
    await _dio.post<void>(
      '/approvals/$id/reject',
      data: {'reason': reason},
      options: _idempotent(),
    );
  }

  Options _idempotent() => Options(headers: {'X-Idempotency-Key': generateUuidV4()});
}
