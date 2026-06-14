import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../forms/data/forms_repository.dart';
import '../../../organization/data/organization_repository.dart';
import '../../data/visit_plans_repository.dart';
import '../../domain/visit_plan_models.dart';
import 'visit_plans_list_screen.dart' show formatVisitDate;

/// Leader creates a visit plan (M11, AC-28): pick a date, optional notes, and a
/// list of stores each with a report form. Routed to the BUH for approval.
class VisitPlanCreateScreen extends ConsumerStatefulWidget {
  const VisitPlanCreateScreen({super.key});

  @override
  ConsumerState<VisitPlanCreateScreen> createState() => _VisitPlanCreateScreenState();
}

class _VisitPlanCreateScreenState extends ConsumerState<VisitPlanCreateScreen> {
  final _notesCtl = TextEditingController();
  DateTime _date = DateTime.now().add(const Duration(days: 1));
  final List<VisitItemDraft> _items = [];
  bool _saving = false;

  @override
  void dispose() {
    _notesCtl.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 120)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _addItem() async {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;

    final stores = await ref.read(myStoresProvider.future);
    if (!mounted) return;
    final storeId = await _pickFromSheet(
      title: l.visitPlanPickStore,
      options: stores.map((s) => (id: s.id, label: '${s.code} · ${s.name}')).toList(),
    );
    if (storeId == null || !mounted) return;

    final forms = await ref.read(myFormsProvider.future);
    if (!mounted) return;
    if (forms.isEmpty) {
      showAppToast(context, message: l.visitPlanNoForms, kind: AppToastKind.warning);
      return;
    }
    final formId = await _pickFromSheet(
      title: l.visitPlanPickForm,
      options: forms.map((f) => (id: f.formId, label: f.localizedName(lang))).toList(),
    );
    if (formId == null) return;

    setState(() => _items.add(VisitItemDraft(storeId: storeId, formId: formId)));
  }

  Future<String?> _pickFromSheet({required String title, required List<({String id, String label})> options}) {
    return showAppSheet<String>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
              child: Text(title,
                  style: const TextStyle(color: AppPalette.ink, fontSize: 16, fontWeight: FontWeight.w800)),
            ),
            Flexible(
              child: ListView.builder(
                shrinkWrap: true,
                itemCount: options.length,
                itemBuilder: (_, i) => ListTile(
                  title: Text(options[i].label),
                  onTap: () => Navigator.pop(ctx, options[i].id),
                ),
              ),
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    final l = AppLocalizations.of(context);
    if (_items.isEmpty) {
      showAppToast(context, message: l.visitPlanItemsRequired, kind: AppToastKind.warning);
      return;
    }
    setState(() => _saving = true);
    try {
      await ref.read(visitPlansRepositoryProvider).create(
            visitDate: _date,
            notes: _notesCtl.text.trim().isEmpty ? null : _notesCtl.text.trim(),
            items: _items,
          );
      ref.invalidate(myVisitPlansProvider);
      if (!mounted) return;
      showAppToast(context, message: l.visitPlanCreated, kind: AppToastKind.success);
      context.pop();
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final storesAsync = ref.watch(myStoresProvider);
    final formsAsync = ref.watch(myFormsProvider);

    String storeLabel(String id) =>
        storesAsync.maybeWhen(data: (s) {
          final m = s.where((e) => e.id == id);
          return m.isEmpty ? id : '${m.first.code} · ${m.first.name}';
        }, orElse: () => id);
    String formLabel(String id) =>
        formsAsync.maybeWhen(data: (f) {
          final m = f.where((e) => e.formId == id);
          return m.isEmpty ? id : m.first.localizedName(lang);
        }, orElse: () => id);

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            AppTopBar(title: l.visitPlanNewTitle),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                children: [
                  AppCard(
                    onTap: _pickDate,
                    child: Row(
                      children: [
                        const Icon(Icons.event_rounded, color: AppPalette.indigo),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(l.visitPlanVisitDate, style: const TextStyle(color: AppPalette.muted, fontSize: 12)),
                              const SizedBox(height: 2),
                              Text(formatVisitDate(_date),
                                  style: const TextStyle(color: AppPalette.ink, fontSize: 16, fontWeight: FontWeight.w700)),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right_rounded, color: AppPalette.faint),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    controller: _notesCtl,
                    maxLines: 3,
                    decoration: InputDecoration(
                      labelText: l.visitPlanNotes,
                      hintText: l.visitPlanNotesHint,
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 22),
                  Row(
                    children: [
                      Expanded(
                        child: Text(l.visitPlanItems,
                            style: const TextStyle(color: AppPalette.ink, fontSize: 15, fontWeight: FontWeight.w800)),
                      ),
                      AppButton.soft(label: l.visitPlanAddItem, icon: Icons.add_rounded, expand: false, onPressed: _addItem),
                    ],
                  ),
                  const SizedBox(height: 10),
                  if (_items.isEmpty)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 20),
                      child: Text(l.visitPlanNoItems,
                          textAlign: TextAlign.center, style: const TextStyle(color: AppPalette.muted)),
                    )
                  else
                    ..._items.asMap().entries.map((e) {
                      final i = e.key;
                      final item = e.value;
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: AppCard(
                          child: Row(
                            children: [
                              AppIconTile(icon: Icons.store_rounded, tone: AppTone.sky, size: 40),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(storeLabel(item.storeId),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(color: AppPalette.ink, fontWeight: FontWeight.w700)),
                                    const SizedBox(height: 2),
                                    Text(formLabel(item.formId),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(color: AppPalette.muted, fontSize: 13)),
                                  ],
                                ),
                              ),
                              IconButton(
                                icon: const Icon(Icons.delete_outline_rounded, color: AppPalette.rose),
                                onPressed: () => setState(() => _items.removeAt(i)),
                                tooltip: l.visitPlanRemove,
                              ),
                            ],
                          ),
                        ),
                      );
                    }),
                ],
              ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                child: AppButton.primary(
                  label: l.visitPlanCreate,
                  icon: Icons.send_rounded,
                  loading: _saving,
                  onPressed: _submit,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
