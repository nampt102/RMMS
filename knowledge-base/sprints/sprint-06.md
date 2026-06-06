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
- [x] approvals + approval_email_tokens entities (migration `M09_Approvals`, applied to server DB)
- [x] Approval endpoints — `GET /approvals/pending`, `GET /approvals/:id`, `POST /approvals/:id/{approve,reject}`, `POST /admin/approvals/:id/override`
- [x] HMAC-signed token generation — `IApprovalTokenService` (HS256, 24h TTL, nonce); SHA-256 hash persisted for one-time use
- [x] Public email-link action endpoint — `GET /approvals/email-action` (preview) + `POST /approvals/email-action/confirm` (consume, logs IP/UA), both `[AllowAnonymous]`
- [x] `IApprovalService` producer — creates approval + (BUH) issues token + sends bilingual email
- [~] Wire Schedule into Approval — **deferred** (M07 has its own approve/reject; retrofitting risks regressions — next step)
- [x] Audit log integration — `approval.{requested,approved,rejected,overridden}` (CR-1)
- [x] 17 unit tests (state machine + token round-trip/tamper/expiry + email one-time/expired/preview + producer email) → suite **208 green**

### Mobile
- [x] Approval list (Leader) — `features/approvals` (Freezed `Approval`, api/repo/`pendingApprovalsProvider`), `ApprovalsScreen` with pull-to-refresh + empty/error states (themed `SoftCard`/`StatusPill`)
- [x] Inline approve + reject-with-reason dialog → `POST /approvals/:id/{approve,reject}`; home "Approvals" tile gated to Leader role; ARB vi/en

### Web
- [x] BUH/Leader approval queue — `/approvals` (role-aware ProTable: Leader/BUH pending + approve/reject; Admin all-approvals + override) — needs BE `GET /admin/approvals` (added)
- [x] Email-link landing page — public `/[locale]/approve?token=` (no login; friendly expired/used/already-decided states; approve/reject + reason) — AC-18
- [x] Admin override UI — reason modal → `POST /admin/approvals/:id/override` (AC-19)

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