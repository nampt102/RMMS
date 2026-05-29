# Sprint 01 — Identity, Device Foundation (Phase 1A, W3-W4)

**Dates:** 2026-05-28 → 2026-06-11 (10 working days)
**Modules:** M01 Identity & Access (full) + M02 Device Management (foundation)
**Acceptance Criteria targeted:** AC-1 (PG email register + login), AC-2 (PG locked to 1 device, hard requirement)
**Capacity:** 2 senior devs × 10 days × 80% = ~32 dev-days net; plan budgets 30 to leave 2-day buffer.

> Why M01 + part of M02 in one sprint? BR-105 ("PG has exactly 1 active device") must apply from the very first `POST /auth/login` call. Shipping login without the device check would create a security gap that's expensive to retrofit. We include the **device fingerprint + active-device check** in this sprint; the **device-change approval workflow** lives in Sprint 02.

---

## 1. Sprint Goal

> By end of Sprint 01, a fresh PG can install the mobile app, register with email, verify, log in, see their profile via `/auth/me`, and have all activity recorded in `audit_log` per CR-1. Login on a *second* device returns `DEVICE_NOT_AUTHORIZED` (creates the request row, but Leader/Admin approval UI is deferred to Sprint 02).

This satisfies AC-1 fully and AC-2 at the backend contract level.

---

## 2. In Scope

### Modules

| Module | Coverage | Notes |
|---|---|---|
| **M01 Identity & Access** | Full | All 10 endpoints, all screens, email flow, audit log |
| **M02 Device Management** | Foundation only | `user_devices` entity, login-time device check, `DEVICE_NOT_AUTHORIZED` response, fingerprint recording. **NOT in scope:** approval endpoints, Leader/Admin device-request UI (Sprint 02). |

### Business rules implemented

- **BR-101**: PG registers via email
- **BR-102**: Leader account provisioned by Admin (no self-register)
- **BR-103**: BUH account provisioned by Admin
- **BR-104**: Admin account provisioned via seed CLI
- **BR-105**: PG has EXACTLY 1 active device (enforced on every login + token refresh)
- **BR-106**: Foundation row exists for device-change request (approval workflow in Sprint 02)
- **CR-1**: Audit log for login/logout success+failure, device change request, admin user create/update

### Cross-cutting (must be wired in this sprint, not as afterthought)

- ✅ `audit_log` table + append-only enforcement at DB user level
- ✅ `X-Idempotency-Key` middleware for mutation endpoints
- ✅ Error envelope per `05-api-conventions.md` for ALL endpoints
- ✅ Localization (vi/en) for ALL user-visible strings (mobile + web + error messages)
- ✅ Serilog correlation IDs on every log line + traceId in error response
- ✅ Rate limiting on `/auth/login` (5 fails → 15min lockout per BR / M01 edge case)
- ✅ Health check endpoints `/health/ready` updated to verify DB + Redis connectivity

---

## 3. Out of Scope (explicit, prevents scope creep)

- ❌ Device-change approval UI (Sprint 02)
- ❌ Leader assignment management (Sprint 02 / M03)
- ❌ Store assignment (Sprint 03 / M03)
- ❌ Face enrollment (Sprint 04 / M06) — only `face_enrolled_at` + `face_template_external_id` columns reserved
- ❌ MFA / SSO / SAML (Phase 2)
- ❌ Multi-tenant tenancy (Phase 2)
- ❌ Account self-deletion (Phase 2)
- ❌ Password complexity beyond "min 8 chars + 1 letter + 1 digit"
- ❌ CAPTCHA (deferred until abuse is observed)

---

## 4. Data Model — First EF Core Migration

Single migration `Init_M01_M02_Foundation` creates these tables (all with `id`, `created_at`, `updated_at`, soft delete via `deleted_at` where applicable per `04-data-model.md`):

| Table | Source spec | Reason for inclusion |
|---|---|---|
| `users` | `04-data-model.md` §Core | M01 core |
| `refresh_tokens` | `04-data-model.md` §Core | M01 core |
| `login_history` | `04-data-model.md` §Core | M01 core (auth-related, separate from audit_log) |
| `user_devices` | `04-data-model.md` §Core | M02 — required for BR-105 from day 1 |
| `audit_log` | `04-data-model.md` §M16 (append-only) | Cross-cutting CR-1 |
| `email_verification_tokens` | M01 spec | NEW — store hashed verification tokens |
| `password_reset_tokens` | M01 spec | NEW — store hashed reset tokens |

