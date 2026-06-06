import 'package:dio/dio.dart';

import '../../../core/utils/app_uuid.dart';
import '../domain/work_schedule.dart';

/// Input for one shift when registering a schedule.
class ShiftInput {
  const ShiftInput({
    required this.storeId,
    required this.startTime,
    required this.endTime,
  });

  final String storeId;
  final String startTime; // "HH:mm"
  final String endTime; // "HH:mm"

  Map<String, dynamic> toJson() => {
        'storeId': storeId,
        'startTime': startTime,
        'endTime': endTime,
      };
}

/// Input for one day (a date + its shifts).
class ScheduleDayInput {
  const ScheduleDayInput({required this.date, required this.shifts});

  final String date; // "YYYY-MM-DD"
  final List<ShiftInput> shifts;

  Map<String, dynamic> toJson() => {
        'date': date,
        'shifts': shifts.map((s) => s.toJson()).toList(),
      };
}

/// Hand-written Dio client for the M07 schedule self-service surface.
class ScheduleApi {
  ScheduleApi(this._dio);

  final Dio _dio;

  /// The caller's schedules in a date range — `GET /schedule/me?from&to`.
  Future<List<WorkSchedule>> mySchedule(String from, String to) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/schedule/me',
      queryParameters: {'from': from, 'to': to},
    );
    final list = _dataList(response);
    return list
        .whereType<Map<String, dynamic>>()
        .map(WorkSchedule.fromJson)
        .toList(growable: false);
  }

  /// Register one or more days — `POST /schedule/me`. Returns created ids.
  Future<List<String>> create(List<ScheduleDayInput> days) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/schedule/me',
      data: {'days': days.map((d) => d.toJson()).toList()},
      options: _idempotent(),
    );
    final data = response.data?['data'];
    if (data is Map<String, dynamic> && data['ids'] is List) {
      return (data['ids'] as List).whereType<String>().toList(growable: false);
    }
    return const [];
  }

  /// Submit a draft for approval — `POST /schedule/{id}/submit`.
  Future<void> submit(String id) async {
    await _dio.post<void>('/schedule/$id/submit', options: _idempotent());
  }

  /// Withdraw a pending / edit-pending schedule — `DELETE /schedule/{id}`.
  Future<void> withdraw(String id) async {
    await _dio.delete<void>('/schedule/$id');
  }

  List<dynamic> _dataList(Response<Map<String, dynamic>> response) {
    final body = response.data;
    if (body == null || body['data'] is! List) {
      throw DioException(
        requestOptions: response.requestOptions,
        response: response,
        message: 'Malformed success envelope (missing "data" array).',
      );
    }
    return body['data'] as List<dynamic>;
  }

  Options _idempotent() =>
      Options(headers: {'X-Idempotency-Key': generateUuidV4()});
}
