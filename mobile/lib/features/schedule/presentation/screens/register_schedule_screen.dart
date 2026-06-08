import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../organization/data/organization_repository.dart';
import '../../../organization/domain/assigned_store.dart';
import '../../data/schedule_api.dart';
import '../../data/schedule_repository.dart';
import '../../domain/work_schedule.dart';

enum _Mode { day, week, month }

class _ShiftDraft {
  _ShiftDraft({required this.start, required this.end});
  String? storeId;
  TimeOfDay start;
  TimeOfDay end;
}

/// Redesign 2026 — Đăng ký lịch (pushed).
///
/// Visual: AppTopBar; segmented Ngày/Tuần/Tháng track surface-2 (hidden when
/// editing); date IconTile card; per-shift cards with a store row + 2 time
/// pickers (surface-2 wells, big 22/800 time); dashed "+ Thêm ca" CTA;
/// sticky bottom AppButton.primary "Lưu lịch".
class RegisterScheduleScreen extends ConsumerStatefulWidget {
  const RegisterScheduleScreen({super.key, this.editSchedule});

  final WorkSchedule? editSchedule;

  @override
  ConsumerState<RegisterScheduleScreen> createState() =>
      _RegisterScheduleScreenState();
}

class _RegisterScheduleScreenState
    extends ConsumerState<RegisterScheduleScreen> {
  _Mode _mode = _Mode.day;
  late DateTime _anchor;
  late final List<_ShiftDraft> _shifts;
  bool _saving = false;

  bool get _isEdit => widget.editSchedule != null;

  @override
  void initState() {
    super.initState();
    final edit = widget.editSchedule;
    if (edit != null) {
      _anchor = DateTime.tryParse(edit.scheduleDate) ?? DateTime.now();
      _shifts = edit.shifts
          .map((s) => _ShiftDraft(
                start: _parseTime(s.startTime),
                end: _parseTime(s.endTime),
              )..storeId = s.storeId)
          .toList();
      if (_shifts.isEmpty) {
        _shifts.add(_ShiftDraft(
            start: const TimeOfDay(hour: 8, minute: 0),
            end: const TimeOfDay(hour: 17, minute: 0)));
      }
    } else {
      _anchor = DateTime.now();
      _shifts = [
        _ShiftDraft(
            start: const TimeOfDay(hour: 8, minute: 0),
            end: const TimeOfDay(hour: 17, minute: 0)),
      ];
    }
  }

  TimeOfDay _parseTime(String hhmm) {
    final p = hhmm.split(':');
    return TimeOfDay(
      hour: int.tryParse(p.isNotEmpty ? p[0] : '') ?? 8,
      minute: int.tryParse(p.length > 1 ? p[1] : '') ?? 0,
    );
  }

  String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  String _hhmm(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  List<DateTime> _targetDays() {
    final today = DateTime.now();
    final todayDate = DateTime(today.year, today.month, today.day);
    final anchor = DateTime(_anchor.year, _anchor.month, _anchor.day);
    final days = <DateTime>[];
    switch (_mode) {
      case _Mode.day:
        days.add(anchor);
      case _Mode.week:
        for (var i = 0; i < 7; i++) {
          days.add(anchor.add(Duration(days: i)));
        }
      case _Mode.month:
        final first = DateTime(anchor.year, anchor.month, 1);
        final last = DateTime(anchor.year, anchor.month + 1, 0);
        for (var d = first;
            !d.isAfter(last);
            d = d.add(const Duration(days: 1))) {
          days.add(d);
        }
    }
    return days.where((d) => !d.isBefore(todayDate)).toList();
  }

  Future<void> _save() async {
    final l = AppLocalizations.of(context);
    if (_shifts.any((s) => s.storeId == null)) {
      showAppToast(context, message: l.registerSelectStore, kind: AppToastKind.warning);
      return;
    }
    if (_shifts.isEmpty) {
      showAppToast(context, message: l.registerNeedShift, kind: AppToastKind.warning);
      return;
    }

    final shiftInputs = _shifts
        .map((s) => ShiftInput(
            storeId: s.storeId!,
            startTime: _hhmm(s.start),
            endTime: _hhmm(s.end)))
        .toList();

    setState(() => _saving = true);
    try {
      final repo = ref.read(scheduleRepositoryProvider);
      if (_isEdit) {
        await repo.edit(widget.editSchedule!.id, shiftInputs);
      } else {
        final days = _targetDays()
            .map((d) => ScheduleDayInput(date: _ymd(d), shifts: shiftInputs))
            .toList();
        await repo.create(days);
      }
      ref.invalidate(myScheduleProvider);
      if (!mounted) return;
      showAppToast(
        context,
        message: _isEdit ? l.registerEditSaved : l.registerSaved,
        kind: AppToastKind.success,
      );
      context.pop();
    } on ApiException catch (e) {
      if (!mounted) return;
      showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _anchor,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _anchor = picked);
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final storesAsync = ref.watch(myStoresProvider);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: _isEdit ? l.registerEditTitle : l.registerTitle),
            Expanded(
              child: storesAsync.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => Center(
                  child: Text(e is ApiException ? e.message : l.commonRetry),
                ),
                data: (stores) {
                  if (stores.isEmpty) {
                    return Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child: Text(
                          l.registerNoStores,
                          textAlign: TextAlign.center,
                          style: GoogleFonts.plusJakartaSans(
                            color: AppPalette.muted,
                            fontSize: 14.5,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    );
                  }
                  return _Body(
                    mode: _mode,
                    onModeChanged: (m) => setState(() => _mode = m),
                    isEdit: _isEdit,
                    anchor: _anchor,
                    targetDayCount: _targetDays().length,
                    onPickDate: _pickDate,
                    shifts: _shifts,
                    stores: stores,
                    lang: lang,
                    onShiftChanged: () => setState(() {}),
                    onAddShift: () => setState(() => _shifts.add(_ShiftDraft(
                          start: const TimeOfDay(hour: 8, minute: 0),
                          end: const TimeOfDay(hour: 17, minute: 0),
                        ))),
                    onRemoveShift: (i) => setState(() => _shifts.removeAt(i)),
                    pickTime: (t) => showTimePicker(
                      context: context,
                      initialTime: t,
                    ),
                    hhmm: _hhmm,
                  );
                },
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
          child: AppButton.primary(
            label: l.registerSave,
            icon: Icons.save_rounded,
            loading: _saving,
            onPressed: _save,
          ),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({
    required this.mode,
    required this.onModeChanged,
    required this.isEdit,
    required this.anchor,
    required this.targetDayCount,
    required this.onPickDate,
    required this.shifts,
    required this.stores,
    required this.lang,
    required this.onShiftChanged,
    required this.onAddShift,
    required this.onRemoveShift,
    required this.pickTime,
    required this.hhmm,
  });

  final _Mode mode;
  final ValueChanged<_Mode> onModeChanged;
  final bool isEdit;
  final DateTime anchor;
  final int targetDayCount;
  final VoidCallback onPickDate;
  final List<_ShiftDraft> shifts;
  final List<AssignedStore> stores;
  final String lang;
  final VoidCallback onShiftChanged;
  final VoidCallback onAddShift;
  final ValueChanged<int> onRemoveShift;
  final Future<TimeOfDay?> Function(TimeOfDay initial) pickTime;
  final String Function(TimeOfDay) hhmm;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        if (!isEdit) ...[
          _ModeSegmented(value: mode, onChanged: onModeChanged),
          const SizedBox(height: 14),
        ],
        _DateCard(
          anchor: anchor,
          hint: isEdit
              ? l.registerEditHint
              : l.registerDaysHint(targetDayCount),
          lang: lang,
          onTap: isEdit ? null : onPickDate,
        ),
        const SizedBox(height: 22),
        const _SectionHeader(text: 'Ca làm'),
        const SizedBox(height: 10),
        for (var i = 0; i < shifts.length; i++) ...[
          _ShiftCard(
            index: i,
            draft: shifts[i],
            stores: stores,
            onChanged: onShiftChanged,
            onRemove:
                shifts.length > 1 ? () => onRemoveShift(i) : null,
            pickTime: pickTime,
            hhmm: hhmm,
          ),
          const SizedBox(height: 12),
        ],
        _AddShiftButton(onTap: onAddShift),
      ],
    );
  }
}

