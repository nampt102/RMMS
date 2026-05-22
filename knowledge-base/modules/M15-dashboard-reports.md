# M15_DASHBOARD_REPORTS — Dashboard & Reports

## Quick Reference

| | |
|---|---|
| **Module ID** | M15 |
| **Phase** | 1A (basic), 1B (full) |
| **Priority** | P0 (basic) / P1 (full) |
| **Complexity** | Medium |
| **Est. dev-days** | 22 |
| **Sprints** | S10, S16 |
| **Depends on** | M3, M5, M7, M8, M10, M11 |
| **Acceptance criteria** | AC-27 |

## Purpose

Cho Admin/BUH xem tình trạng vận hành và xử lý báo cáo.

## Scope (Phase 1)

**Mandatory Phase 1 (basic, in 1A):**
  - PG/Leader Online list
  - Check-in/out today
**Phase 1 reports (full, in 1B):**
  - Attendance report
  - Anomaly report (face fail, GPS violation, fake GPS)
  - Schedule report
  - Leave/OT report
  - Pending approval report
  - Form pending / overdue report
  - Form submission report
  - Test/survey results report
  - Visit Plan report
**Export:**
  - Each report exportable to Excel/CSV
  - Import NOT in Phase 1

## Data Entities

- (queries existing tables)

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/admin/dashboard/summary — KPIs for top of page`
- `GET /api/v1/admin/reports/attendance — query with filters`
- `GET /api/v1/admin/reports/anomalies`
- `... (one per report)`
- `POST /api/v1/admin/reports/:type/export — generate file, returns URL`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Web Admin
- Dashboard with KPI cards and charts
- Each report screen: filter bar + table + export button
- Drill-down to detail

### Web BUH
- Same as Admin but scoped to area/category

## Business Rules Applied

- (No specific decision tables; standard CRUD logic)

See `06-business-rules.md` for rule details.

## Edge Cases

- Large date range: paginate, max 1 year per query
- Export of huge dataset (>100k rows): async via Hangfire, notify when ready
- Real-time vs batch: dashboard uses live queries; reports may be cached 5min

## Key Implementation Notes

- Charts: Recharts (web)
- Excel export: ClosedXML
- Optimize queries: dedicated index for each report's filter combination
- Pre-aggregated daily summary table (Phase 2 optimization)
- Permission: Admin sees all; BUH scoped to their area; Leader sees own PGs only

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
