# 04 — Data Model

Complete entity model for RMMS 2026 Phase 1.

## Conventions

- All entities have: `id` (UUID v7), `created_at`, `updated_at`, `created_by`, `updated_by`
- Soft delete: `deleted_at` (nullable). Hard delete only for non-critical entities.
- All timestamps stored as UTC (`timestamptz`).
- Enums stored as `varchar` with check constraints (not Postgres ENUM, to allow easier alteration).

## Core Entities

### users
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| email | varchar(255) | Unique, lowercase |
| password_hash | varchar(255) | bcrypt cost 12 |
| full_name | varchar(255) | |
| phone | varchar(20) | Nullable |
| role | varchar(20) | `pg` / `leader` / `admin` / `buh` |
| status | varchar(20) | `active` / `inactive` / `pending_email_verify` |
| email_verified_at | timestamptz | Nullable |
| last_login_at | timestamptz | Nullable |
| preferred_language | varchar(5) | `vi` / `en`, default `vi` |
| face_enrolled_at | timestamptz | Nullable (for PG/Leader) |
| face_template_external_id | varchar(255) | ID from FPT.AI |
| created_at, updated_at, deleted_at | timestamptz | |

### user_devices
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK users |
| device_id | varchar(255) | Unique per user, from app |
| device_name | varchar(255) | e.g. "iPhone 14 Pro" |
| os | varchar(20) | `ios` / `android` |
| os_version | varchar(20) | |
| app_version | varchar(20) | |
| fcm_token | varchar(500) | For push notification |
| status | varchar(20) | `active` / `pending_approval` / `rejected` / `replaced` |
| approved_by | UUID | FK users (Leader or Admin) |
| approved_at | timestamptz | |
| last_used_at | timestamptz | |
| created_at, updated_at | timestamptz | |

### login_history
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| device_id | UUID | FK user_devices, nullable |
| ip_address | inet | |
| user_agent | text | |
| success | boolean | |
| failure_reason | varchar(100) | Nullable |
| created_at | timestamptz | |

### refresh_tokens
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| token_hash | varchar(255) | SHA256 of token |
| device_id | UUID | FK |
| expires_at | timestamptz | |
| revoked_at | timestamptz | Nullable |
| created_at | timestamptz | |

## Organization

### stores
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| code | varchar(50) | Unique |
| name | varchar(255) | |
| address | text | |
| latitude | numeric(10,7) | |
| longitude | numeric(10,7) | |
| area_id | UUID | FK areas |
| status | varchar(20) | `active` / `inactive` |
| created_at, updated_at, deleted_at | timestamptz | |

### areas
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| code | varchar(50) | Unique |
| name | varchar(255) | |
| parent_area_id | UUID | FK areas (for hierarchy if needed) |
| created_at, updated_at, deleted_at | timestamptz | |

### categories
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| code | varchar(50) | Unique |
| name | varchar(255) | |
| created_at, updated_at, deleted_at | timestamptz | |

### user_leader_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| pg_user_id | UUID | FK users (PG) — unique |
| leader_user_id | UUID | FK users (Leader) |
| effective_from | date | |
| effective_to | date | Nullable |
| created_at, updated_at | timestamptz | |

> Constraint: 1 PG → 1 active Leader at a time.

### user_store_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK users (PG or Leader) |
| store_id | UUID | FK stores |
| effective_from | date | |
| effective_to | date | Nullable |
| created_at, updated_at | timestamptz | |

> 1 user can have multiple active store assignments.

### user_category_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK users |
| category_id | UUID | FK categories |
| created_at | timestamptz | |

## Product Master

### products
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| sku | varchar(100) | Unique |
| name | varchar(255) | |
| brand | varchar(255) | |
| category_id | UUID | FK |
| attributes | jsonb | Flexible attrs |
| status | varchar(20) | `active` / `inactive` |
| created_at, updated_at, deleted_at | timestamptz | |

## Attendance

### work_schedules
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| schedule_date | date | |
| status | varchar(20) | `pending` / `approved` / `rejected` / `edit_pending` |
| version | int | Increments on edit |
| previous_version_id | UUID | Self FK |
| submitted_at | timestamptz | |
| approved_at | timestamptz | Nullable |
| approved_by | UUID | FK users |
| reject_reason | text | Nullable |
| created_at, updated_at | timestamptz | |

### work_schedule_shifts
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| work_schedule_id | UUID | FK work_schedules |
| store_id | UUID | FK stores |
| start_time | time | e.g., 08:00 |
| end_time | time | e.g., 17:00 |
| ordering | int | If multiple shifts/day |
| created_at | timestamptz | |

