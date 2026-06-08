import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/requests_repository.dart';
import '../widgets/request_form_widgets.dart';

/// Redesign 2026 — Đăng ký OT (pushed).
///
/// 1 date card + 2 time cards + an amber "Số giờ OT" computed card + reason
/// textarea + sticky bottom Gửi đơn.
class OtRequestScreen extends ConsumerStatefulWidget {
  const OtRequestScreen({super.key});

  @override
  ConsumerState<OtRequestScreen> createState() => _OtRequestScreenState();
}

class _OtRequestScreenState extends ConsumerState<OtRequestScreen> {
  DateTime _date = DateTime.now();
  TimeOfDay _start = const TimeOfDay(hour: 18, minute: 0);
  TimeOfDay _end = const TimeOfDay(hour: 20, minute: 0);
  final _reason = TextEditingController();
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _reason.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  String _hms(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}:00';

  String _hhmm(TimeOfDay t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  int _mins(TimeOfDay t) => t.hour * 60 + t.minute;

  double get _hours {
    final d = _mins(_end) - _mins(_start);
    return d <= 0 ? 0 : d / 60.0;
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year - 1),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _pickTime(bool isStart) async {
    final t = await showTimePicker(
      context: context,
      initialTime: isStart ? _start : _end,
    );
    if (t == null) return;
    setState(() {
      if (isStart) {
        _start = t;
      } else {
        _end = t;
      }
    });
  }

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    if (_reason.text.trim().isEmpty) {
      showAppToast(context,
          message: l.requestReasonRequired, kind: AppToastKind.warning);
      return;
    }
    if (_mins(_end) <= _mins(_start)) {
      showAppToast(context,
          message: l.requestOtTimeInvalid, kind: AppToastKind.warning);
      return;
    }
    setState(() => _saving = true);
    try {
      await ref.read(requestsRepositoryProvider).createOt(
            otDate: _ymd(_date),
            startTime: _hms(_start),
            endTime: _hms(_end),
            reason: _reason.text.trim(),
          );
      ref.invalidate(myOtProvider);
      if (!mounted) return;
      showAppToast(context,
          message: l.requestCreated, kind: AppToastKind.success);
      context.pop();
    } on ApiException catch (e) {
      if (mounted) {
        showAppToast(context, message: e.message, kind: AppToastKind.error);
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final hoursText = _hours == _hours.truncateToDouble()
        ? '${_hours.toInt()}.0'
        : _hours.toStringAsFixed(1);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.requestsAddOt),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                children: [
                  DateFieldCard(
                    label: l.requestFieldDate,
                    value: DateFormat.yMMMEd(lang).format(_date),
                    tone: AppTone.indigo,
                    onTap: _pickDate,
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: DateFieldCard(
                          label: l.requestFieldStart,
                          value: _hhmm(_start),
                          tone: AppTone.violet,
                          icon: Icons.schedule_rounded,
                          onTap: () => _pickTime(true),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: DateFieldCard(
                          label: l.requestFieldEnd,
                          value: _hhmm(_end),
                          tone: AppTone.violet,
                          icon: Icons.schedule_rounded,
                          onTap: () => _pickTime(false),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  _HoursCard(
                    label: l.requestOtHours,
                    valueText: l.requestOtHoursValue(hoursText),
                  ),
                  const SizedBox(height: 22),
                  Text(
                    l.requestFieldReason,
                    style: GoogleFonts.spaceGrotesk(
                      color: AppPalette.ink,
                      fontSize: 17,
                      fontWeight: FontWeight.w800,
                      letterSpacing: -0.3,
                    ),
                  ),
                  const SizedBox(height: 10),
                  ReasonField(
                    controller: _reason,
                    hint: l.requestOtPlaceholder,
                    maxLength: 1000,
                  ),
                ],
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
            label: l.requestSubmit,
            icon: Icons.send_rounded,
            loading: _saving,
            onPressed: _submit,
          ),
        ),
      ),
    );
  }
}

class _HoursCard extends StatelessWidget {
  const _HoursCard({required this.label, required this.valueText});
  final String label;
  final String valueText;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      child: Row(
        children: [
          const AppIconTile(
            icon: Icons.timelapse_rounded,
            tone: AppTone.amber,
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
                  label,
                  style: GoogleFonts.plusJakartaSans(
                    color: AppPalette.muted,
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  valueText,
                  style: GoogleFonts.spaceGrotesk(
                    color: AppPalette.chipAmberFg,
                    fontSize: 22,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.4,
                    fontFeatures: const [FontFeature.tabularFigures()],
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
