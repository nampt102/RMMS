import 'package:freezed_annotation/freezed_annotation.dart';

part 'ot_request.freezed.dart';
part 'ot_request.g.dart';

/// An OT request — mirrors `OtRequestDto` (M08). Status: pending/approved/rejected.
@freezed
sealed class OtRequest with _$OtRequest {
  const factory OtRequest({
    required String id,
    required String otDate, // "YYYY-MM-DD"
    required String startTime, // "HH:mm:ss" or "HH:mm"
    required String endTime,
    String? reason,
    required String status,
    String? approvalId,
    DateTime? createdAt,
  }) = _OtRequest;

  factory OtRequest.fromJson(Map<String, dynamic> json) => _$OtRequestFromJson(json);
}
