# CHANGELOG — RMMS 2026

Append-only chronological log of significant project milestones, decisions, and infrastructure changes.

**Format:** newest entries at the top. Each entry has a date (ISO 8601), short title, and bullet list of what changed. Reference module / business-rule / acceptance-criterion IDs where relevant. Keep entries factual — opinions and rationale belong in ADRs.

---

## 2026-05-26 (late) — ADR-001..008 authored + GitHub Actions CI workflows added

**By:** AI-assisted, after tech lead confirmed .NET 10 scaffold built cleanly on Windows machine.

**Architecture Decision Records — all 8 previously-planned ADRs Accepted:**

- [ADR-001](decisions/ADR-001-modular-monolith.md) — Modular Monolith over microservices / .NET Aspire. Reasons: 2-dev team, single customer, single VPS, scope still solidifying.
- [ADR-002](decisions/ADR-002-mediator-martin-othmar.md) — `Mediator` by Martin Othmar (MIT, source-generator-based) replaces MediatR (v12 commercial license).
- [ADR-003](decisions/ADR-003-uuid-v7-app-generated.md) — UUID v7 generated in C# (`Rmms.Domain.Common.UuidV7`), no Postgres extension. Time-ordered → B-tree friendly. Enables offline-form-draft client-side ID minting.
- [ADR-004](decisions/ADR-004-soft-delete-interceptor.md) — Soft delete via EF Core `SaveChangesInterceptor` (`AuditableEntityInterceptor`). Single enforcement point. Satisfies CR-1.
- [ADR-005](decisions/ADR-005-snake-case-postgres.md) — `EFCore.NamingConventions` `UseSnakeCaseNamingConvention()`. PascalCase entities → snake_case tables/columns/FKs automatically.
- [ADR-006](decisions/ADR-006-postgis-deferred.md) — PostGIS deferred to Phase 2. NetTopologySuite + Haversine handles BR-204 (300m geofence). `GpsCoordinate.DistanceMetersTo()` works on both mobile and backend.
- [ADR-007](decisions/ADR-007-caddy-reverse-proxy.md) — Caddy 2.x with auto-SSL via Let's Encrypt. Single Caddyfile per environment. `caddy_data` volume backed up daily.
- [ADR-008](decisions/ADR-008-tailwind-preflight-disabled.md) — `corePlugins.preflight: false` in `web/tailwind.config.ts`. Ant Design's reset is canonical; Tailwind keeps utility classes only.

**CI/CD workflows added under `.github/workflows/`:**

- `backend.yml` — .NET 10 build + test. `actions/setup-dotnet@v4` with `dotnet-version: 10.0.x`. NuGet cache keyed on `Directory.Packages.props` + csproj hashes. Postgres 16 + Redis 7 service containers for integration tests. `dotnet format --verify-no-changes` enforces .editorconfig. Test results + cobertura coverage uploaded as artifacts.
- `web.yml` — pnpm 9.15 + Node 20 LTS. Steps: `pnpm install --frozen-lockfile`, `pnpm lint`, `pnpm type-check`, `pnpm test` (vitest), `pnpm build`. pnpm cache via `actions/setup-node@v4`.
- `mobile.yml` — Flutter 3.22.x via `subosito/flutter-action@v2`. Steps: `flutter pub get`, `dart format --set-exit-if-changed`, `flutter analyze --fatal-warnings`, `flutter test --coverage`, `flutter build apk --debug` (smoke build). Java 17 via `actions/setup-java@v4` for Android Gradle Plugin.

All 3 workflows use:

- `paths:` filters → workflow only triggers when its app's files change
- `concurrency:` with `cancel-in-progress: true` on PRs → no wasted runner minutes
- `workflow_dispatch:` for manual runs
- Artifact upload (test results, coverage) for visibility

**Files changed:**

