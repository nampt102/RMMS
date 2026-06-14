# M10 Form Engine — Engineering Design

> Thiết kế kỹ thuật cho Form Engine (M10, Phase 1B, Sprints 11–14). Bổ sung chi tiết triển khai trên nền [M10-form-engine.md](./M10-form-engine.md) (spec) + [04-data-model.md §Form Engine](../04-data-model.md) (entities) + BR-501..507.
>
> **Cập nhật:** 2026-06-14 · **Quyết định kiến trúc:** ADR-016 (engine), ADR-014 (@dnd-kit), ADR-015 (Recharts).

---

## 1. Mục tiêu & nguyên tắc

**Mục tiêu:** Admin tự định nghĩa form bằng dữ liệu (JSONB), không cần code/migration; mobile render động; submission **bất biến theo version**; hỗ trợ offline + scoring. Một engine phục vụ mọi `form_type` (stock/market/photo/PC checklist/survey/knowledge_test/training/visit_report/free_report).

**Nguyên tắc thiết kế:**
1. **Schema-driven, không cột cứng** — toàn bộ định nghĩa field + rules nằm trong `form_versions.schema` (JSONB). Thêm loại form mới = thêm dữ liệu, không migration.
2. **Version bất biến (BR-505)** — submission FK tới `form_version_id`; sửa form đã publish → version mới, version cũ giữ nguyên.
3. **Một nguồn schema, hai consumer** — web Builder ghi schema; mobile renderer + BE validator đọc cùng một contract. Không định nghĩa trùng.
4. **Mượn convention đã kiểm chứng** — đặt tên/cấu trúc field theo SurveyJS/JSON-Schema (`type`, `name/id`, `title`, `validators[]`, `visibleIf`), không kéo lib ngoài (Flutter + AntD đã chốt).

---

## 2. Field schema contract (input-type registry)

Mỗi field trong `schema.fields[]` tuân theo contract chung; `type` quyết định thuộc tính bổ sung. **Registry** là nguồn sự thật cho 3 phía (Builder / Renderer / Validator).

### 2.1. Field cơ bản (mọi type)
```jsonc
{
  "id": "q1",                  // ổn định, KHÔNG đổi khi sửa form (key trong answers)
  "type": "text",              // khóa registry
  "label_vi": "Họ tên",
  "label_en": "Full name",
  "hint_vi": null, "hint_en": null,
  "required": true,
  "visible_if": null,          // biểu thức điều kiện (xem §2.3)
  "validators": []             // (xem §2.4)
}
```

### 2.2. Bảng input types (12+)
| `type` | Thuộc tính riêng | Giá trị lưu trong `answers` |
|---|---|---|
| `text` | `max_length`, `multiline` | string |
| `number` | `min`, `max`, `decimals` | number |
| `single_choice` | `options[]` (`{value,label_vi,label_en}`), `correct` (nếu scored) | value |
| `multi_choice` | `options[]`, `min_select`, `max_select`, `correct[]` | value[] |
| `dropdown` | `options[]` | value |
| `datetime` | `mode` (`date`/`time`/`datetime`), `min`, `max` | ISO-8601 |
| `image_upload` / `camera` | `max_files`, `max_mb` | attachment ref[] (xem §6) |
| `file` | `accept[]`, `max_mb` | attachment ref[] |
| `product_selector` | `filter` (`{category_id?}`), `multiple` | product_id[] (M04) |
| `store_selector` | `scope` (`assigned`/`all`) | store_id |
| `brand_sku_selector` | `filter` | sku_id[] |
| `section` (layout) | `title_vi/en` | — (không phải input) |

> Loại mới (vd `rating`, `signature`) = thêm 1 entry registry + 1 widget mobile + 1 control web, **không đụng schema DB**.

### 2.3. Conditional logic (`visible_if`) — mượn SurveyJS
Biểu thức boolean tham chiếu field khác theo `id`, vd `"{q2} == 'yes'"`. Đánh giá ở **cả** mobile (ẩn/hiện realtime) **và** BE (field ẩn → bỏ qua validate `required`). Phase 1: hỗ trợ toán tử `== != > < >= <= && ||` + `in`. Mở rộng `enable_if`/`required_if` về sau nếu cần.

