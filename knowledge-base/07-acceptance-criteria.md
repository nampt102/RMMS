# 07 — Acceptance Criteria

35 acceptance criteria for Phase 1 from PRD section 8. ALL must pass for formal acceptance at end of Phase 1B.

Format: `AC-{N}` | `{description}` | `{module ref}` | `{test approach}` | `{status}`

| ID | Criterion | Module | How to verify |
|---|---|---|---|
| AC-1 | PG registers via email and can login to mobile app | M1 | Integration test + manual UAT: register flow, email verification, login |
| AC-2 | PG can only login on ONE device at a time | M2 | Integration test: login on device A → login attempt device B blocked until approval |
| AC-3 | PG device change must be approved by Leader/Admin | M2 | E2E: login new device → triggers request → Leader app receives → approve → login succeeds |
| AC-4 | PG/Leader can check-in/check-out at assigned store | M5 | E2E: check-in at assigned store works; check-in at non-assigned store fails with `STORE_NOT_ASSIGNED` |
| AC-5 | Check-in allowed up to 60 minutes before shift start | M5 | Unit + integration: time math, boundary tests at 59min, 60min, 61min |
| AC-6 | Check-in >5 min after shift start is marked `Late` | M5 | Unit + integration: boundary tests at 4min, 5min, 6min |
| AC-7 | Check-in and check-out BOTH require Face Recognition | M6 | E2E: attempt without face → blocked |
| AC-8 | Both check-in and check-out capture a selfie for review | M5, M6 | E2E: verify selfie URL stored, viewable by Admin |
| AC-9 | GPS distance >300m from store → sent to Admin Review | M5 | Unit + E2E: stub GPS coords, verify status `GpsViolationPendingReview` and Admin queue entry |
| AC-10 | Fake GPS detected → blocked (no valid attendance) | M5 | Mobile test with mock location app; verify `FakeGpsBlocked` status |
| AC-11 | Admin can review Face fail / GPS violation cases | M16 | E2E: Admin opens review queue, sees pending items, takes action |
| AC-12 | Admin confirms RIGHT person → check-in/out succeeds | M16 | E2E: face fail case → Admin approves → attendance status → `AdminApproved` |
| AC-13 | Admin confirms WRONG person → no attendance | M16 | E2E: face fail case → Admin rejects → attendance status → `AdminRejected`; user notified |
| AC-14 | PG/Leader registers schedule by day/week/month | M7 | E2E: three registration paths all work |
| AC-15 | Old schedule remains effective while edit is pending | M7 | Integration: approved schedule → edit → during pending, old version queried |
| AC-16 | PG/Leader can create OT / Leave / Emergency leave | M8 | E2E: all 3 request types create-able |
| AC-17 | Leader can approve/reject PG requests | M9 | E2E: PG creates request → Leader app shows → approve → status updates → PG notified |
| AC-18 | BUH can approve via email link WITHOUT login | M9 | E2E: email link click → simple approve/reject page → action recorded |
| AC-19 | Admin override works and is in audit log | M9, M16 | E2E: Admin overrides a decision → entry in audit_logs with reason |
| AC-20 | Admin can create forms via Form Engine | M10 | E2E: open Form Builder → drag inputs → save → form available |
| AC-21 | Editing a published form creates a NEW version | M10 | Integration: publish v1 → edit → v2 created → v1 submissions intact |
| AC-22 | PG/Leader can fill assigned forms | M10 | E2E: assign form → user sees in list → fills → submits |
| AC-23 | Forms support offline draft | M10 | Mobile test: airplane mode → fill form → save draft → reconnect → submit |
| AC-24 | Edit-after-submit works per form configuration | M10 | E2E: form configured allow_edit → user edits submitted form → version tracked |
| AC-25 | Product Master visible (read-only) in mobile app | M4 | E2E: open product selector in form → see products |
| AC-26 | Leader can view PG Online status | M12 | E2E: PG checks in → Leader's screen shows them online |
| AC-27 | Admin/BUH can view PG/Leader Online + check-in/out today | M12, M15 | E2E: dashboard shows counts and list |
| AC-28 | Leader can create Visit Plan for themselves | M11 | E2E: create visit plan with stores + forms |
| AC-29 | BUH can approve Visit Plan | M11 | E2E: BUH receives notification, approves via web or email link |
| AC-30 | Leader submits post-visit report via Form Engine | M11 | E2E: executed visit → submit visit_report form → linked to plan |
| AC-31 | Admin uploads public AND private documents | M13 | E2E: upload, assign, PG sees public; only assigned user sees private |
| AC-32 | Admin sends payslip as private file | M13 | E2E: upload payslip → assign to user → only that user sees |
| AC-33 | Admin sends News and Notifications | M14 | E2E: create news → publish → users see in app + push received |
| AC-34 | Important news requires read confirmation | M14 | E2E: important news → user must click confirm → badge until confirmed |
| AC-35 | Audit log captures critical actions | M16 | Integration: perform critical actions, query audit_logs, verify entries |

