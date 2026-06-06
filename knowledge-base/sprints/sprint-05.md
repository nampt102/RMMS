# Sprint 5 — Phase 1A (W11-W12)

**Goal:** Work Schedule with versioning

**Modules touched:** M7

**Acceptance criteria targeted:** AC-14, AC-15

## Deliverables (Definition of Done)

- [x] Schedule registration day/week/month
- [x] Multi-shift, multi-store
- [x] Versioning: old active while edit pending (BR-308)

## User Stories / Key outcomes

- AC-14: PG/Leader registers schedule by day/week/month
- AC-15: Old schedule remains effective while edit pending

## Tasks by Discipline

### BE (done — built ahead in the M07 sprint, migration `M07_WorkSchedule`)
- [x] work_schedules + shifts entities (`WorkSchedule` + owned `WorkScheduleShift`, status enum pending/approved/rejected/edit_pending/superseded, `Version`+`PreviousVersionId`)
- [x] Schedule CRUD — create / edit (PATCH) / withdraw (soft-delete) / submit / approve / reject
- [x] Version chain logic — `CreateEditedVersion()` + `Supersede()`; editing an approved schedule spawns a new edit_pending version, old stays effective until approved (BR-308)
- [x] Active schedule query — `GET /schedule/me` + `GET /schedule/user/{id}` (Leader-scoped, Admin bypass)

### Mobile
- [~] Schedule calendar UI — list + date picker (no visual grid; acceptable for MVP)
- [x] Day detail — shown inline per card (date, shifts, status, reject reason)
- [x] Register wizard for day/week/month
- [x] Multi-shift editor
- [x] **Edit schedule** (`PATCH /schedule/{id}`) — reuses RegisterScheduleScreen in edit mode; approved-edit triggers BR-308 version flow
- [x] Pending approval status — `StatusPill` (themed) for pending/approved/rejected/edit_pending/superseded

### Web
- [x] Team schedule overview — `/schedules` (user picker + date range + table + approve/reject)

### QA
- [x] Versioning behavior — covered by 16 backend unit tests incl. `Edit_ApprovedSchedule_CreatesEditVersion_OldStaysApproved` + `ApproveEdit_SupersedesOldApproved_NewBecomesEffective`
- [~] Date math edge cases — covered for create/range; mobile manual UAT on Mac

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M7`