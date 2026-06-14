import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_palette.dart';
import '../../../../core/widgets/app_widgets.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../../organization/data/organization_repository.dart';
import '../../data/forms_repository.dart';
import '../../domain/form_models.dart';

/// Renders ONE schema field as the right input widget (factory pattern, M10
/// design doc §7). Common types are supported; media/entity-picker types show a
/// safe placeholder (deferred — needs camera/MinIO + product/store fetch).
class DynamicField extends StatefulWidget {
  const DynamicField({
    super.key,
    required this.field,
    required this.formId,
    required this.lang,
    required this.value,
    required this.onChanged,
  });

  final FieldDef field;
  final String formId;
  final String lang;
  final Object? value;
  final ValueChanged<Object?> onChanged;

  @override
  State<DynamicField> createState() => _DynamicFieldState();
}

class _DynamicFieldState extends State<DynamicField> {
  TextEditingController? _text;

  @override
  void initState() {
    super.initState();
    final t = widget.field.type;
    if (t == 'text' || t == 'number') {
      _text = TextEditingController(text: widget.value?.toString() ?? '');
    }
  }

  @override
  void dispose() {
    _text?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final f = widget.field;
    if (f.isSection) {
      return Padding(
        padding: const EdgeInsets.only(top: 8, bottom: 4),
        child: Text(
          f.label(widget.lang),
          style: GoogleFonts.spaceGrotesk(
            color: AppPalette.ink,
            fontSize: 18,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.3,
          ),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _label(f),
        const SizedBox(height: 8),
        _input(f),
      ],
    );
  }

  Widget _label(FieldDef f) => RichText(
        text: TextSpan(
          text: f.label(widget.lang),
          style: GoogleFonts.plusJakartaSans(
            color: AppPalette.ink,
            fontSize: 15,
            fontWeight: FontWeight.w700,
          ),
          children: [
            if (f.required)
              const TextSpan(text: ' *', style: TextStyle(color: AppPalette.rose)),
          ],
        ),
      );

  Widget _input(FieldDef f) {
    switch (f.type) {
      case 'text':
        return TextField(
          controller: _text,
          maxLines: null,
          decoration: _decoration(),
          onChanged: widget.onChanged,
        );
      case 'number':
        return TextField(
          controller: _text,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.\-]'))],
          decoration: _decoration(),
          onChanged: (v) => widget.onChanged(v),
        );
      case 'single_choice':
      case 'dropdown':
        return _choiceList(f, multi: false);
      case 'multi_choice':
        return _choiceList(f, multi: true);
      case 'datetime':
        final current = widget.value as String?;
        return InkWell(
          onTap: () async {
            final now = DateTime.now();
            final picked = await showDatePicker(
              context: context,
              initialDate: current != null ? DateTime.tryParse(current) ?? now : now,
              firstDate: DateTime(now.year - 2),
              lastDate: DateTime(now.year + 2),
            );
            if (picked != null) {
              widget.onChanged(DateFormat('yyyy-MM-dd').format(picked));
            }
          },
          child: InputDecorator(
            decoration: _decoration(),
            child: Text(current ?? '—'),
          ),
        );
      case 'product_selector':
      case 'brand_sku_selector':
        return _entityField(productMode: true);
      case 'store_selector':
        return _entityField(productMode: false);
      case 'image_upload':
      case 'camera':
        return _ImageField(
          formId: widget.formId,
          value: widget.value as String?,
          fromCamera: f.type == 'camera',
          onChanged: widget.onChanged,
        );
      default:
        // file (arbitrary files) — needs a file picker package; deferred.
        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppPalette.bg,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppPalette.line),
          ),
          child: Text(
            '“${f.type}” — chưa hỗ trợ ở phiên bản này',
            style: const TextStyle(color: AppPalette.muted, fontSize: 13),
          ),
        );
    }
  }

  // Single/multi select rendered from stable primitives (no Radio/Checkbox API).
  Widget _choiceList(FieldDef f, {required bool multi}) {
    final selected = multi
        ? ((widget.value as List?)?.cast<String>() ?? const <String>[])
        : <String>[if (widget.value is String) widget.value as String];

    void toggle(String value) {
      if (multi) {
        final next = List<String>.from(selected);
        next.contains(value) ? next.remove(value) : next.add(value);
        widget.onChanged(next);
      } else {
        widget.onChanged(value);
      }
    }

    return Column(
      children: f.options.map((o) {
        final on = selected.contains(o.value);
        return Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: () => toggle(o.value),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
              decoration: BoxDecoration(
                color: on ? AppPalette.violet.withValues(alpha: 0.08) : Colors.white,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: on ? AppPalette.violet : AppPalette.line),
              ),
              child: Row(
                children: [
                  Icon(
                    multi
                        ? (on ? Icons.check_box_rounded : Icons.check_box_outline_blank_rounded)
                        : (on ? Icons.radio_button_checked_rounded : Icons.radio_button_unchecked_rounded),
                    size: 22,
                    color: on ? AppPalette.violet : AppPalette.faint,
                  ),
                  const SizedBox(width: 10),
                  Expanded(child: Text(o.label(widget.lang))),
                ],
              ),
            ),
          ),
        );
      }).toList(growable: false),
    );
  }

  // Product / SKU / store pickers — async data via Riverpod, single-select via bottom sheet.
  Widget _entityField({required bool productMode}) {
    return Consumer(builder: (context, ref, _) {
      Widget loading() => const Padding(
            padding: EdgeInsets.symmetric(vertical: 10),
            child: LinearProgressIndicator(minHeight: 3),
          );
      Widget error() => Text(AppLocalizations.of(context).commonError, style: const TextStyle(color: AppPalette.muted));

      if (productMode) {
        return ref.watch(formProductsProvider).when(
              loading: loading,
              error: (_, __) => error(),
              data: (list) => _entityTile(context, [for (final p in list) (id: p.id, label: p.pickerLabel)]),
            );
      }
      return ref.watch(myStoresProvider).when(
            loading: loading,
            error: (_, __) => error(),
            data: (list) => _entityTile(context, [for (final s in list) (id: s.id, label: '${s.code} · ${s.name}')]),
          );
    });
  }

  Widget _entityTile(BuildContext context, List<({String id, String label})> items) {
    final current = widget.value as String?;
    String? selectedLabel;
    for (final e in items) {
      if (e.id == current) {
        selectedLabel = e.label;
        break;
      }
    }
    selectedLabel ??= current; // fall back to the raw id if not in the list
    return InkWell(
      onTap: () => _openEntitySheet(context, items),
      child: InputDecorator(
        decoration: _decoration(),
        child: Row(
          children: [
            Expanded(
              child: Text(
                selectedLabel ?? AppLocalizations.of(context).formSelectHint,
                style: TextStyle(color: selectedLabel == null ? AppPalette.muted : AppPalette.ink),
              ),
            ),
            const Icon(Icons.arrow_drop_down_rounded, color: AppPalette.faint),
          ],
        ),
      ),
    );
  }

  void _openEntitySheet(BuildContext context, List<({String id, String label})> items) {
    final l = AppLocalizations.of(context);
    showAppSheet<void>(
      context: context,
      builder: (ctx) {
        var query = '';
        return StatefulBuilder(
          builder: (ctx, setSheet) {
            final q = query.trim().toLowerCase();
            final filtered = q.isEmpty ? items : items.where((e) => e.label.toLowerCase().contains(q)).toList();
            return Padding(
              padding: EdgeInsets.only(bottom: MediaQuery.of(ctx).viewInsets.bottom),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                    child: TextField(
                      decoration: InputDecoration(
                        prefixIcon: const Icon(Icons.search_rounded),
                        hintText: l.formSearchHint,
                        border: const OutlineInputBorder(),
                      ),
                      onChanged: (v) => setSheet(() => query = v),
                    ),
                  ),
                  Flexible(
                    child: ListView.builder(
                      shrinkWrap: true,
                      itemCount: filtered.length,
                      itemBuilder: (_, i) {
                        final e = filtered[i];
                        return ListTile(
                          title: Text(e.label),
                          trailing: e.id == widget.value ? const Icon(Icons.check_rounded, color: AppPalette.violet) : null,
                          onTap: () {
                            widget.onChanged(e.id);
                            Navigator.of(ctx).pop();
                          },
                        );
                      },
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
              ),
            );
          },
        );
      },
    );
  }

  InputDecoration _decoration() => InputDecoration(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppPalette.line),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppPalette.line),
        ),
      );
}