### 2.4. Validators
Mảng khai báo, đánh giá cả client + server (server là nguồn cuối):
```jsonc
"validators": [
  {"type":"required","message_vi":"Bắt buộc","message_en":"Required"},
  {"type":"regex","pattern":"^[0-9]{10}$","message_vi":"..."},
  {"type":"range","min":0,"max":100},
  {"type":"max_length","value":255}
]
```

---

## 3. Versioning (BR-505, AC-21)

```
forms (1) ──< form_versions (N)         forms.current_version → version đang publish
   │
   └──< form_submissions  ── form_version_id (snapshot bất biến)
```

- **Tạo/sửa khi `status=draft`** → ghi đè schema của version draft hiện tại (chưa có submission).
- **Publish** → `form_versions.published_at` set, `forms.current_version = version`, `forms.status=published`.
- **Sửa form ĐÃ publish** → tạo `form_versions` mới `version = current+1` (status form quay về có draft mới chờ publish); version cũ **giữ nguyên**. Submission đang/đã làm trên version cũ không đổi (AC-21).
- **Mobile đang điền** mà có version mới publish → chỉ thấy version mới sau khi refresh (Edge case spec). Draft offline gắn `form_version_id` lúc bắt đầu.

---

## 4. Assignment resolution (OR logic)

`form_assignments`: mỗi row = 1 luật targeting; nhiều row = **OR**. Query "form của tôi" (`GET /forms/me`) gom các form có ≥1 assignment khớp viewer:
- `assigned_to_role == viewer.role`, hoặc
- `assigned_to_user_id == viewer.id`, hoặc
- `assigned_to_store_id ∈ stores viewer được phân công` (M03), hoặc area/category/product tương ứng,
- **và** `now ∈ [valid_from, valid_to]` (hoặc rule `always_on`).

Assignment trỏ `form_id` (không phải version) → luôn lấy `current_version`. Index: `form_assignments(assigned_to_role)`, `(assigned_to_user_id)`, `(assigned_to_store_id)`; cân nhắc materialized view nếu chậm ở scale.

---

## 5. Server-side validation

Khi submit: BE **không tin client**. Pipeline:
1. Lấy `schema` của `form_version_id`.
2. Bỏ field có `visible_if` = false.
3. Với mỗi field còn lại: chạy `required` + `validators[]` + ràng buộc theo `type` (range, max_length, option hợp lệ, số file/ảnh…).
4. Ràng buộc rule cấp form: `store_required`, `gps_required`, `photo_required`, `require_check_in`, `time_limit_minutes` (auto-submit nếu quá).

**Cách triển khai:** validator tự viết theo registry (mỗi `type` 1 validator class, Strategy pattern) thay vì JSON-Schema generic — vì ta có toán tử i18n + product/store reference cần tra DB. (Spec gợi ý NJsonSchema; ta chọn validator theo registry để kiểm tra được reference + i18n — ghi trong ADR-016.)

---

## 6. Attachment (ảnh/file) — tái dùng MinIO

1. Mobile xin **presigned PUT URL**: `POST /forms/:id/attachments/presign` → `{url, objectKey}`.
2. Mobile PUT trực tiếp lên MinIO (không qua API).
3. Submit gửi `attachments: { "q5": [objectKey...] }`; BE verify objectKey tồn tại + thuộc user trước khi lưu.
4. Reuse `IAttachmentStorage`/presign đã có ở M13/M05 (MinIO). Retention theo rule form nếu cần.

---

## 7. Mobile renderer động (factory pattern)

```
FormRenderer(schema)
  └─ for field in schema.fields (đã lọc visible_if)
       └─ FieldWidgetFactory.create(field.type) → 1 widget/loại
            (TextField, NumberField, SingleChoice, ProductSelector, CameraField, …)
```
- 1 `FormController` (Riverpod) giữ `answers` map + validate client realtime.
- Mỗi widget tự render label vi/en theo locale + đẩy giá trị vào controller theo `field.id`.
- Loại chưa hỗ trợ → fallback widget "unsupported" (an toàn, không crash).

