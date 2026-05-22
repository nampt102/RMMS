# Sprint 3 — Phase 1A (W7-W8)

**Goal:** Attendance core (without Face Recognition yet)

**Modules touched:** M5

**Acceptance criteria targeted:** AC-4, AC-5, AC-6, AC-9, AC-10

## Deliverables (Definition of Done)

- [ ] Check-in/out flow with GPS + selfie + store photo
- [ ] Fake GPS detection
- [ ] Attendance status state machine
- [ ] History view

## User Stories / Key outcomes

- AC-4: PG/Leader check-in/check-out tại assigned store
- AC-5: Early check-in within 60min
- AC-6: Late marking
- AC-9: GPS >300m → Admin Review queue
- AC-10: Fake GPS blocked

## Tasks by Discipline

### BE
- [ ] attendance_records entity
- [ ] Check-in/out endpoints (multipart)
- [ ] GPS validation logic (Haversine)
- [ ] Fake GPS detection logic
- [ ] Status state machine
- [ ] MinIO file upload for selfies
- [ ] Hangfire job placeholders

### Mobile
- [ ] Check-in screen with camera
- [ ] Check-out screen
- [ ] geolocator integration
- [ ] trust_location for fake GPS
- [ ] Camera capture + compress photos
- [ ] EXIF write for store photo
- [ ] Attendance history list

### Web
- [ ] Attendance list (Admin read-only first)

### QA
- [ ] GPS edge cases
- [ ] Fake GPS testing on Android

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M5`