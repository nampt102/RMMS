/** Mirrors `Rmms.Application.Forms.*` DTOs + the JSONB schema contract (M10 design doc §2). */

export type FormStatus = "draft" | "published" | "archived";

export const FORM_TYPES = [
  "stock_report",
  "market_report",
  "photo_report",
  "pc_checklist",
  "free_report",
  "survey",
  "knowledge_test",
  "training",
  "visit_report",
] as const;
export type FormTypeKey = (typeof FORM_TYPES)[number];

/** Input-type registry (must match server FormSchema.KnownFieldTypes / ADR-016). */
export const FIELD_TYPES = [
  "text",
  "number",
  "single_choice",
  "multi_choice",
  "dropdown",
  "datetime",
  "image_upload",
  "camera",
  "file",
  "product_selector",
  "store_selector",
  "brand_sku_selector",
  "section",
] as const;
export type FieldTypeKey = (typeof FIELD_TYPES)[number];

export const CHOICE_TYPES: FieldTypeKey[] = ["single_choice", "multi_choice", "dropdown"];

export type FieldOption = { value: string; label_vi: string; label_en: string };

export type FieldDef = {
  id: string;
  type: FieldTypeKey;
  label_vi: string;
  label_en: string;
  required?: boolean;
  options?: FieldOption[];
  // passthrough for type-specific extras (max_length, min, max, ...) — preserved on round-trip
  [key: string]: unknown;
};

export type FormRules = {
  target_users?: string[];
  require_check_in?: boolean;
  gps_required?: boolean;
  photo_required?: boolean;
  scored?: boolean;
  allow_edit_after_submit?: boolean;
  allow_offline_draft?: boolean;
  [key: string]: unknown;
};

export type FormSchema = { fields: FieldDef[]; rules: FormRules };

export type FormSummary = {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  formType: FormTypeKey;
  status: FormStatus;
  currentVersion: number;
  hasDraft: boolean;
  createdAt: string;
};

export type FormDetail = {
  id: string;
  code: string;
  nameVi: string;
  nameEn: string;
  descriptionVi: string | null;
  descriptionEn: string | null;
  formType: FormTypeKey;
  status: FormStatus;
  currentVersion: number;
  editableVersion: number;
  hasDraft: boolean;
  schema: string; // raw JSON
};

export type FormVersionInfo = {
  version: number;
  isPublished: boolean;
  publishedAt: string | null;
  createdAt: string;
};

export type CreateFormPayload = {
  code: string;
  nameVi: string;
  nameEn: string;
  descriptionVi?: string;
  descriptionEn?: string;
  formType: string;
  schema: string;
};

export type UpdateFormPayload = {
  nameVi: string;
  nameEn: string;
  descriptionVi?: string;
  descriptionEn?: string;
  schema: string;
};

export const EMPTY_SCHEMA: FormSchema = { fields: [], rules: { allow_offline_draft: true } };

/** Safe-parse a raw schema string into a FormSchema (tolerates legacy/empty). */
export function parseSchema(raw: string | null | undefined): FormSchema {
  if (!raw) return { fields: [], rules: {} };
  try {
    const obj = JSON.parse(raw) as Partial<FormSchema>;
    return { fields: Array.isArray(obj.fields) ? obj.fields : [], rules: obj.rules ?? {} };
  } catch {
    return { fields: [], rules: {} };
  }
}