### 7.1. Offline draft (AC-23, BR-504)
- Lưu draft vào **Hive** box `form_drafts`: `{form_id, form_version_id, answers, attachments(local paths), client_idempotency_key, started_at}`.
- `client_idempotency_key` (UUID v4) sinh **lúc bắt đầu draft**, không đổi.
- Ảnh lưu local trong draft; khi online: PUT ảnh → đổi sang objectKey → submit.
- **Reconnect sync:** submit với `X-Idempotency-Key = client_idempotency_key` → BE dedup (đã có IdempotencyMiddleware từ M01); trùng → trả 2xx cached, không tạo bản 2.
- `form_submissions.status`: `draft_offline` → `submitted` (→ `edited` nếu sửa sau submit).

---

## 8. Scoring engine (knowledge_test/training)

- Bật khi `rules.scored = true`. Mỗi field choice có `correct`/`correct[]`.
- Chấm phía **server** lúc submit: so `answers[id]` với `correct`. Hỗ trợ `single_choice` (exact), `multi_choice` mode `one_of_N`/`all_of_N` (Edge case spec).
- `score` lưu `numeric(5,2)` (vd % đúng). `show_results`: `immediately` (trả luôn) / `delayed` / `never`.
- `randomize_questions`/`randomize_answers`: server chọn N từ pool + trả thứ tự đã trộn; lưu thứ tự đã trộn vào draft để chấm nhất quán.

---

## 9. API ↔ thiết kế (khớp spec §API)

| Endpoint | Ghi chú thiết kế |
|---|---|
| `POST/PATCH /admin/forms` | ghi vào version `draft`; validate schema theo registry |
| `POST /admin/forms/:id/publish` | tạo/đóng băng version, cập nhật `current_version` (§3) |
| `GET /admin/forms/:id/versions` | lịch sử version (audit) |
| `POST /admin/forms/:id/assignments` | thêm 1 row targeting (OR) |
| `GET /forms/me` | resolution §4 (kèm deadline, version hiện hành) |
| `GET /forms/:id` | trả schema `current_version` + rules để render |
| `POST /forms/:id/attachments/presign` | §6 |
| `POST /forms/:id/submit` | validate §5 + scoring §8 + idempotency §7.1 |
| `PATCH /form-submissions/:id` | edit-after-submit nếu `allow_edit_after_submit` (BR-506), ghi `edited_at`, lịch sử edit |
| `GET /admin/form-submissions` | query + filter; export → Sprint 16 |

Audit (CR-1): `form.created/published/assigned`, `form_submission.submitted/edited`.

---

## 10. Lộ trình theo sprint

| Sprint | Nội dung design này phủ |
|---|---|
| **S11** | Chốt registry (§2) + entities + `forms` CRUD (draft) + **M04 Product Master** (product_selector phụ thuộc). Form Builder skeleton (list-based). |
| **S12** | Builder đầy đủ (drag-drop **@dnd-kit/ADR-014**) + rules panel + assignment + **versioning §3** (AC-20/21). Web. |
| **S13** | Mobile **renderer §7** + **offline §7.1** + submit/validate §5 + **scoring §8** (AC-22/23/24). |
| **S14** | Form templates preset + **Visit Plan (M11)** post-visit report cắm vào engine. |

---

## 11. Quyết định mở → ADR

- **ADR-016 — Form Engine:** schema-driven JSONB + validator theo registry (không NJsonSchema generic) + factory renderer. *(viết kèm đợt này)*
- **ADR-014 — @dnd-kit** cho Form Builder drag-drop (web). *(viết kèm)*
- **ADR-015 — Recharts** cho Reports (Sprint 16) — chốt sớm để thống nhất chart lib. *(viết kèm)*
- Mở: cần materialized view cho assignment resolution ở scale? → đo ở S17.

---

## 12. Tham chiếu
- Spec: [M10-form-engine.md](./M10-form-engine.md) · Data: [04-data-model.md](../04-data-model.md) · Rules: BR-501..507 trong [06-business-rules.md](../06-business-rules.md)
- Convention tham khảo (không phải dependency): SurveyJS JSON schema (`type/title/validators/visibleIf`), JSON-Schema/RJSF (tách schema vs giá trị).