- `knowledge-base/decisions/ADR-001-modular-monolith.md` (new)
- `knowledge-base/decisions/ADR-002-mediator-martin-othmar.md` (new)
- `knowledge-base/decisions/ADR-003-uuid-v7-app-generated.md` (new)
- `knowledge-base/decisions/ADR-004-soft-delete-interceptor.md` (new)
- `knowledge-base/decisions/ADR-005-snake-case-postgres.md` (new)
- `knowledge-base/decisions/ADR-006-postgis-deferred.md` (new)
- `knowledge-base/decisions/ADR-007-caddy-reverse-proxy.md` (new)
- `knowledge-base/decisions/ADR-008-tailwind-preflight-disabled.md` (new)
- `knowledge-base/decisions/README.md` — ADR index updated, all 9 rows linked + Accepted
- `.github/workflows/backend.yml` (new)
- `.github/workflows/web.yml` (new)
- `.github/workflows/mobile.yml` (new)
- `backend/src/Rmms.Api/Rmms.Api.csproj` — rewritten via bash heredoc to remove 11 trailing NULL bytes (latent from earlier Edit; build was passing despite them but file is now clean)
- `knowledge-base/PROJECT-STATE.md` — phase progress bumped to ~85%; CI/CD and ADRs moved out of "not working yet"; next-steps list updated.

**Not yet verified:**

- First CI run on a real push (workflows are syntactically valid YAML, but only an actual GitHub Actions execution can confirm caches/services/test invocations work end-to-end).
- `dotnet format --verify-no-changes` may flag style issues on first run — fix-then-merge cycle expected on the first PR that exercises the backend workflow.

---

## 2026-05-26 — Relocate Flutter platform folders to `mobile/` (fix mistaken root scaffold)

**By:** AI-assisted, after `flutter create` was accidentally run at monorepo root (commit 38fc4b2).

**Problem:** `android/`, `ios/`, `lib/main.dart`, root `pubspec.yaml`, and Flutter web artifacts (`web/index.html`, …) landed at repo root — conflicting with Next.js `web/` app.

**Fix:**

- Moved `android/`, `ios/`, `linux/`, `macos/`, `windows/`, `.metadata` → `mobile/`
- Removed root Flutter artifacts: `pubspec.yaml`, `pubspec.lock`, `analysis_options.yaml`, `lib/`, `test/`, `rmms.iml`
- Removed Flutter web pollution from `web/` (kept Next.js `src/`, `package.json`, …)
- Fixed Android `flutter.source` path (`..`), applicationId/namespace → `com.rmms`
- Fixed iOS/macOS bundle identifiers → `com.rmms`
- Added `mobile/assets/{images,icons}/.gitkeep`; updated `mobile/README.md`, root `README.md`, `PROJECT-STATE.md`

**Outcome:** Monorepo layout restored — mobile code + native runners all under `mobile/` only.

---

## 2026-05-24 (evening) — Backend restore failures resolved (NU1008 / NU1109 / NU1507 / NU1903)

**By:** AI-assisted, in response to `dotnet restore` output on dev machine after .NET 10 migration.

**Errors hit and fixes:**

