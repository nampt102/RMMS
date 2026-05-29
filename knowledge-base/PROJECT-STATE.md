# PROJECT STATE — RMMS 2026

> **READ THIS FIRST.** This file is the single source of truth for "where the project is right now". Every AI session and every new dev should open this file before doing anything else. Update on every significant milestone.

**Last updated:** 2026-05-28 (evening — Sprint 01 Day 1 complete: domain entities + EF migration generated)
**Current phase:** Phase 1A — **Sprint 01 (M01 Auth & Devices), Day 1 of 10**
**Sprint 00 status:** ✅ **CLOSED** (100% — scaffold + .NET 10 + 9 ADRs + 3 CI workflows green)
**Sprint 01 progress:** ~10% — Day 1 deliverables shipped (domain entities, EF config, DI wiring, first migration `Init_M01_M02_Foundation` generated locally; ready for Day 2 endpoint work)

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
- **External integrations not wired.** SendGrid (Day 8 of Sprint 01), FPT.AI Face (M06), Firebase FCM (M14), MinIO (M13) all have TODOs in `Rmms.Infrastructure/DependencyInjection.cs`. Email currently uses `ConsoleEmailSender` that logs to Serilog (Dev/CI default).
- **No outbox pattern, no SignalR hub, no Hangfire jobs registered yet.** Hangfire host runs but has no jobs scheduled — cleanup job for expired refresh / verification tokens lands Day 6.
- **Auth-related screens not wired.** Mobile `Register` / `EmailVerify` / `Login` / `Forgot` / `Reset` and Web Admin `User Management` UI ship Day 3-7.

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

## Sprint 01 — M01 Auth & Devices (Day 1 of 10 done, 2026-05-28 → 2026-06-11)

**Day 1 ✅** (2026-05-28 evening): Domain layer + EF migration + DI wiring

- 7 Domain entities + 3 enums + 6 service abstractions + 7 EF configurations + 4 Infrastructure impls
- Migration `Init_M01_M02_Foundation` generated locally (7 tables: `users`, `user_devices`, `refresh_tokens`, `login_history`, `audit_log`, `email_verification_tokens`, `password_reset_tokens`)
- Post-migration SQL `001_audit_log_append_only.sql` for CR-1 enforcement
- See `sprints/sprint-01.md` for the full day-by-day plan

**Day 2 ⏭** (2026-05-29): JWT issuance + Register + Verify-Email endpoints

**Day 3–10**: see Sprint 01 plan

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
