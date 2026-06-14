import 'dart:convert';

/// Plain (non-Freezed) models for the dynamic Form Engine (M10). The schema is
/// dynamic JSON, so hand-written parsing is clearer and avoids codegen for the
/// open-ended field shape. DTOs mirror `Rmms.Application.Forms.*`.

/// One assigned form in the filler's list (GET /forms/me).
class AssignedForm {
  const AssignedForm({
    required this.formId,
    required this.code,
    required this.nameVi,
    required this.nameEn,
    required this.formType,
    required this.version,
    this.validTo,
  });

  final String formId;
  final String code;
  final String nameVi;
  final String nameEn;
  final String formType;
  final int version;
  final DateTime? validTo;

  String localizedName(String lang) => lang == 'en' ? nameEn : nameVi;

  factory AssignedForm.fromJson(Map<String, dynamic> j) => AssignedForm(
        formId: j['formId'] as String,
        code: j['code'] as String? ?? '',
        nameVi: j['nameVi'] as String? ?? '',
        nameEn: j['nameEn'] as String? ?? '',
        formType: j['formType'] as String? ?? '',
        version: (j['version'] as num?)?.toInt() ?? 0,
        validTo: j['validTo'] == null ? null : DateTime.tryParse(j['validTo'] as String),
      );
}

/// A form ready to render (GET /forms/:id) — meta + parsed schema.
class FormFill {
  const FormFill({
    required this.formId,
    required this.code,
    required this.nameVi,
    required this.nameEn,
    required this.formType,
    required this.version,
    required this.fields,
  });

  final String formId;
  final String code;
  final String nameVi;
  final String nameEn;
  final String formType;
  final int version;
  final List<FieldDef> fields;

  String localizedName(String lang) => lang == 'en' ? nameEn : nameVi;

  factory FormFill.fromJson(Map<String, dynamic> j) {
    final schemaRaw = j['schema'];
    final schema = _decodeSchema(schemaRaw);
    final rawFields = (schema['fields'] as List?) ?? const [];
    return FormFill(
      formId: j['formId'] as String,
      code: j['code'] as String? ?? '',
      nameVi: j['nameVi'] as String? ?? '',
      nameEn: j['nameEn'] as String? ?? '',
      formType: j['formType'] as String? ?? '',
      version: (j['version'] as num?)?.toInt() ?? 0,
      fields: rawFields
          .whereType<Map<String, dynamic>>()
          .map(FieldDef.fromJson)
          .toList(growable: false),
    );
  }

  // `schema` arrives as a JSON string (the server stores raw JSONB string),
  // or already-decoded as a Map depending on the client.
  static Map<String, dynamic> _decodeSchema(Object? raw) {
    if (raw is Map<String, dynamic>) return raw;
    if (raw is String && raw.trim().isNotEmpty) {
      try {
        final decoded = jsonDecode(raw);
        if (decoded is Map<String, dynamic>) return decoded;
      } catch (_) {
        // fall through to empty schema
      }
    }
    return const {'fields': []};
  }
}

/// Lightweight product for the product/SKU selector field (GET /products).
class ProductLite {
  const ProductLite({required this.id, required this.sku, required this.name, this.brand});

  final String id;
  final String sku;
  final String name;
  final String? brand;

  String get pickerLabel => brand == null || brand!.isEmpty ? '$sku · $name' : '$sku · $name ($brand)';

  factory ProductLite.fromJson(Map<String, dynamic> j) => ProductLite(
        id: j['id'] as String,
        sku: j['sku'] as String? ?? '',
        name: j['name'] as String? ?? '',
        brand: j['brand'] as String?,
      );
}

class FieldOption {
  const FieldOption({required this.value, required this.labelVi, required this.labelEn});

  final String value;
  final String labelVi;
  final String labelEn;

  String label(String lang) => lang == 'en' ? labelEn : labelVi;

  factory FieldOption.fromJson(Map<String, dynamic> j) => FieldOption(
        value: (j['value'] ?? '').toString(),
        labelVi: (j['label_vi'] ?? j['value'] ?? '').toString(),
        labelEn: (j['label_en'] ?? j['value'] ?? '').toString(),
      );
}

class FieldDef {
  const FieldDef({
    required this.id,
    required this.type,
    required this.labelVi,
    required this.labelEn,
    required this.required,
    this.options = const [],
  });

  final String id;
  final String type;
  final String labelVi;
  final String labelEn;
  final bool required;
  final List<FieldOption> options;

  String label(String lang) => (lang == 'en' ? labelEn : labelVi).isNotEmpty
      ? (lang == 'en' ? labelEn : labelVi)
      : id;

  bool get isSection => type == 'section';

  factory FieldDef.fromJson(Map<String, dynamic> j) => FieldDef(
        id: (j['id'] ?? '').toString(),
        type: (j['type'] ?? 'text').toString(),
        labelVi: (j['label_vi'] ?? '').toString(),
        labelEn: (j['label_en'] ?? '').toString(),
        required: j['required'] == true,
        options: ((j['options'] as List?) ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(FieldOption.fromJson)
            .toList(growable: false),
      );
}
