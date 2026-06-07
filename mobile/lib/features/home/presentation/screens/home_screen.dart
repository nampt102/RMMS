import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/brand_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../auth/application/auth_controller.dart';
import '../../../auth/application/auth_state.dart';
import '../../../auth/domain/auth_user.dart';
import '../../../face/data/face_repository.dart';

/// Home dashboard. Anchors the brand with a gradient hero, surfaces the primary
/// "check-in" action, nudges face enrollment when missing (M06), and offers a
/// quick-access grid to the other feature areas.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final auth = ref.watch(authControllerProvider);
    final user = auth is AuthAuthenticated ? auth.user : null;
    final faceStatus = ref.watch(faceStatusProvider);
    final scheme = context.scheme;

    return Scaffold(
      body: ListView(
        padding: EdgeInsets.zero,
        children: [
          _Hero(
            name: user?.fullName,
            role: user?.role.name,
            onLogout: () => ref.read(authControllerProvider.notifier).logout(),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 20, 16, 28),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Primary action: check-in -------------------------------
                SoftCard(
                  onTap: () => context.push(AppRoutes.attendance),
                  padding: const EdgeInsets.all(18),
                  child: Row(
                    children: [
                      const IconBadge(Icons.how_to_reg_outlined,
                          tone: BrandTone.brand, size: 52),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(l.homeViewAttendance,
                                style: const TextStyle(
                                    fontSize: 17, fontWeight: FontWeight.w700)),
                            const SizedBox(height: 3),
                            Text(l.homeCheckInCardSub,
                                style: TextStyle(
                                    fontSize: 13,
                                    color: scheme.onSurfaceVariant)),
                          ],
                        ),
                      ),
                      Icon(Icons.arrow_forward_ios,
                          size: 16, color: scheme.onSurfaceVariant),
                    ],
                  ),
                ),

                // Face enrollment nudge -----------------------------------
                faceStatus.maybeWhen(
                  data: (s) => s.enrolled
                      ? const SizedBox.shrink()
                      : Padding(
                          padding: const EdgeInsets.only(top: 12),
                          child: _FaceEnrollBanner(
                            onEnroll: () async {
                              await context.push(AppRoutes.faceEnroll);
                              ref.invalidate(faceStatusProvider);
                            },
                          ),
                        ),
                  orElse: () => const SizedBox.shrink(),
                ),

                const SizedBox(height: 28),
                SectionLabel(l.homeSectionQuick),
                const SizedBox(height: 12),

                // Quick-access grid ---------------------------------------
                GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 12,
                  crossAxisSpacing: 12,
                  childAspectRatio: 1.12,
                  children: [
                    FeatureTile(
                      icon: Icons.assignment_outlined,
                      title: l.homeViewAssignments,
                      subtitle: l.homeFeatureAssignmentsSub,
                      tone: BrandTone.brand,
                      onTap: () => context.push(AppRoutes.myAssignments),
                    ),
                    FeatureTile(
                      icon: Icons.calendar_month_outlined,
                      title: l.homeViewSchedule,
                      subtitle: l.homeFeatureScheduleSub,
                      tone: BrandTone.info,
                      onTap: () => context.push(AppRoutes.schedule),
                    ),
                    FeatureTile(
                      icon: Icons.history,
                      title: l.attendanceHistory,
                      subtitle: l.homeFeatureAttendanceSub,
                      tone: BrandTone.success,
                      onTap: () => context.push(AppRoutes.attendanceHistory),
                    ),
                    FeatureTile(
                      icon: Icons.face_retouching_natural,
                      title: l.homeFeatureFace,
                      subtitle: l.homeFeatureFaceSub,
                      tone: BrandTone.warning,
                      onTap: () async {
                        await context.push(AppRoutes.faceEnroll);
                        ref.invalidate(faceStatusProvider);
                      },
                    ),
                    FeatureTile(
                      icon: Icons.event_note_outlined,
                      title: l.homeFeatureRequests,
                      subtitle: l.homeFeatureRequestsSub,
                      tone: BrandTone.info,
                      onTap: () => context.push(AppRoutes.requests),
                    ),
                    // Approval queue is a Leader responsibility (M09, AC-17).
                    if (user?.role == UserRole.leader)
                      FeatureTile(
                        icon: Icons.fact_check_outlined,
                        title: l.homeFeatureApprovals,
                        subtitle: l.homeFeatureApprovalsSub,
                        tone: BrandTone.brand,
                        onTap: () => context.push(AppRoutes.approvals),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({required this.name, required this.role, required this.onLogout});

  final String? name;
  final String? role;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final initial =
        (name != null && name!.trim().isNotEmpty) ? name!.trim()[0].toUpperCase() : '?';

    return GradientHero(
      padding: EdgeInsets.fromLTRB(
          20, MediaQuery.of(context).padding.top + 16, 12, 26),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 26,
                backgroundColor: Colors.white.withValues(alpha: 0.2),
                child: Text(initial,
                    style: const TextStyle(
                        color: AppPalette.white,
                        fontSize: 20,
                        fontWeight: FontWeight.w700)),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name == null ? l.appName : l.homeWelcome(name!),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: AppPalette.white,
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -0.3,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      l.homeGreetingSub,
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.85),
                        fontSize: 13,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                tooltip: l.homeLogout,
                onPressed: onLogout,
                icon: const Icon(Icons.logout, color: AppPalette.white),
              ),
            ],
          ),
          if (role != null) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.18),
                borderRadius: BorderRadius.circular(999),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.badge_outlined,
                      size: 15, color: AppPalette.white),
                  const SizedBox(width: 6),
                  Text(
                    l.homeRoleLabel(role!),
                    style: const TextStyle(
                      color: AppPalette.white,
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _FaceEnrollBanner extends StatelessWidget {
  const _FaceEnrollBanner({required this.onEnroll});

  final VoidCallback onEnroll;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: s.warningContainer,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Row(
        children: [
          Icon(Icons.face_retouching_natural, color: s.onWarningContainer),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(l.faceEnrollPromptTitle,
                    style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: s.onWarningContainer)),
                const SizedBox(height: 2),
                Text(l.faceEnrollPromptBody,
                    style: TextStyle(
                        fontSize: 12.5,
                        height: 1.3,
                        color: s.onWarningContainer)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          TextButton(
            onPressed: onEnroll,
            style: TextButton.styleFrom(foregroundColor: s.onWarningContainer),
            child: Text(l.faceEnrollNow),
          ),
        ],
      ),
    );
  }
}
