# M06_FACE_VERIFICATION — Face Verification

## Quick Reference

| | |
|---|---|
| **Module ID** | M06 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 22 |
| **Sprints** | S4 |
| **Depends on** | M1, M5 |
| **Acceptance criteria** | AC-7, AC-8, AC-11, AC-12, AC-13 |

## Purpose

Xác định đúng PG/Leader khi chấm công, tránh check-in hộ.

## Scope (Phase 1)

- PG tự enroll khuôn mặt lần đầu (3 ảnh: front, left, right)
- Lưu face template trong FPT.AI (chỉ ID reference trong DB)
- Face verification BẮT BUỘC tại check-in/out
- Capture selfie HD (1080p min) tại thời điểm verify
- Nếu fail → tạo Admin Review entry
- Admin xem selfie + so sánh với enrolled photo
- Admin action: confirm correct → attendance success / confirm wrong → no attendance
- Re-enroll face khi cần (Admin trigger)

## Data Entities

- (uses users.face_template_external_id, users.face_enrolled_at)

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/face/enroll — multipart, 3 photos, returns template id`
- `POST /api/v1/face/verify — multipart, 1 photo, returns match result`
- `POST /api/v1/admin/face/re-enroll/:userId — trigger user to re-enroll`
- `DELETE /api/v1/admin/face/template/:userId — remove enrollment`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Face enrollment wizard (3 angles)
- Face verify during check-in/out (auto)
- Re-enrollment prompt if requested by Admin

### Web Admin
- Face fail review screen (selfie vs enrolled)
- User face status overview

## Business Rules Applied

- BR-206
- BR-207
- BR-208
- BR-209

See `06-business-rules.md` for rule details.

## Edge Cases

- Face API down → attendance goes to pending_review with note
- PG wears mask → likely fail → Admin review picks up
- Low light (dark store) → fail → Admin review
- PG enrolled with different appearance (haircut, glasses) → re-enroll option
- Twins / siblings (rare) → confidence threshold needed, fall to Admin review

## Key Implementation Notes

- Vendor: FPT.AI Face API (primary), AWS Rekognition (backup option in S0 PoC)
- Confidence threshold: 0.85 default — to be tuned during S4
- Latency budget: <2s API call (else Mobile shows progress)
- Privacy: face template stored in vendor, NOT in our DB
- Selfies stored in MinIO with 90-day TTL, encrypted at rest
- Liveness detection: enable if FPT.AI supports (paid feature), else manual via Admin review
- Cost monitoring: alert if monthly verify count >10x average

## Definition of Done

This module is considered DONE when:
- [ ] All endpoints implemented and documented in Swagger
- [ ] Unit tests cover happy path + error cases (≥70%)
- [ ] Integration tests via Testcontainers for critical flows
- [ ] Mobile/Web screens implemented per spec
- [ ] i18n strings present for both `vi` and `en`
- [ ] Acceptance criteria listed above pass manual verification
- [ ] Audit log entries for relevant actions (see CR-1)
- [ ] PR reviewed and merged
- [ ] Deployed to staging and smoke-tested