// ─── Segmented Ngày / Tuần / Tháng ─────────────────────────────────────────

class _ModeSegmented extends StatelessWidget {
  const _ModeSegmented({required this.value, required this.onChanged});
  final _Mode value;
  final ValueChanged<_Mode> onChanged;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final items = <(_Mode, String)>[
      (_Mode.day, l.registerModeDay),
      (_Mode.week, l.registerModeWeek),
      (_Mode.month, l.registerModeMonth),
    ];
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: AppPalette.surface2,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          for (final (mode, label) in items)
            Expanded(
              child: _SegmentItem(
                label: label,
                active: mode == value,
                onTap: () => onChanged(mode),
              ),
            ),
        ],
      ),
    );
  }
}

class _SegmentItem extends StatelessWidget {
  const _SegmentItem({
    required this.label,
    required this.active,
    required this.onTap,
  });
  final String label;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return PressScale(
      onTap: onTap,
      child: Container(
        height: 38,
        margin: const EdgeInsets.symmetric(horizontal: 2),
        decoration: BoxDecoration(
          color: active ? AppPalette.surface : Colors.transparent,
          borderRadius: BorderRadius.circular(12),
          boxShadow: active ? context.semantics.shadowSm : null,
        ),
        alignment: Alignment.center,
        child: Text(
          label,
          style: GoogleFonts.plusJakartaSans(
            color: active ? AppPalette.ink : AppPalette.muted,
            fontSize: 13.5,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}

// ─── Date card ────────────────────────────────────────────────────────────

class _DateCard extends StatelessWidget {
  const _DateCard({
    required this.anchor,
    required this.hint,
    required this.lang,
    required this.onTap,
  });
  final DateTime anchor;
  final String hint;
  final String lang;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          AppIconTile(
            icon: Icons.calendar_today_rounded,
            tone: AppTone.indigo,
            size: 46,
            radius: 14,
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  l.registerPickDate,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  DateFormat.yMMMEd(lang).format(anchor),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.ink,
                    fontSize: 17,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  hint,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.emeraldDeep,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          if (onTap != null)
            const Icon(Icons.chevron_right_rounded,
                color: AppPalette.faint, size: 22),
        ],
      ),
    );
  }
}

// ─── Section header ───────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.text});
  final String text;
  @override
  Widget build(BuildContext context) => Text(
        text,
        style: GoogleFonts.spaceGrotesk(
          color: AppPalette.ink,
          fontSize: 17,
          fontWeight: FontWeight.w800,
          letterSpacing: -0.3,
        ),
      );
}

