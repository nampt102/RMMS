# Sprint 6 — Phase 1A (W13-W14)

**Goal:** Approval Workflow Engine + BUH email-link

**Modules touched:** M9

**Acceptance criteria targeted:** AC-17, AC-18, AC-19

## Deliverables (Definition of Done)

- [ ] Generic approval entity
- [ ] PG→Leader, Leader→BUH routing
- [ ] BUH email-link approval working
- [ ] Admin override with audit

## User Stories / Key outcomes

- AC-17: Leader approves/rejects PG requests
- AC-18: BUH approves via email link without login
- AC-19: Admin override works with audit log

## Tasks by Discipline

### BE
- [ ] approvals + approval_email_tokens entities
- [ ] Approval endpoints (approve/reject/override)
- [ ] HMAC-signed token generation
- [ ] Public email-link action endpoint
- [ ] Wire Schedule into Approval
- [ ] Audit log integration

### Mobile
- [ ] Approval list (Leader)
- [ ] Approval detail with inline approve

### Web
- [ ] BUH approval list + detail
- [ ] Email-link landing page
- [ ] Admin override UI

### DevOps
- [ ] SendGrid integration + email templates

### QA
- [ ] Email link security testing
- [ ] Token reuse prevention

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M9`