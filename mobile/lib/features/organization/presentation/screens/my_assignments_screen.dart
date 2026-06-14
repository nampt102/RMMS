import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/organization_repository.dart';
import '../../domain/assigned_leader.dart';
import '../../domain/assigned_store.dart';

/// Redesign 2026 — Phân công của tôi (pushed).
///
/// Two eyebrowed sections:
///   - LEADER QUẢN LÝ — gradient-tile initials avatar, name, email, phone chip
///     and a trailing emerald "bell" button (placeholder ping action).
///   - ĐIỂM BÁN CỦA TÔI — mesh-gradient image header with the store icon,
///     then the store name + "Mã ST-001" + status chip.
class MyAssignmentsScreen extends ConsumerWidget {
  const MyAssignmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final stores = ref.watch(myStoresProvider);
    final leader = ref.watch(myLeaderProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.assignmentsTitle),
            Expanded(
              child: RefreshIndicator(
                onRefresh: () async {
                  ref.invalidate(myStoresProvider);
                  ref.invalidate(myLeaderProvider);
                  await Future.wait([
                    ref.read(myStoresProvider.future),
                    ref.read(myLeaderProvider.future),
                  ]);
                },
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                  children: [
                    SectionEyebrow(l.assignmentsLeader.toUpperCase()),
                    const SizedBox(height: 10),
                    leader.when(
                      loading: () => const _Loading(),
                      error: (_, __) => _ErrorBlock(
                        message: l.assignmentsError,
                        onRetry: () => ref.invalidate(myLeaderProvider),
                      ),
                      data: (data) => data == null
                          ? _EmptyText(text: l.assignmentsNoLeader)
                          : _LeaderCard(leader: data),
                    ),
                    const SizedBox(height: 22),
                    SectionEyebrow(l.assignmentsStores.toUpperCase()),
                    const SizedBox(height: 10),
                    stores.when(
                      loading: () => const _Loading(),
                      error: (_, __) => _ErrorBlock(
                        message: l.assignmentsError,
                        onRetry: () => ref.invalidate(myStoresProvider),
                      ),
                      data: (data) {
                        if (data.isEmpty) {
                          return _EmptyText(text: l.assignmentsNoStores);
                        }
                        return Column(
                          children: [
                            for (final s in data) ...[
                              _StoreCard(store: s),
                              const SizedBox(height: 12),
                            ],
                          ],
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─── Leader card ──────────────────────────────────────────────────────────

class _LeaderCard extends StatelessWidget {
  const _LeaderCard({required this.leader});
  final AssignedLeader leader;

  String _initials(String name) {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.first.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first[0] + parts.last[0]).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  gradient: s.brandGradient,
                  borderRadius: BorderRadius.circular(18),
                  boxShadow: s.shadowBrand,
                ),
                alignment: Alignment.center,
                child: Text(
                  _initials(leader.fullName),
                  style: GoogleFonts.spaceGrotesk(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.2,
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      leader.fullName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.plusJakartaSans(
                        color: AppPalette.ink,
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      leader.email,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.plusJakartaSans(
                        color: AppPalette.muted,
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
              PressScale(
                onTap: () {},
                child: Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    gradient: s.emeraldGradient,
                    borderRadius: BorderRadius.circular(13),
                    boxShadow: const [
                      BoxShadow(
                        color: Color(0x4D10B981),
                        blurRadius: 18,
                        offset: Offset(0, 6),
                      ),
                    ],
                  ),
                  alignment: Alignment.center,
                  child: const Icon(Icons.notifications_rounded,
                      color: Colors.white, size: 20),
                ),
              ),
            ],
          ),
          if ((leader.phone ?? '').isNotEmpty) ...[
            const SizedBox(height: 12),
            AppChip(
              icon: Icons.phone_rounded,
              label: leader.phone!,
              tone: AppTone.indigo,
            ),
          ],
        ],
      ),
    );
  }
}

// ─── Store card ───────────────────────────────────────────────────────────

class _StoreCard extends StatelessWidget {
  const _StoreCard({required this.store});
  final AssignedStore store;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;
    final active = store.status == 'active';
    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: AppPalette.surface,
        borderRadius: BorderRadius.circular(28),
        boxShadow: context.semantics.shadowSm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Mesh-gradient header (110 tall) with stripe overlay + store icon.
          SizedBox(
            height: 110,
            child: Stack(
              children: [
                Positioned.fill(
                  child: DecoratedBox(
                    decoration: BoxDecoration(gradient: s.meshGradient),
                  ),
                ),
                const MeshRadialOverlay(),
                Positioned.fill(
                  child: CustomPaint(painter: _DiagonalStripePainter()),
                ),
                Positioned(
                  right: 14,
                  top: 14,
                  child: Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.22),
                      borderRadius: BorderRadius.circular(18),
                    ),
                    alignment: Alignment.center,
                    child: const Icon(Icons.storefront_rounded,
                        color: Colors.white, size: 28),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  store.name,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  l.storeCodeLabel(store.code),
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                if ((store.address ?? '').isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Text(
                    store.address!,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.plusJakartaSans(
                      color: AppPalette.body,
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                      height: 1.4,
                    ),
                  ),
                ],
                const SizedBox(height: 10),
                AppChip(
                  icon: active ? Icons.circle : Icons.block_rounded,
                  label: active
                      ? l.storeStatusActive
                      : l.storeStatusInactive,
                  tone: active ? AppTone.emerald : AppTone.neutral,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Soft diagonal "barber-pole" stripe overlay for the mesh header.
class _DiagonalStripePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withValues(alpha: 0.06)
      ..style = PaintingStyle.fill;
    const stripe = 14.0;
    const gap = 24.0;
    const step = stripe + gap;
    // Draw parallel slanted stripes covering the rect.
    for (var x = -size.height; x < size.width + size.height; x += step) {
      final p = Path()
        ..moveTo(x, 0)
        ..lineTo(x + stripe, 0)
        ..lineTo(x + stripe + size.height, size.height)
        ..lineTo(x + size.height, size.height)
        ..close();
      canvas.drawPath(p, paint);
    }
  }

  @override
  bool shouldRepaint(covariant _DiagonalStripePainter oldDelegate) => false;
}

// ─── Misc ─────────────────────────────────────────────────────────────────

class _Loading extends StatelessWidget {
  const _Loading();
  @override
  Widget build(BuildContext context) => const Padding(
        padding: EdgeInsets.symmetric(vertical: 18),
        child: Center(child: CircularProgressIndicator()),
      );
}

class _EmptyText extends StatelessWidget {
  const _EmptyText({required this.text});
  final String text;
  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Text(
        text,
        style: GoogleFonts.plusJakartaSans(
          color: AppPalette.muted,
          fontSize: 13.5,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _ErrorBlock extends StatelessWidget {
  const _ErrorBlock({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          const AppIconTile(
            icon: Icons.error_outline_rounded,
            tone: AppTone.rose,
            size: 40,
            radius: 12,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              message,
              style: GoogleFonts.plusJakartaSans(
                color: AppPalette.ink,
                fontSize: 13.5,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          AppButton.soft(
            label: l.commonRetry,
            icon: Icons.refresh_rounded,
            expand: false,
            onPressed: onRetry,
          ),
        ],
      ),
    );
  }
}
