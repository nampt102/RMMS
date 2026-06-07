import 'package:freezed_annotation/freezed_annotation.dart';

part 'leave_request.freezed.dart';
part 'leave_request.g.dart';

/// A leave request — mirrors `LeaveRequestDto` (M08). Status: pending/approved/rejected.
@freezed
sealed class LeaveRequest with _$LeaveRequest {
  const LeaveRequest._();

  const factory LeaveRequest({
    required String id,
    required String leaveType, // regular | emergency
    required String startDate, // "YYYY-MM-DD"
    required String endDate,
    String? reason,
    required String status,
    String? approvalId,
    DateTime? createdAt,
  }) = _LeaveRequest;

  factory LeaveRequest.fromJson(Map<String, dynamic> json) => _$LeaveRequestFromJson(json);

  bool get isPending => status == 'pending';
}
