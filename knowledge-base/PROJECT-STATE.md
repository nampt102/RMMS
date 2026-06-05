# PROJECT STATE — RMMS 2026

> **READ THIS FIRST.** This file is the single source of truth for "where the project is right now". Every AI session and every new dev should open this file before doing anything else. Update on every significant milestone.

**Last updated:** 2026-06-05 — **Sprint 02 M03 in progress.** M03 Organization & Assignment: BE (6 entities + migration + CRUD stores/areas/categories + assignments) and web admin master pages (Stores/Areas/Categories) done & verified. Backend builds clean + runtime smoke pass; web type-check/lint/build green. Dev web server moved to **port 3010** (Cursor hijacks 3000).
**Current phase:** Phase 1A — **Sprint 02 in progress.** ✅ M02 device-approval BE + web done. ✅ **M03 (partial):** domain (Area/Store+GPS/Category + 3 assignment aggregates), migration `M03_Organization_Assignment` (6 tables + partial unique: 1 active Leader/PG, 1 active store/user), backend CRUD `/admin/{stores,areas,categories}` + assignments `/admin/assignments/{pg-leader,user-store,user-category}` + `GET assignments/user/{id}` (all AdminOnly, audit, soft-delete), web Stores/Areas/Categories pages + nav. **Next (M03 remaining):** assignment-panel UI (PG↔Leader, User↔Store/Category), unit tests (DoD ≥70%), mobile read endpoints (`/users/me/stores|leader`) → then enable Leader-scoped device approval. _(Sprint 01 (M01) closed; Day 10 manual UAT/demo skipped.)_
**Sprint 00 status:** ✅ **CLOSED** (100% — scaffold + .NET 10 + 9 ADRs + 3 CI workflows green)
**Sprint 01 status:** ✅ **CODE-COMPLETE** — Day 1–9 shipped; **Day 10 (manual UAT on real devices + stakeholder demo + Sprint 02 grooming) SKIPPED** by decision. Delivered: 10 auth endpoints + admin user CRUD + CLI seed + authz policies + idempotency + login rate-limit + `/auth/me` + `/auth/me/device-status`; BR-105 device check PG-only; Hangfire token-cleanup job; error localization vi/en + Serilog enrichment + audit (CR-1); Web Admin login + user-management (list/create/edit/detail/reset) + client route guard + 401 refresh; full mobile FE auth surface (register→verify→login→forgot/reset→device-pending, deep links, auto-refresh). Tests: 96 BE unit + integration (CI) + web vitest 8/8 + mobile widget tests. **⚠️ Not done (skipped Day 10):** manual UAT on physical Android/iOS, formal AC-1/AC-2 sign-off walkthrough, stakeholder demo, Swagger export review. **Next: Sprint 02.**

> **Verification note (2026-05-30):** Backend integration tests (Testcontainers PostGIS+Redis) were NOT re-run on the Windows dev box this session — Docker Desktop host resolution fails in `RmmsApiFactory.InitializeAsync` ("No such host is known") before any test logic. This is an environment/Testcontainers-on-Windows issue, not a code regression; the suite is green on the Linux CI runner. Backend `dotnet build` (0 errors) + `dotnet test` on `Rmms.UnitTests` (45/45) were run and pass locally.

> **Auth pipeline fix (2026-05-31):** `[Authorize(Roles="admin")]` was returning 403 for valid admin tokens. Root cause: JwtBearer default `MapInboundClaims = true` remaps short claim names (`sub`, etc.), desyncing the explicit `RoleClaimType="role"` / `NameClaimType="sub"`. Fixed by setting `options.MapInboundClaims = false` in `Program.cs`. This class of bug is invisible to handler unit tests (they bypass the HTTP auth pipeline) — caught only by the smoke test. **Action item for Day 5:** add a `WebApplicationFactory` integration test asserting `/admin/*` returns 200 for `admin` role and 403 for non-admin.

---

## TL;DR for AI sessions

The repository now contains a working **scaffold** for all three apps:

