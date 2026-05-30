import 'package:freezed_annotation/freezed_annotation.dart';

part 'auth_user.freezed.dart';
part 'auth_user.g.dart';

/// Authenticated user — mirrors the JWT payload + `/auth/me` profile.
/// See knowledge-base/05-api-conventions.md.
@freezed
sealed class AuthUser with _$AuthUser {
  const factory AuthUser({
    required String id,
    required String email,
    required String fullName,
    required UserRole role,
    String? phone,
    @Default('active') String status,
    @Default('vi') String preferredLanguage,
  }) = _AuthUser;

  factory AuthUser.fromJson(Map<String, dynamic> json) =>
      _$AuthUserFromJson(json);
}

enum UserRole {
  @JsonValue('pg')
  pg,
  @JsonValue('leader')
  leader,
  @JsonValue('buh')
  buh,
  @JsonValue('admin')
  admin;

  /// Parses the backend's lowercase role string, defaulting to [UserRole.pg]
  /// for forward-compatibility with roles this app build doesn't yet know.
  static UserRole fromValue(String value) {
    return UserRole.values.firstWhere(
      (r) => r.name == value.toLowerCase(),
      orElse: () => UserRole.pg,
    );
  }
}
