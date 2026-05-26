import 'package:freezed_annotation/freezed_annotation.dart';

part 'auth_user.freezed.dart';
part 'auth_user.g.dart';

/// Authenticated user — mirrors backend JWT payload (sub, email, role).
/// See knowledge-base/05-api-conventions.md.
@freezed
class AuthUser with _$AuthUser {
  const factory AuthUser({
    required String id,
    required String email,
    required String fullName,
    required UserRole role,
  }) = _AuthUser;

  factory AuthUser.fromJson(Map<String, dynamic> json) => _$AuthUserFromJson(json);
}

enum UserRole {
  @JsonValue('pg')
  pg,
  @JsonValue('leader')
  leader,
  @JsonValue('buh')
  buh,
  @JsonValue('admin')
  admin,
}
