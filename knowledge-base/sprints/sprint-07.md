# Sprint 7 — Phase 1A (W15-W16)

**Goal:** Leave & OT integration with approval

**Modules touched:** M8, M9

**Acceptance criteria targeted:** AC-16

## Deliverables (Definition of Done)

- [ ] OT, Leave, Emergency leave requests
- [ ] All wired into Approval Workflow
- [ ] Emergency leave tied to check-out flow

## User Stories / Key outcomes

- AC-16: Create OT/Leave/Emergency leave

## Tasks by Discipline

### BE
- [x] leave_requests, ot_requests entities (migration `M08_LeaveOt`, applied to server DB) + enums `LeaveType` / `RequestStatus`
- [x] Request endpoints — `POST /leave-requests`, `POST /leave-requests/emergency`, `GET /leave-requests/me`, `DELETE /leave-requests/:id`; `POST /ot-requests`, `GET /ot-requests/me`
- [x] Emergency leave wired to attendance — requires an open check-in (else 409 `NO_OPEN_ATTENDANCE`); links `linked_attendance_id`
- [x] **Wired into M09 approval** — create routes to the PG's Leader via `IApprovalService` (links `approval_id`); the generalized `ApprovalActuation` drives leave/ot status when the approval is decided (queue/mobile/email-link); withdraw clears the pending approval
- [x] 5 unit tests (route+link / no-leader / emergency-409 / approve→leave / reject→ot) → suite **215 green**

### Mobile
- [ ] Leave/OT request forms
- [ ] Emergency leave action from check-out flow
- [ ] Request history

### Web
- [x] Admin view of all requests — `/requests` (tabs Leave / OT, status filter, requester name). BE `GET /admin/leave-requests` + `/admin/ot-requests` (paginated). Override via the `/approvals` queue (admin)

### QA
- [ ] End-to-end approval flow tests

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M8, M9`