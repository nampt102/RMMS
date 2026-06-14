import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:hive/hive.dart';

import '../../../core/utils/app_uuid.dart';

/// A locally-saved form draft (M10 offline draft, AC-23 / BR-504). Stored in Hive
/// as a JSON string so no Hive adapter/codegen is needed. The [clientKey] is
/// generated once per draft and reused on every submit so the server dedups
/// offline retries (idempotency).
class FormDraft {
  const FormDraft({
    required this.formId,
    required this.answers,
    required this.clientKey,
    required this.savedAt,
  });

  final String formId;
  final Map<String, dynamic> answers;
  final String clientKey;
  final DateTime savedAt;

  Map<String, dynamic> toJson() => {
        'formId': formId,
        'answers': answers,
        'clientKey': clientKey,
        'savedAt': savedAt.toIso8601String(),
      };

  factory FormDraft.fromJson(Map<String, dynamic> j) => FormDraft(
        formId: j['formId'] as String,
        answers: (j['answers'] as Map?)?.cast<String, dynamic>() ?? <String, dynamic>{},
        clientKey: j['clientKey'] as String? ?? generateUuidV4(),
        savedAt: DateTime.tryParse(j['savedAt'] as String? ?? '') ?? DateTime.now(),
      );
}

/// Hive-backed store for form drafts. Box is opened lazily.
class FormDraftStore {
  static const _boxName = 'form_drafts';

  Future<Box<String>> _box() async =>
      Hive.isBoxOpen(_boxName) ? Hive.box<String>(_boxName) : await Hive.openBox<String>(_boxName);

  Future<FormDraft?> load(String formId) async {
    final box = await _box();
    final raw = box.get(formId);
    if (raw == null) return null;
    try {
      return FormDraft.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  Future<void> save(FormDraft draft) async {
    final box = await _box();
    await box.put(draft.formId, jsonEncode(draft.toJson()));
  }

  Future<void> delete(String formId) async {
    final box = await _box();
    await box.delete(formId);
  }
}

final formDraftStoreProvider = Provider<FormDraftStore>((ref) => FormDraftStore());
