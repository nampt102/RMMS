import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../domain/attendance.dart';
import 'attendance_api.dart';

final attendanceApiProvider = Provider<AttendanceApi>((ref) {
  return AttendanceApi(ref.watch(dioProvider));
});

final attendanceRepositoryProvider = Provider<AttendanceRepository>((ref) {
  return AttendanceRepository(ref.watch(attendanceApiProvider));
});

/// Self-service attendance for the mobile app (M05). Dio failures are normalized
/// to [ApiException] so callers branch on a stable error code (BR-201..BR-210).
class AttendanceRepository {
  AttendanceRepository(this._api);

  final AttendanceApi _api;

  Future<List<TodayShift>> today() => _guard(_api.today);

  Future<CheckInInfo> checkInInfo() => _guard(_api.checkInInfo);

  Future<AttendanceRecord> checkIn(String storeId, AttendanceSubmission s) =>
      _guard(() => _api.checkIn(storeId, s));

  Future<AttendanceRecord> checkOut(String attendanceId, AttendanceSubmission s) =>
      _guard(() => _api.checkOut(attendanceId, s));

  Future<List<AttendanceRecord>> history({String? from, String? to}) =>
      _guard(() => _api.history(from: from, to: to));

  Future<T> _guard<T>(Future<T> Function() action) async {
    try {
      return await action();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Today's expected shifts + status. Invalidate after a check-in/out to refresh.
final todayShiftsProvider =
    FutureProvider.autoDispose<List<TodayShift>>((ref) {
  return ref.watch(attendanceRepositoryProvider).today();
});

/// The caller's attendance history for the last 30 days.
final attendanceHistoryProvider =
    FutureProvider.autoDispose<List<AttendanceRecord>>((ref) {
  final now = DateTime.now();
  final from = DateTime(now.year, now.month, now.day).subtract(const Duration(days: 30));
  String ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  return ref
      .watch(attendanceRepositoryProvider)
      .history(from: ymd(from), to: ymd(now));
});
