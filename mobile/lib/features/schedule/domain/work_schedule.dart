import 'package:freezed_annotation/freezed_annotation.dart';

part 'work_schedule.freezed.dart';
part 'work_schedule.g.dart';

/// One shift within a work schedule — mirrors `WorkScheduleShiftDto` (M07).
@freezed
sealed class WorkScheduleShift with _$WorkScheduleShift {
  const factory WorkScheduleShift({
    required String id,
    required String storeId,
    required String storeCode,
    required String storeName,
    required String startTime, // "HH:mm"
    required String endTime, // "HH:mm"
    @Default(0) int ordering,
  }) = _WorkScheduleShift;

  factory WorkScheduleShift.fromJson(Map<String, dynamic> json) =>
      _$WorkScheduleShiftFromJson(json);
}

/// A PG/Leader's schedule for one day — mirrors `WorkScheduleDto`
/// from `GET /api/v1/schedule/me` (M07). Status is one of
/// pending / approved / rejected / edit_pending / superseded.
@freezed
sealed class WorkSchedule with _$WorkSchedule {
  const WorkSchedule._();

  const factory WorkSchedule({
    required String id,
    required String userId,
    required String scheduleDate, // "YYYY-MM-DD"
    required String status,
    @Default(1) int version,
    String? previousVersionId,
    DateTime? submittedAt,
    DateTime? approvedAt,
    String? rejectReason,
    @Default(<WorkScheduleShift>[]) List<WorkScheduleShift> shifts,
  }) = _WorkSchedule;

  factory WorkSchedule.fromJson(Map<String, dynamic> json) =>
      _$WorkScheduleFromJson(json);

  /// A draft the owner can still edit/submit (pending, not yet sent for approval).
  bool get isDraft => status == 'pending' && submittedAt == null;

  /// Pending or edit-pending — can be withdrawn by the owner.
  bool get isWithdrawable => status == 'pending' || status == 'edit_pending';
}