### attendance_records
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| work_schedule_shift_id | UUID | FK |
| store_id | UUID | FK stores |
| check_in_at | timestamptz | |
| check_in_latitude | numeric(10,7) | |
| check_in_longitude | numeric(10,7) | |
| check_in_distance_meters | numeric(8,2) | From store coords |
| check_in_face_result | varchar(20) | `success` / `fail` / `pending_review` |
| check_in_face_confidence | numeric(5,4) | 0..1 from API |
| check_in_selfie_url | text | MinIO URL |
| check_in_store_photo_url | text | MinIO URL |
| check_in_fake_gps_detected | boolean | |
| check_in_note | text | Nullable |
| check_out_at | timestamptz | Nullable |
| check_out_latitude | numeric(10,7) | |
| check_out_longitude | numeric(10,7) | |
| check_out_distance_meters | numeric(8,2) | |
| check_out_face_result | varchar(20) | |
| check_out_face_confidence | numeric(5,4) | |
| check_out_selfie_url | text | |
| check_out_store_photo_url | text | |
| check_out_note | text | |
| status | varchar(30) | See AttendanceStatus enum |
| admin_review_id | UUID | FK admin_reviews, nullable |
| created_at, updated_at | timestamptz | |

> Index: `(user_id, check_in_at DESC)`, `(status, created_at)` for review queue.

## Leave & OT

### leave_requests
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| leave_type | varchar(20) | `regular` / `emergency` |
| start_date | date | |
| end_date | date | |
| start_time | time | For partial-day |
| end_time | time | |
| reason | text | |
| status | varchar(20) | `pending` / `approved` / `rejected` |
| approval_id | UUID | FK approvals |
| linked_attendance_id | UUID | FK attendance_records (if emergency tied to check-out) |
| created_at, updated_at | timestamptz | |

### ot_requests
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| ot_date | date | |
| start_time | time | |
| end_time | time | |
| reason | text | |
| status | varchar(20) | `pending` / `approved` / `rejected` |
| approval_id | UUID | FK approvals |
| created_at, updated_at | timestamptz | |

## Approval Workflow (Generic)

### approvals
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| entity_type | varchar(50) | `work_schedule` / `ot_request` / `leave_request` / `visit_plan` |
| entity_id | UUID | The thing being approved |
| requester_id | UUID | FK users |
| approver_id | UUID | FK users — who needs to approve |
| approver_role | varchar(20) | `leader` / `buh` |
| status | varchar(20) | `pending` / `approved` / `rejected` / `overridden` |
| decision_reason | text | Required if rejected |
| decided_at | timestamptz | |
| decided_via | varchar(20) | `app` / `web` / `email_link` |
| overridden_by | UUID | FK users (Admin) — nullable |
| override_reason | text | Required if overridden |
| created_at, updated_at | timestamptz | |

### approval_email_tokens
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| approval_id | UUID | FK |
| token_hash | varchar(255) | SHA256 of signed JWT |
| expires_at | timestamptz | 24h after creation |
| used_at | timestamptz | One-time use; nullable |
| ip_address | inet | When used |
| user_agent | text | When used |
| created_at | timestamptz | |

## Form Engine

### forms
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| code | varchar(50) | Unique per form, NOT per version |
| name_vi | varchar(255) | |
| name_en | varchar(255) | |
| description_vi | text | |
| description_en | text | |
| form_type | varchar(50) | `stock_report` / `market_report` / `photo_report` / `pc_checklist` / `free_report` / `survey` / `knowledge_test` / `training` / `visit_report` |
| current_version | int | Latest published version |
| status | varchar(20) | `draft` / `published` / `archived` |
| created_at, updated_at | timestamptz | |

### form_versions
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| form_id | UUID | FK forms |
| version | int | 1, 2, 3, ... |
| schema | jsonb | All fields, rules, layout (see below) |
| published_at | timestamptz | |
| published_by | UUID | FK users |
| created_at | timestamptz | |

> `schema` JSONB structure:
> ```json
> {
>   "fields": [
>     {"id":"q1","type":"text","label_vi":"Tên","label_en":"Name","required":true,"max_length":100},
>     {"id":"q2","type":"single_choice","label_vi":"...","options":[...]},
>     {"id":"q3","type":"product_selector","filter":{"category":"..."}},
>     ...
>   ],
>   "rules": {
>     "target_users": ["pg","leader"],
>     "valid_from": "2026-01-01",
>     "valid_to": "2026-12-31",
>     "always_on": false,
>     "deadline_minutes": 1440,
>     "store_required": true,
>     "auto_fill_store_from_checkin": true,
>     "require_check_in": false,
>     "gps_required": false,
>     "photo_required": true,
>     "use_product_master": true,
>     "scored": false,
>     "randomize_questions": false,
>     "randomize_answers": false,
>     "time_limit_minutes": null,
>     "show_results": "immediately",
>     "allow_edit_after_submit": false,
>     "allow_offline_draft": true
>   }
> }
> ```

