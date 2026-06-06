import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/work_schedule.dart';
import 'schedule_api.dart';

final scheduleApiProvider = Provider<ScheduleApi>((ref) {
  return ScheduleApi(ref.watch(dioProvider));
});

final scheduleRepositoryProvider = Provider<ScheduleRepository>((ref) {
  return ScheduleRepository(ref.watch(scheduleApiProvider));
});

/// Self-service schedule operations for the mobile app (M07). All Dio failures
/// are normalized to [ApiException] so callers branch on a stable error code.
class ScheduleRepository {
  ScheduleRepository(this._api);

  final ScheduleApi _api;

  Future<List<WorkSchedule>> mySchedule(String from, String to) =>
      _guard(() => _api.mySchedule(from, to));

  Future<List<String>> create(List<ScheduleDayInput> days) =>
      _guard(() => _api.create(days));

  Future<void> submit(String id) => _guard(() => _api.submit(id));

  Future<void> withdraw(String id) => _guard(() => _api.withdraw(id));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

String _ymd(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// The caller's schedules for the next 30 days (today inclusive). Auto-refetches
/// when invalidated after create/submit/withdraw.
final myScheduleProvider = FutureProvider.autoDispose<List<WorkSchedule>>((ref) {
  final now = DateTime.now();
  final from = DateTime(now.year, now.month, now.day);
  final to = from.add(const Duration(days: 30));
  return ref.watch(scheduleRepositoryProvider).mySchedule(_ymd(from), _ymd(to));
});
