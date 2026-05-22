# M09_APPROVAL_WORKFLOW — Approval Workflow

## Quick Reference

| | |
|---|---|
| **Module ID** | M09 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 30 |
| **Sprints** | S6 |
| **Depends on** | M1 |
| **Acceptance criteria** | AC-17, AC-18, AC-19 |

## Purpose

Workflow engine dùng chung cho Schedule, OT, Leave, Visit Plan.

## Scope (Phase 1)

- Generic approval entity (`approvals` table)
- Routing: PG → Leader, Leader → BUH
- Hybrid: inline approval (list view) + detail view
- Reject mandatory reason
- Admin override với reason + audit
- BUH email-link approval (no login)
-   - Signed token (HMAC), 24h TTL, single-use
-   - Log IP/UA on use
-   - Show user-friendly result page
- Notification to requester on decision

## Data Entities

- `approvals`
- `approval_email_tokens`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `GET /api/v1/approvals/pending — pending for current user`
- `GET /api/v1/approvals/:id — detail`
- `POST /api/v1/approvals/:id/approve — approve`
- `POST /api/v1/approvals/:id/reject — reject (reason required)`
- `POST /api/v1/admin/approvals/:id/override — override (Admin)`
- `GET /api/v1/approvals/email-action?token=...&action=... — public BUH endpoint`
- `POST /api/v1/approvals/email-action/confirm — submit decision via link`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (Leader)
- Pending approvals list (inline approve)
- Approval detail

### Web BUH
- Pending approvals list
- Approval detail

### Email
- HTML email with approve/reject buttons (BUH)

### Web (public)
- Email-link landing page with confirm action

### Web Admin
- All approvals search/override

## Business Rules Applied

- BR-401
- BR-402
- BR-403
- BR-404
- BR-405
- BR-406
- BR-407
- BR-408

See `06-business-rules.md` for rule details.

## Edge Cases

- Email link expired → friendly page 'Link expired, please use app'
- Email link used twice → friendly page 'Already decided'
- Approver changes role (no longer leader) → existing pending stays with old leader
- Concurrent approve+override → first wins, second returns 409

## Key Implementation Notes

- Strategy: simple state machine (Pending → Approved/Rejected/Overridden) — no need for Workflow Core library Phase 1
- Approval token JWT-like format: header.payload.signature
-   payload = { approval_id, approver_user_id, action_options, expires_at, nonce }
-   signature = HMAC-SHA256(payload, secret_key)
- Use a strong server-side secret in env var
- On token consume, mark token used (one-time)
- Email template bilingual based on approver's preferred_language

## Definition of Done

This module is considered DONE when:
- [ ] All endpoints implemented and documented in Swagger
- [ ] Unit tests cover happy path + error cases (≥70%)
- [ ] Integration tests via Testcontainers for critical flows
- [ ] Mobile/Web screens implemented per spec
- [ ] i18n strings present for both `vi` and `en`
- [ ] Acceptance criteria listed above pass manual verification
- [ ] Audit log entries for relevant actions (see CR-1)
- [ ] PR reviewed and merged
- [ ] Deployed to staging and smoke-tested
