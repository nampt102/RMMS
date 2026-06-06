import 'package:freezed_annotation/freezed_annotation.dart';

part 'face_status.freezed.dart';
part 'face_status.g.dart';

/// The caller's face-enrollment status — mirrors `FaceStatusDto` (M06, ADR-011).
@freezed
sealed class FaceStatus with _$FaceStatus {
  const factory FaceStatus({
    @Default(false) bool enrolled,
    DateTime? enrolledAt,
  }) = _FaceStatus;

  factory FaceStatus.fromJson(Map<String, dynamic> json) =>
      _$FaceStatusFromJson(json);
}
