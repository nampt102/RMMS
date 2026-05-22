# 06 — Business Rules (Decision Tables)

Trích xuất từ PRD section 3. Đây là **single source of truth** cho business logic. Mọi implementation phải tuân thủ.

## 3.1 Tài khoản & Thiết bị

| Rule ID | Condition | Decision |
|---|---|---|
| BR-101 | PG registers | Via email |
| BR-102 | Leader account | Provisioned by Admin (no self-register) |
| BR-103 | BUH account | Provisioned by Admin |
| BR-104 | Admin account | Provisioned (initial via seed/CLI) |
| BR-105 | PG device count | EXACTLY 1 active device |
| BR-106 | PG changes phone | New device → Leader OR Admin notified → Leader/Admin approves → Old device invalidated → New device active |

**Implementation notes:**
- On login, compare `device_id` in request to user's active device
- If different and no pending request → create device change request
- Notify Leader (or Admin if no Leader) via in-app + push + email
- Until approved, new device cannot login (returns `DEVICE_NOT_AUTHORIZED`)
- When approved, mark old device `replaced`, new device `active`

## 3.2 Check-in / Check-out

| Rule ID | Condition | Decision |
|---|---|---|
| BR-201 | Store for check-in/out | MUST be a store assigned to user |
| BR-202 | Early check-in window | Up to 60 minutes before shift start |
| BR-203 | Late threshold | More than 5 minutes after shift start → mark `Late` |
| BR-204 | GPS distance > 300m from store | Send to Admin Review |
| BR-205 | Fake GPS detected | BLOCK (don't even create attendance record marked valid; log for audit) |
| BR-206 | Face Recognition | MANDATORY at BOTH check-in AND check-out |
| BR-207 | Face fail | Selfie still captured → Admin Review |
| BR-208 | Face fail + Admin confirms IS the right person | Check-in/out succeeds (status `AdminApproved`) |
| BR-209 | Face fail + Admin confirms NOT the right person | NOT counted as attendance (status `AdminRejected`) |
| BR-210 | Offline check-in/out | NOT SUPPORTED in Phase 1 — return error if offline |

**Status decision matrix:**

| GPS | Fake GPS | Face | Late | Final Status |
|---|---|---|---|---|
| ≤300m | No | Pass | No | `Valid` |
| ≤300m | No | Pass | Yes | `Late` |
| >300m | No | Pass | * | `GpsViolationPendingReview` |
| ≤300m | No | Fail | * | `FaceFailPendingReview` |
| >300m | No | Fail | * | `GpsViolationPendingReview` + face issue noted |
| * | Yes | * | * | `FakeGpsBlocked` (no valid record) |

After Admin Review:
- `GpsViolationPendingReview` + Admin approves → `AdminApproved`
- `FaceFailPendingReview` + Admin approves (right person) → `AdminApproved`
- Any review + Admin rejects → `AdminRejected`

## 3.3 Lịch làm việc

| Rule ID | Condition | Decision |
|---|---|---|
| BR-301 | Schedule registration granularity | Per day, week, or month |
| BR-302 | Shifts belong to | Per PG/Leader (NOT per store) |
| BR-303 | Store role in schedule | Place of work + GPS validation reference |
| BR-304 | Multiple shifts per day | ALLOWED |
| BR-305 | Multiple stores per day | ALLOWED (one shift = one store) |
| BR-306 | Edit schedule of future date | ALLOWED |
| BR-307 | After edit | Must re-approve to take effect |
| BR-308 | Old schedule while edit is pending | OLD remains effective until edit approved |

**Implementation notes:**
- When user edits an approved schedule:
  - Create new `work_schedules` row with `status='edit_pending'` and `previous_version_id` pointing to old
  - Old row remains `status='approved'` (effective)
  - On approval of new: mark old as `status='superseded'` (or similar), new becomes `approved`
  - On rejection of edit: delete edit row, old stays approved

## 3.4 Phê duyệt

| Rule ID | Condition | Decision |
|---|---|---|
| BR-401 | Approval model | Hybrid |
| BR-402 | Simple requests (e.g., single-day leave, short OT) | Inline approval allowed |
| BR-403 | Sensitive requests (multi-day, etc.) | Must enter detail screen |
| BR-404 | Rejection | Reason text REQUIRED |
| BR-405 | PG request | Routes to assigned Leader |
| BR-406 | Leader request | Routes to BUH |
| BR-407 | BUH approval | Can be done via email link, no login required |
| BR-408 | Admin override | Allowed, MUST provide reason, MUST be logged in audit |

**Decision: which requests are "simple" vs "sensitive"?**
- Phase 1 simplification: All inline-approvable; "detail" is just a UX option, not gating
- Future: define rules (e.g., leave > 3 days = sensitive)

**Email-link flow:**
1. Generate HMAC-signed JWT-like token containing `approvalId + approverUserId + nonce`
2. Token stored hashed in `approval_email_tokens` table
3. Email contains URL: `https://api/.../approve?token=xxx&action=approve|reject`
4. Validate: token exists, not expired (24h), not used, signature valid
5. Reject requires reason → show simple form
6. Mark token used after action
7. Log IP/user-agent

## 3.5 Form Engine

| Rule ID | Condition | Decision |
|---|---|---|
| BR-501 | Form approach | One unified Form Engine |
| BR-502 | Form when user not checked-in | Configurable per form (`require_check_in`) |
| BR-503 | Form at store | If checked-in, auto-fill store from check-in; if not, may require store selection (per form rule) |
| BR-504 | Offline | ONLY draft mode for forms (not full app offline) |
| BR-505 | Admin edits published form | NEW VERSION created. Old version stays for users who already started/submitted on it |
| BR-506 | After submit | Editable depending on form rule `allow_edit_after_submit` |
| BR-507 | Product Master | Foundation data for many forms |

**Versioning logic:**
- Form has multiple `form_versions`, only one is "current published"
- Submissions FK to specific version
- When Admin edits: create new version, increment `current_version`
- Old submissions remain pointing to old version (immutable history)

## 3.6 Dashboard Phase 1 (Mandatory)

| Rule ID | Item |
|---|---|
| BR-601 | PG/Leader Online list |
| BR-602 | Check-in/Check-out status in current day |

Other reports are P1 (should-have), not strict Phase 1 minimum.

## Cross-cutting Rules

### CR-1: Audit log mandatory for these actions
- Login / Logout (success + failure)
- Device change (request + approve/reject)
- Check-in / Check-out
- Face verification fail
- GPS violation
- Fake GPS detected
- Approval action (approve / reject / override)
- Admin override of any approval
- Form create / edit / publish
- Form assignment changes
- Form submission
- Private document upload
- Payslip send
- Data export

### CR-2: Notification mandatory for these events
- New form assigned
- Form deadline approaching / past due
- Schedule approved / rejected
- OT approved / rejected
- Leave approved / rejected (regular and emergency)
- PG request awaiting Leader approval
- Leader request awaiting BUH approval
- PG device change awaiting approval
- New document available
- New payslip
- New news / important news
- Attendance sent to Admin Review (notify user)

### CR-3: Multi-channel notification policy

| Event | In-app | Push | Email |
|---|---|---|---|
| Approval needed (you are approver) | ✓ | ✓ | ✓ |
| Your request approved/rejected | ✓ | ✓ | ✓ |
| New important news | ✓ | ✓ | ✓ |
| New regular news | ✓ | ✓ | — |
| New document | ✓ | ✓ | — |
| New payslip (private) | ✓ | ✓ | ✓ |
| Form deadline (24h before) | ✓ | ✓ | — |
| Form deadline (passed) | ✓ | ✓ | ✓ |
| Device change request | ✓ | ✓ | ✓ |
| Attendance in review (notify user) | ✓ | — | — |

### CR-4: Data Privacy
- Selfie / face photos retention: 90 days, then auto-delete
- Audit logs retention: 12 months hot, then S3 archive
- Notification retention: 90 days
- User deletion: soft delete + retain attendance/audit records for compliance

### CR-5: Time zone
- All times stored as UTC in DB
- Displayed in user's local TZ (default `Asia/Ho_Chi_Minh`)
- Email timestamps formatted with TZ name visible

### CR-6: Internationalization
- UI strings: VN + EN via resource files / next-intl / Flutter intl
- User-generated content (news title, form labels): bilingual fields in DB
- Error messages: localized via `Accept-Language` header
- Email templates: per language
- Push notification text: per user's `preferred_language`

## Edge Cases Documented

### EC-1: PG works 2 shifts at 2 stores in same day
- Check out from Store A first (closes shift 1)
- Travel to Store B
- Check in at Store B (opens shift 2)
- GPS distance reset per check-in
- Late mark applies per shift's start time

### EC-2: Schedule edit during active day
- Cannot edit a day that's already started (PG already checked in)
- Only future-dated days can be edited
- Today: only if no check-in yet AND shift hasn't started

### EC-3: Emergency leave during shift
- PG checks-out early via "emergency leave" action
- Creates `leave_requests` row with `linked_attendance_id`
- Goes to Leader for approval
- Attendance closes with note "emergency leave"

### EC-4: Approval token re-use attempt
- Token marked `used_at` after first action
- Second attempt → 403 with `EMAIL_TOKEN_USED`
- Display friendly message: "This link has already been used"

### EC-5: Form filled offline, network back during submit
- Hive stores draft with `client_idempotency_key`
- On submit, server checks if key seen before → returns cached response
- If never seen, processes as new submission

### EC-6: PG submits form for a store they're NOT assigned to
- Validate at submission time, not draft time
- If form requires `auto_fill_store_from_checkin`, only checked-in store accepted
- If form allows free selection, check user-store assignment

### EC-7: Leader changes for a PG mid-month
- New leader becomes approver for new requests
- Existing pending requests stay with old leader OR transfer to new (Phase 1 decision: STAY with old leader for consistency)
- Document this clearly to Admin
