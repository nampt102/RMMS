import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/application/auth_state.dart';
import '../../features/auth/presentation/screens/device_pending_screen.dart';
import '../../features/auth/presentation/screens/forgot_password_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/register_screen.dart';
import '../../features/auth/presentation/screens/reset_password_screen.dart';
import '../../features/auth/presentation/screens/verify_email_screen.dart';
import '../../features/home/presentation/screens/home_screen.dart';
import '../../features/organization/presentation/screens/my_assignments_screen.dart';
import '../../features/schedule/presentation/screens/my_schedule_screen.dart';
import '../../features/schedule/presentation/screens/register_schedule_screen.dart';
import '../../l10n/generated/app_localizations.dart';

/// Named routes — keep all path strings here.
class AppRoutes {
  AppRoutes._();

  static const String splash = '/splash';
  static const String login = '/login';
  static const String register = '/register';
  static const String verifyEmail = '/verify-email';
  static const String forgotPassword = '/forgot-password';
  static const String resetPassword = '/reset-password';
  static const String devicePending = '/device-pending';
  static const String home = '/';
  static const String myAssignments = '/my-assignments';
  static const String schedule = '/schedule';
  static const String scheduleRegister = '/schedule/register';
  static const String checkIn = '/check-in';
  static const String checkOut = '/check-out';
}

/// Screens reachable while signed out.
const _publicRoutes = {
  AppRoutes.login,
  AppRoutes.register,
  AppRoutes.verifyEmail,
  AppRoutes.forgotPassword,
  AppRoutes.resetPassword,
};

final appRouterProvider = Provider<GoRouter>((ref) {
  // Bridge the Riverpod auth state into a Listenable so GoRouter re-evaluates
  // `redirect` on every transition without rebuilding the router itself.
  final refresh = ValueNotifier<AuthState>(const AuthState.unknown());
  ref.onDispose(refresh.dispose);
  ref.listen<AuthState>(
    authControllerProvider,
    (_, next) => refresh.value = next,
    fireImmediately: true,
  );

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: refresh,
    redirect: (context, state) {
      final auth = refresh.value;
      final loc = state.matchedLocation;

      return switch (auth) {
        AuthUnknown() => loc == AppRoutes.splash ? null : AppRoutes.splash,
        AuthUnauthenticated() =>
          _publicRoutes.contains(loc) ? null : AppRoutes.login,
        AuthDeviceNotAuthorized() =>
          loc == AppRoutes.devicePending ? null : AppRoutes.devicePending,
        AuthAuthenticated() => (_publicRoutes.contains(loc) ||
                loc == AppRoutes.splash ||
                loc == AppRoutes.devicePending)
            ? AppRoutes.home
            : null,
      };
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (context, state) => const _SplashScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.register,
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: AppRoutes.verifyEmail,
        builder: (context, state) => VerifyEmailScreen(
          initialToken: state.uri.queryParameters['token'],
          email: state.uri.queryParameters['email'],
        ),
      ),
      GoRoute(
        path: AppRoutes.forgotPassword,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: AppRoutes.resetPassword,
        builder: (context, state) => ResetPasswordScreen(
          initialToken: state.uri.queryParameters['token'],
        ),
      ),
      GoRoute(
        path: AppRoutes.devicePending,
        builder: (context, state) => const DevicePendingScreen(),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (context, state) => const HomeScreen(),
      ),
      GoRoute(
        path: AppRoutes.myAssignments,
        builder: (context, state) => const MyAssignmentsScreen(),
      ),
      GoRoute(
        path: AppRoutes.schedule,
        builder: (context, state) => const MyScheduleScreen(),
        routes: [
          GoRoute(
            path: 'register',
            builder: (context, state) => const RegisterScheduleScreen(),
          ),
        ],
      ),
    ],
  );
});

class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 16),
            Text(l.commonLoading),
          ],
        ),
      ),
    );
  }
}
