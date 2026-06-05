# Sprint 2 — Phase 1A (W5-W6)

> **STATUS: ✅ CLOSED (2026-06-05).** Core goal + AC-2 + AC-3 delivered (M02 + M03, BE/web/mobile, 149 unit tests green). Two items deferred with owners (see "Deferred" below) — not blocking.

**Goal:** Org structure & assignment + Device management

**Modules touched:** M2, M3

**Acceptance criteria targeted:** AC-2, AC-3

## Deliverables (Definition of Done)

- [x] Admin CRUD for users, stores, areas, categories
- [x] Assignment management
- [x] Single-device enforcement for PG
- [x] Device change request flow end-to-end

## Deferred (tracked, non-blocking)

- **Store map view (web)** — list + CRUD + lat/lon shipped; interactive map view needs a map library (react-leaflet / Google Maps) → **requires a new ADR** (no new UI lib without ADR per CLAUDE.md). Deferred until that ADR.
- **Mobile device-change push notification handling** — pending-approval screen shipped; FCM **push delivery** deferred to **M14 Notification** (FCM infra). FCM token is already captured/updated on login (`device.fcmToken`).
- **CSV bulk assignment** — spec-marked Phase 2.

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