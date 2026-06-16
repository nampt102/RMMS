import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
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
  /// Reserved answer key for the implicit `photo_required` capture shown when a
  /// form enables the rule but has no image/camera field of its own.
  static const _rulePhotoKey = '__rule_photo__';

  final Map<String, dynamic> _answers = {};
  final DateTime _started = DateTime.now();
  String _clientKey = '';
  bool _initStarted = false;
  bool _ready = false;
  bool _submitting = false;
  bool _savingDraft = false;

  bool _hasImageField(FormFill form) =>
      form.fields.any((f) => f.type == 'image_upload' || f.type == 'camera');

  /// True when we must render the standalone photo capture (rule on, no image field).
  bool _needsImplicitPhoto(FormFill form) => form.rules.photoRequired && !_hasImageField(form);

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
    if (_savingDraft) return;
    if (!silent) {
      FocusScope.of(context).unfocus();
      setState(() => _savingDraft = true);
    }
    try {
      await ref.read(formDraftStoreProvider).save(FormDraft(
            formId: form.formId,
            answers: _answers,
            clientKey: _clientKey,
            savedAt: DateTime.now(),
          ));
      if (!silent && mounted) {
        showAppToast(
          context,
          message: AppLocalizations.of(context).formSaved,
          kind: AppToastKind.success,
        );
      }
    } finally {
      if (!silent && mounted) setState(() => _savingDraft = false);
    }
  }

  /// Image/camera field answers carry the uploaded object key — surface them as `attachments`
  /// so the server's `photo_required` rule (which inspects attachments) is satisfied.
  Map<String, dynamic> _collectAttachments(FormFill form) {
    final out = <String, dynamic>{};
    for (final f in form.fields) {
      if (f.type != 'image_upload' && f.type != 'camera') continue;
      final v = _answers[f.id];
      if (v is String && v.trim().isNotEmpty) out[f.id] = v;
    }
    // Implicit photo (photo_required with no image field) lives under a reserved key.
    final rulePhoto = _answers[_rulePhotoKey];
    if (rulePhoto is String && rulePhoto.trim().isNotEmpty) out[_rulePhotoKey] = rulePhoto;
    return out;
  }

  /// Capture the current GPS position for the `gps_required` rule; null if unavailable/denied.
  Future<({double lat, double lng})?> _capturePosition() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) return null;
      var perm = await Geolocator.checkPermission();
      if (perm == LocationPermission.denied) perm = await Geolocator.requestPermission();
      if (perm == LocationPermission.denied || perm == LocationPermission.deniedForever) return null;
      final pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high),
      );
      return (lat: pos.latitude, lng: pos.longitude);
    } catch (_) {
      return null;
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

    // Form-level rules (server enforces too) — surface clear messages before posting.
    final attachments = _collectAttachments(form);
    if (form.rules.photoRequired && attachments.isEmpty) {
      showAppToast(context, message: l.formPhotoRequired, kind: AppToastKind.warning);
      return;
    }

    setState(() => _submitting = true);

    ({double lat, double lng})? pos;
    if (form.rules.gpsRequired) {
      pos = await _capturePosition();
      if (pos == null) {
        if (mounted) {
          showAppToast(context, message: l.formGpsRequired, kind: AppToastKind.error);
          setState(() => _submitting = false);
        }
        return;
      }
    }

    // Persist before the network attempt so an offline failure keeps the data.
    await _saveDraft(form, silent: true);
    try {
      final submissionId = await ref.read(formsRepositoryProvider).submit(
            formId: form.formId,
            answers: _answers,
            attachments: attachments.isEmpty ? null : attachments,
            timeSpentSeconds: DateTime.now().difference(_started).inSeconds,
            clientIdempotencyKey: _clientKey,
            lat: pos?.lat,
            lng: pos?.lng,
          );
      await ref.read(formDraftStoreProvider).delete(form.formId);
      ref.invalidate(myFormsProvider);
      if (!mounted) return;
      showAppToast(context, message: l.formSubmitted, kind: AppToastKind.success);
      // Return the submission id so callers (e.g. a Visit Plan item) can link it.
      context.pop(submissionId);
    } on ApiException catch (e) {
      if (mounted) showAppToast(context, message: e.message, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// Standalone "photo required" capture for forms that enable `photo_required`
  /// but declare no image/camera field. Stored under [_rulePhotoKey].
  Widget _implicitPhotoSection(FormFill form, AppLocalizations l) {
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 4),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppPalette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          RichText(
            text: TextSpan(
              text: l.formRulePhotoLabel,
              style: const TextStyle(color: AppPalette.ink, fontSize: 15, fontWeight: FontWeight.w700),
              children: const [TextSpan(text: ' *', style: TextStyle(color: AppPalette.rose))],
            ),
          ),
          const SizedBox(height: 10),
          FormImageField(
            formId: form.formId,
            value: _answers[_rulePhotoKey] as String?,
            fromCamera: true,
            onChanged: (v) => setState(() => _answers[_rulePhotoKey] = v),
          ),
        ],
      ),
    );
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
                if (_needsImplicitPhoto(form)) _implicitPhotoSection(form, l),
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
                            loading: _savingDraft,
                            onPressed: _savingDraft ? null : () => _saveDraft(form),
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
