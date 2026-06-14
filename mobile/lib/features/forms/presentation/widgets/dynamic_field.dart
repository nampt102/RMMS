import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../../../core/theme/app_palette.dart';
import '../../domain/form_models.dart';

/// Renders ONE schema field as the right input widget (factory pattern, M10
/// design doc §7). Common types are supported; media/entity-picker types show a
/// safe placeholder (deferred — needs camera/MinIO + product/store fetch).
class DynamicField extends StatefulWidget {
  const DynamicField({
    super.key,
    required this.field,
    required this.lang,
    required this.value,
    required this.onChanged,
  });

  final FieldDef field;
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
      default:
        // image_upload / camera / file / product_selector / store_selector / brand_sku_selector
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
