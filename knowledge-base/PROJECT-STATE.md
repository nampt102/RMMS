# PROJECT STATE — RMMS 2026

> **READ THIS FIRST.** This file is the single source of truth for "where the project is right now". Every AI session and every new dev should open this file before doing anything else. Update on every significant milestone.

**Last updated:** 2026-05-26 (ADR-001..008 authored + GitHub Actions workflows added)
**Current phase:** Phase 1A — Sprint 00 (Foundation)
**Current sprint progress:** ~85% (scaffold + .NET 10 + NuGet restore resolved + 9/9 ADRs authored + CI/CD for backend/web/mobile; first DB migration is the last outstanding piece before Sprint 01 / M01 can start)

---

## TL;DR for AI sessions

The repository now contains a working **scaffold** for all three apps:

- ✅ **Backend** — **.NET 10 LTS** modular monolith (Clean Architecture), 7 projects + 2 test projects, builds expected. Migrated from initial .NET 8 target 2026-05-24 per [ADR-009](./decisions/ADR-009-dotnet-10-lts.md).
- ✅ **Web** — Next.js 14 (App Router) + Ant Design Pro + TanStack Query + Zustand + next-intl
- ✅ **Mobile** — Flutter 3.22 + Riverpod 2 + Dio + Hive + ARB-based l10n
- ✅ **Local infra** — `docker-compose.yml` for PostgreSQL 16 + Redis 7 + MinIO + Seq + Caddy
- ❌ **No business logic yet** — no User/Auth/Store entities, no real endpoints (only `/api/v1/health`)
- ✅ **CI/CD authored** — `.github/workflows/{backend,web,mobile}.yml`: lint + build + test, with concurrency control, path filters, NuGet / pnpm / Flutter caching, Postgres+Redis service containers for backend integration tests
- ❌ **No migrations applied** — `AppDbContext` has zero `DbSet<T>` properties
- ✅ **All 9 ADRs authored** — `decisions/ADR-001..009.md` covers Modular Monolith, Mediator (Othmar), UUID v7, soft-delete interceptor, snake_case naming, PostGIS-deferred, Caddy reverse proxy, Tailwind preflight-off, and .NET 10 LTS

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

- **No real auth.** JWT middleware is wired in `Program.cs` but no `User` entity exists, no `/auth/login` endpoint, no token issuance. Login screen on web + mobile are stub forms only.
- **No database migrations.** `AppDbContext` has zero `DbSet<T>`. Running `dotnet ef database update` will create an empty DB.
- **No business endpoints.** Only `/api/v1/health` exists.
- **External integrations not wired.** FPT.AI Face, SendGrid, Firebase FCM, MinIO are listed in `Directory.Packages.props` but `Rmms.Infrastructure/DependencyInjection.cs` has only `TODO(M01+)` markers for them.
- **No outbox pattern, no SignalR hub, no Hangfire jobs registered.**

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

## Immediate next steps (priority order)

1. ✅ **VERIFY** scaffold builds locally on **.NET 10** — **done 2026-05-26**. All 8 projects produce `bin/Debug/net10.0/*.dll`; `Rmms.Api.exe` + `Rmms.Worker.exe` runtime artifacts present; restore caches up to date.
2. ✅ **ADR-001 through ADR-008** — **done 2026-05-26**. All 9 ADRs Accepted in `knowledge-base/decisions/`.
3. ✅ **CI/CD** — **done 2026-05-26**. `.github/workflows/{backend,web,mobile}.yml` written with lint + build + test + caching + path filters. Backend workflow uses `actions/setup-dotnet@v4` with `10.0.x` and Postgres 16 + Redis 7 service containers for integration tests.
4. ⏭ **(NEXT)** Push the workflow files and confirm the first CI run goes green on a small "noop" PR — required before any feature PR can use CI as the gate. — ~30 min
5. **M01 Day 1 (Sprint 00 → Sprint 01 transition)**:
   - First EF Core migration: `users`, `devices`, `refresh_tokens`, `audit_log`
   - Endpoints: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`
   - JWT issuance with rotation
   - Authorization policies for `pg`/`leader`/`buh`/`admin`

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
