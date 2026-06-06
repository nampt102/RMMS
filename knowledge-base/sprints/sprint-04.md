# Sprint 4 — Phase 1A (W9-W10)

**Goal:** Face Verification + Attendance integration

**Modules touched:** M5, M6

**Acceptance criteria targeted:** AC-7, AC-8, AC-11, AC-12, AC-13

## Deliverables (Definition of Done)

- [ ] Face enrollment for PG
- [ ] Mandatory face check at check-in/out
- [ ] Face fail flows to Admin Review
- [ ] Initial Admin Review UI

## User Stories / Key outcomes

- AC-7: Face Recognition mandatory at both check-in and out
- AC-8: Selfies captured for review
- AC-11: Admin can review face fail / GPS violation
- AC-12: Admin confirms right person → success
- AC-13: Admin confirms wrong → no attendance

## Tasks by Discipline

### BE
- [x] Face client — **CompreFace** (ADR-011, self-hosted) via `IFaceClient`; `DevFaceClient` fallback when no API key (config-gated like SendGrid/FCM)
- [x] Enrollment endpoint — `POST /api/v1/face/enroll` (multipart, replaces prior subject) + `GET /face/status`
- [x] Verify endpoint — `POST /api/v1/face/verify`; check-in/out verification automatic via `FaceVerificationService`
- [x] Integrate into check-in/out — unenrolled → `FaceFailPendingReview` (BR-206); engine down → pending review (BR-207)
- [x] Admin — `POST /admin/face/re-enroll/:userId`, `DELETE /admin/face/template/:userId`
- [~] admin_reviews entity + queue endpoints — **using M05 status-driven review queue** (no new table; consistent with M05)
- [x] 7 unit tests (enroll / upstream-fail / admin-remove + FaceVerificationService not-enrolled/match/no-match/engine-down) → suite **191 green**

### Mobile
- [ ] Face enrollment wizard
- [ ] Face capture during check-in/out
- [ ] Error handling for face fail

### Web
- [ ] Admin Review queue
- [ ] Review detail (compare selfie vs enrolled)
- [ ] Approve/reject actions

### QA
- [ ] Face accuracy testing on real devices in various lighting
- [ ] Verify Face API timeouts handled

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M5, M6`