**Indexes (must be in initial migration):**
- `users(email)` UNIQUE
- `users(status, deleted_at)` partial WHERE deleted_at IS NULL
- `refresh_tokens(token_hash)` UNIQUE
- `refresh_tokens(user_id, revoked_at)` for fast "active token for user" lookup
- `refresh_tokens(expires_at)` for cleanup job
- `login_history(user_id, created_at DESC)` for "recent logins" query
- `user_devices(user_id) WHERE status = 'active'` — supports BR-105 uniqueness
- `audit_log(actor_user_id, created_at DESC)` — admin audit search
- `audit_log(target_entity, target_id, created_at DESC)` — entity history

**Database-level enforcement of CR-1 (audit_log append-only):**
```sql
-- Run during seed / first migration apply
REVOKE UPDATE, DELETE ON audit_log FROM rmms_app;
GRANT INSERT, SELECT ON audit_log TO rmms_app;
```

This is enforced at PostgreSQL role level, not application level — see ADR-004 commentary.

---

## 5. API Surface

### Public (no auth)
- `POST /api/v1/auth/register` — PG self-registration with email
- `POST /api/v1/auth/verify-email?token=...` — confirm email (single-use, 24h TTL)
- `POST /api/v1/auth/login` — issue access (15min) + refresh (30d), records device, returns 403 `DEVICE_NOT_AUTHORIZED` if device unknown for existing PG
- `POST /api/v1/auth/refresh` — rotate tokens (old refresh marked revoked, new issued)
- `POST /api/v1/auth/logout` — revoke refresh token (per device)
- `POST /api/v1/auth/forgot-password` — send reset link
- `POST /api/v1/auth/reset-password` — apply new password with token

### Authenticated (any role)
- `GET /api/v1/auth/me` — current user profile + device

### Admin only
- `POST /api/v1/admin/users` — create Leader/BUH/Admin (sets initial password, emails it; user changes on first login)
- `GET /api/v1/admin/users` — paginated list with filters (role, status, search by email/name)
- `GET /api/v1/admin/users/:id` — single user detail
- `PATCH /api/v1/admin/users/:id` — update status (`active` ↔ `inactive`), name, phone
- `POST /api/v1/admin/users/:id/reset-password` — admin-triggered password reset

### Mobile-only headers required (per `05-api-conventions.md`)
- `X-Device-Id` — REQUIRED on `/auth/login` and `/auth/refresh`
- `X-App-Version` — REQUIRED for telemetry
- `X-Idempotency-Key` — RECOMMENDED on `register`, `verify-email`, `forgot-password`, `reset-password`

### Error codes catalogue (added in this sprint, lives in `Rmms.Shared/Errors/ErrorCodes.cs`)

| Code | HTTP | Localized message key |
|---|---|---|
| `EMAIL_ALREADY_REGISTERED` | 409 | `errors.email.duplicate` |
| `EMAIL_NOT_VERIFIED` | 403 | `errors.email.not_verified` |
| `INVALID_CREDENTIALS` | 401 | `errors.auth.invalid_credentials` |
| `ACCOUNT_INACTIVE` | 403 | `errors.auth.account_inactive` |
| `ACCOUNT_LOCKED` | 423 | `errors.auth.locked` (after 5 failed attempts) |
| `DEVICE_NOT_AUTHORIZED` | 403 | `errors.device.not_authorized` |
| `REFRESH_TOKEN_INVALID` | 401 | `errors.token.refresh_invalid` |
| `REFRESH_TOKEN_REUSED` | 401 | `errors.token.refresh_reused` (rotation detected reuse → revoke all) |
| `EMAIL_TOKEN_EXPIRED` | 410 | `errors.email_token.expired` |
| `EMAIL_TOKEN_USED` | 410 | `errors.email_token.used` |
| `RESET_TOKEN_INVALID` | 400 | `errors.reset_token.invalid` |
| `RATE_LIMITED` | 429 | `errors.rate_limited` |
| `PERMISSION_DENIED` | 403 | `errors.permission_denied` |

