import 'package:freezed_annotation/freezed_annotation.dart';

part 'assigned_leader.freezed.dart';
part 'assigned_leader.g.dart';

/// The current PG's active managing Leader — mirrors `MyLeaderDto` from
/// `GET /api/v1/users/me/leader` (M03). Null when the PG has no Leader assigned.
@freezed
sealed class AssignedLeader with _$AssignedLeader {
  const factory AssignedLeader({
    required String leaderUserId,
    required String fullName,
    required String email,
    String? phone,
  }) = _AssignedLeader;

  factory AssignedLeader.fromJson(Map<String, dynamic> json) =>
      _$AssignedLeaderFromJson(json);
}
