import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../auth/application/auth_controller.dart';
import '../../../auth/application/auth_state.dart';

/// Account-only Hồ sơ tab (Redesign 2026 IA): Thông báo, Đổi mật khẩu,
/// Trợ giúp & hỗ trợ, Đăng xuất.
///
/// Phân công / Lịch sử / Khuôn mặt no longer live here — they have moved
/// to Home as quick-access shortcuts.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final auth = ref.watch(authControllerProvider);
    final user = auth is AuthAuthenticated ? auth.user : null;

    return SafeArea(
      bottom: false,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 140),
        children: [
          _ProfileHeader(name: user?.fullName ?? 'PG', subtitle: user?.role.name),
          const SizedBox(height: 20),
          AppCard(
            padding: EdgeInsets.zero,
            child: Column(
              children: [
                _Row(
                  icon: Icons.notifications_rounded,
                  tone: AppTone.indigo,
                  label: l.profileNotifications,
                  onTap: () {},
                ),
                const _Divider(),
                _Row(
                  icon: Icons.lock_rounded,
                  tone: AppTone.violet,
                  label: l.profileChangePassword,
                  onTap: () {},
                ),
                const _Divider(),
                _Row(
                  icon: Icons.help_rounded,
                  tone: AppTone.sky,
                  label: l.profileHelp,
                  onTap: () {},
                ),
                const _Divider(),
                _Row(
                  icon: Icons.logout_rounded,
                  tone: AppTone.rose,
                  label: l.profileLogout,
                  destructive: true,
                  onTap: () =>
                      ref.read(authControllerProvider.notifier).logout(),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader({required this.name, this.subtitle});

  final String name;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return AppCard(
      padding: const EdgeInsets.fromLTRB(20, 22, 20, 22),
      shadow: s.shadowLg,
      child: Row(
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              gradient: s.brandGradient,
              borderRadius: BorderRadius.circular(22),
              boxShadow: s.shadowBrand,
            ),
            alignment: Alignment.center,
            child: Text(
              name.isEmpty ? '?' : name.substring(0, 1).toUpperCase(),
              style: GoogleFonts.spaceGrotesk(
                color: Colors.white,
                fontSize: 26,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 22,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.5,
                  ),
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 6),
                  AppChip(label: subtitle!, tone: AppTone.indigo),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.icon,
    required this.tone,
    required this.label,
    required this.onTap,
    this.destructive = false,
  });

  final IconData icon;
  final AppTone tone;
  final String label;
  final VoidCallback onTap;
  final bool destructive;

  @override
  Widget build(BuildContext context) {
    return PressScale(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
        child: Row(
          children: [
            AppIconTile(icon: icon, tone: tone, size: 44, radius: 14),
            const SizedBox(width: 14),
            Expanded(
              child: Text(
                label,
                style: GoogleFonts.plusJakartaSans(
                  color: destructive ? AppPalette.rose : AppPalette.ink,
                  fontSize: 15.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            Icon(Icons.chevron_right_rounded,
                color: AppPalette.faint, size: 22),
          ],
        ),
      ),
    );
  }
}

class _Divider extends StatelessWidget {
  const _Divider();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 16),
      child: Divider(height: 1, thickness: 1, color: AppPalette.line),
    );
  }
}
