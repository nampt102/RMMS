import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/leave_request.dart';
import '../domain/ot_request.dart';
import 'requests_api.dart';

final requestsApiProvider = Provider<RequestsApi>((ref) => RequestsApi(ref.watch(dioProvider)));

final requestsRepositoryProvider = Provider<RequestsRepository>((ref) {
  return RequestsRepository(ref.watch(requestsApiProvider));
});

/// Self-service leave/OT for the mobile app (M08). Dio failures → [ApiException].
class RequestsRepository {
  RequestsRepository(this._api);

  final RequestsApi _api;

  Future<LeaveRequest> createLeave({
    required String startDate,
    required String endDate,
    String? startTime,
    String? endTime,
    required String reason,
  }) =>
      _guard(() => _api.createLeave(
            startDate: startDate, endDate: endDate, startTime: startTime, endTime: endTime, reason: reason));

  Future<LeaveRequest> createEmergency(String reason) => _guard(() => _api.createEmergency(reason));

  Future<OtRequest> createOt({
    required String otDate,
    required String startTime,
    required String endTime,
    required String reason,
  }) =>
      _guard(() => _api.createOt(otDate: otDate, startTime: startTime, endTime: endTime, reason: reason));

  Future<List<LeaveRequest>> myLeave() => _guard(_api.myLeave);
  Future<List<OtRequest>> myOt() => _guard(_api.myOt);
  Future<void> withdrawLeave(String id) => _guard(() => _api.withdrawLeave(id));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

final myLeaveProvider = FutureProvider.autoDispose<List<LeaveRequest>>((ref) {
  return ref.watch(requestsRepositoryProvider).myLeave();
});

final myOtProvider = FutureProvider.autoDispose<List<OtRequest>>((ref) {
  return ref.watch(requestsRepositoryProvider).myOt();
});
