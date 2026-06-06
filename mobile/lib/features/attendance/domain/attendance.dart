import 'package:freezed_annotation/freezed_annotation.dart';

part 'attendance.freezed.dart';
part 'attendance.g.dart';

/// A full attendance record — mirrors `AttendanceDto` (M05). Only the fields the
/// mobile app reads are modelled; the rest are ignored by json_serializable.
@freezed
sealed class AttendanceRecord with _$AttendanceRecord {
  const AttendanceRecord._();

  const factory AttendanceRecord({
    required String id,
    required String storeId,
    required String storeCode,
    required String storeName,
    required String status,
    @Default(false) bool isLate,
    required DateTime checkInAt,
    @Default(0) double checkInDistanceMeters,
    String? checkInNote,
    DateTime? checkOutAt,
    String? checkOutNote,
  }) = _AttendanceRecord;

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) =>
      _$AttendanceRecordFromJson(json);

  /// Open = checked in, awaiting check-out (server is source of truth).
  bool get isOpen =>
      checkOutAt == null &&
      const {
        'valid',
        'late',
        'gps_violation_pending_review',
        'face_fail_pending_review',
        'admin_approved',
      }.contains(status);
}

/// One shift the caller is expected to work today + its current attendance.
@freezed
sealed class TodayShift with _$TodayShift {
  const TodayShift._();

  const factory TodayShift({
    required String workScheduleShiftId,
    required String storeId,
    required String storeCode,
    required String storeName,
    @Default(0) double storeLatitude,
    @Default(0) double storeLongitude,
    required String startTime, // "HH:mm"
    required String endTime, // "HH:mm"
    String? attendanceId,
    String? attendanceStatus,
    DateTime? checkInAt,
    DateTime? checkOutAt,
  }) = _TodayShift;

  factory TodayShift.fromJson(Map<String, dynamic> json) =>
      _$TodayShiftFromJson(json);

  bool get isCheckedIn => attendanceId != null && checkOutAt == null;
  bool get isDone => checkOutAt != null;
}

/// A store the caller is assigned to (check-in store picker).
@freezed
sealed class AssignedStore with _$AssignedStore {
  const factory AssignedStore({
    required String storeId,
    required String code,
    required String name,
    String? address,
    @Default(0) double latitude,
    @Default(0) double longitude,
  }) = _AssignedStore;

  factory AssignedStore.fromJson(Map<String, dynamic> json) =>
      _$AssignedStoreFromJson(json);
}

/// Check-in screen bootstrap — mirrors `CheckInInfoDto`.
@freezed
sealed class CheckInInfo with _$CheckInInfo {
  const factory CheckInInfo({
    @Default(300) int geofenceRadiusMeters,
    @Default(60) int earlyCheckInMinutes,
    @Default(5) int lateThresholdMinutes,
    @Default(<AssignedStore>[]) List<AssignedStore> stores,
    @Default(<TodayShift>[]) List<TodayShift> todayShifts,
  }) = _CheckInInfo;

  factory CheckInInfo.fromJson(Map<String, dynamic> json) =>
      _$CheckInInfoFromJson(json);
}
