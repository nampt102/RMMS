import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/home/presentation/screens/home_screen.dart';

/// Named routes — keep all path strings here.
class AppRoutes {
  AppRoutes._();

  static const String login = '/login';
  static const String home = '/';
  static const String checkIn = '/check-in';
  static const String checkOut = '/check-out';
}

final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: AppRoutes.login,
    routes: [
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.home,
        builder: (context, state) => const HomeScreen(),
      ),
    ],
    // TODO(M01): redirect based on auth state from authStateProvider.
  );
});
