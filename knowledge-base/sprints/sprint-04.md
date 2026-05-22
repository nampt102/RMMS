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
- [ ] FPT.AI Face client (with Polly retry)
- [ ] Enrollment endpoint
- [ ] Verify endpoint
- [ ] Integrate into check-in/out
- [ ] admin_reviews entity + queue endpoints

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