### form_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| form_id | UUID | FK forms (NOT version — assignment applies to latest) |
| assigned_to_role | varchar(20) | Nullable |
| assigned_to_user_id | UUID | Nullable |
| assigned_to_store_id | UUID | Nullable |
| assigned_to_area_id | UUID | Nullable |
| assigned_to_category_id | UUID | Nullable |
| assigned_to_product_id | UUID | Nullable |
| valid_from | timestamptz | |
| valid_to | timestamptz | Nullable |
| created_at | timestamptz | |

> One assignment row = one targeting rule. Multiple rows = OR logic.

### form_submissions
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| form_id | UUID | FK forms |
| form_version_id | UUID | FK form_versions (version at submission time) |
| user_id | UUID | FK users |
| store_id | UUID | Nullable, if form linked to store |
| answers | jsonb | Field id → value |
| attachments | jsonb | File URLs by field id |
| score | numeric(5,2) | Nullable, for scored forms |
| time_spent_seconds | int | |
| submitted_at | timestamptz | |
| edited_at | timestamptz | Nullable |
| status | varchar(20) | `submitted` / `draft_offline` / `edited` |
| client_idempotency_key | varchar(100) | Prevent dup submission |
| created_at, updated_at | timestamptz | |

### form_drafts (mobile-only, may not need server)
| Column | Type | Notes |
|---|---|---|
| (stored in Hive locally) | | |

## Visit Plan

### visit_plans
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| leader_user_id | UUID | FK users |
| visit_date | date | |
| notes | text | |
| status | varchar(20) | `pending` / `approved` / `rejected` / `executed` |
| approval_id | UUID | FK approvals |
| created_at, updated_at | timestamptz | |

### visit_plan_items
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| visit_plan_id | UUID | FK |
| store_id | UUID | FK |
| form_id | UUID | FK — form to be filled at this store |
| ordering | int | |
| executed_at | timestamptz | Nullable |
| form_submission_id | UUID | FK form_submissions, nullable |
| created_at | timestamptz | |

## Documents

### documents
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| name | varchar(255) | |
| description | text | |
| folder_type | varchar(20) | `public` / `private` |
| file_url | text | MinIO key |
| file_size_bytes | bigint | |
| mime_type | varchar(100) | |
| uploaded_by | UUID | FK users |
| created_at, updated_at, deleted_at | timestamptz | |

### document_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| document_id | UUID | FK |
| assigned_to_role | varchar(20) | Nullable |
| assigned_to_user_id | UUID | Nullable |
| created_at | timestamptz | |

> Payslip = private document, assigned to single user.

## News & Notifications

### news
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| title_vi | varchar(255) | |
| title_en | varchar(255) | |
| content_vi | text | |
| content_en | text | |
| category | varchar(50) | |
| is_important | boolean | If true, requires read confirmation |
| published_at | timestamptz | |
| published_by | UUID | FK users |
| created_at, updated_at | timestamptz | |

### news_assignments
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| news_id | UUID | FK |
| assigned_to_role | varchar(20) | Nullable |
| assigned_to_user_id | UUID | Nullable |
| created_at | timestamptz | |

### news_reads
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| news_id | UUID | FK |
| user_id | UUID | FK |
| read_at | timestamptz | |
| confirmed_at | timestamptz | Nullable (for important news) |

### notifications
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| user_id | UUID | FK |
| type | varchar(50) | See notification types enum |
| title | varchar(255) | |
| body | text | |
| data | jsonb | Deep link, refs |
| channels_sent | varchar(50)[] | e.g. `['in_app','push','email']` |
| is_read | boolean | |
| read_at | timestamptz | |
| created_at | timestamptz | |

## Admin Review

### admin_reviews
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| review_type | varchar(50) | `face_fail` / `gps_violation` / `device_change` / `manual_attendance` |
| entity_type | varchar(50) | `attendance` / `device` / etc. |
| entity_id | UUID | |
| user_id | UUID | FK users |
| status | varchar(20) | `pending` / `approved` / `rejected` / `info_requested` |
| reviewed_by | UUID | FK users (Admin) |
| reviewed_at | timestamptz | |
| notes | text | |
| created_at, updated_at | timestamptz | |

## Audit Log

### audit_logs
| Column | Type | Notes |
|---|---|---|
| id | UUID v7 | PK |
| actor_user_id | UUID | FK users |
| actor_role | varchar(20) | Snapshot for safety |
| action | varchar(100) | `login` / `check_in` / `approve_request` / `override_approval` / `form_publish` / `document_upload` / `export_data` / ... |
| entity_type | varchar(50) | |
| entity_id | UUID | Nullable |
| ip_address | inet | |
| user_agent | text | |
| metadata | jsonb | Action-specific data |
| created_at | timestamptz | |

> Index: `(entity_type, entity_id, created_at)`, `(actor_user_id, created_at DESC)`.
> Append-only — no UPDATE or DELETE at DB level (revoke permissions for app user).

## ER Diagram

See `diagrams/erd.svg` for visual representation.
