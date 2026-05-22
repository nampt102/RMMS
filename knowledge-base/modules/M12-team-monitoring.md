# M12_TEAM_MONITORING — Team Monitoring / PG Online

## Quick Reference

| | |
|---|---|
| **Module ID** | M12 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 14 |
| **Sprints** | S8 |
| **Depends on** | M3, M5 |
| **Acceptance criteria** | AC-26, AC-27 |

## Purpose

Cho Leader/Admin/BUH xem trạng thái làm việc trong ngày.

## Scope (Phase 1)

- Danh sách PG/Leader online (đang check-in trong ngày)
- Trạng thái: Working / NotCheckedIn / CheckedOut / OnLeave / NoScheduleToday / PendingReview
- Leader: xem PG thuộc quyền
- Admin: xem tất cả
- BUH: xem theo area/category
- Manual refresh + last-update timestamp
- Optional: real-time updates via SignalR

## Data Entities

- (computed from attendance + schedule + leave)

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/team-monitoring/today — paginated list with status`
- `GET /api/v1/team-monitoring/summary — counts by status`
- `WebSocket /hubs/team-monitoring — real-time updates`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (Leader)
- PG Online list
- PG detail with current status

### Web Admin/BUH
- Team Monitoring dashboard
- Filter by area/store/role

## Business Rules Applied

- BR-601
- BR-602

See `06-business-rules.md` for rule details.

## Edge Cases

- PG no schedule today + no check-in → status NoScheduleToday
- PG on leave (approved) → status OnLeave
- PG checked-in but anomaly → status PendingReview (with note)

## Key Implementation Notes

- Status calculation: pure SQL view OR computed in handler
- Cache: Redis 30s TTL acceptable
- Real-time push: SignalR group per Leader (their PGs)
- Initial Phase 1: manual refresh sufficient; SignalR Phase 1B polish

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
