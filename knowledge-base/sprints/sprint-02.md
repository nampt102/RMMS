# Sprint 2 — Phase 1A (W5-W6)

**Goal:** Org structure & assignment + Device management

**Modules touched:** M2, M3

**Acceptance criteria targeted:** AC-2, AC-3

## Deliverables (Definition of Done)

- [ ] Admin CRUD for users, stores, areas, categories
- [ ] Assignment management
- [ ] Single-device enforcement for PG
- [ ] Device change request flow end-to-end

## User Stories / Key outcomes

- AC-2: PG chỉ login được trên 1 thiết bị
- AC-3: PG đổi thiết bị phải được Leader/Admin duyệt

## Tasks by Discipline

### BE
- [ ] Entities: stores, areas, categories, assignments
- [ ] CRUD endpoints for each
- [ ] Device fingerprint detection on login
- [ ] Device change request flow
- [ ] FCM token registration

### Web
- [ ] User management screens
- [ ] Store management with map
- [ ] Areas + categories CRUD
- [ ] Assignment matrix UI
- [ ] Device requests list

### Mobile
- [ ] Device-change notification handling
- [ ] Pending approval screen

### QA
- [ ] Assignment edge cases
- [ ] Device change race conditions

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M2, M3`