# Sprint 2 — Phase 1A (W5-W6)

> **STATUS: ✅ CLOSED (2026-06-05).** Core goal + AC-2 + AC-3 delivered (M02 + M03, BE/web/mobile, 149 unit tests green). The two deferred items were cleared on **2026-06-06** (store map view + mobile FCM client-side) — see "Deferred" below.

**Goal:** Org structure & assignment + Device management

**Modules touched:** M2, M3

**Acceptance criteria targeted:** AC-2, AC-3

## Deliverables (Definition of Done)

- [x] Admin CRUD for users, stores, areas, categories
- [x] Assignment management
- [x] Single-device enforcement for PG
- [x] Device change request flow end-to-end

## Deferred (tracked, non-blocking)

- **Store map view (web)** — ✅ **DONE 2026-06-06.** [ADR-010](../decisions/ADR-010-store-map-react-leaflet.md) accepts **react-leaflet + OpenStreetMap**. `/stores` now has a `Segmented` table/map toggle; map plots status-colored markers (active=green, inactive=gray) with code/name/status/address popups, fit-bounds, area/status/search filters, reduced-motion, and lazy-loaded Leaflet (`StoreMap.tsx` / `StoreMapView.tsx`). Web type-check + prod build green.
- **Mobile device-change push notification handling** — ✅ **DONE 2026-06-06 (client-side).** `core/notifications/` adds a fail-safe `FcmService` (guarded `Firebase.initializeApp`, permission, token, refresh, fg/opened/initial streams), `FcmCoordinator` (wires streams → state), foreground in-app `MaterialBanner` (`app.dart`), and `device_pending_screen` now reacts live to a `device_changed` push (approved → "sign in again" CTA; rejected → contact-admin state). FCM token is now actually sent on login (`auth_repository` → `auth_api.login(fcmToken:)`). **Server-side push delivery remains →M14 Notification.** Build needs `flutterfire configure` (Firebase config files) on the Mac to emit real tokens; without it the guarded init keeps the app running with FCM disabled.
- **CSV bulk assignment** — spec-marked Phase 2 (still deferred).

## User Stories / Key outcomes

- AC-2: PG chỉ login được trên 1 thiết bị
- AC-3: PG đổi thiết bị phải được Leader/Admin duyệt

## Tasks by Discipline

### BE
- [x] Entities: stores, areas, categories, assignments
- [x] CRUD endpoints for each
- [x] Device fingerprint detection on login
- [x] Device change request flow
- [x] FCM token registration (login accepts `device.fcmToken`; mobile now sends it, 2026-06-06)

### Web
- [x] User management screens
- [x] Store management with map (ADR-010: react-leaflet + OSM, 2026-06-06)
- [x] Areas + categories CRUD
- [x] Assignment matrix UI
- [x] Device requests list

### Mobile
- [x] Device-change notification handling (client-side FCM, 2026-06-06; server push →M14)
- [x] Pending approval screen

### QA
- [ ] Assignment edge cases
- [ ] Device change race conditions

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M2, M3`