import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/team_today.dart';

final monitoringRepositoryProvider = Provider<MonitoringRepository>((ref) {
  return MonitoringRepository(ref.watch(dioProvider));
});

/// Team monitoring (M12) for the mobile app — Leader's PG online list.
class MonitoringRepository {
  MonitoringRepository(this._dio);

  final Dio _dio;

  Future<TeamToday> today() async {
    try {
      final res = await _dio.get<Map<String, dynamic>>('/team-monitoring/today');
      final data = res.data?['data'];
      if (data is! Map<String, dynamic>) {
        throw DioException(requestOptions: res.requestOptions, response: res, message: 'Malformed envelope.');
      }
      return TeamToday.fromJson(data);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

final teamTodayProvider = FutureProvider.autoDispose<TeamToday>((ref) {
  return ref.watch(monitoringRepositoryProvider).today();
});
