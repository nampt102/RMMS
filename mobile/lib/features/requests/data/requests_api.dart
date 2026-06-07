import 'package:dio/dio.dart';

import '../../../core/utils/app_uuid.dart';
import '../domain/leave_request.dart';
import '../domain/ot_request.dart';

/// Hand-written Dio client for the M08 leave / OT self-service surface.
class RequestsApi {
  RequestsApi(this._dio);

  final Dio _dio;

  Future<LeaveRequest> createLeave({
    required String startDate,
    required String endDate,
    String? startTime,
    String? endTime,
    required String reason,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/leave-requests',
      data: {
        'startDate': startDate,
        'endDate': endDate,
        if (startTime != null) 'startTime': startTime,
        if (endTime != null) 'endTime': endTime,
        'reason': reason,
      },
      options: _idempotent(),
    );
    return LeaveRequest.fromJson(_data(res));
  }

  Future<LeaveRequest> createEmergency(String reason) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/leave-requests/emergency',
      data: {'reason': reason},
      options: _idempotent(),
    );
    return LeaveRequest.fromJson(_data(res));
  }

  Future<OtRequest> createOt({
    required String otDate,
    required String startTime,
    required String endTime,
    required String reason,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>(
      '/ot-requests',
      data: {'otDate': otDate, 'startTime': startTime, 'endTime': endTime, 'reason': reason},
      options: _idempotent(),
    );
    return OtRequest.fromJson(_data(res));
  }

  Future<List<LeaveRequest>> myLeave() async {
    final res = await _dio.get<Map<String, dynamic>>('/leave-requests/me');
    return _list(res).whereType<Map<String, dynamic>>().map(LeaveRequest.fromJson).toList(growable: false);
  }

  Future<List<OtRequest>> myOt() async {
    final res = await _dio.get<Map<String, dynamic>>('/ot-requests/me');
    return _list(res).whereType<Map<String, dynamic>>().map(OtRequest.fromJson).toList(growable: false);
  }

  Future<void> withdrawLeave(String id) async {
    await _dio.delete<void>('/leave-requests/$id');
  }

  Map<String, dynamic> _data(Response<Map<String, dynamic>> res) {
    final data = res.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
    }
    return data;
  }

  List<dynamic> _list(Response<Map<String, dynamic>> res) {
    final body = res.data;
    if (body == null || body['data'] is! List) {
      throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
    }
    return body['data'] as List<dynamic>;
  }

  Options _idempotent() => Options(headers: {'X-Idempotency-Key': generateUuidV4()});
}
