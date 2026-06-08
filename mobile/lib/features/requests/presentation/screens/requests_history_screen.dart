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
import '../../data/requests_repository.dart';
import '../../domain/leave_request.dart';
import '../../domain/ot_request.dart';

/// Redesign 2026 — Đơn từ (tab).
///
/// Header: "Đơn của tôi" (display 26/800) + 46×46 gradient "+" → Create sheet
/// with two rows (Xin nghỉ phép · Đăng ký OT). Segmented Nghỉ phép / Làm thêm
/// (OT) with ink-fill active state; cards = type Chip + status Chip + date +
/// reason; pending leave shows a destructive-soft "Thu hồi" action.
class RequestsHistoryScreen extends ConsumerStatefulWidget {
  const RequestsHistoryScreen({super.key});

  @override
  ConsumerState<RequestsHistoryScreen> createState() =>
      _RequestsHistoryScreenState();
}

class _RequestsHistoryScreenState
    extends ConsumerState<RequestsHistoryScreen> {
  bool _showLeave = true;

  Future<void> _openCreateSheet() async {
    final l = AppLocalizations.of(context);
    await showAppSheet<void>(
      context: context,
      builder: (ctx) => _CreateSheetBody(
        title: l.requestsCreateTitle,
        onLeave: () {
          Navigator.pop(ctx);
          context.push(AppRoutes.leaveRequest);
        },
        onOt: () {
          Navigator.pop(ctx);
          context.push(AppRoutes.otRequest);
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final s = context.semantics;

    return SafeArea(
      bottom: false,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 140),
        children: [
          // Header.
          Row(
            children: [
              Expanded(
                child: Text(
                  l.requestsTitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 26,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.6,
                  ),
                ),
              ),
              PressScale(
                onTap: _openCreateSheet,
                child: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    gradient: s.brandGradient,
                    borderRadius: BorderRadius.circular(15),
                    boxShadow: s.shadowBrand,
                  ),
                  alignment: Alignment.center,
                  child: const Icon(Icons.add_rounded,
                      color: Colors.white, size: 22),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          // Segmented tabs.
          _SegTabs(
            value: _showLeave,
            onChanged: (v) => setState(() => _showLeave = v),
            leaveLabel: l.requestsTabLeave,
            otLabel: l.requestsTabOt,
          ),
          const SizedBox(height: 14),
          // List.
          if (_showLeave) const _LeaveList() else const _OtList(),
        ],
      ),
    );
  }
}

// ─── Segmented tabs ───────────────────────────────────────────────────────

class _SegTabs extends StatelessWidget {
  const _SegTabs({
    required this.value,
    required this.onChanged,
    required this.leaveLabel,
    required this.otLabel,
  });
  final bool value; // true = Leave
  final ValueChanged<bool> onChanged;
  final String leaveLabel;
  final String otLabel;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _SegItem(
            label: leaveLabel,
            icon: Icons.beach_access_rounded,
            active: value,
            onTap: () => onChanged(true),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _SegItem(
            label: otLabel,
            icon: Icons.more_time_rounded,
            active: !value,
            onTap: () => onChanged(false),
          ),
        ),
      ],
    );
  }
}