## Verification Strategy

### Automated coverage targets
- Unit tests: ≥70% on Domain + Application layers
- Integration tests: ≥80% of critical paths
- E2E tests: All 35 ACs have at least one E2E test
- Mobile golden tests for key screens

### Manual / UAT
- Final 2 sprints (S17, S18): full UAT walkthrough
- Each AC demoed live to stakeholder
- Sign-off document with stakeholder signature per AC

### Tooling
- BE: xUnit + Moq + Testcontainers (real PostgreSQL)
- Mobile: flutter_test + integration_test + Mocktail
- Web: Vitest + React Testing Library + Playwright
- Manual: documented test plans in `test-plans/` folder (separate)

## Notes per Acceptance Criterion

### AC-2 & AC-3 Device handling
- "One device" means one active session, not one device record forever
- History of devices preserved in `user_devices` (status `replaced`)
- When new device is approved, old refresh tokens revoked, old sessions force-logged-out

### AC-7 Face Recognition mandatory
- Even if Face API is down: the system should NOT silently bypass
- If API unavailable: queue check-in as `pending_review`, notify Admin
- Document this behavior in runbook

### AC-10 Fake GPS detection limitations
- Android: `mock_location` flag check + `trust_location` plugin
- iOS: no native fake GPS support, but JailbreakDetection helps catch tweaked devices
- 100% detection impossible — document policy clearly to users
- Admin Review is the safety net

### AC-15 Schedule versioning
- Implementation choice: chain of versions via `previous_version_id`
- Active query: "give me the schedule for this PG on this date" returns the **approved** version
- After approve of edit: old version status → `superseded`

### AC-18 BUH email link security
- Token TTL: 24h
- One-time use
- HMAC signed
- Log IP + UA on use
- Show user-friendly page (not raw 401 if expired)

### AC-21 Form versioning
- Submissions FK to `form_versions.id`, never `forms.id` alone
- `forms.current_version` points to latest published version
- Listing UI shows current version's labels

### AC-23 Offline draft
- Drafts stored in Hive box `form_drafts`
- Each draft includes `client_idempotency_key` (UUID generated at draft creation)
- On reconnect, retry submission with same key

### AC-35 Audit log scope
- Action set defined in business rules CR-1
- Append-only: app DB role granted INSERT, SELECT only — no UPDATE/DELETE
- Backup user has DELETE for retention archiving

## Sign-off Template

```
PHASE 1 ACCEPTANCE SIGN-OFF

Date: ____________
Project: RMMS 2026 — Phase 1
Build version: ____________

Acceptance Criteria Status:
[ ] AC-1   [ ] AC-13   [ ] AC-25
[ ] AC-2   [ ] AC-14   [ ] AC-26
[ ] AC-3   [ ] AC-15   [ ] AC-27
[ ] AC-4   [ ] AC-16   [ ] AC-28
[ ] AC-5   [ ] AC-17   [ ] AC-29
[ ] AC-6   [ ] AC-18   [ ] AC-30
[ ] AC-7   [ ] AC-19   [ ] AC-31
[ ] AC-8   [ ] AC-20   [ ] AC-32
[ ] AC-9   [ ] AC-21   [ ] AC-33
[ ] AC-10  [ ] AC-22   [ ] AC-34
[ ] AC-11  [ ] AC-23   [ ] AC-35
[ ] AC-12  [ ] AC-24

Outstanding issues: (list any known issues accepted with workarounds)
1. ____________________________________
2. ____________________________________

Approved for production:
Stakeholder name: ____________
Signature: ____________
Date: ____________
```