/// Image field (image_upload / camera): pick or capture → multipart upload →
/// store the returned object key in the answer. Preview url is session-local.
class _ImageField extends ConsumerStatefulWidget {
  const _ImageField({
    required this.formId,
    required this.value,
    required this.fromCamera,
    required this.onChanged,
  });

  final String formId;
  final String? value; // stored object key
  final bool fromCamera;
  final ValueChanged<Object?> onChanged;

  @override
  ConsumerState<_ImageField> createState() => _ImageFieldState();
}

class _ImageFieldState extends ConsumerState<_ImageField> {
  bool _busy = false;
  String? _url; // session-local preview

  Future<void> _pick() async {
    final l = AppLocalizations.of(context);
    final picker = ImagePicker();
    final XFile? x = await picker.pickImage(
      source: widget.fromCamera ? ImageSource.camera : ImageSource.gallery,
      imageQuality: 70,
      maxWidth: 1600,
    );
    if (x == null) return;
    setState(() => _busy = true);
    try {
      final r = await ref.read(formsRepositoryProvider).uploadAttachment(widget.formId, x.path);
      widget.onChanged(r.objectKey);
      if (mounted) setState(() => _url = r.url);
    } catch (_) {
      if (mounted) showAppToast(context, message: l.commonError, kind: AppToastKind.error);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context);
    final hasValue = widget.value != null && widget.value!.isNotEmpty;

    if (hasValue) {
      return Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: _url != null
                ? Image.network(_url!, width: 56, height: 56, fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => _attachedBox())
                : _attachedBox(),
          ),
          const SizedBox(width: 10),
          Expanded(child: Text(l.formAttached, style: const TextStyle(color: AppPalette.muted))),
          TextButton(onPressed: _busy ? null : _pick, child: Text(l.formReplaceImage)),
          IconButton(
            onPressed: () {
              widget.onChanged(null);
              setState(() => _url = null);
            },
            icon: const Icon(Icons.delete_outline_rounded, color: AppPalette.rose),
          ),
        ],
      );
    }

    return AppButton.soft(
      label: widget.fromCamera ? l.formTakePhoto : l.formPickImage,
      icon: widget.fromCamera ? Icons.photo_camera_rounded : Icons.image_rounded,
      loading: _busy,
      expand: false,
      onPressed: _pick,
    );
  }

  Widget _attachedBox() => Container(
        width: 56,
        height: 56,
        color: AppPalette.bg,
        child: const Icon(Icons.attach_file_rounded, color: AppPalette.muted),
      );
}
