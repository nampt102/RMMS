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

/// Redesign 2026 — Xin nghỉ phép (pushed).
class LeaveRequestScreen extends ConsumerStatefulWidget {
  const LeaveRequestScreen({super.key});

  @override
  ConsumerState<LeaveRequestScreen> createState() =>
      _LeaveRequestScreenState();
}

class _LeaveRequestScreenState extends ConsumerState<LeaveRequestScreen> {
  DateTime _start = DateTime.now();
  DateTime _end = DateTime.now();
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

  Future<void> _pick(bool isStart) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: isStart ? _start : _end,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked == null) return;
    setState(() {
      if (isStart) {
        _start = picked;
        if (_end.isBefore(_start)) _end = _start;
      } else {
        _end = picked.isBefore(_start) ? _start : picked;
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
    setState(() => _saving = true);
    try {
      await ref.read(requestsRepositoryProvider).createLeave(
            startDate: _ymd(_start),
            endDate: _ymd(_end),
            reason: _reason.text.trim(),
          );
      ref.invalidate(myLeaveProvider);
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
    final fmt = DateFormat.yMMMEd(lang);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.requestsAddLeave),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                children: [
                  DateFieldCard(
                    label: l.requestFieldStartDate,
                    value: fmt.format(_start),
                    tone: AppTone.indigo,
                    onTap: () => _pick(true),
                  ),
                  const SizedBox(height: 12),
                  DateFieldCard(
                    label: l.requestFieldEndDate,
                    value: fmt.format(_end),
                    tone: AppTone.violet,
                    onTap: () => _pick(false),
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
                    hint: l.requestLeavePlaceholder,
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
