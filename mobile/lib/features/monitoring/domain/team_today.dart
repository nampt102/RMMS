import 'package:freezed_annotation/freezed_annotation.dart';

part 'team_today.freezed.dart';
part 'team_today.g.dart';

/// One team member's today status — mirrors `TeamMemberStatusDto` (M12).
@freezed
sealed class TeamMember with _$TeamMember {
  const factory TeamMember({
    required String userId,
    required String fullName,
    required String role,
    required String status,
    DateTime? checkInAt,
    String? storeName,
  }) = _TeamMember;

  factory TeamMember.fromJson(Map<String, dynamic> json) => _$TeamMemberFromJson(json);
}

/// Today's team snapshot — mirrors `TeamTodayDto` (M12).
@freezed
sealed class TeamToday with _$TeamToday {
  const factory TeamToday({
    @Default(<TeamMember>[]) List<TeamMember> members,
    @Default(<String, int>{}) Map<String, int> summary,
    required DateTime asOf,
  }) = _TeamToday;

  factory TeamToday.fromJson(Map<String, dynamic> json) => _$TeamTodayFromJson(json);
}
