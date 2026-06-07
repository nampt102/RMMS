import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_router.dart';
import '../../../core/widgets/app_widgets.dart';
import '../../../l10n/generated/app_localizations.dart';

/// Root shell for the PG-facing IA (Redesign 2026).
///
/// Wraps the 4 indexed branches (Home, Schedule, Requests, Profile) and adds
/// the glass [AppBottomNav] with the raised centre "Chấm công" FAB. The FAB
/// does not switch branches — it pushes the Attendance screen onto the root
/// navigator (so the bottom bar disappears, matching the screenshots).
class MainShell extends StatelessWidget {
  const MainShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  // Bottom-nav indices (5) ↔ shell branch indices (4):
  //   nav 0 → branch 0 (Home)
  //   nav 1 → branch 1 (Schedule)
  //   nav 2 → push Attendance (no branch)
  //   nav 3 → branch 2 (Requests)
  //   nav 4 → branch 3 (Profile)
  static int _navToBranch(int nav) => switch (nav) {
        0 => 0,
        1 => 1,
        3 => 2,
        4 => 3,
        _ => -1,
      };

  static int _branchToNav(int branch) => switch (branch) {
        0 => 0,
        1 => 1,
        2 => 3,
        3 => 4,
        _ => 0,
      };

  void _onTap(BuildContext context, int navIndex) {
    if (navIndex == 2) {
      context.push(AppRoutes.attendance);
      return;
    }
    final target = _navToBranch(navIndex);
    if (target < 0) return;
    navigationShell.goBranch(
      target,
      initialLocation: target == navigationShell.currentIndex,
    );
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final navIdx = _branchToNav(navigationShell.currentIndex);

    return Scaffold(
      body: navigationShell,
      extendBody: true,
      bottomNavigationBar: AppBottomNav(
        currentIndex: navIdx,
        onTap: (i) => _onTap(context, i),
        items: [
          AppNavItem(icon: Icons.home_rounded, label: l.navHome),
          AppNavItem(icon: Icons.calendar_month_rounded, label: l.navSchedule),
          AppNavItem(
            icon: Icons.sentiment_satisfied_alt_rounded,
            label: l.navAttendance,
          ),
          AppNavItem(icon: Icons.description_rounded, label: l.navRequests),
          AppNavItem(icon: Icons.person_rounded, label: l.navProfile),
        ],
      ),
    );
  }
}