- **NU1507** "Multiple package sources defined under central package management" — dev machine had user/machine-level NuGet sources (`Devextreme`, `DevEx 24`) in addition to `nuget.org`. Created `backend/NuGet.config` with `<clear/>` + sole `nuget.org` source + explicit `<packageSourceMapping>` (scoped to backend/ only — does not affect other projects on the machine).
- **NU1008** "PackageReference cannot define Version when CPM is enabled" — `Rmms.Application.csproj` still had inline `Version="8.0.2"` attrs on `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`. Earlier Edit had failed silently (file hadn't been Read first). Rewrote the file.
- **NU1109** "Detected package downgrade from 10.0.1 to 10.0.0" — `EFCore.NamingConventions 10.0.0` transitively requires `Microsoft.EntityFrameworkCore >= 10.0.1` and `Microsoft.Extensions.DependencyInjection.Abstractions >= 10.0.1`. Bumped all Microsoft-owned packages in `Directory.Packages.props` from `10.0.0` to `10.0.1`.
- **NU1903** "Package System.Security.Cryptography.Xml 9.0.0 has known high-severity vulnerabilities" (CVE GHSA-37gx-xxp4-5rgx and GHSA-w3x6-4m5h-cxqf, pulled transitively). Added explicit override `<PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.1" />` in `Directory.Packages.props` (Security / Auth group). Combined with `CentralPackageTransitivePinningEnabled=true`, this forces the patched version through the entire graph.
- **Bonus:** Discovered `Rmms.Application.csproj` had 32 trailing NULL bytes after Edit (Write tool didn't truncate on shorter content — Windows/CIFS quirk). Rewrote via bash heredoc which truncates correctly.

**Files changed:**

- `backend/NuGet.config` — new file pinning `nuget.org` only via clear + packageSourceMapping
- `backend/Directory.Packages.props` — bumped Microsoft.* `10.0.0` → `10.0.1` (EF Core, Extensions, AspNetCore, Caching, Hosting, Mvc.Testing); added `System.Security.Cryptography.Xml = 10.0.1` override
- `backend/src/Rmms.Application/Rmms.Application.csproj` — removed inline `Version` attrs; rewrote clean (no NULL bytes)

**Outcome:** Local restore now expected to succeed. User to confirm via `dotnet restore && dotnet build` from clean `bin/obj`.

---

## 2026-05-24 (afternoon) — Migrated backend target from .NET 8 to .NET 10 LTS

**By:** AI-assisted, prompted by tech lead's local build failure (no .NET 8 SDK installed; .NET 9 STS rejected on grounds of EOL ~now).

**Decision recorded:** [`decisions/ADR-009-dotnet-10-lts.md`](decisions/ADR-009-dotnet-10-lts.md)

**Files changed:**

- `backend/global.json` — SDK version `8.0.100` → `10.0.100`
- `Directory.Build.props` (repo root) — `TargetFramework` `net8.0` → `net10.0`; `LangVersion` `12.0` → `latest` (C# 14)
- `backend/Directory.Packages.props` — bumped Microsoft-owned packages to `10.0.0`:
  - `Microsoft.AspNetCore.{OpenApi,Authentication.JwtBearer,SignalR.StackExchangeRedis,Mvc.Testing}`
  - `Microsoft.EntityFrameworkCore.{,.Relational,.Design,.Tools}`
  - `Microsoft.Extensions.{Http,Http.Polly,Caching.StackExchangeRedis,Hosting,DependencyInjection.Abstractions,Logging.Abstractions}`
  - `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`
- Replaced obsolete `Microsoft.AspNetCore.Mvc.Versioning` with `Asp.Versioning.{Mvc,Mvc.ApiExplorer}` 8.x
- `Microsoft.AspNetCore.Localization` removed (built into ASP.NET Core)
- `Serilog.AspNetCore` and `Serilog.Extensions.Hosting` bumped to 9.0.0
- `Swashbuckle.AspNetCore` bumped to 7.2.0
- `Rmms.Application.csproj` and `Rmms.Worker.csproj` — removed inline `Version` attrs (central management conflict)
- `backend/src/Rmms.Api/Dockerfile` — base images `dotnet/aspnet:8.0-jammy` → `dotnet/aspnet:10.0`, `dotnet/sdk:8.0-jammy` → `dotnet/sdk:10.0`
- `backend/src/Rmms.Worker/Dockerfile` — switched from `runtime:8.0-jammy` to `aspnet:10.0` (Hangfire.AspNetCore needs ASP.NET shared framework)
- `knowledge-base/02-tech-stack.md` — bumped table + "Why .NET 10 LTS?" section + ADR-009 cross-link
- `knowledge-base/PROJECT-STATE.md` — updated phase / decisions / next-steps sections
- `knowledge-base/decisions/README.md` — added ADR-009 to index

**Third-party packages unchanged** (Hangfire, Polly, Mediator, FluentValidation, Mapster, Sentry, BCrypt, Refit, SendGrid, FirebaseAdmin, Minio, NetTopologySuite, ClosedXML, QuestPDF, xUnit, Testcontainers, Bogus, Moq) — all already target `netstandard2.0` or declare `net10.0` support.

**Not yet verified on real SDK** — user to run `dotnet restore && dotnet build` and report any package-resolution issues; if a Microsoft package patch >= `10.0.0` isn't published, bump to nearest available.

---

## 2026-05-24 (morning) — Repository scaffold complete (Sprint 00 partial)

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
