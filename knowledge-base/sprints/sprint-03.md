# Sprint 3 — Phase 1A (W7-W8)

**Goal:** Attendance core (without Face Recognition yet)

**Modules touched:** M5

**Acceptance criteria targeted:** AC-4, AC-5, AC-6, AC-9, AC-10

## Deliverables (Definition of Done)

- [x] Check-in/out flow with GPS + selfie + store photo — BE + web + mobile (photos stubbed → M13/MinIO)
- [x] Fake GPS detection — `isMocked` flag (mobile) → BR-205 block + audit (BE)
- [x] Attendance status state machine — `AttendanceStatus` (§3.2), owned by `AttendanceRecord`
- [x] History view — mobile `AttendanceHistoryScreen` + web admin list/review queue (AC-9)

## User Stories / Key outcomes

- AC-4: PG/Leader check-in/check-out tại assigned store
- AC-5: Early check-in within 60min
- AC-6: Late marking
- AC-9: GPS >300m → Admin Review queue
- AC-10: Fake GPS blocked

## Tasks by Discipline

### BE
- [x] attendance_records entity (`AttendanceRecord` aggregate + enums)
- [x] Check-in/out endpoints (multipart) — `AttendanceController`
- [x] GPS validation logic (Haversine) — via `GpsCoordinate.DistanceMetersTo`, 300 m geofence
- [x] Fake GPS detection logic — block + audit (BR-205), no valid record
- [x] Status state machine — `DetermineCheckInStatus` + check-out escalation + admin review
- [~] MinIO file upload for selfies — **stubbed** (`IAttendancePhotoStorage` + `LocalAttendancePhotoStorage`) → real MinIO at M13
- [~] Hangfire job placeholders — deferred (Face retry job lands with M06)
- [x] Face Verification abstraction — **stubbed** (`IFaceVerificationService`) → FPT.AI at M06
- [x] 18 unit tests (status matrix, early/late, gps, fake-gps, double check-in, check-out, review, queries)

### Mobile (code-only — Flutter on Mac)
- [x] Check-in screen with camera — `CheckInScreen` (image_picker)
- [x] Check-out screen — same screen, `CheckMode.checkOut`
- [x] geolocator integration — GPS + accuracy
- [x] fake GPS — geolocator `Position.isMocked` (replaces planned trust_location)
- [x] Camera capture + compress photos — image_picker `imageQuality`/`maxWidth`
- [~] EXIF write for store photo — deferred to M13 (server reads EXIF on real upload)
- [x] Attendance history list — `AttendanceHistoryScreen` (ListView.builder)

### Web
- [x] Attendance list (Admin) — `/attendance` ProTable + filters + status Tag (icon+text) + review modal

### QA
- [ ] GPS edge cases
- [ ] Fake GPS testing on Android

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M5`