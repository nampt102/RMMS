# CHANGELOG — RMMS 2026

Append-only chronological log of significant project milestones, decisions, and infrastructure changes.

**Format:** newest entries at the top. Each entry has a date (ISO 8601), short title, and bullet list of what changed. Reference module / business-rule / acceptance-criterion IDs where relevant. Keep entries factual — opinions and rationale belong in ADRs.

---

## 2026-05-24 — Repository scaffold complete (Sprint 00 partial)

**By:** AI-assisted (initial scaffold session)

**What was added:**

- Root config: `Directory.Build.props`, `Directory.Packages.props` (central package management), `.editorconfig`, `.gitignore`, `.env.example`
- `docker-compose.yml` with services: `postgres:16-alpine`, `redis:7-alpine`, `minio`, `seq` (dev profile), `caddy:2-alpine`, `api`, `worker`
- `infra/caddy/Caddyfile` (HTTP dev, prod stub with auto-SSL)
- `infra/postgres/init/01-extensions.sql` — pgcrypto, citext, pg_trgm (PostGIS commented)
- **Backend solution** `Rmms.sln` with 7 projects:
  - `Rmms.Domain` — Entity, AuditableEntity, AggregateRoot, ValueObject, Result, Error, UuidV7, GpsCoordinate (BR-204 ready), UserRole enum
  - `Rmms.Application` — Mediator + FluentValidation + Mapster wired; ValidationBehavior + LoggingBehavior pipeline behaviors
  - `Rmms.Infrastructure` — AppDbContext (EF Core + snake_case + NetTopologySuite + retry-on-failure), AuditableEntityInterceptor (soft-delete + audit stamps), Redis multiplexer
  - `Rmms.Shared` — ErrorEnvelope, ErrorCodes (full domain catalogue from 05-api-conventions.md), PaginatedResponse
  - `Rmms.Api` — Program.cs (JWT bearer, CORS, i18n vi/en, Serilog, Swagger with Bearer auth, health checks), ExceptionHandlingMiddleware (maps to ErrorEnvelope), HttpContextCurrentUser, HealthController, Dockerfile
  - `Rmms.Worker` — Hangfire host backed by PostgreSQL, Dockerfile
  - `Rmms.UnitTests`, `Rmms.IntegrationTests` (xUnit + FluentAssertions + Testcontainers ready)
- **Web app** at `web/` — Next.js 14 App Router, TypeScript strict, Ant Design Pro, TanStack Query, Zustand (with persist), next-intl (vi default + en), middleware locale routing, axios client with JWT interceptor, auth store, login screen, Dockerfile
- **Mobile app** at `mobile/` — Flutter 3.22 scaffold with Riverpod 2, Dio + interceptors (auth, device headers per 05-api-conventions.md), go_router, flutter_secure_storage, ARB-based l10n, Material 3 theme aligned with web brand color, login + home screens
- Sample unit tests: `GpsCoordinateTests`, `UuidV7Tests`

**Implicit decisions made (to be formalized as ADRs):**

- Modular Monolith over microservices / `.NET Aspire`
- `Martin Othmar's Mediator` replaces MediatR (license change)
- UUID v7 app-generated (no PG extension)
- Soft delete via EF Core SaveChangesInterceptor
- snake_case PostgreSQL via `EFCore.NamingConventions`
- PostGIS deferred (NetTopologySuite covers BR-204)
- Tailwind preflight disabled (AntD reset wins)

**Verified:**

- All JSON/YAML/XML files pass syntax validation (in scaffold sandbox)
- Project references between layers correct (Clean Architecture: Domain ← Application ← Infrastructure / Api)

**NOT verified yet:**

- `dotnet build` on real .NET 8 SDK (user to verify on Windows machine)
- `pnpm install && pnpm build`
- `flutter pub get && flutter analyze`

**Not included (planned next):**

- ADR-001 (Modular Monolith decision)
- CI/CD workflows
- Initial EF Core migration (Users, Devices, RefreshTokens, AuditLog)
- Any actual M01 endpoint

---

## 2026-05-22 — Knowledge base initial commit

**By:** User + AI-assisted spec authoring

- Knowledge base created with files `00-overview.md` through `08-coding-standards.md`
- `modules/M01..M16.md` — one detail doc per module
- `sprints/sprint-00..sprint-18.md` — sprint plan covering Phase 1A (5 months) + Phase 1B (4 months)
- `prompts/` — system-prompt template, implementation/review/test prompt templates
- `diagrams/diagrams.md` — high-level architecture diagrams
- `01-glossary.md` — canonical domain vocabulary (PG, Leader, BUH, Store, Shift, Form Engine, Visit Plan…)
- `06-business-rules.md` — SOURCE OF TRUTH for business logic (BR-101 through BR-810+)
- `07-acceptance-criteria.md` — 35 acceptance criteria for Phase 1
- `04-data-model.md` — entities + relationships
- `05-api-conventions.md` — REST, JWT, error envelope, pagination, rate limiting