- ✅ **Backend** — **.NET 10 LTS** modular monolith (Clean Architecture), 7 projects + 2 test projects, builds expected. Migrated from initial .NET 8 target 2026-05-24 per [ADR-009](./decisions/ADR-009-dotnet-10-lts.md).
- ✅ **Web** — Next.js 14 (App Router) + Ant Design Pro + TanStack Query + Zustand + next-intl
- ✅ **Mobile** — Flutter 3.22 + Riverpod 2 + Dio + Hive + ARB-based l10n
- ✅ **Local infra** — `docker-compose.yml` for PostgreSQL 16 + Redis 7 + MinIO + Seq + Caddy
- ✅ **Domain layer M01+M02 entities** — `User`, `UserDevice`, `RefreshToken`, `LoginHistory`, `AuditLog`, `EmailVerificationToken`, `PasswordResetToken` with factory methods, invariants, future-proofing hooks (SSO/MFA/Face columns reserved)
- ✅ **First EF migration generated** — `Init_M01_M02_Foundation` creates 7 tables + indexes + partial unique index `WHERE status='active'` enforcing BR-105 at DB level; post-migration SQL `001_audit_log_append_only.sql` revokes UPDATE/DELETE on `audit_log` per CR-1
- ✅ **CI/CD green on main** — all 3 workflows pass: backend (Restore→Format→Build→Tests with Postgres+Redis services), web (Lint→Type-check→Test→Build), mobile (pub get→build_runner→Format→Analyze→Tests). Android APK smoke build deferred to Sprint 03 release.yml (Flutter#169475 upstream regression).
- ❌ **No endpoints yet** — only `/api/v1/health` from scaffold; M01 endpoints (`/auth/*`, `/admin/users/*`) land in Day 2-7
- ✅ **All 9 ADRs authored** — `decisions/ADR-001..009.md` covers Modular Monolith, Mediator (Othmar), UUID v7, soft-delete interceptor, snake_case naming, PostGIS-deferred, Caddy reverse proxy, Tailwind preflight-off, and .NET 10 LTS
- ✅ **Mobile toolchain pinned** — Flutter 3.44, Dart 3.12, AGP 8.11, Kotlin 2.2.20, Gradle 8.13, JDK 17 (verified compatible with Flutter team's stable matrix)
- ✅ **Service abstractions in Application** — `IPasswordHasher`, `IJwtTokenService`, `IRefreshTokenGenerator`, `IEmailSender`, `IAuditLogger`, `IClientContext` (implementations in Infrastructure: BCrypt cost 12, HS256 JWT, SHA-256 hashed refresh tokens, console email for Dev, append-only audit logger)

If you are an AI tool generating code: assume the scaffold is the foundation, but ANY entity/endpoint/feature you propose must be implemented from scratch — there is no existing User table, no auth middleware applied to any route, no SignalR hub, no Hangfire job. Follow the modules in `modules/M01..M16` order; do NOT skip ahead unless explicitly asked.

---

## Repository layout (actual, as of this update)

```
RMMS/                                       # root
├── README.md, .editorconfig, .gitignore, .env.example
├── Directory.Build.props                   # nullable + warnings-as-errors for all .NET projects
├── docker-compose.yml                      # postgres / redis / minio / seq / api / worker / caddy
├── knowledge-base/                         # spec docs (this folder)
├── infra/
│   ├── caddy/Caddyfile
│   └── postgres/init/01-extensions.sql
├── backend/
│   ├── Rmms.sln, global.json, Directory.Build.props, Directory.Packages.props
│   ├── src/
│   │   ├── Rmms.Domain/                    # Entity, AuditableEntity, AggregateRoot, Result, Error,
│   │   │                                     ValueObject, UuidV7, GpsCoordinate, UserRole enum
│   │   ├── Rmms.Application/               # Mediator (Martin Othmar fork — MediatR alternative),
│   │   │                                     FluentValidation, Mapster, pipeline behaviors
│   │   ├── Rmms.Infrastructure/            # AppDbContext (EF Core + snake_case + NetTopologySuite),
│   │   │                                     AuditableEntityInterceptor (soft-delete + audit stamps),
│   │   │                                     Redis multiplexer
│   │   ├── Rmms.Shared/                    # ErrorEnvelope, ErrorCodes catalogue, PaginatedResponse
│   │   ├── Rmms.Api/                       # Program.cs (JWT bearer, CORS, i18n vi/en, Serilog,
│   │   │                                     Swagger), ExceptionHandlingMiddleware,
│   │   │                                     HealthController, Dockerfile
│   │   └── Rmms.Worker/                    # Hangfire host (Postgres storage) + Dockerfile
│   └── tests/
│       ├── Rmms.UnitTests/                 # GpsCoordinateTests, UuidV7Tests (sample)
│       └── Rmms.IntegrationTests/          # WebApplicationFactory + Testcontainers ready
├── web/
│   ├── package.json, tsconfig.json, next.config.mjs, tailwind.config.ts,
│   │   .eslintrc.json, .prettierrc, .env.example, Dockerfile
│   ├── messages/{vi,en}.json               # next-intl
│   └── src/
│       ├── app/
│       │   ├── [locale]/layout.tsx, page.tsx
│       │   ├── [locale]/(auth)/login/page.tsx
│       │   ├── providers.tsx, globals.css, layout.tsx
│       ├── features/auth/api/login.ts      # useLoginMutation (stub)
│       ├── lib/api/{client,query-client}.ts
│       ├── lib/i18n/{config,request}.ts
│       ├── lib/stores/auth-store.ts        # Zustand + persist
│       ├── middleware.ts                   # next-intl locale routing
│       └── types/api.ts                    # mirrors backend Rmms.Shared
└── mobile/
    ├── android/ ios/ linux/ macos/ windows/  # Flutter platform (org com.rmms)
    ├── pubspec.yaml, pubspec.lock, analysis_options.yaml, l10n.yaml, .gitignore, README.md
    ├── assets/{images,icons}/
    └── lib/
        ├── main.dart, app.dart
        ├── core/
        │   ├── config/app_config.dart      # --dart-define driven
        │   ├── network/api_client.dart, api_interceptors.dart
        │   ├── router/app_router.dart      # go_router
        │   ├── storage/secure_storage.dart # flutter_secure_storage wrapper
        │   └── theme/app_theme.dart
        ├── features/auth/{data,domain,presentation}/  # login screen + auth_user model
        ├── features/home/presentation/     # placeholder home screen
        ├── l10n/{app_vi.arb,app_en.arb}    # ARB-based l10n
        └── test/widget_test.dart
```

---

## What works RIGHT NOW

- `dotnet restore && dotnet build` should succeed (not yet verified on real .NET SDK by user)
- `dotnet run --project src/Rmms.Api` should expose:
  - `GET /api/v1/health` — returns `{ data: { status: "ok" } }`
  - `GET /swagger` — Swagger UI in Development
  - `GET /health/live`, `/health/ready` — minimal health checks
- `pnpm dev` in `web/` should serve `http://localhost:3000` with home page and `/login`
- `flutter run` in `mobile/` should boot to Login screen (requires Flutter SDK + platform folders under `mobile/`)
- `docker compose up -d postgres redis minio` brings up infra (Caddy + api need `--profile full`)

## What does NOT work yet

- **No `/auth/*` endpoints.** Domain entities + EF migration are in place but commands/handlers/controllers ship Day 2-5. Mobile + web login screens are still stub forms.
- **External integrations not wired.** SendGrid (Sprint 01 stretch), FPT.AI Face (M06), Firebase FCM (M14), MinIO (M13) all have TODOs in `Rmms.Infrastructure/DependencyInjection.cs`. Email currently uses `ConsoleEmailSender` that logs to Serilog (Dev/CI default).
- ✅ **Error messages localized (vi/en)** by error code (`ErrorMessageCatalog` + `ErrorLocalizationFilter`, Accept-Language driven). ✅ **Serilog enrichment** (`TraceId`/`UserId`/`DeviceId`/`Role`) on handler logs + request-completion log (Day 8).
- **No outbox pattern, no SignalR hub yet.** ✅ First Hangfire recurring job registered: `auth-token-cleanup` (hourly) in `Rmms.Worker` hard-deletes used/expired verification + reset tokens and expired refresh tokens (Day 6). `GET /auth/me/device-status` skeleton shipped (authenticated read).
- **Auth-related screens wired (mobile + web login).** ✅ Mobile `Register` / `EmailVerify` / `Login` / `Forgot` / `Reset` / device-pending all wired to the live API + `rmms://` deep links (full M01 mobile FE, **code complete, pending macOS verification**). ✅ **Web Admin login** wired to `POST /auth/login` (device-less, BR-105 PG-only) → Zustand persist + localized errors → redirect (`FE-W-D5`, green on type-check/lint/build). ✅ **Web Admin `User Management`** shipped (Day 7, gaps closed): ProTable list (role/status/search filters) + create/edit ModalForm (status toggle) + reset-password action + **user detail Drawer** (read-only fields + status toggle + reset, designed via `ui-ux-pro-max`), behind a client-side route guard (`(admin)/layout.tsx`) — login redirects to `/{locale}/users`. apiClient now **refreshes tokens on 401** (single-flight, replay once, else → login). (Guard is client-side because tokens live in localStorage, not an httpOnly cookie.) Refresh-reuse covered by unit **and** integration tests (integration runs on CI).

---

## Architecture decisions made (all formalized as ADRs)

All 9 ADRs are **Accepted** and live in `knowledge-base/decisions/`:

| ID | Decision | Date |
|---|---|---|
| [ADR-001](./decisions/ADR-001-modular-monolith.md) | Modular Monolith over microservices / .NET Aspire | 2026-05-26 |
| [ADR-002](./decisions/ADR-002-mediator-martin-othmar.md) | Mediator (Martin Othmar fork, MIT) replaces MediatR (v12 commercial) | 2026-05-26 |
| [ADR-003](./decisions/ADR-003-uuid-v7-app-generated.md) | UUID v7 generated in C# code, not via Postgres extension | 2026-05-26 |
| [ADR-004](./decisions/ADR-004-soft-delete-interceptor.md) | Soft delete enforced via EF Core `SaveChangesInterceptor` | 2026-05-26 |
| [ADR-005](./decisions/ADR-005-snake-case-postgres.md) | snake_case Postgres naming via `EFCore.NamingConventions` | 2026-05-26 |
| [ADR-006](./decisions/ADR-006-postgis-deferred.md) | PostGIS deferred; NetTopologySuite handles Haversine for BR-204 | 2026-05-26 |
| [ADR-007](./decisions/ADR-007-caddy-reverse-proxy.md) | Caddy 2.x reverse proxy with auto-SSL via Let's Encrypt | 2026-05-26 |
| [ADR-008](./decisions/ADR-008-tailwind-preflight-disabled.md) | Tailwind Preflight disabled — Ant Design reset wins | 2026-05-26 |
| [ADR-009](./decisions/ADR-009-dotnet-10-lts.md) | .NET 10 LTS adopted; .NET 8 and .NET 9 rejected | 2026-05-24 |

---

## Sprint 01 — M01 Auth & Devices (Day 2 of 10 done, 2026-05-28 → 2026-06-11)

**Day 1 ✅** (2026-05-28 evening): Domain layer + EF migration + DI wiring

- 7 Domain entities + 3 enums + 6 service abstractions + 7 EF configurations + 4 Infrastructure impls
- Migration `Init_M01_M02_Foundation` generated locally (7 tables: `users`, `user_devices`, `refresh_tokens`, `login_history`, `audit_log`, `email_verification_tokens`, `password_reset_tokens`)
- Post-migration SQL `001_audit_log_append_only.sql` for CR-1 enforcement
- See `sprints/sprint-01.md` for the full day-by-day plan

**Day 2 ✅** (2026-05-29): Register + Verify-Email endpoints (smoke-tested end-to-end)

- `POST /api/v1/auth/register` (BR-101) + `POST /auth/verify-email`
- `IEmailTemplateRenderer` vi/en; `ConsoleEmailSender` Dev/CI; `OpaqueToken` shared helper
- `ResultMapping` (Result.Error → HTTP + ErrorEnvelope); `EnumFormatting.ToSnakeCase<T>()`
- Audit `user.registered` + `user.email_verified`

**Day 3 ✅** (2026-05-30): Login + Device check (BR-105) + Refresh rotation + Logout

- `POST /auth/login` — credential check + device fingerprint resolution (BR-105):
  - First device → auto-active; same device → reuse; different device → `403 DEVICE_NOT_AUTHORIZED` + pending row for Sprint 02 approval UI
- `POST /auth/refresh` — rotation + **reuse detection** (revokes ALL active tokens on detected reuse, audit `auth.refresh_reused` severity=high)
- `POST /auth/logout` — idempotent token revoke
- `JwtOptions` moved Infrastructure → Application (Clean Architecture fix)
- Audit: `auth.login_success`, `auth.login_failed`, `auth.refresh_rotated`, `auth.refresh_reused`, `auth.logout`, `device.registered`, `device.change_requested`

**Day 4 ✅** (2026-05-31): Forgot/Reset password + Admin user CRUD + CLI seed + **unit tests**

- `POST /auth/forgot-password` — silent success; emails reset link only for Active users (timing-attack mitigation)
- `POST /auth/reset-password` — applies new password + revokes ALL active refresh tokens (force re-login everywhere)
- `GET /admin/users` — paginated, filters role/status/search (case-insensitive Contains, EF translates to ILIKE on Postgres)
- `POST /admin/users` (Authorize Roles=admin) — Leader/BUH/Admin only (PG must self-register); random 12-char initial password emailed
- `PATCH /admin/users/:id` — profile + status toggle; revokes refresh on inactivate; audit `user.status_changed` + `user.updated_by_admin`
- `POST /admin/users/:id/reset-password` — admin force-issues reset link
- `dotnet run --project src/Rmms.Api -- seed-admin --email=... --password=...` — bootstrap CLI (idempotent)
- JWT `RoleClaimType="role"` mapping so `[Authorize(Roles="admin")]` works with our JWT
- **6 handler unit-test classes (~37 tests)** with EF InMemory + Moq-free helpers (FakePasswordHasher / TestClock / CapturingEmailSender / InMemoryAuditLogger / FakeTemplateRenderer / UserFactory) — full happy + failure-path coverage on Day 4 handlers

**Day 5 ✅ (build + tests GREEN)** (2026-06-01): Authorization policies + middleware hardening + /auth/me + integration tests

- **Authorization policies** — `AuthorizationPolicies` catalogue (`PgOnly`, `LeaderOnly`, `BuhOnly`, `AdminOnly`, `PgOrLeader`, `AnyAuthenticated`) registered via `AddRmmsAuthorization()`; `AdminUsersController` switched from `[Authorize(Roles="admin")]` to `[Authorize(Policy = AdminOnly)]`.
- **JWT claims projection** — verified `HttpContextCurrentUser` (`UserId`/`Email`/`Role`/`DeviceId`) resolves correctly after the `MapInboundClaims=false` fix (fallbacks retained).
- **`X-Idempotency-Key` middleware** (`IdempotencyMiddleware`) — Redis-backed, scoped per (user+method+path+key); replays cached 2xx for 24h, returns `409 IDEMPOTENCY_KEY_REUSED` on concurrent in-flight duplicate, fails open if Redis down.
- **Rate limit `/auth/login`** — `ILoginRateLimiter` / `RedisLoginRateLimiter`: 5 failures / 15 min per (email+IP) → `429 RATE_LIMIT_EXCEEDED`; resets on success; counts only `INVALID_CREDENTIALS`.
- **`GET /auth/me`** — `GetMeQuery`/`Handler`/`MeDto` returns profile + current device, identity from JWT only.
- **Integration tests** — Testcontainers (PostGIS + Redis) `RmmsApiFactory` + `AuthFlowTests` (register→verify→login→/me→refresh→logout) + `AdminAuthorizationTests` (admin 200 / leader 403 / no-token 401 — the regression for the MapInboundClaims bug).

> ✅ Day 5 backend built clean (warnings-as-errors) and `dotnet test` is green, including the Testcontainers integration tests (PostGIS + Redis).
>
> **Build/test gotchas fixed during Day 5:**
> - Analyzer-as-error fixes: `CA1822` (static helpers), `ASP0026` (class-level `[AllowAnonymous]` replaced by per-action `[AllowAnonymous]`, `/auth/me` keeps `[Authorize]`), `CA1859`, `CA1861`.
> - Integration test fixture: needs `using Microsoft.AspNetCore.TestHost;` for `ConfigureTestServices`; collection class renamed to avoid `CA1711` (no `Collection` suffix).
> - **JWT signing key in tests:** `Program.cs` reads `Jwt:SigningKey` EAGERLY at top-level, before `WebApplicationFactory` appends its in-memory config — so the test fixture must NOT override `Jwt:SigningKey` (issuance via runtime `JwtOptions` would then mismatch validation → 401). Both sides use the `appsettings.json` key. Connection strings / Email are read at runtime so overriding them is safe. *(Future option: validate JWT via `IOptions<JwtOptions>` to make the key overridable in tests.)*
>
> **Mobile FE (full M01, Day 3–6) ⏳ code complete, pending macOS verification:** Register (BR-101) → Email-verify (auto via deep link or manual code) → Login → secure-storage token persistence → router guard; `DEVICE_NOT_AUTHORIZED` → device-pending screen; auto-refresh interceptor (single-flight, R-3); install-scoped device UUID v4 (R-7); Forgot/Reset screens; `rmms://` custom-scheme deep links wired in `AndroidManifest.xml` + iOS `Info.plist` (R-4 manual fallback retained, Universal/App Links → Sprint 02); typed `ApiException` + bilingual error copy; unit + widget tests. Verify on Mac: `flutter pub get` → `dart run build_runner build --delete-conflicting-outputs` → `dart format .` → `flutter analyze --fatal-infos --fatal-warnings` → `flutter test`. **Web Admin login (`FE-W-D5`) wired + green (type-check/lint/build); user-management UI + route guard still pending (Day 7, Week 2).**

> **Login device check correction (2026-05-30, Week 1 close-out):** `/auth/login` originally required a `device` object and ran the BR-105 single-active-device check for ALL roles, which blocked Leader/BUH/Admin (web) from logging in. BR-105/BR-106 are PG-scoped. Fixed: `device` is now optional; the device resolution runs only for `Role == Pg` (PG without device → 403). Non-PG tokens carry `device_id = Guid.Empty` and create no `user_devices` row (no FK, so safe). Covered by `AdminAuthorizationTests.WebUser_LogsInWithoutDevice_AndCanCallApi` + a PG-requires-device assertion in `AuthFlowTests`.

**Day 6–10**: see Sprint 01 plan

## Sprint 00 — closed 2026-05-28 ✅

1. ✅ Scaffold verified building locally on .NET 10
2. ✅ 9 ADRs (001–009) Accepted
3. ✅ CI/CD — `.github/workflows/{backend,web,mobile}.yml` all green on main
4. ✅ Mobile toolchain stabilized (Flutter 3.44 + AGP 8.11 + Kotlin 2.2.20 + Gradle 8.13 + JDK 17)

## Sprint 01 — M01 Auth & Devices (2026-05-28 → 2026-06-11, 2 weeks)

**Goal:** First production-grade module — user identity, single-device enforcement, JWT issuance + rotation, auth-loggable per CR-1.

**Day 1 deliverables (the "first migration" milestone):**
- EF Core migration: `users`, `devices`, `refresh_tokens`, `audit_log`
- Endpoints: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`
- JWT issuance with rotation per `05-api-conventions.md`
- Authorization policies for `pg` / `leader` / `buh` / `admin`

See `sprints/sprint-01.md` for the full day-by-day plan, risks, and extensibility hooks.

After M01 ships, the order is M02 → M03 → M04 per `modules/M*.md` per Phase 1A plan.

---

## What is OFF-LIMITS right now

Per project instructions and to avoid scope creep, **do NOT** implement:

- Salary calculation (Phase 1 delivers payslips as files only)
- Beacon, Target, KPI, Gifts, Promotions, Invoice modules
- Migration from old system
- Full app offline mode (only form drafts allowed offline)
- Excel mandatory import
- Multi-tenancy
- Event Sourcing
- Saga (orchestration or choreography)
- Kafka or any external message broker (Hangfire + PostgreSQL is the queue)

If a user asks for any of the above, surface the question and refer to `06-business-rules.md` / `00-overview.md` "Not in scope".

---

## File-level cross-reference

Use these when navigating the codebase against the spec:

| Spec file | Implements in code |
|---|---|
| `02-tech-stack.md` Backend section | `backend/Directory.Packages.props`, `Rmms.*.csproj` files |
| `03-architecture.md` Layered Architecture | `backend/src/Rmms.{Domain,Application,Infrastructure,Api}/` |
| `05-api-conventions.md` Error envelope | `backend/src/Rmms.Shared/Errors/ErrorEnvelope.cs`, `ErrorCodes.cs` |
| `05-api-conventions.md` JWT payload | `backend/src/Rmms.Api/Authentication/HttpContextCurrentUser.cs` |
| `08-coding-standards.md` Database section | `backend/src/Rmms.Domain/Common/AuditableEntity.cs`, `UuidV7.cs`, `AppDbContext` snake_case |
| `08-coding-standards.md` Mobile / Riverpod | `mobile/lib/core/network/api_client.dart`, `app_router.dart` |
| `08-coding-standards.md` Frontend / TanStack | `web/src/lib/api/query-client.ts`, `features/auth/api/login.ts` |

---

## How to keep this file fresh

- Update **on every PR that adds/removes a top-level capability** (new module, new external integration, new sprint completed).
- The format is intentionally short — long detail belongs in module/sprint docs, not here.
- When a section becomes stale (e.g., "what does NOT work yet" item is now done), MOVE it, don't just delete it — leave a `CHANGELOG.md` entry.
- Last-updated date and current sprint must always be correct.
