import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/notifications/fcm_coordinator.dart';
import '../data/auth_repository.dart';
import 'auth_state.dart';

final authControllerProvider =
    NotifierProvider<AuthController, AuthState>(AuthController.new);

/// Owns the session lifecycle. Login/logout mutate [AuthState]; the router
/// guard ([appRouterProvider]) watches it to decide which screen to show.
class AuthController extends Notifier<AuthState> {
  AuthRepository get _repo => ref.read(authRepositoryProvider);

  @override
  AuthState build() {
    // Kick off a silent session restore; stay in `unknown` until it resolves.
    _bootstrap();
    return const AuthState.unknown();
  }

  Future<void> _bootstrap() async {
    if (!await _repo.hasSession()) {
      state = const AuthState.unauthenticated();
      return;
    }
    try {
      state = AuthState.authenticated(await _repo.me());
      await _syncFcmToken();
    } on ApiException {
      // Tokens are stale/invalid and refresh failed — force a fresh login.
      await _repo.clearSession();
      state = const AuthState.unauthenticated();
    }
  }

  /// Signs in. On `DEVICE_NOT_AUTHORIZED` transitions to the pending state
  /// (the router shows the approval-pending screen) instead of throwing.
  /// Any other failure is rethrown as [ApiException] for the form to display.
  Future<void> login({
    required String email,
    required String password,
  }) async {
    try {
      final user = await _repo.login(email: email, password: password);
      state = AuthState.authenticated(user);
      await _syncFcmToken();
    } on ApiException catch (e) {
      if (e.code == ApiErrorCodes.deviceNotAuthorized) {
        state = const AuthState.deviceNotAuthorized();
        return;
      }
      rethrow;
    }
  }

  Future<void> logout() async {
    await _repo.logout();
    state = const AuthState.unauthenticated();
  }

  Future<void> _syncFcmToken() async {
    try {
      await ref.read(fcmCoordinatorProvider).syncTokenWithServer();
    } catch (_) {
      // Best-effort — token refresh / next app open will retry.
    }
  }

  /// Returns to the login screen from the device-pending screen.
  Future<void> cancelPendingDevice() async {
    await _repo.clearSession();
    state = const AuthState.unauthenticated();
  }
}