---

## 6. Day-by-Day Plan

> Notation: **[BE]** = backend dev, **[FE-M]** = mobile dev (Flutter), **[FE-W]** = web dev (Next.js). Each task line lists the discipline, expected dev-days, and which dev (D1 / D2 ; with PM at -20% capacity overlapping D1).

### **Week 1: Backend foundation + mobile shell**

#### Day 1 (Mon 05-28) — Migration + DI setup
- [BE-D1] `Init_M01_M02_Foundation` EF migration (all 7 tables + indexes + audit_log role grant) — **1d**
- [BE-D1] `DependencyInjection.cs` wiring: `IJwtTokenService`, `IPasswordHasher` (BCrypt cost 12), `IEmailSender` (stubbed → console in dev, SendGrid in prod), `IAuditLogger` — **0.5d**
- [BE-D2] `Rmms.Domain.Users`: `User`, `UserStatus`, `UserRole` value objects + invariants — **0.5d**
- [BE-D2] `Rmms.Domain.Devices`: `UserDevice`, `DeviceStatus` value objects — **0.5d**
- [FE-M-D2] Mobile project: bump deps verified, run `flutter pub get` clean — **0.5d**

#### Day 2 (Tue 05-29) — JWT issuance + register endpoint
- [BE-D1] `JwtTokenService` (HS256, secret from config) — emits per `05-api-conventions.md` payload, satisfies `device_id` claim — **0.5d**
- [BE-D1] `POST /auth/register` handler + `RegisterUserCommand` + FluentValidation rules (email format, password ≥8 chars + 1 letter + 1 digit) — **0.5d**
- [BE-D1] Audit log emit on register (action: `user.registered`) — **0.25d**
- [BE-D2] `POST /auth/verify-email` handler + token table CRUD — **0.5d**
- [BE-D2] `EmailSender` console implementation + Serilog email-content logging in Dev — **0.5d**
- [FE-M-D2] Login screen wire-up to Dio client (no actual call yet) — **0.5d**

