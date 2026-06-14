import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_palette.dart';
import '../../../../core/utils/app_uuid.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../data/form_draft_store.dart';
import '../../data/forms_repository.dart';
import '../../domain/form_models.dart';
import '../widgets/dynamic_field.dart';

/// Fill + submit a form (M10, AC-22/23). Renders the schema dynamically, restores
/// any offline draft, and submits with a stable client key (server dedups retries).
class FormFillScreen extends ConsumerStatefulWidget {
  const FormFillScreen({super.key, required this.formId});

  final String formId;

  @override
  ConsumerState<FormFillScreen> createState() => _FormFillScreenState();
}

class _FormFillScreenState extends ConsumerState<FormFillScreen> {
  final Map<String, dynamic> _answers = {};
  final DateTime _started = DateTime.now();
  String _clientKey = '';
  bool _initStarted = false;
  bool _ready = false;
  bool _submitting = false;

  Future<void> _init(FormFill form) async {
    final store = ref.read(formDraftStoreProvider);
    final draft = await store.load(form.formId);
    if (draft != null) {
      _answers.addAll(draft.answers);
      _clientKey = draft.clientKey;
    } else {
      _clientKey = generateUuidV4();
    }
    if (mounted) setState(() => _ready = true);
  }

  bool _isEmpty(Object? v) {
    if (v == null) return true;
    if (v is String) return v.trim().isEmpty;
    if (v is List) return v.isEmpty;
    return false;
  }

  Future<void> _saveDraft(FormFill form, {bool silent = false}) async {
    await ref.read(formDraftStoreProvider).save(FormDraft(
          formId: form.formId,
          answers: _answers,
          clientKey: _clientKey,
          savedAt: DateTime.now(),
        ));
    if (!silent && mounted) {
      showAppToast(context, message: AppLocalizations.of(context).formSaved, kind: AppToastKind.success);
    }
  }

  Future<void> _submit(FormFill form) async {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;

    // Client-side required check (server re-validates).
    for (final f in form.fields) {
      if (f.isSection || !f.required) continue;
      if (_isEmpty(_answers[f.id])) {
        showAppToast(context, message: l.formRequiredMissing(f.label(lang)), kind: AppToastKind.warning);
        return;
      }
    }

    setState(() => _submitting = true);
    // Persist before the network attempt so an offline failure keeps the data.
    await _saveDraft(form, silent: true);
    try {
      await ref.read(formsRepositoryProvider).submit(
            formId: form.formId,
            answers: _answers,
            timeSpentSeconds: DateTime.now().difference(_started).inSeconds,
            clientIdempotencyKey: _clientKey,
          );
      await ref.read(formDraftStoreProvider).delete(form.formId);
      ref.invalidate(myFormsProvider);
      if (!mounted) return;
      showAppToast(context, message: l.formSubmitted, kind: AppToastKind.success);
      context.pop();
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final lang = Localizations.localeOf(context).languageCode;
    final async = ref.watch(formFillProvider(widget.formId));

    return Scaffold(
      backgroundColor: AppPalette.bg,
      body: SafeArea(
        bottom: false,
        child: async.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => Center(child: Text(l.commonError)),
          data: (form) {
            if (!_initStarted) {
              _initStarted = true; // guard synchronously so we schedule once
              WidgetsBinding.instance.addPostFrameCallback((_) => _init(form));
            }
            if (!_ready) return const Center(child: CircularProgressIndicator());

            final inputFields = form.fields;
            return Column(
              children: [
                AppTopBar(title: form.localizedName(lang)),
                Expanded(
                  child: inputFields.isEmpty
                      ? Center(child: Text(l.formNoFields, style: const TextStyle(color: AppPalette.muted)))
                      : ListView.separated(
                          padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                          itemCount: inputFields.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 18),
                          itemBuilder: (context, i) {
                            final f = inputFields[i];
                            return DynamicField(
                              key: ValueKey(f.id),
                              field: f,
                              formId: form.formId,
                              lang: lang,
                              value: _answers[f.id],
                              onChanged: (v) => setState(() => _answers[f.id] = v),
                            );
                          },
                        ),
                ),
                SafeArea(
                  top: false,
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                    child: Row(
                      children: [
                        Expanded(
                          child: AppButton.soft(
                            label: l.formSaveDraft,
                            icon: Icons.save_rounded,
                            onPressed: () => _saveDraft(form),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: AppButton.primary(
                            label: l.formFillSubmit,
                            icon: Icons.send_rounded,
                            loading: _submitting,
                            onPressed: () => _submit(form),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}
