# M08_LEAVE_OT — Leave & OT

## Quick Reference

| | |
|---|---|
| **Module ID** | M08 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 16 |
| **Sprints** | S7 |
| **Depends on** | M1, M9 |
| **Acceptance criteria** | AC-16 |

## Purpose

Ghi nhận và phê duyệt nghỉ phép, nghỉ đột xuất, OT.

## Scope (Phase 1)

- Tạo đơn nghỉ phép (date range, lý do)
- Tạo đơn nghỉ đột xuất khi đang làm — gắn với check-out
- Tạo đơn OT (date + start_time + end_time + lý do)
- Gửi duyệt qua Approval Workflow (M9)
- Xem lịch sử & status
- Notification khi approve/reject

## Data Entities

- `leave_requests`
- `ot_requests`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/leave-requests — create leave`
- `POST /api/v1/leave-requests/emergency — create emergency (links to attendance)`
- `POST /api/v1/ot-requests — create OT`
- `GET /api/v1/leave-requests/me, /api/v1/ot-requests/me — own history`
- `DELETE /api/v1/leave-requests/:id — withdraw if still pending`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Leave request form
- Emergency leave action (from check-out flow)
- OT request form
- Requests history
- Request status detail

### Web Admin
- All requests view, override capability

## Business Rules Applied

- (No specific decision tables; standard CRUD logic)

See `06-business-rules.md` for rule details.

## Edge Cases

- Emergency leave when no active check-in → return 409
- Leave overlapping with scheduled shifts → mark shifts as 'on_leave' upon approval
- OT outside scheduled shift → allowed
- Reject reason required (enforced by validator)

## Key Implementation Notes

- Emergency leave: special flow tied to attendance check-out
- After approve, schedule shifts in the leave range auto-marked
- Multi-day leave: single request, expanded for display

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
