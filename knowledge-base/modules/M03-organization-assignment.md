# M03_ORGANIZATION_ASSIGNMENT — Organization & Assignment

## Quick Reference

| | |
|---|---|
| **Module ID** | M03 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 20 |
| **Sprints** | S2 |
| **Depends on** | M1 |
| **Acceptance criteria** | — |

## Purpose

Quản lý cấu trúc tổ chức và phân công làm việc.

## Scope (Phase 1)

- Admin CRUD PG, Leader, BUH
- Admin CRUD Store (với GPS coords)
- Admin CRUD Area
- Admin CRUD Category (ngành hàng)
- Gán PG → 1 Leader (1:1 active)
- Gán PG/Leader → nhiều Store (1:N)
- Gán PG/Leader → Category (nếu cần cho Form)
- Leader có thể quản lý PG ở nhiều khu vực/store

## Data Entities

- `stores`
- `areas`
- `categories`
- `user_leader_assignments`
- `user_store_assignments`
- `user_category_assignments`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET / POST / PATCH / DELETE /api/v1/admin/stores`
- `GET / POST / PATCH / DELETE /api/v1/admin/areas`
- `GET / POST / PATCH / DELETE /api/v1/admin/categories`
- `POST /api/v1/admin/assignments/pg-leader`
- `POST /api/v1/admin/assignments/user-store`
- `POST /api/v1/admin/assignments/user-category`
- `GET /api/v1/users/me/stores — for Mobile to know assigned stores`
- `GET /api/v1/users/me/leader — for PG`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Web Admin
- Stores list + map view + CRUD
- Areas tree view + CRUD
- Categories list + CRUD
- Assignment matrix (PG ↔ Leader)
- Assignment matrix (User ↔ Store)
- Bulk assignment (CSV import in Phase 2)

## Business Rules Applied

- (No specific decision tables; standard CRUD logic)

See `06-business-rules.md` for rule details.

## Edge Cases

- Re-assign PG to new Leader: pending requests stay with old Leader
- Deactivate store: ongoing schedules at that store keep working until end date
- Move PG between areas: all current assignments updated

## Key Implementation Notes

- Effective dating: assignments have `effective_from` / `effective_to`
- Active assignment query: `WHERE effective_from <= NOW() AND (effective_to IS NULL OR effective_to >= NOW())`
- Geo column for stores: PostGIS optional (nice-to-have); plain lat/lon enough for Phase 1
- GPS distance calculation: Haversine formula or PostGIS `ST_Distance` if installed

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
