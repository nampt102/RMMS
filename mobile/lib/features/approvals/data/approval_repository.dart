import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/approval.dart';
import 'approval_api.dart';

final approvalApiProvider = Provider<ApprovalApi>((ref) {
  return ApprovalApi(ref.watch(dioProvider));
});

final approvalRepositoryProvider = Provider<ApprovalRepository>((ref) {
  return ApprovalRepository(ref.watch(approvalApiProvider));
});

/// Self-service approval queue for the mobile app (M09). Dio failures are
/// normalised to [ApiException] so callers branch on a stable error code.
class ApprovalRepository {
  ApprovalRepository(this._api);

  final ApprovalApi _api;

  Future<List<Approval>> pending() => _guard(_api.pending);
  Future<void> approve(String id) => _guard(() => _api.approve(id));
  Future<void> reject(String id, String reason) => _guard(() => _api.reject(id, reason));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// The caller's pending approvals. Invalidate after an approve/reject.
final pendingApprovalsProvider =
    FutureProvider.autoDispose<List<Approval>>((ref) {
  return ref.watch(approvalRepositoryProvider).pending();
});
