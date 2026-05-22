# M02_DEVICE_MANAGEMENT — Device Management

## Quick Reference

| | |
|---|---|
| **Module ID** | M02 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 14 |
| **Sprints** | S2 |
| **Depends on** | M1 |
| **Acceptance criteria** | AC-3 |

## Purpose

Đảm bảo PG chỉ dùng 1 thiết bị, hạn chế gian lận check-in hộ.

## Scope (Phase 1)

- PG chỉ login được trên 1 thiết bị active
- Khi PG đổi điện thoại → tạo device change request
- Notify Leader (hoặc Admin nếu PG không có Leader) qua in-app + push + email
- Leader/Admin duyệt → device cũ → status `replaced`, device mới → `active`
- Refresh tokens của device cũ bị revoke
- Lưu lịch sử thiết bị

## Data Entities

- `user_devices`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/auth/login — trên device mới → trả về 403 DEVICE_NOT_AUTHORIZED + tạo request`
- `GET /api/v1/devices/me — PG xem device hiện tại`
- `GET /api/v1/devices/pending — Leader/Admin xem requests pending`
- `POST /api/v1/devices/:id/approve — duyệt`
- `POST /api/v1/devices/:id/reject — từ chối, kèm reason`
- `GET /api/v1/devices/history — admin xem history`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG)
- Device change request status screen
- Notification on approval

### Mobile (Leader)
- Device change requests list
- Approve/reject UI

### Web Admin
- Device requests management screen

## Business Rules Applied

- BR-105
- BR-106

See `06-business-rules.md` for rule details.

## Edge Cases

- PG bị mất điện thoại, không thể login → Admin reset device manually
- Race condition: PG login đồng thời 2 devices → first wins, second creates request
- Leader không phản hồi trong 48h → escalate Admin (Phase 2)
- Sau approve, app cũ phải auto logout khi token next refresh

## Key Implementation Notes

- Device fingerprint: combo of UUID generated on first install + device model + os
- Stored in flutter_secure_storage (Mobile)
- Verify on every login + on each token refresh
- Don't block login flow entirely — show 'pending approval' screen with status

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
