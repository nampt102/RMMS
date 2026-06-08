import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/notifications_repository.dart';
import '../../domain/app_notification.dart';

/// Redesign 2026 — Thông báo. New-kit list of in-app notifications (M14).
///
/// Header (display 26/800) + "Đọc tất cả" soft action; cards carry a tone-tinted
/// type icon, title/body/relative time, and an unread dot. Tapping marks the row
/// read and follows its deep link (rmms://approvals|requests|schedules/...).
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(notificationsPageProvider);

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.invalidate(notificationsPageProvider),
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
            physics: const AlwaysScrollableScrollPhysics(),
            children: [
              AppTopBar(
                title: l.notifTitle,
                trailing: [
                  if (async.maybeWhen(data: (p) => p.unreadCount > 0, orElse: () => false))
                    AppButton.soft(
                      label: l.notifMarkAll,
                      icon: Icons.done_all_rounded,
                      expand: false,
                      onPressed: () async {
                        try {
                          await ref.read(notificationsRepositoryProvider).markAllRead();
                          ref.invalidate(notificationsPageProvider);
                        } on ApiException catch (e) {
                          if (context.mounted) {
                            showAppToast(context, message: e.message, kind: AppToastKind.error);
                          }
                        }
                      },
                    ),
                ],
              ),
              const SizedBox(height: 6),
              async.when(
                loading: () => const Padding(
                  padding: EdgeInsets.symmetric(vertical: 80),
                  child: Center(child: CircularProgressIndicator()),
                ),
                error: (e, _) => _ErrorBlock(
                  message: e is ApiException ? e.message : l.commonRetry,
                  onRetry: () => ref.invalidate(notificationsPageProvider),
                ),
                data: (p) {
                  if (p.items.isEmpty) {
                    return _EmptyBlock(message: l.notifEmpty);
                  }
                  return Column(
                    children: [
                      for (var i = 0; i < p.items.length; i++)
                        AppRiseIn(
                          delay: Duration(milliseconds: 40 * i),
                          child: Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: _NotificationCard(item: p.items[i]),
                          ),
                        ),
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

({IconData icon, AppTone tone}) _typeMeta(String type) => switch (type) {
      'approval_needed' => (icon: Icons.fact_check_rounded, tone: AppTone.indigo),
      'request_approved' => (icon: Icons.check_circle_rounded, tone: AppTone.emerald),
      'request_rejected' => (icon: Icons.cancel_rounded, tone: AppTone.rose),
      'device_change_request' => (icon: Icons.phonelink_lock_rounded, tone: AppTone.amber),
      'attendance_in_review' => (icon: Icons.rule_rounded, tone: AppTone.sky),
      'news' => (icon: Icons.campaign_rounded, tone: AppTone.violet),
      'document' => (icon: Icons.description_rounded, tone: AppTone.sky),
      'payslip' => (icon: Icons.payments_rounded, tone: AppTone.emerald),
      'form_deadline' => (icon: Icons.timer_rounded, tone: AppTone.amber),
      _ => (icon: Icons.notifications_rounded, tone: AppTone.neutral),
    };

/// Map a server deep link (rmms://host/id) to an in-app route. Unknown links no-op.
String? _routeForDeepLink(String? deepLink) {
  if (deepLink == null) return null;
  final uri = Uri.tryParse(deepLink);
  if (uri == null) return null;
  return switch (uri.host) {
    'approvals' => AppRoutes.approvals,
    'requests' => AppRoutes.requests,
    'schedules' => AppRoutes.schedule,
    'attendance' => AppRoutes.attendance,
    _ => null,
  };
}

class _NotificationCard extends ConsumerWidget {
  const _NotificationCard({required this.item});
  final AppNotification item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lang = Localizations.localeOf(context).languageCode;
    final m = _typeMeta(item.type);
    final time = DateFormat.MMMd(lang).add_Hm().format(item.createdAt.toLocal());

    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      color: item.isRead ? null : AppPalette.chipIndigoBg,
      onTap: () async {
        if (!item.isRead) {
          try {
            await ref.read(notificationsRepositoryProvider).markRead(item.id);
            ref.invalidate(notificationsPageProvider);
          } on ApiException {
            // Non-blocking — navigation still proceeds.
          }
        }
        final route = _routeForDeepLink(item.data?['deepLink']);
        if (route != null && context.mounted) context.push(route);
      },
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppIconTile(icon: m.icon, tone: m.tone, size: 44, radius: 14),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        item.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.plusJakartaSans(
                          color: AppPalette.ink,
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    if (!item.isRead) ...[
                      const SizedBox(width: 8),
                      Container(
                        width: 9,
                        height: 9,
                        margin: const EdgeInsets.only(top: 4),
                        decoration: const BoxDecoration(
                          color: AppPalette.indigo,
                          shape: BoxShape.circle,
                        ),
                      ),
                    ],
                  ],
                ),
                if (item.body.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Text(
                    item.body,
                    maxLines: 3,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.plusJakartaSans(
                      color: AppPalette.body,
                      fontSize: 13.5,
                      fontWeight: FontWeight.w500,
                      height: 1.4,
                    ),
                  ),
                ],
                const SizedBox(height: 6),
                Text(
                  time,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.faint,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyBlock extends StatelessWidget {
  const _EmptyBlock({required this.message});
  final String message;
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 80),
      child: Column(
        children: [
          const AppIconTile(
              icon: Icons.notifications_off_rounded,
              tone: AppTone.indigo,
              size: 64,
              radius: 22),
          const SizedBox(height: 16),
          Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 14.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
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
    return Padding(
      padding: const EdgeInsets.only(top: 80),
      child: Column(
        children: [
          const AppIconTile(
              icon: Icons.error_outline_rounded,
              tone: AppTone.rose,
              size: 64,
              radius: 22),
          const SizedBox(height: 16),
          Text(message,
              textAlign: TextAlign.center,
              style: GoogleFonts.plusJakartaSans(
                  color: AppPalette.ink, fontSize: 14.5, fontWeight: FontWeight.w600)),
          const SizedBox(height: 16),
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