class _SegItem extends StatelessWidget {
  const _SegItem({
    required this.label,
    required this.icon,
    required this.active,
    required this.onTap,
  });
  final String label;
  final IconData icon;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final s = context.semantics;
    return PressScale(
      onTap: onTap,
      child: Container(
        height: 46,
        padding: const EdgeInsets.symmetric(horizontal: 14),
        decoration: BoxDecoration(
          color: active ? AppPalette.ink : AppPalette.surface,
          borderRadius: BorderRadius.circular(15),
          boxShadow: active ? null : s.shadowSm,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon,
                color: active ? Colors.white : AppPalette.muted, size: 18),
            const SizedBox(width: 8),
            Flexible(
              child: Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.plusJakartaSans(
                  color: active ? Colors.white : AppPalette.muted,
                  fontSize: 13.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─── Lists ────────────────────────────────────────────────────────────────

({AppTone tone, String label}) _statusMeta(AppLocalizations l, String status) =>
    switch (status) {
      'approved' => (tone: AppTone.emerald, label: l.requestStatusApproved),
      'rejected' => (tone: AppTone.rose, label: l.requestStatusRejected),
      _ => (tone: AppTone.amber, label: l.requestStatusPending),
    };

class _LeaveList extends ConsumerWidget {
  const _LeaveList();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myLeaveProvider);
    return async.when(
      loading: () => const Padding(
        padding: EdgeInsets.symmetric(vertical: 60),
        child: Center(child: CircularProgressIndicator()),
      ),
      error: (e, _) => _ErrorBlock(
        message: e is ApiException ? e.message : l.commonRetry,
        onRetry: () => ref.invalidate(myLeaveProvider),
      ),
      data: (items) {
        if (items.isEmpty) {
          return _EmptyBlock(
            icon: Icons.beach_access_rounded,
            message: l.requestsLeaveEmpty,
          );
        }
        return Column(
          children: [
            for (final item in items)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _LeaveCard(item: item),
              ),
            const SizedBox(height: 4),
            _EndFooter(text: l.requestsEndOfList),
          ],
        );
      },
    );
  }
}

class _LeaveCard extends ConsumerWidget {
  const _LeaveCard({required this.item});
  final LeaveRequest item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final m = _statusMeta(l, item.status);
    String fmt(String d) {
      final parsed = DateTime.tryParse(d);
      return parsed == null ? d : DateFormat.yMMMEd(lang).format(parsed);
    }

    final range = item.startDate == item.endDate
        ? fmt(item.startDate)
        : '${fmt(item.startDate)} → ${fmt(item.endDate)}';
    final typeLabel = item.leaveType == 'emergency'
        ? l.requestLeaveEmergency
        : l.requestLeaveRegular;
    final typeTone = item.leaveType == 'emergency'
        ? AppTone.amber
        : AppTone.indigo;

    return AppCard(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              AppChip(
                  label: typeLabel,
                  icon: Icons.beach_access_rounded,
                  tone: typeTone),
              const Spacer(),
              AppChip(label: m.label, tone: m.tone),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            range,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.ink,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          if ((item.reason ?? '').isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              item.reason!,
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
          if (item.isPending) ...[
            const SizedBox(height: 10),
            Align(
              alignment: Alignment.centerRight,
              child: AppButton.destructiveSoft(
                label: l.requestWithdraw,
                icon: Icons.undo_rounded,
                expand: false,
                onPressed: () async {
                  try {
                    await ref
                        .read(requestsRepositoryProvider)
                        .withdrawLeave(item.id);
                    ref.invalidate(myLeaveProvider);
                    if (context.mounted) {
                      showAppToast(context,
                          message: l.requestWithdrawn,
                          kind: AppToastKind.success);
                    }
                  } on ApiException catch (e) {
                    if (context.mounted) {
                      showAppToast(context,
                          message: e.message, kind: AppToastKind.error);
                    }
                  }
                },
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _OtList extends ConsumerWidget {
  const _OtList();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = AppLocalizations.of(context);
    final async = ref.watch(myOtProvider);
    return async.when(
      loading: () => const Padding(
        padding: EdgeInsets.symmetric(vertical: 60),
        child: Center(child: CircularProgressIndicator()),
      ),
      error: (e, _) => _ErrorBlock(
        message: e is ApiException ? e.message : l.commonRetry,
        onRetry: () => ref.invalidate(myOtProvider),
      ),
      data: (items) {
        if (items.isEmpty) {
          return _EmptyBlock(
            icon: Icons.more_time_rounded,
            message: l.requestsOtEmpty,
          );
        }
        return Column(
          children: [
            for (final item in items)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _OtCard(item: item),
              ),
            const SizedBox(height: 4),
            _EndFooter(text: l.requestsEndOfList),
          ],
        );
      },
    );
  }
}

class _OtCard extends StatelessWidget {
  const _OtCard({required this.item});
  final OtRequest item;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final m = _statusMeta(l, item.status);
    final parsed = DateTime.tryParse(item.otDate);
    final dateText =
        parsed == null ? item.otDate : DateFormat.yMMMEd(lang).format(parsed);
    String hhmm(String t) => t.length >= 5 ? t.substring(0, 5) : t;

    return AppCard(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              AppChip(
                  label: l.requestsTabOt,
                  icon: Icons.more_time_rounded,
                  tone: AppTone.amber),
              const Spacer(),
              AppChip(label: m.label, tone: m.tone),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            dateText,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.ink,
              fontSize: 16,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            '${hhmm(item.startTime)} – ${hhmm(item.endTime)}',
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
          if ((item.reason ?? '').isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              item.reason!,
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
        ],
      ),
    );
  }
}

// ─── Create sheet body ────────────────────────────────────────────────────

class _CreateSheetBody extends StatelessWidget {
  const _CreateSheetBody({
    required this.title,
    required this.onLeave,
    required this.onOt,
  });
  final String title;
  final VoidCallback onLeave;
  final VoidCallback onOt;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Text(
            title,
            style: GoogleFonts.spaceGrotesk(
              color: AppPalette.ink,
              fontSize: 19,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.3,
            ),
          ),
        ),
        _SheetRow(
          icon: Icons.beach_access_rounded,
          tone: AppTone.indigo,
          label: l.requestsAddLeave,
          onTap: onLeave,
        ),
        const SizedBox(height: 10),
        _SheetRow(
          icon: Icons.more_time_rounded,
          tone: AppTone.amber,
          label: l.requestsAddOt,
          onTap: onOt,
        ),
      ],
    );
  }
}

class _SheetRow extends StatelessWidget {
  const _SheetRow({
    required this.icon,
    required this.tone,
    required this.label,
    required this.onTap,
  });
  final IconData icon;
  final AppTone tone;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return PressScale(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
        decoration: BoxDecoration(
          color: AppPalette.surface2,
          borderRadius: BorderRadius.circular(18),
        ),
        child: Row(
          children: [
            AppIconTile(icon: icon, tone: tone, size: 46, radius: 14),
            const SizedBox(width: 14),
            Expanded(
              child: Text(
                label,
                style: GoogleFonts.plusJakartaSans(
                  color: AppPalette.ink,
                  fontSize: 15.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const Icon(Icons.chevron_right_rounded,
                color: AppPalette.faint, size: 22),
          ],
        ),
      ),
    );
  }
}

// ─── Footer / empty / error ───────────────────────────────────────────────

class _EndFooter extends StatelessWidget {
  const _EndFooter({required this.text});
  final String text;
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 18),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: GoogleFonts.plusJakartaSans(
          color: AppPalette.faint,
          fontSize: 12.5,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

class _EmptyBlock extends StatelessWidget {
  const _EmptyBlock({required this.icon, required this.message});
  final IconData icon;
  final String message;
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 60),
      child: Column(
        children: [
          AppIconTile(icon: icon, tone: AppTone.indigo, size: 64, radius: 22),
          const SizedBox(height: 16),
          Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 14.5,
              fontWeight: FontWeight.w600,
              height: 1.4,
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
      padding: const EdgeInsets.only(top: 60),
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
                  color: AppPalette.ink,
                  fontSize: 14.5,
                  fontWeight: FontWeight.w600)),
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
