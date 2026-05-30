import 'package:freezed_annotation/freezed_annotation.dart';

import '../domain/auth_user.dart';

part 'auth_state.freezed.dart';

/// Session state that drives the router guard.
///
/// - [AuthState.unknown] — initial; a silent session restore is in flight.
/// - [AuthState.unauthenticated] — no valid session; show login.
/// - [AuthState.authenticated] — signed in with a resolved profile.
/// - [AuthState.deviceNotAuthorized] — credentials were valid but this device
///   is pending Leader/Admin approval (BR-105); show the pending screen.
@freezed
sealed class AuthState with _$AuthState {
  const factory AuthState.unknown() = AuthUnknown;
  const factory AuthState.unauthenticated() = AuthUnauthenticated;
  const factory AuthState.authenticated(AuthUser user) = AuthAuthenticated;
  const factory AuthState.deviceNotAuthorized() = AuthDeviceNotAuthorized;
}
