import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/requests_repository.dart';

/// Create an OT request (M08): date + start/end time + reason.
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
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  String _hms(TimeOfDay t) => '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}:00';
  String _hhmm(TimeOfDay t) => '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  int _mins(TimeOfDay t) => t.hour * 60 + t.minute;

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    if (_reason.text.trim().isEmpty) {
      messenger.showSnackBar(SnackBar(content: Text(l.requestReasonRequired)));
      return;
    }
    if (_mins(_end) <= _mins(_start)) {
      messenger.showSnackBar(SnackBar(content: Text(l.requestOtTimeInvalid)));
      return;
    }
    setState(() => _saving = true);
    try {
      await ref.read(requestsRepositoryProvider).createOt(
            otDate: _ymd(_date), startTime: _hms(_start), endTime: _hms(_end), reason: _reason.text.trim());
      ref.invalidate(myOtProvider);
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(l.requestCreated)));
      context.pop();
    } on ApiException catch (e) {
      if (mounted) messenger.showSnackBar(SnackBar(content: Text(e.message)));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;

    return Scaffold(
      appBar: AppBar(title: Text(l.requestsAddOt)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.event_outlined),
              title: Text(l.requestFieldDate),
              subtitle: Text(DateFormat.yMMMEd(lang).format(_date)),
              trailing: const Icon(Icons.chevron_right),
              onTap: () async {
                final now = DateTime.now();
                final picked = await showDatePicker(
                  context: context,
                  initialDate: _date,
                  firstDate: DateTime(now.year - 1),
                  lastDate: now.add(const Duration(days: 365)),
                );
                if (picked != null) setState(() => _date = picked);
              },
            ),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.schedule),
                    label: Text('${l.requestFieldStart}: ${_hhmm(_start)}'),
                    onPressed: () async {
                      final t = await showTimePicker(context: context, initialTime: _start);
                      if (t != null) setState(() => _start = t);
                    },
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.schedule),
                    label: Text('${l.requestFieldEnd}: ${_hhmm(_end)}'),
                    onPressed: () async {
                      final t = await showTimePicker(context: context, initialTime: _end);
                      if (t != null) setState(() => _end = t);
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _reason,
              maxLength: 1000,
              maxLines: 3,
              decoration: InputDecoration(labelText: l.requestFieldReason),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.all(16),
        child: FilledButton(
          onPressed: _saving ? null : _submit,
          child: _saving
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(l.requestSubmit),
        ),
      ),
    );
  }
}
