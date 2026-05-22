# M16_ADMIN_REVIEW_AUDIT — Admin Review & Audit Log

## Quick Reference

| | |
|---|---|
| **Module ID** | M16 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 25 |
| **Sprints** | S8, S9 |
| **Depends on** | M1, M2, M5, M6 |
| **Acceptance criteria** | AC-11, AC-12, AC-13, AC-19, AC-35 |

## Purpose

Cho Admin xử lý bất thường và lưu vết các thao tác quan trọng.

## Scope (Phase 1)

**Admin Review queue:**
  - GPS > 300m violations
  - Face verification failures
  - Device change requests
  - Manual check-in/out cases
**Admin actions:**
  - Approve (correct)
  - Reject (wrong / no attendance)
  - Add note
  - Request more info (user notified)
**Effect:**
  - Approve → attendance status updates to AdminApproved
  - Reject → status updates to AdminRejected (no attendance counted)
  - Notify user of decision
**Audit Log:**
  - Captures critical actions (see CR-1)
  - Append-only at DB level
  - Query interface for Admin (filter by actor / action / entity / time)
  - 12-month hot storage, archive to S3 after

## Data Entities

- `admin_reviews`
- `audit_logs`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/admin/reviews/pending — queue`
- `GET /api/v1/admin/reviews/:id — detail (with selfie viewer)`
- `POST /api/v1/admin/reviews/:id/decision — approve/reject + note`
- `GET /api/v1/admin/audit-logs — paginated query with filters`
- `GET /api/v1/admin/audit-logs/export — Excel export`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Web Admin
- Admin Review queue (with badge count)
- Review detail screen (selfie vs enrolled, GPS map, history context)
- Audit Log search/explorer with filters
- Audit Log detail (entity changes timeline)

## Business Rules Applied

- CR-1
- BR-208
- BR-209
- BR-408

See `06-business-rules.md` for rule details.

## Edge Cases

- Multiple Admins reviewing same item → first action wins, others get 409
- Audit log INSERT must succeed atomically with business action (use DB transaction)
- Cannot delete audit logs (enforced by DB role permissions)
- Archived logs (>12mo): query goes through S3, slower

## Key Implementation Notes

- EF Core SaveChangesInterceptor to write audit entries automatically
- OR explicit `IAuditLogService.Log(...)` calls in handlers (more explicit, less magic)
- Recommend explicit logging Phase 1 — easier to debug
- DB role: app user gets INSERT, SELECT only on audit_logs
- Backup process uses separate user with delete privilege
- Photos in review queue: signed URLs, 5min expiry per view

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
