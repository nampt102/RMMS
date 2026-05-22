# M13_DOCUMENT_CENTER — Document Center

## Quick Reference

| | |
|---|---|
| **Module ID** | M13 |
| **Phase** | 1B |
| **Priority** | P1 |
| **Complexity** | Medium |
| **Est. dev-days** | 16 |
| **Sprints** | S15 |
| **Depends on** | M1, M3, M14 |
| **Acceptance criteria** | AC-31, AC-32 |

## Purpose

Quản lý tài liệu public/private cho PG/Leader, bao gồm payslip dạng private file.

## Scope (Phase 1)

- Folder Public / Private
- Admin upload tài liệu
- Admin gán tài liệu cho user / role
- PG/Leader xem tài liệu
- Support PDF, image (jpg/png), text
- Search by name
- Notification khi có tài liệu mới
- Payslip = private file gán cho 1 user cụ thể

## Data Entities

- `documents`
- `document_assignments`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/admin/documents — upload (multipart)`
- `POST /api/v1/admin/documents/:id/assignments — assign`
- `GET /api/v1/documents/me — list documents accessible to current user`
- `GET /api/v1/documents/:id/download — signed URL (5min expiry)`
- `DELETE /api/v1/admin/documents/:id`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Documents list (public + private mixed, separated by section)
- Document viewer (PDF in-app, image in-app, others via system viewer)
- Search bar

### Web Admin
- Upload UI
- Documents list
- Assignment management

## Business Rules Applied

- (No specific decision tables; standard CRUD logic)

See `06-business-rules.md` for rule details.

## Edge Cases

- Large PDF (>10MB): stream, don't load fully in mobile
- Document deleted while user viewing: signed URL still works until expiry
- Private payslip: signed URL with very short expiry (60s) recommended

## Key Implementation Notes

- Files in MinIO with random keys (not predictable filenames)
- Access check on each download URL request
- Mobile: cache last 10 viewed documents (optional Phase 1)
- Audit log: download event for private files

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
