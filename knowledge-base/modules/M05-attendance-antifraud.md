# M05_ATTENDANCE_ANTIFRAUD — Attendance & Anti-Fraud

## Quick Reference

| | |
|---|---|
| **Module ID** | M05 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 35 |
| **Sprints** | S3, S4 |
| **Depends on** | M1, M3, M6, M16 |
| **Acceptance criteria** | AC-4, AC-5, AC-6, AC-9, AC-10 |

## Purpose

Chấm công từ xa đúng người, đúng store, hạn chế gian lận tối đa.

## Scope (Phase 1)

**Check-in:**
  - Hiển thị stores được phân công
  - Auto-detect store gần nhất bằng GPS
  - Cho phép check-in sớm 60 phút (trước shift start)
  - Late marking nếu >5 phút sau shift start
  - GPS validation: distance ≤300m từ store
  - Fake GPS detection (Mock Location + Jailbreak check) → BLOCK
  - Bắt buộc Face Verification (M6)
  - Chụp selfie
  - Chụp ảnh cửa hàng (có EXIF GPS + datetime)
  - Ghi chú tùy chọn
  - Lưu: thời gian, store, shift, GPS, face result, selfie URL, store photo URL, status
**Check-out:**
  - Gắn với attendance đang mở
  - Bắt buộc Face Verification
  - Selfie + store photo + GPS như check-in
  - Tổng kết ca làm
**Lịch sử chấm công:**
  - PG/Leader xem lịch sử
  - Hiển thị: ngày, ca, store, check-in, check-out, tổng giờ, status

## Data Entities

- `attendance_records`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/attendance/today — today's expected shifts + status`
- `GET /api/v1/attendance/check-in/info — get assigned store + validation rules`
- `POST /api/v1/attendance/check-in (multipart) — submit check-in`
- `POST /api/v1/attendance/:id/check-out (multipart) — submit check-out`
- `GET /api/v1/attendance/history — paginated history`
- `GET /api/v1/admin/attendance — Admin view all, filterable`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Check-in screen (camera, GPS, store info)
- Check-out screen
- Attendance history list
- Attendance detail

### Web Admin
- Attendance list with filter
- Anomaly review queue
- Attendance detail with selfies

## Business Rules Applied

- BR-201
- BR-202
- BR-203
- BR-204
- BR-205
- BR-206
- BR-210

See `06-business-rules.md` for rule details.

## Edge Cases

- PG starts check-in then loses network → resume by re-submitting with idempotency key
- GPS not available (indoor) → wait + show clear error, no fallback to network location for accuracy
- Check-in for a shift that's been cancelled → 409 with clear error
- PG already checked in but app reinstalled → can still check out (server is source of truth)
- Multiple shifts same day, same store: each shift = separate attendance record
- PG forgets to check-out → next check-in fails until previous closed
- Time zone: store all UTC, calculate late based on store's local TZ (Phase 1: assume all VN)

## Key Implementation Notes

- Order of validations in check-in handler:
-   1. User has active session
-   2. Device matches
-   3. No fake GPS
-   4. Store in user's assignments
-   5. Within 60min before shift OR after shift start
-   6. GPS within 300m (or set status to GpsViolation)
-   7. Face verify with FPT.AI (or set status to FaceFail)
-   8. Calculate late status
-   9. Save attendance, upload photos to MinIO
-   10. If review-needed, create admin_reviews entry
-   11. Audit log
-   12. Push SignalR event for Team Monitoring
- Photos: pre-validate size/type on mobile, compress to ~500KB before upload
- EXIF: read GPS + timestamp from store photo, verify matches submission
- Hangfire job retries Face API on transient failures

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