// ─── Shift editor card ────────────────────────────────────────────────────

class _ShiftCard extends StatelessWidget {
  const _ShiftCard({
    required this.index,
    required this.draft,
    required this.stores,
    required this.onChanged,
    required this.onRemove,
    required this.pickTime,
    required this.hhmm,
  });

  final int index;
  final _ShiftDraft draft;
  final List<AssignedStore> stores;
  final VoidCallback onChanged;
  final VoidCallback? onRemove;
  final Future<TimeOfDay?> Function(TimeOfDay initial) pickTime;
  final String Function(TimeOfDay) hhmm;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            l.registerShiftLabel(index + 1),
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.muted,
              fontSize: 11.5,
              fontWeight: FontWeight.w800,
              letterSpacing: 0.8,
            ),
          ),
          const SizedBox(height: 10),
          _StoreDropdown(
            value: draft.storeId,
            stores: stores,
            onChanged: (v) {
              draft.storeId = v;
              onChanged();
            },
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _TimeWell(
                  label: l.registerStart,
                  value: hhmm(draft.start),
                  onTap: () async {
                    final t = await pickTime(draft.start);
                    if (t != null) {
                      draft.start = t;
                      onChanged();
                    }
                  },
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _TimeWell(
                  label: l.registerEnd,
                  value: hhmm(draft.end),
                  onTap: () async {
                    final t = await pickTime(draft.end);
                    if (t != null) {
                      draft.end = t;
                      onChanged();
                    }
                  },
                ),
              ),
            ],
          ),
          if (onRemove != null) ...[
            const SizedBox(height: 10),
            Align(
              alignment: Alignment.centerRight,
              child: AppButton.destructiveSoft(
                label: l.registerRemoveShift,
                icon: Icons.delete_outline_rounded,
                expand: false,
                onPressed: onRemove,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _StoreDropdown extends StatelessWidget {
  const _StoreDropdown({
    required this.value,
    required this.stores,
    required this.onChanged,
  });
  final String? value;
  final List<AssignedStore> stores;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(
        color: AppPalette.surface2,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          const Icon(Icons.storefront_rounded,
              color: AppPalette.indigo, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: value,
                isExpanded: true,
                hint: Text(
                  l.registerStore,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                icon: const Icon(Icons.expand_more_rounded,
                    color: AppPalette.faint),
                items: stores
                    .map((s) => DropdownMenuItem(
                          value: s.id,
                          child: Text(
                            '${s.code} — ${s.name}',
                            overflow: TextOverflow.ellipsis,
                            style: GoogleFonts.plusJakartaSans(
                              color: AppPalette.ink,
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ))
                    .toList(),
                onChanged: onChanged,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _TimeWell extends StatelessWidget {
  const _TimeWell({required this.label, required this.value, required this.onTap});
  final String label;
  final String value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return PressScale(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.fromLTRB(14, 10, 14, 12),
        decoration: BoxDecoration(
          color: AppPalette.surface2,
          borderRadius: BorderRadius.circular(14),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: GoogleFonts.plusJakartaSans(
                color: AppPalette.muted,
                fontSize: 11.5,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.4,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              value,
              style: GoogleFonts.spaceGrotesk(
                color: AppPalette.ink,
                fontSize: 22,
                fontWeight: FontWeight.w800,
                letterSpacing: -0.4,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─── Dashed "+ Thêm ca" button ────────────────────────────────────────────

class _AddShiftButton extends StatelessWidget {
  const _AddShiftButton({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    return PressScale(
      onTap: onTap,
      child: CustomPaint(
        painter: _DashedBorderPainter(),
        child: Container(
          height: 54,
          alignment: Alignment.center,
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Text(
            '+ ${l.registerAddShift}',
            style: GoogleFonts.plusJakartaSans(
              color: AppPalette.indigo,
              fontSize: 14.5,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ),
    );
  }
}

class _DashedBorderPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    const radius = Radius.circular(17);
    final rrect = RRect.fromRectAndRadius(
      Rect.fromLTWH(0, 0, size.width, size.height),
      radius,
    );
    final paint = Paint()
      ..color = AppPalette.indigo.withValues(alpha: 0.45)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.4;

    // Build a path and draw dashed segments.
    final path = Path()..addRRect(rrect);
    final metrics = path.computeMetrics();
    const dash = 6.0;
    const gap = 5.0;
    for (final metric in metrics) {
      var distance = 0.0;
      while (distance < metric.length) {
        final next = (distance + dash).clamp(0, metric.length).toDouble();
        canvas.drawPath(
          metric.extractPath(distance, next),
          paint,
        );
        distance = next + gap;
      }
    }
  }

  @override
  bool shouldRepaint(covariant _DashedBorderPainter oldDelegate) => false;
}
