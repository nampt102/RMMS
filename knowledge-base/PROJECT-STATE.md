# PROJECT STATE — RMMS 2026

> **READ THIS FIRST.** This file is the single source of truth for "where the project is right now". Every AI session and every new dev should open this file before doing anything else. Update on every significant milestone.

**Last updated:** 2026-05-24
**Current phase:** Phase 1A — Sprint 00 (Foundation)
**Current sprint progress:** ~60% (scaffold done, CI + first migration outstanding)

---

## TL;DR for AI sessions

The repository now contains a working **scaffold** for all three apps:

- ✅ **Backend** — .NET 8 modular monolith (Clean Architecture), 7 projects + 2 test projects, builds expected
- ✅ **Web** — Next.js 14 (App Router) + Ant Design Pro + TanStack Query + Zustand + next-intl
- ✅ **Mobile** — Flutter 3.22 + Riverpod 2 + Dio + Hive + ARB-based l10n
- ✅ **Local infra** — `docker-compose.yml` for PostgreSQL 16 + Redis 7 + MinIO + Seq + Caddy
- ❌ **No business logic yet** — no User/Auth/Store entities, no real endpoints (only `/api/v1/health`)
- ❌ **No CI/CD** — `.github/workflows/` not created
- ❌ **No migrations applied** — `AppDbContext` has zero `DbSet<T>` properties
- ❌ **No ADR yet** — `decisions/` folder exists but empty (ADR-001 planned next)

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
    ├── pubspec.yaml, analysis_options.yaml, l10n.yaml, .gitignore, README.md
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
- `flutter run` (after `flutter create .` to fill platform folders) should boot to Login screen
- `docker compose up -d postgres redis minio` brings up infra (Caddy + api need `--profile full`)

## What does NOT work yet

- **No real auth.** JWT middleware is wired in `Program.cs` but no `User` entity exists, no `/auth/login` endpoint, no token issuance. Login screen on web + mobile are stub forms only.
- **No database migrations.** `AppDbContext` has zero `DbSet<T>`. Running `dotnet ef database update` will create an empty DB.
- **No business endpoints.** Only `/api/v1/health` exists.
- **No CI/CD pipelines.** `.github/workflows/` directory not created.
- **External integrations not wired.** FPT.AI Face, SendGrid, Firebase FCM, MinIO are listed in `Directory.Packages.props` but `Rmms.Infrastructure/DependencyInjection.cs` has only `TODO(M01+)` markers for them.
- **No outbox pattern, no SignalR hub, no Hangfire jobs registered.**

---

## Architecture decisions made (informally — pending ADR)

These are decisions the team has converged on but no ADR is written yet (planned next):

1. **Modular Monolith, NOT microservices.** Reasons: 2-dev team, single customer, single VPS, scope still solidifying. Rejected `.NET Aspire` from `nampt102/microservice-patterns` as base; selectively borrow Mediator/Outbox/CircuitBreaker patterns when needed.
2. **MediatR replaced by `Martin Othmar's Mediator`** (`Mediator.SourceGenerator` + `Mediator.Abstractions`). Reason: MediatR went commercial in v12. The replacement is MIT-licensed, source-generator-based, faster, and API-compatible enough for migration if needed.
3. **UUID v7 app-generated** (`Rmms.Domain.Common.UuidV7`). No PostgreSQL extension needed; still time-ordered for B-tree locality.
4. **Soft delete via EF Core interceptor.** `AuditableEntityInterceptor` converts `EntityState.Deleted` into `Modified` with `DeletedAt` stamped. Satisfies CR-1 (audit-loggable).
5. **Snake_case PostgreSQL** via `EFCore.NamingConventions`. PascalCase entities map automatically.
6. **PostGIS deferred.** `NetTopologySuite` handles haversine for BR-204 (check-in geofence). PostGIS extension is commented in `01-extensions.sql` for later.
7. **Caddy as reverse proxy** with auto-SSL. Hangfire/Swagger/API all proxied through it in production.
8. **Tailwind preflight disabled** in web (`corePlugins.preflight: false`) to avoid AntD reset conflicts.

These should become **ADR-001 through ADR-008** once formalized. Folder `knowledge-base/decisions/` is ready.

---

## Immediate next steps (priority order)

1. **VERIFY** scaffold builds locally (`dotnet build`, `pnpm build`, `flutter analyze`). Required before any further work. — **user to run on Windows machine**
2. **ADR-001**: Modular Monolith over Microservices. — small doc, ~1 hour
3. **CI/CD**: 3 GitHub Actions workflows (`backend.yml`, `web.yml`, `mobile.yml`). — ~1 day
4. **M01 Day 1 (Sprint 00 → Sprint 01 transition)**:
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