#### Day 3 (Wed 05-30) — Login + device fingerprint + refresh
- [BE-D1] `POST /auth/login` handler — credential validation, device check (BR-105), 403 `DEVICE_NOT_AUTHORIZED` path, login_history insert — **1d**
- [BE-D1] Audit log emit on login success+fail (action: `auth.login_success`, `auth.login_failed`) — **0.25d**
- [BE-D2] `POST /auth/refresh` with rotation, reuse detection (if same token used twice → revoke ALL user's refresh tokens + emit `auth.refresh_reused` audit) — **0.75d**
- [BE-D2] `POST /auth/logout` — revoke refresh token for current device — **0.25d**
- [FE-M-D1] Register screen (full UX, validation, error display from envelope) — **1d**

#### Day 4 (Thu 05-31) — Password reset + admin user CRUD
- [BE-D1] `POST /auth/forgot-password` + `POST /auth/reset-password` (single-use token, 24h TTL, hashed in DB) — **0.5d**
- [BE-D1] `POST /admin/users` + `GET /admin/users` + filter + pagination — **0.5d**
- [BE-D2] `PATCH /admin/users/:id` (status toggle, name/phone update) + audit emit — **0.5d**
- [BE-D2] `POST /admin/users/:id/reset-password` (admin-triggered) — **0.25d**
- [BE-D2] Admin seed CLI command `dotnet run --project src/Rmms.Api -- seed-admin --email=admin@... --password=...` — **0.25d**
- [FE-M-D1] Email-verify screen + deep link handling (`rmms://verify-email?token=...`) — **1d**

#### Day 5 (Fri 06-01) — Authorization policies + middleware + auth-related UI
- [BE-D1] Authorization policies: `PgOnly`, `LeaderOnly`, `BuhOnly`, `AdminOnly`, `PgOrLeader`, `AnyAuthenticated` — **0.5d**
- [BE-D1] JWT middleware claims projection (`User`, `Role`, `DeviceId`) — **0.25d**
- [BE-D1] `X-Idempotency-Key` middleware (Redis-backed, 24h TTL, returns cached response if hit) — **0.5d**
- [BE-D1] Rate-limit policy on `/auth/login` (5 fails / 15min per email+IP, stored in Redis) — **0.5d**
- [BE-D2] `GET /auth/me` endpoint with device info — **0.25d**
- [BE-D2] Backend integration tests for register + verify + login + refresh + logout (Testcontainers Postgres + Redis) — **1d**
- [FE-M-D2] Login screen full (calls `/auth/login`, stores tokens in `flutter_secure_storage`, handles `DEVICE_NOT_AUTHORIZED` with pending-screen) — **1d**
- [FE-W-D2] Web Admin login screen (uses `useLoginMutation`, persists in Zustand+localStorage hooks) — **0.5d**

### **Week 2: Mobile/Web full coverage + i18n + audit + Sprint 02 prep**

#### Day 6 (Mon 06-04) — Mobile flows complete
- [FE-M-D1] Forgot-password + reset-password screens (with deep-link `rmms://reset-password?token=...`) — **1d**
- [FE-M-D1] Auto-refresh interceptor in Dio (on 401 → call /refresh → retry once) — **0.5d**
- [FE-M-D2] "Pending device approval" screen (state for `DEVICE_NOT_AUTHORIZED` response — polls `/auth/me/device-status` later in Sprint 02) — **0.5d**
- [BE-D1] `GET /auth/me/device-status` skeleton returning `pending` (full polling/notification in Sprint 02) — **0.25d**
- [BE-D2] Verification cleanup Hangfire job (expire >24h tokens, revoke expired refresh tokens) — **0.5d**

#### Day 7 (Tue 06-05) — Web Admin user management
- [FE-W-D1] User management list page (AntD ProTable with filters: role, status, search) — **1d**
- [FE-W-D1] Create user form (Leader/BUH/Admin) — **0.5d**
- [FE-W-D1] User detail page with status toggle + reset-password action — **0.5d**
- [FE-W-D2] Logged-in route guard (middleware checks JWT + redirects unauthenticated to /login) — **0.5d**
- [BE-D2] Refresh-token reuse detection unit + integration test — **0.5d**

#### Day 8 (Wed 06-06) — i18n + audit log + correlation
- [BE-D1] All error messages localized (vi default, en) — `Resources/Errors/{Errors.vi.resx, Errors.en.resx}` — **1d**
- [BE-D1] All audit log entries verified for CR-1 actions (login, logout, device change request, admin user create/update/delete, reset password trigger) — **0.5d**
- [BE-D2] Serilog scope enrichment: `TraceId`, `UserId`, `DeviceId`, `Role` on every request log — **0.5d**
- [FE-M-D2] Mobile i18n: ARB strings for all auth screens in `vi` + `en` — **1d**
- [FE-W-D2] Web i18n: next-intl strings for all auth + admin screens in `vi` + `en` — **0.5d**

#### Day 9 (Thu 06-07) — Tests + hardening
- [BE-D1] Unit tests: JWT issuance, password hasher, token rotation, device check, idempotency middleware — **1d**
- [BE-D2] Integration tests: full register→verify→login→refresh→logout flow + device-not-authorized path — **1d**
- [FE-M-D1] Widget tests: login screen, register screen — **0.5d**
- [FE-W-D1] Unit tests: useLoginMutation, AdminUserForm — **0.5d**
- [BE-D1+D2] Code coverage gate ≥70% (Domain + Application) — fix any uncovered critical paths — **0.5d**

#### Day 10 (Fri 06-08) — Acceptance + demo prep + Sprint 02 grooming
- [Both] Manual UAT on real Android + iOS device — **0.5d**
- [Both] Acceptance criteria checklist walk-through (AC-1, AC-2) — **0.25d**
- [Both] Swagger + OpenAPI export reviewed for correctness — **0.25d**
- [Both] Demo run-through: stakeholder demo script + bug-fix buffer — **0.5d**
- [Both] Sprint 02 grooming: groom M02 finishing tasks (device approval UI, Leader assignment management) — **0.5d**

### Buffer / Spill
- Days 9-10 budget 0.5d each as buffer
- If overrun: Web Admin user create form simplification (defer "search by name" to Sprint 02), defer admin-trigger password reset (use SQL fallback)

---

## 7. Definition of Done (Sprint 01)

A task is "done" only when ALL of:

- [ ] Code merged via PR with green CI (backend + web + mobile)
- [ ] Endpoint has Swagger annotations (request/response examples, error codes)
- [ ] Endpoint validates idempotency for mutations (`X-Idempotency-Key`)
- [ ] Endpoint returns localized error envelope per `05-api-conventions.md`
- [ ] All audit-loggable actions emit to `audit_log` (verified by integration test)
- [ ] Unit + integration tests cover happy + failure paths
- [ ] Mobile / Web screens have `vi` + `en` strings
- [ ] No `TODO(security)` or `FIXME(M01)` left in code
- [ ] Smoke test on staging (when staging exists; otherwise on Docker Compose local)

**Sprint-level acceptance:**
- [ ] AC-1 demonstrated: PG email register → verify → login on mobile
- [ ] AC-2 demonstrated: second-device login returns 403 with stored pending request
- [ ] Audit log query returns expected entries for: register, login success, login fail, refresh, logout, password reset, admin user create
- [ ] Manual penetration probes pass: token without signature → 401, expired token → 401, token from another user → 403, replay refresh token → revoke all + 401
- [ ] i18n verified: switch app language → all screens show translated copy

---

## 8. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **R-1: Email delivery in dev environment is unreliable** | High | Medium | Use console sink for emails in Dev; SendGrid only in Staging+. Provide CLI command to fetch latest verification token for E2E tests. |
| **R-2: BCrypt cost 12 on dev machine is slow (~200ms)** | High | Low | Acceptable. Don't downgrade for testing — use a separate `IPasswordHasher` test double in unit tests. |
| **R-3: Refresh token rotation race condition** | Medium | High | Implement reuse detection: if old refresh token submitted twice, revoke ALL user's refresh tokens + audit log + force-logout. Test with concurrent requests. |
| **R-4: Deep-link verification URL doesn't open mobile app** | Medium | Medium | Web fallback page that says "Open in app" + manual code entry. iOS Universal Links + Android App Links setup deferred to Sprint 02 if needed. |
| **R-5: Time-zone bugs in token expiry** | Medium | High | Strictly use `DateTimeOffset.UtcNow` everywhere. Add architecture test that rejects `DateTime.Now` / `DateTime.UtcNow` (without offset) in `Rmms.Application`. |
| **R-6: First migration is wrong, requires re-do** | Medium | High | Schema review with team before generating migration. Run on fresh Postgres + verify queries from `04-data-model.md` work. |
| **R-7: Device fingerprint collision** | Low | Medium | Use UUID v4 generated on app install + stored in secure storage. If lost (app reinstall) → counts as new device → goes through approval flow. |
| **R-8: Admin seed CLI accidentally creates duplicate** | Low | Low | Idempotent: check email exists before insert; print "already exists" instead. |

---

## 9. Extensibility Hooks (future-proofing — must be in Sprint 01 code even if features come later)

These are hooks we **wire now** so future modules don't need invasive changes:

### **9.1 Identity providers (Phase 2 SSO, MFA)**
- `User` entity has nullable `external_provider` + `external_id` columns reserved (Phase 2 Google/Microsoft SSO)
- Password column is nullable to support future password-less users
- Authentication pipeline goes through `IUserAuthenticator` interface → multiple implementations addable

### **9.2 Multi-factor auth (Phase 2)**
- `User` reserves `mfa_enabled` (bool), `mfa_secret_external_id` columns (commented in migration, applied via Phase 2 migration)
- Login response shape includes optional `requires_mfa` flag (always false in Phase 1)

### **9.3 Device approval workflow (Sprint 02)**
- `user_devices` table already has `pending_approval` / `approved_by` / `approved_at` columns from this sprint
- Sprint 02 adds: notification emission (FCM + in-app), Leader/Admin UI, `/devices/:id/approve|reject` endpoints
- No data model migration needed in Sprint 02 — only endpoint + UI work

### **9.4 Audit log structure (M16)**
- `audit_log` table has flexible `metadata jsonb` column for module-specific context
- Sprint 01 entries use it lightly (just `{ ip, ua }`); M02+ will add `{ approver_id, request_id, ... }`
- Append-only at DB level (REVOKE UPDATE/DELETE) — no application changes needed when M16 ships query/search UI

### **9.5 Email approval (BR-407, M09)**
- Email template engine introduced in Sprint 01 for verification + reset emails
- Same engine reusable for M09 BUH email approval flow (HMAC-signed token URL pattern is identical)
- Token table pattern (single-use, 24h TTL, hashed) is reused

### **9.6 Face enrollment (M06)**
- `users.face_enrolled_at` + `users.face_template_external_id` columns reserved in initial migration
- `/auth/me` response includes `faceEnrolled: bool` field — mobile reads it; M06 sets it to true when enrollment completes

### **9.7 Localization expansion**
- All strings keyed (no hardcoded vi text) — adding zh/th/km later just adds new `.resx` / `.arb` files
- Default language fallback chain: user pref → `Accept-Language` header → `vi`

### **9.8 Observability**
- Serilog scope properties: `TraceId`, `UserId`, `DeviceId`, `Role` — sets foundation for M15 dashboard log analysis
- Sentry integration scaffolded (DSN from config) but disabled in Dev — turn on via env var when Staging ships

### **9.9 Rate limit policy framework**
- `AspNetCoreRateLimit` configured with per-endpoint policies
- Sprint 01 only configures `/auth/login` — adding more policies (e.g., `/forms/submit` for M10) is config-only

---

## 10. Dependencies & External Coordination

| Dependency | Status | Owner |
|---|---|---|
| **SendGrid account + API key** | Need before Day 8 if not stubbed | DevOps (Tech lead) |
| **Domain `rmms.example.com` MX + SPF for outbound email** | Need before Staging | DevOps |
| **Deep-link scheme registration** (`rmms://`) | Mobile config — Day 4 task | FE-M dev |
| **Postgres connection string in CI** | Already done in `backend.yml` service container | — |
| **JWT secret rotation policy** | Document decision in ADR-010 (TODO) | Tech lead, end of Sprint 01 |

---

## 11. Demo Script (end of Sprint 01)

> 30-minute live walkthrough for stakeholder (PM, customer rep)

1. **Fresh mobile install on iPhone** → tap "Đăng ký" → email + password → tap submit
2. **Show inbox** (console log or real SendGrid) → click verification link → land on success screen
3. **Login on iPhone** → see home screen + profile via `/auth/me`
4. **Install mobile on Android** with same account → login attempt → "Thiết bị này chưa được phê duyệt" screen
5. **Open Postgres admin tool** → query `user_devices` → show Android entry with status `pending_approval`
6. **Open `audit_log`** → show all 5 events recorded (register, verify, login, login_fail_device_block, …)
7. **Open Web Admin** → log in as seeded admin → User management → create new Leader → show password sent via email
8. **Walk through Swagger** → live API exploration of auth endpoints
9. **Q&A** + Sprint 02 preview (device approval workflow)

---

## 12. Carryover from Sprint 00 (already done — listed for traceability)

- ✅ .NET 10 SDK, AGP 8.11, Flutter 3.44 toolchain locked
- ✅ 9 ADRs Accepted
- ✅ 3 CI workflows green on main
- ✅ Docker Compose dev infra ready
- ✅ Mobile platform folders relocated under `mobile/`

## 13. Carry-forward to Sprint 02 (M02 finish + M03 start)

| Task | Discipline | Est. |
|---|---|---|
| `POST /devices/:id/approve` + `/devices/:id/reject` endpoints | BE | 1d |
| Leader/Admin device-request list + detail UI (mobile + web) | FE | 3d |
| FCM push notification on new device request | BE + FE-M | 1.5d |
| Email notification to Leader/Admin on new device request | BE | 0.5d |
| M03 `stores`, `areas`, `user_leader_assignments`, `user_store_assignments` migration | BE | 1d |
| M03 admin assignment management screens | FE-W | 2d |
| Sprint 02 buffer | — | 1d |

---

## 14. References

- `modules/M01-identity-access.md` — module spec
- `modules/M02-device-management.md` — module spec
- `04-data-model.md` — entity definitions
- `05-api-conventions.md` — JWT, headers, error envelope
- `06-business-rules.md` — BR-101..106, CR-1
- `07-acceptance-criteria.md` — AC-1, AC-2
- `08-coding-standards.md` — patterns for layered architecture
- `decisions/ADR-001..009.md` — architectural decisions

---

**Status:** Draft, awaiting user review. Update to **Active** when approved by tech lead.
