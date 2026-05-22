# M10_FORM_ENGINE — Form Engine

## Quick Reference

| | |
|---|---|
| **Module ID** | M10 |
| **Phase** | 1B |
| **Priority** | P0 |
| **Complexity** | Very High |
| **Est. dev-days** | 55 |
| **Sprints** | S11, S12, S13, S14 |
| **Depends on** | M1, M3, M4 |
| **Acceptance criteria** | AC-20, AC-21, AC-22, AC-23, AC-24 |

## Purpose

Cho Admin tự tạo và deploy form mà không cần dev. Mini-product riêng.

## Scope (Phase 1)

**Form Builder (Web Admin):**
  - 12+ input types: text/number/single-choice/multi-choice/dropdown/datetime/image-upload/camera/file/product-selector/store-selector/brand-sku-selector
  - Required / Optional toggle
  - Drag-drop reorder (Phase 1) or simple list (fallback)
**Form Templates (preset for common cases):**
  - Stock report, Market report, Photo report, PC checklist, Free report, Survey, Knowledge test, Training, Visit report
**Form Rules (per form configuration):**
  - Target: PG / Leader / Both
  - Time: valid_from, valid_to, always_on flag
  - Deadline (minutes from assignment or absolute)
  - Store required?
  - Auto-fill store from current check-in?
  - Require check-in to fill?
  - GPS required?
  - Photo required?
  - Use Product Master?
  - Scoring on/off
  - Randomize questions?
  - Randomize answers?
  - Time limit per session
  - Show results: immediately / delayed / never
  - Edit after submit allowed?
  - Offline draft allowed?
**Assignment (Admin):**
  - By role / specific user / store / area / category / product / time window
**Offline Draft (Mobile):**
  - Save draft locally (Hive)
  - Save images in draft
  - Resume on reconnect
  - Submit with idempotency key
**Versioning:**
  - Admin edits published form → new version created
  - Existing submissions reference their version (immutable)

## Data Entities

- `forms`
- `form_versions`
- `form_assignments`
- `form_submissions`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET / POST / PATCH /api/v1/admin/forms — Form Builder CRUD`
- `POST /api/v1/admin/forms/:id/publish — publish version`
- `GET /api/v1/admin/forms/:id/versions — list versions`
- `POST /api/v1/admin/forms/:id/assignments — assign`
- `GET /api/v1/forms/me — list assigned forms (mobile)`
- `GET /api/v1/forms/:id — form schema + rules for filling`
- `POST /api/v1/forms/:id/submit — submit`
- `PATCH /api/v1/form-submissions/:id — edit-after-submit`
- `GET /api/v1/admin/form-submissions — query results`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Web Admin
- Form Builder (drag-drop or list-based)
- Form rules config panel
- Form assignment matrix
- Form list with version history
- Submission viewer / results aggregation

### Mobile (PG/Leader)
- Forms list (assigned, with deadline)
- Form renderer (dynamic based on schema)
- Offline drafts list
- Submission detail
- Test/survey result view (if allowed)

## Business Rules Applied

- BR-501
- BR-502
- BR-503
- BR-504
- BR-505
- BR-506
- BR-507

See `06-business-rules.md` for rule details.

## Edge Cases

- Form deleted while user has draft → notify user, can still submit if version exists
- Form version edited while user filling → new version visible only after refresh
- Photo upload fails mid-submit → keep draft, retry button
- Scoring with multiple correct answers → support 'one of N' and 'all of N' modes
- Random questions: server picks N from pool, returns ordered subset
- Time limit exceeded: auto-submit current answers
- Edit after submit: track edit history (max N edits or unlimited per config)

## Key Implementation Notes

- Schema in JSONB: validate against JSON schema in BE (use NJsonSchema lib)
- Mobile renderer: factory pattern, one widget per input type
- Idempotency key generated client-side at draft start
- Photo storage: pre-sign URL → direct PUT to MinIO → reference in submission
- Submission stored with snapshot of form_version_id (immutable)
- Scoring engine: simple rule-based (per question, exact match or pattern)
- Performance: form list query joins assignments, can be slow at scale — index well
- Form Builder UX is biggest unknown — consider start simple (list-based form editor) then add drag-drop if time

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
