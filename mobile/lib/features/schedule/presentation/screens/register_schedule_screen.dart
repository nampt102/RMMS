import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../organization/data/organization_repository.dart';
import '../../../organization/domain/assigned_store.dart';
import '../../data/schedule_api.dart';
import '../../data/schedule_repository.dart';
import '../../domain/work_schedule.dart';

enum _Mode { day, week, month }

/// Draft of one shift while composing a registration.
class _ShiftDraft {
  _ShiftDraft({required this.start, required this.end});
  String? storeId;
  TimeOfDay start;
  TimeOfDay end;
}

/// Register a schedule for a day / week / month (M07, BR-301). The chosen shift
/// template is applied to every selected (future) day; the client expands the
/// range into days and the backend stores one schedule per day.
///
/// When [editSchedule] is provided the screen switches to single-day **edit**
/// mode: the mode/date selectors are hidden, the shifts are pre-filled, and Save
/// calls `PATCH /schedule/{id}` — editing an approved schedule creates a new
/// edit-pending version (BR-308).
class RegisterScheduleScreen extends ConsumerStatefulWidget {
  const RegisterScheduleScreen({super.key, this.editSchedule});

  final WorkSchedule? editSchedule;

  @override
  ConsumerState<RegisterScheduleScreen> createState() => _RegisterScheduleScreenState();
}

class _RegisterScheduleScreenState extends ConsumerState<RegisterScheduleScreen> {
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
        _ShiftDraft(start: const TimeOfDay(hour: 8, minute: 0), end: const TimeOfDay(hour: 17, minute: 0)),
      ];
    }
  }

  TimeOfDay _parseTime(String hhmm) {
    final parts = hhmm.split(':');
    return TimeOfDay(
      hour: int.tryParse(parts.isNotEmpty ? parts[0] : '') ?? 8,
      minute: int.tryParse(parts.length > 1 ? parts[1] : '') ?? 0,
    );
  }

  String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  String _hhmm(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  /// Days covered by the current mode, dropping any in the past.
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
        for (var d = first; !d.isAfter(last); d = d.add(const Duration(days: 1))) {
          days.add(d);
        }
    }
    return days.where((d) => !d.isBefore(todayDate)).toList();
  }

  Future<void> _save() async {
    final l = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);

    if (_shifts.any((s) => s.storeId == null)) {
      messenger.showSnackBar(SnackBar(content: Text(l.registerSelectStore)));
      return;
    }
    if (_shifts.isEmpty) {
      messenger.showSnackBar(SnackBar(content: Text(l.registerNeedShift)));
      return;
    }

    final shiftInputs = _shifts
        .map((s) => ShiftInput(storeId: s.storeId!, startTime: _hhmm(s.start), endTime: _hhmm(s.end)))
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
      messenger.showSnackBar(
          SnackBar(content: Text(_isEdit ? l.registerEditSaved : l.registerSaved)));
      context.pop();
    } on ApiException catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(e.message)));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final storesAsync = ref.watch(myStoresProvider);

    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? l.registerEditTitle : l.registerTitle)),
      body: SafeArea(
        child: storesAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Text(e is ApiException ? e.message : l.commonRetry)),
          data: (stores) {
            if (stores.isEmpty) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(l.registerNoStores, textAlign: TextAlign.center),
                ),
              );
            }
            return ListView(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                if (_isEdit)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.calendar_today_outlined),
                    title: Text(DateFormat.yMMMEd(lang).format(_anchor)),
                    subtitle: Text(l.registerEditHint),
                  ),
                if (!_isEdit) ...[
                  SegmentedButton<_Mode>(
                    segments: [
                      ButtonSegment(value: _Mode.day, label: Text(l.registerModeDay)),
                      ButtonSegment(value: _Mode.week, label: Text(l.registerModeWeek)),
                      ButtonSegment(value: _Mode.month, label: Text(l.registerModeMonth)),
                    ],
                    selected: {_mode},
                    onSelectionChanged: (s) => setState(() => _mode = s.first),
                  ),
                  const SizedBox(height: 16),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.calendar_today_outlined),
                    title: Text(l.registerPickDate),
                    subtitle: Text(DateFormat.yMMMEd(lang).format(_anchor)),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () async {
                      final now = DateTime.now();
                      final picked = await showDatePicker(
                        context: context,
                        initialDate: _anchor,
                        firstDate: DateTime(now.year, now.month, now.day),
                        lastDate: now.add(const Duration(days: 365)),
                      );
                      if (picked != null) setState(() => _anchor = picked);
                    },
                  ),
                  Text('${_targetDays().length} ${l.registerDaysCount}',
                      style: Theme.of(context).textTheme.bodySmall),
                ],
                const Divider(height: 32),
                Text(l.registerShifts, style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                ..._shifts.asMap().entries.map((e) => _ShiftEditor(
                      key: ValueKey(e.key),
                      index: e.key,
                      draft: e.value,
                      stores: stores,
                      onChanged: () => setState(() {}),
                      onRemove: _shifts.length > 1 ? () => setState(() => _shifts.removeAt(e.key)) : null,
                      pickTime: _pickTime,
                      hhmm: _hhmm,
                    )),
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  onPressed: () => setState(() => _shifts.add(_ShiftDraft(
                        start: const TimeOfDay(hour: 8, minute: 0),
                        end: const TimeOfDay(hour: 17, minute: 0),
                      ))),
                  icon: const Icon(Icons.add),
                  label: Text(l.registerAddShift),
                ),
              ],
            );
          },
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: FilledButton(
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
            onPressed: _saving ? null : _save,
            child: _saving
                ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(l.registerSave),
          ),
        ),
      ),
    );
  }

  Future<TimeOfDay?> _pickTime(TimeOfDay initial) =>
      showTimePicker(context: context, initialTime: initial);
}

class _ShiftEditor extends StatelessWidget {
  const _ShiftEditor({
    super.key,
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
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            DropdownButtonFormField<String>(
              initialValue: draft.storeId,
              isExpanded: true,
              decoration: InputDecoration(labelText: l.registerStore, border: const OutlineInputBorder()),
              items: stores
                  .map((s) => DropdownMenuItem(value: s.id, child: Text('${s.code} — ${s.name}', overflow: TextOverflow.ellipsis)))
                  .toList(),
              onChanged: (v) {
                draft.storeId = v;
                onChanged();
              },
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    icon: const Icon(Icons.schedule),
                    label: Text('${l.registerStart}: ${hhmm(draft.start)}'),
                    onPressed: () async {
                      final t = await pickTime(draft.start);
                      if (t != null) {
                        draft.start = t;
                        onChanged();
                      }
                    },
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    icon: const Icon(Icons.schedule),
                    label: Text('${l.registerEnd}: ${hhmm(draft.end)}'),
                    onPressed: () async {
                      final t = await pickTime(draft.end);
                      if (t != null) {
                        draft.end = t;
                        onChanged();
                      }
                    },
                  ),
                ),
                if (onRemove != null)
                  IconButton(
                    tooltip: l.commonCancel,
                    icon: const Icon(Icons.delete_outline),
                    onPressed: onRemove,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
