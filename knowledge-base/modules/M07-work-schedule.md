# M07_WORK_SCHEDULE — Work Schedule

## Quick Reference

| | |
|---|---|
| **Module ID** | M07 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 28 |
| **Sprints** | S5 |
| **Depends on** | M1, M3, M9 |
| **Acceptance criteria** | AC-14, AC-15 |

## Purpose

Quản lý lịch làm việc per PG/Leader với approval flow và versioning.

## Scope (Phase 1)

- Calendar view (month/week/day) trên Mobile
- Đăng ký lịch theo ngày / tuần / tháng
- Chọn store từ danh sách được phân công
- Tạo shift (start_time, end_time) per ngày
- Multi-shift/day
- Multi-store/day
- Gửi duyệt (Leader cho PG; BUH cho Leader)
- Sửa lịch ngày chưa diễn ra
- Versioning: bản sửa chờ duyệt — bản cũ vẫn effective
- Status: pending / approved / rejected / edit_pending

## Data Entities

- `work_schedules`
- `work_schedule_shifts`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/schedule/me?from=...&to=... — current user's schedule`
- `POST /api/v1/schedule/me — create schedule for date range`
- `PATCH /api/v1/schedule/:id — edit (creates new version)`
- `DELETE /api/v1/schedule/:id — withdraw pending`
- `GET /api/v1/schedule/:userId — Leader view PG's schedule`
- `POST /api/v1/schedule/:id/submit — submit for approval`
- (approval endpoints in M9)

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Schedule calendar (month view)
- Day detail view
- Register schedule wizard (day/week/month modes)
- Multi-shift/store editor for a day
- Edit schedule screen
- Pending approval status

### Web Admin/Leader
- Team schedule overview
- Schedule conflicts view

## Business Rules Applied

- BR-301
- BR-302
- BR-303
- BR-304
- BR-305
- BR-306
- BR-307
- BR-308

See `06-business-rules.md` for rule details.

## Edge Cases

- Edit a day that's TODAY but not yet started → allowed
- Edit a day that's TODAY and shift started → blocked
- Conflict: same time at 2 stores → frontend prevent, backend validate
- Schedule deleted: history preserved (soft-delete)
- Pending edit while old is approved: old remains active until edit approved or rejected

## Key Implementation Notes

- Versioning: when editing, create NEW row with status=edit_pending and previous_version_id
- Active query: status=approved AND (no descendant with status=approved newer)
- Use Hangfire to check for upcoming shifts and remind users (Phase 2)
- Date range registrations: backend expands to per-day rows internally
- Bulk creation transactional (all-or-nothing)

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
