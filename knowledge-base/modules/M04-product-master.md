# M04_PRODUCT_MASTER — Product Master

## Quick Reference

| | |
|---|---|
| **Module ID** | M04 |
| **Phase** | 1B |
| **Priority** | P1 |
| **Complexity** | Low |
| **Est. dev-days** | 8 |
| **Sprints** | S11 |
| **Depends on** | M3 |
| **Acceptance criteria** | AC-25 |

## Purpose

Product là dữ liệu nền cho Form Engine (product selector, brand/SKU selector).

## Scope (Phase 1)

- Admin CRUD Product (name, SKU, brand, category, attributes JSONB)
- PG/Leader xem product master (read-only) khi chọn trong form
- Search by name / SKU / brand
- Filter by category

## Data Entities

- `products`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET / POST / PATCH / DELETE /api/v1/admin/products`
- `GET /api/v1/products — read-only for mobile (paginated, searchable)`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Web Admin
- Products list with search/filter
- Product detail/edit
- Bulk operations (later)

### Mobile
- Product selector embedded in form fields

## Business Rules Applied

- BR-507

See `06-business-rules.md` for rule details.

## Edge Cases

- Product deleted but form submissions reference it → keep soft-delete, don't break history
- Product attributes flexibility: JSONB for ad-hoc fields per category

## Key Implementation Notes

- Pagination 50 per page
- Cache product list in Redis (15min TTL)
- Mobile caches list locally in Hive for offline form filling

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
