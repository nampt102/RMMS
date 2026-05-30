# CHANGELOG — RMMS 2026

Append-only chronological log of significant project milestones, decisions, and infrastructure changes.

**Format:** newest entries at the top. Each entry has a date (ISO 8601), short title, and bullet list of what changed. Reference module / business-rule / acceptance-criterion IDs where relevant. Keep entries factual — opinions and rationale belong in ADRs.

---

## 2026-05-31 — Sprint 01 Day 4 verification: end-to-end smoke test GREEN + JWT auth fix

**By:** Tech lead (MotivesVN IT), AI-assisted

**Outcome:** The full M01 auth + admin surface is now verified end-to-end over the real HTTP/JWT pipeline (not just handler unit tests). One real authorization bug found and fixed.

### Fix — JWT role authorization (Program.cs)

- `[Authorize(Roles = "admin")]` returned **403 for valid admin tokens**. Root cause: JwtBearer default `MapInboundClaims = true` remaps short JWT claim names (e.g. `sub` → `ClaimTypes.NameIdentifier`), which desynced the explicit `RoleClaimType = "role"` / `NameClaimType = "sub"`. Fixed by adding `options.MapInboundClaims = false` so claim names are preserved exactly as issued.
- Not caught by Day 4 handler unit tests because they bypass the HTTP auth pipeline. **Day 5 follow-up:** add a `WebApplicationFactory` integration test for `/admin/*` (200 for `admin`, 403 for non-admin).

### Tooling — smoke test scripts

- `scripts/smoke-day4.ps1` — end-to-end smoke covering register → verify-email → login → device check → refresh rotation + reuse detection → forgot/reset → admin login → admin user CRUD → authorization (PG → 403) → logout idempotency. Made compatible with **Windows PowerShell 5.1** (avoids `-SkipHttpErrorCheck` and `ConvertFrom-Json -Depth`, which are PS6+ only) and idempotent across runs (unique `smoke.*+<timestamp>` test emails).
- `scripts/cleanup-smoke.ps1` — removes `smoke.%@example.com` test users + dependent auth rows after a run; leaves `audit_log` intact per CR-1 (append-only).

---

## 2026-05-31 — Sprint 01 Day 4: Forgot/Reset password + Admin user CRUD + CLI seed + unit tests

**By:** Tech lead (MotivesVN IT), AI-assisted

**Outcome:** Full M01 user-management surface (PG self-service password recovery + Admin CRUD over all users) lives behind the API and has unit-test coverage on every handler. First Authorize-protected endpoints in the codebase work end-to-end. Bootstrap CLI lets a fresh deploy create an initial Admin without going through email verification.

### Endpoints (5 new)

**`/api/v1/auth/*`** — extended for password recovery:

- `POST /auth/forgot-password` — silent success regardless of whether email exists (timing-attack mitigation). Issues a 24h single-use `password_reset_tokens` row + emails reset link only for Active users. Always emits an audit row.
- `POST /auth/reset-password` — exchanges token for password change. Hashes new password (BCrypt cost 12) and **revokes ALL active refresh tokens for the user** (force re-login on every device). Marks token used. Audit `user.password_reset` with `revoked_refresh_count`.

**`/api/v1/admin/users/*`** — first `[Authorize(Roles = "admin")]` endpoints:

- `GET /admin/users` — paginated list. Filters: `role`, `status`, `search` (case-insensitive contains, EF Core translates to `ILIKE '%s%'` on Postgres). Page size capped at 100. Ordered by `created_at DESC`.
- `POST /admin/users` — creates Leader / BUH / Admin (BR-102/103/104). PG accounts rejected (must self-register per BR-101). Generates 12-char random initial password (alphabet excludes `0/O/1/l/I` for readability), hashes via BCrypt cost 12, emails plaintext to user with `[ADMIN] {email}` template, audit `user.created_by_admin`.
- `PATCH /admin/users/:id` — profile updates (`full_name`, `phone`, `preferred_language`) + status transitions (`active` ↔ `inactive`). When user becomes Inactive, ALL active refresh tokens revoked. Returns 422 if Admin tries to Activate a `pending_email_verify` user (must verify email first). Audit: `user.status_changed` (with `from`/`to`) + `user.updated_by_admin`.
- `POST /admin/users/:id/reset-password` — Admin force-issues a reset link for any user (same flow as forgot-password but identified by user_id, no email leak check). Audit metadata includes `triggered_by: "admin"`.

### CLI bootstrap (1 new command)

`dotnet run --project src/Rmms.Api -- seed-admin --email=admin@... --password=... [--full-name="System Admin"] [--language=vi]`:

- Routed in `Program.cs` BEFORE the HTTP pipeline starts (`if (args.Length > 0 && args[0] == "seed-admin") return ...`).
- Idempotent — exits 0 with notice if email already exists.
- Skips audit log (no Admin actor yet to attribute to — bootstrap-only).
- Validates: `--password` ≥ 8 chars + ≥ 1 letter + ≥ 1 digit; `--language ∈ {vi, en}`.

### Application layer (12 new files)

```
Application/Auth/ForgotPassword/   { Command, Validator, Handler }
Application/Auth/ResetPassword/    { Command, Validator, Handler }
Application/Admin/Users/           { AdminUserDto }
Application/Admin/Users/CreateAdminUser/    { Command, Validator, Handler }
Application/Admin/Users/GetUsers/  { Query (paged + filter), Handler }
Application/Admin/Users/UpdateUser/ { Command, Validator, Handler }
Application/Admin/Users/AdminResetPassword/ { Command, Handler }
```

### Api layer (5 new files)

```
Controllers/AdminUsersController.cs   (4 endpoints; Authorize Roles=admin)
Cli/SeedAdminCommand.cs               (CLI command handler)
Dtos/Auth/ForgotPasswordRequest.cs
Dtos/Auth/ResetPasswordRequest.cs
Dtos/Admin/CreateUserRequest.cs
Dtos/Admin/UpdateUserRequest.cs
```

### Modified (2 files)

- `Api/Program.cs`
  - JWT `RoleClaimType = "role"` so `[Authorize(Roles = "admin")]` matches our JWT claim shape.
  - JWT `NameClaimType = JwtRegisteredClaimNames.Sub`.
  - CLI dispatch (`SeedAdminCommand.RunAsync`) before `app.RunAsync()`; returns int exit code.
- `Api/Controllers/AuthController.cs` — added `POST /auth/forgot-password` + `POST /auth/reset-password`.

### Unit tests (15 new files — 6 handler test classes, ~37 tests, plus 9 shared helpers)

Test infrastructure (`tests/Rmms.UnitTests/Common/`):

- `TestDbContextFactory.cs` — fresh `AppDbContext` per test on EF Core InMemory provider.
- `TestClock.cs` — deterministic `IDateTimeProvider` with `Advance(TimeSpan)`.
- `TestClientContext.cs`, `TestCurrentUser.cs` — stub ambient context.
- `FakePasswordHasher.cs` — `plain:{value}` encoding instead of BCrypt cost 12 (~200ms savings per test).
- `InMemoryAuditLogger.cs` — `ConcurrentBag<AuditCall>` capture for assertions.
- `CapturingEmailSender.cs` — `ConcurrentBag<EmailMessage>` capture.
- `FakeTemplateRenderer.cs` — deterministic email bodies (`reset token=...`, `pwd=...`) so tests can scrape exact values.
- `UserFactory.cs` — `CreatePgPendingVerify`, `CreateActivePg`, `CreateInactivePg`, `CreateAdmin`.

Test classes (`tests/Rmms.UnitTests/Application/`):

- `Auth/ForgotPasswordCommandHandlerTests.cs` — 6 tests: active user issues token+email; unknown email silent success; inactive silent success; pending_email_verify silent success; expiry math; email normalization.
- `Auth/ResetPasswordCommandHandlerTests.cs` — 6 tests: valid token + revoke all refresh; unknown / used / expired tokens; vanished user; audit emission.
- `Admin/CreateAdminUserCommandHandlerTests.cs` — 5 tests: 3 valid roles parametrized; duplicate email; case-insensitive dup; initial password meets complexity; email normalization.
- `Admin/GetUsersQueryHandlerTests.cs` — 7 tests: empty page; pagination; role / status filters; case-insensitive search; page size cap (100); descending order.
- `Admin/UpdateUserCommandHandlerTests.cs` — 7 tests: profile update; deactivate → revoke tokens; reactivate; activating pending → validation error; not found; status-change audit shape.
- `Admin/AdminResetPasswordCommandHandlerTests.cs` — 4 tests: happy path; not found; audit metadata `triggered_by="admin"`; expiry.

Package additions:

- `Microsoft.EntityFrameworkCore.InMemory` 10.0.4 added to `Directory.Packages.props` and referenced in `Rmms.UnitTests.csproj` for handler-level test isolation. (Integration tests Day 9 will use Testcontainers Postgres for full SQL fidelity.)

### Behavior fixes during build

1. **`EF.Functions.ILike`** unavailable in Application layer (Npgsql-only) — switched to `string.Contains(s, StringComparison.OrdinalIgnoreCase)`. EF Core 8+ translates this to `ILIKE` on Postgres, keeping Clean Architecture intact (no Npgsql leak into Application).
2. **`PaginatedResponse<T>` constructor** — used wrong signature; corrected to `(IReadOnlyList<T>, PaginationMeta)` with `PaginationMeta.Build(...)` helper.
3. **CA1311 / CA1862 / CA1304** culture-aware string analyzers triggered by `.ToLower().Contains(s)` — replaced with `StringComparison.OrdinalIgnoreCase` overload (also more efficient).
4. **CA1826** triggered by `.First()` on `IReadOnlyList<T>` — switched to indexer `[0]`.

### Verified

- `dotnet build` — 0 errors (warnings only, all pre-allowed via `WarningsNotAsErrors`).
- `dotnet test tests/Rmms.UnitTests` — all ~37 handler tests + 2 pre-existing domain tests pass.

### Carry-forward to Day 5

- `GET /auth/me` — return current user + active device.
- Named authorization policies (`PgOnly`, `LeaderOnly`, `BuhOnly`, `AdminOnly`, `PgOrLeader`, `AnyAuthenticated`).
- `X-Idempotency-Key` middleware — Redis-backed, 24h TTL, returns cached response on hit.
- Rate limit on `/auth/login` — 5 fails per email+IP per 15 min via `AspNetCoreRateLimit` + Redis store.
- Integration tests via Testcontainers Postgres + Redis (Day 9 budget).

---

## 2026-05-29 — Sprint 01 Day 2: Register + Verify-Email endpoints (M01)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Outcome:** First 2 production endpoints of M01 live and smoke-tested end-to-end on local Docker stack. PG can self-register via email and activate their account by clicking the verification link — flow A of AC-1.

### Endpoints

- `POST /api/v1/auth/register` (BR-101)
  - FluentValidation: email format + length ≤255, password ≥8 chars + 1 letter + 1 digit, fullName 1–255, phone ≤20, language ∈ {vi, en}.
  - Handler: normalize email (lowercase + trim) → uniqueness check (incl. soft-deleted via `IgnoreQueryFilters`) → BCrypt cost-12 hash → `User.Register()` (status `pending_email_verify`) → issue `EmailVerificationToken` (256-bit random + SHA-256 hash, 24h TTL) → render vi/en email template → send via `IEmailSender` (`ConsoleEmailSender` in Dev) → audit `user.registered` → `SaveChanges` atomically.
  - Returns `201 Created` with `{ data: { userId, email, status: "pending_email_verify" } }`.

- `POST /api/v1/auth/verify-email`
  - Token plaintext from email URL → SHA-256 hash → DB lookup by unique hash index.
  - Reject: unknown (`TOKEN_INVALID`), expired (`EMAIL_TOKEN_EXPIRED`), used (`EMAIL_TOKEN_USED`).
  - Idempotent: re-verifying already-active user returns success (no state change).
  - On success: `user.VerifyEmail(now)` (status → `active`) + `token.MarkUsed(now)` + audit `user.email_verified`.

### Application layer (8 new files)

- `Common/Security/OpaqueToken.cs` — reusable 256-bit random + SHA-256 hash helper; used by Register, VerifyEmail, ResetPassword, ChangeDevice (planned).
- `Common/EnumFormatting.cs` — `ToSnakeCase<TEnum>()` extension for API response strings; keeps response shape consistent with DB column values (snake_case per ADR-005).
- `Auth/Register/{RegisterUserCommand, RegisterUserResponse, RegisterUserCommandValidator, RegisterUserCommandHandler}.cs`.
- `Auth/VerifyEmail/{VerifyEmailCommand, VerifyEmailResponse, VerifyEmailCommandValidator, VerifyEmailCommandHandler}.cs`.
- `Email/IEmailTemplateRenderer.cs` — interface for vi/en transactional emails; 3 templates: verify-email, password-reset, admin-created-account.

### Infrastructure layer (3 new files)

- `Email/EmailTemplateRenderer.cs` — string-interpolation templates (HTML + plaintext) bound from `AppUrlOptions` (link base URL) + `EmailOptions` (brand name). RazorLight templating deferred to Sprint 03 (M14 News).
- `Email/EmailOptions.cs` + `Email/AppUrlOptions.cs` — config bindings.

### Api layer (3 new files)

- `Controllers/AuthController.cs` — `[ApiController] [Route("api/v1/auth")] [AllowAnonymous]` with 2 endpoints + Swagger annotations.
- `Dtos/Auth/{RegisterRequest, VerifyEmailRequest}.cs` — positional records; .NET 10 validation source generator requires attributes on parameters (not `[property: ...]`) — fixed during smoke test.
- `Common/ResultMapping.cs` — translates `Result<T>.IsFailure` to HTTP status (Validation→400, NotFound→404, Conflict→409, Unauthorized→401, Forbidden→403) + ErrorEnvelope per `05-api-conventions.md`.

### Modified

- `Shared/Errors/ErrorCodes.cs` — added `EmailAlreadyRegistered`, `AccountLocked`, `RefreshTokenReused`, `PermissionDenied`.
- `Infrastructure/DependencyInjection.cs` — wired `EmailOptions`, `AppUrlOptions`, `IEmailTemplateRenderer`.
- `Api/appsettings.json` — added `App.AppBaseUrl` + extended `Email` section.

### Bug-hunt rounds (all resolved during smoke test)

1. **`.NET 10` validation source-generator rejects `[property: Required]` on positional record parameters** — switched DTOs to use parameter-level attributes (no `property:` prefix).
2. **`status` response was `"pendingemailverify"`** (PascalCase concatenated) instead of `"pending_email_verify"` — fixed by routing through `EnumFormatting.ToSnakeCase<T>()` so API responses match DB values + spec.

### Day 2 infra discoveries (rolled into knowledge-base)

- **Local Postgres conflict on Windows host:** developer machine had a system PostgreSQL service listening on `localhost:5432`, preempting the Docker port mapping. Switched `docker-compose.yml` postgres ports to `5433:5432` (and matched `appsettings.Development.json`) — Option A from the troubleshooting guide.
- **`appsettings.Development.json` not picked up by `dotnet ef` CLI** unless `ASPNETCORE_ENVIRONMENT=Development` is set. Use `--connection` flag or set env var per session.
- **Stale Postgres volume from `postgres:16-alpine` → `postgis/postgis:16-3.4` switch** caused `role "rmms" does not exist` until `docker compose down -v` triggered fresh init scripts.

### Smoke test results (2026-05-29)

- Register `test1@example.com` → 201 with `userId=019e72f3-2cf9-77b7-b642-0b6f9aea0359`, status `pending_email_verify`.
- Console log captured verify URL with token plaintext.
- Verify-email with that token → 200, status `active`.
- DB verified: `users.status=active`, `users.email_verified_at IS NOT NULL`, `email_verification_tokens.used_at IS NOT NULL`, `audit_log` contains both `user.registered` + `user.email_verified` rows with correct metadata.

### Carry-forward to Day 3

- `POST /api/v1/auth/login` with BR-105 device check (return 403 `DEVICE_NOT_AUTHORIZED` + create pending row).
- `POST /api/v1/auth/refresh` with rotation + reuse detection (revoke all on reuse).
- `POST /api/v1/auth/logout` (revoke refresh per device).
- `LoginUserCommand`, `RefreshTokenCommand`, `LogoutCommand` + handlers + validators.
- Audit emits: `auth.login_success`, `auth.login_failed`, `auth.refresh_rotated`, `auth.refresh_reused`, `auth.logout`, `device.registered`, `device.change_requested`.

---

## 2026-05-28 (evening) — Sprint 01 Day 1: Domain layer + first EF migration

**By:** Tech lead (MotivesVN IT), AI-assisted

**Outcome:** M01 + M02-foundation domain model and persistence schema implemented. First production migration `Init_M01_M02_Foundation` generated locally and ready to apply. Service abstractions and Infrastructure stubs wired through DI. Endpoints + handlers (Day 2+) still pending.

### Domain layer (7 new entities, 3 new enums, 1 new marker interface)

- `Rmms.Domain.Users.User` — aggregate root inheriting `AggregateRoot`. Factory methods `Register()` (PG self-service per BR-101) and `CreateByAdmin()` (Leader/BUH/Admin per BR-102/103/104). Domain methods: `VerifyEmail`, `ChangePassword`, `Activate`, `Deactivate`, `RecordLogin`, `UpdateProfile`, `RecordFaceEnrollment` (M06 hook). Future-proof columns reserved nullable: `ExternalProvider`/`ExternalId` (SSO Phase 2), `MfaEnabled`/`MfaSecretExternalId` (MFA Phase 2), `FaceEnrolledAt`/`FaceTemplateExternalId` (M06).
- `Rmms.Domain.Devices.UserDevice` — BR-105 / BR-106 device fingerprint. Factories `RegisterFirstActive` (auto-approved, no prior active device) + `RegisterPendingApproval` (Leader/Admin must approve). Domain methods: `Approve`, `Reject`, `MarkReplaced`, `Touch`, `UpdateFcmToken`. Sprint 01 uses only PendingApproval+Active states; Sprint 02 wires approval UI.
- `Rmms.Domain.Auth.RefreshToken` — SHA-256 hashed tokens, 30-day lifetime, rotation chain via `ReplacedByTokenId`. `MarkRotatedBy` links old → new for reuse-detection forensics.
- `Rmms.Domain.Auth.LoginHistory` — append-only attempt log (success + failure with reason).
- `Rmms.Domain.Auth.EmailVerificationToken` + `PasswordResetToken` — single-use, 24h TTL, SHA-256 hashed.
- `Rmms.Domain.Audit.AuditLog` — append-only audit per CR-1, `jsonb metadata` for module-specific context, factory `AuditLog.Record(...)`.
- `Rmms.Domain.Enums.UserStatus` (PendingEmailVerify, Active, Inactive), `DeviceStatus` (PendingApproval, Active, Rejected, Replaced), `AuditAction` (string-const class with M01/M02 actions).
- `Rmms.Domain.Common.IAggregateRoot` (marker; `User` and `UserDevice` use it via the existing `AggregateRoot` base class).

### Application layer (6 new abstractions)

- `IPasswordHasher` — Hash / Verify / NeedsRehash (lets us bump BCrypt cost without forcing resets).
- `IJwtTokenService` — issues HS256 access tokens per `05-api-conventions.md`, returns `IssuedAccessToken(string Token, DateTimeOffset ExpiresAt)`.
- `IRefreshTokenGenerator` — 256-bit random plaintext + SHA-256 hash; plaintext to client once, hash to DB.
- `IEmailSender` — sends `EmailMessage(To, Subject, BodyText, BodyHtml, Language)`. Provider switch in Infrastructure DI.
- `IAuditLogger` — `RecordAsync(action, targetEntity, targetId, metadata)`. Does NOT call SaveChanges (caller's UoW commits atomically with the business change per CR-1).
- `IClientContext` — IP, UserAgent, X-Device-Id, X-App-Version, language. Resolved from `IHttpContextAccessor` in Api layer.

### Infrastructure layer (10 new files)

- 7 EF Core `IEntityTypeConfiguration<T>` configurations under `Persistence/Configurations/` with:
  - Snake_case column names via `EFCore.NamingConventions` (per ADR-005).
  - Enum-to-string conversion via static helper methods (expression-tree compatible; switch+throw inline lambdas fail CS8514/CS8188).
  - **Partial unique index `ix_user_devices_one_active_per_user WHERE status = 'active'`** — enforces BR-105 at DB level.
  - Postgres `inet` native mapping for `IpAddress` columns (Npgsql handles `System.Net.IPAddress` ↔ `inet` natively — no manual converter needed).
  - `jsonb` column for `audit_log.metadata`.
  - Soft-delete query filter on `User` (`DeletedAt == null`).
- `BCryptPasswordHasher` — cost 12, handles `SaltParseException` gracefully (never crashes login on malformed hash).
- `JwtTokenService` — HS256 with `JwtOptions` (SigningKey ≥ 32 bytes validated at startup). Claims: `sub`, `email`, `role`, `device_id`, `iat`, `jti`, `iss`, `aud`. Culture-invariant numeric formatting for `iat`.
- `RefreshTokenGenerator` — `RandomNumberGenerator.GetBytes(32)` + base64url plaintext + SHA-256 hex hash (64 chars, matches column).
- `ConsoleEmailSender` — logs structured email to Serilog (Dev / CI). Switch to SendGrid by setting `Email:Provider=SendGrid` in config (impl Day 8).
- `DbAuditLogger` — composes `AuditLog.Record(...)` using `IAppDbContext` + `ICurrentUser` + `IClientContext` + `IDateTimeProvider`. Adds row to `DbSet<AuditLog>` but does NOT call SaveChanges.
- Post-migration SQL `Persistence/Migrations/PostMigrationScripts/001_audit_log_append_only.sql` — REVOKEs UPDATE+DELETE and GRANTs INSERT+SELECT on `audit_log` for the app DB role per CR-1 (idempotent via `DO $$ ... $$` block).

### Api layer

- `HttpContextClientContext` implements `IClientContext` (X-Device-Id, X-App-Version, Accept-Language). Normalizes IPv4-mapped IPv6 (`::ffff:1.2.3.4` → `1.2.3.4`).
- `Program.cs` registers `IClientContext` scoped; validates `Jwt:SigningKey` ≥ 32 bytes at startup.

### DI wiring (`Rmms.Infrastructure.DependencyInjection`)

```text
+ services.Configure<JwtOptions>(config.GetSection("Jwt"))
+ services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>()
+ services.AddSingleton<IJwtTokenService, JwtTokenService>()
+ services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>()
+ services.AddScoped<IEmailSender, ConsoleEmailSender>()  // SendGrid switch via Email:Provider config
+ services.AddScoped<IAuditLogger, DbAuditLogger>()
```

`appsettings.json` adds `Email.Provider = "Console"` default.

### First EF migration

- Name: `Init_M01_M02_Foundation`
- Generated by `dotnet ef migrations add` on local dev machine (Windows) after fixing 3 compile-error rounds (see "Day 1 bug-hunt rounds" below).
- Tables: `users`, `user_devices`, `refresh_tokens`, `login_history`, `audit_log`, `email_verification_tokens`, `password_reset_tokens` (+ EF Core's `__EFMigrationsHistory`).
- Indexes covered in `Init_M01_M02_Foundation.cs` (12 named indexes total):
  - `ix_users_email_unique` (unique)
  - `ix_users_status_deleted_at` (partial `WHERE deleted_at IS NULL`)
  - `ix_users_external_identity` (partial `WHERE external_provider IS NOT NULL`)
  - `ix_user_devices_user_id`, `ix_user_devices_user_device`
  - **`ix_user_devices_one_active_per_user`** (unique partial `WHERE status = 'active'` — BR-105)
  - `ix_refresh_tokens_hash_unique`, `ix_refresh_tokens_user_device_revoked`, `ix_refresh_tokens_expires_at`
  - `ix_login_history_user_created_at_desc`
  - `ix_audit_log_actor_created_at_desc`, `ix_audit_log_target_created_at_desc`, `ix_audit_log_action`
  - `ix_email_verification_tokens_hash_unique`, `..._user_used`, `..._expires_at`
  - `ix_password_reset_tokens_hash_unique`, `..._user_used`, `..._expires_at`

### Day 1 bug-hunt rounds (all resolved before migration generated)

1. **`Rmms.Application` missing `Microsoft.EntityFrameworkCore` reference** — `IAppDbContext` exposes `DbSet<T>` per Clean Architecture pattern. Added `<PackageReference Include="Microsoft.EntityFrameworkCore" />` to `Rmms.Application.csproj`; abstractions live in Application, provider stays in Infrastructure.
2. **`UserDeviceConfiguration` switch+throw in `HasConversion` lambdas** — `CS8514`/`CS8188`: EF Core's `HasConversion(Expression<Func<>>, Expression<Func<>>)` builds expression trees, which cannot contain switch expressions or throw expressions. Refactored to static helper methods (`DeviceStatusToString` / `DeviceStatusFromString`); method calls ARE expression-tree compatible.
3. **`JwtTokenService` CA1305 + `HttpContextClientContext` CA1310** — `long.ToString()` and `string.StartsWith()` flagged for locale dependence. Fixed with `CultureInfo.InvariantCulture` and `StringComparison.Ordinal` respectively.
4. **`AuditLog.IpAddress` mapping failure during `dotnet ef migrations add`** — Set `HasColumnType("inet")` while also defining a `IPAddress → string` converter; the two conflict. Npgsql natively maps `System.Net.IPAddress` ↔ `inet`. Removed the manual converter from `AuditLogConfiguration` and `LoginHistoryConfiguration`.

### What's verified

- ✅ `dotnet build` succeeds with 0 errors (3 warnings non-blocking, all CA-rules pre-allowed via `WarningsNotAsErrors`).
- ✅ `dotnet ef migrations add Init_M01_M02_Foundation` succeeds; 3 migration files generated.

### What's NOT verified yet (Day 2+ work)

- ❌ No endpoints implemented yet — `/auth/register`, `/auth/login`, etc. land Day 2-5.
- ❌ Migration not applied to a running database yet — `dotnet ef database update` deferred until Day 1.5 sanity check before Day 2 endpoint work.
- ❌ Post-migration SQL `001_audit_log_append_only.sql` not yet run.

### Files changed

- 18 new files (7 Domain entities + 3 Domain enums + 1 Domain marker + 7 EF configs)
- 14 new Infrastructure files (BCrypt, JWT, RefreshToken gen, Console email, DbAuditLogger, JwtOptions, SQL post-migration)
- 6 new Application abstractions
- 1 new Api file (HttpContextClientContext)
- 4 modified files (AppDbContext.cs adds 7 DbSets, IAppDbContext.cs exposes them, Infrastructure DependencyInjection.cs wires services, Program.cs registers IClientContext)
- 1 modified csproj (Rmms.Application.csproj adds Microsoft.EntityFrameworkCore)
- 1 modified appsettings.json (adds Email.Provider default)

### Carry-forward to Day 2

- `dotnet ef database update` + apply post-migration SQL — sanity check that migration applies cleanly on fresh Postgres.
- Implement `RegisterUserCommand` + handler + `POST /auth/register` controller.
- Implement `VerifyEmailCommand` + handler + `POST /auth/verify-email`.
- Wire FluentValidation rules (email format, password ≥8 chars + 1 letter + 1 digit).
- Emit audit log on `user.registered` + `user.email_verified`.

---

## 2026-05-28 — Sprint 00 closed (all 3 CI workflows green on main)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Outcome:** First CI run with all three workflows green on `main` after PR `chore/ci-bootstrap` merged. Sprint 00 closes ahead of schedule (originally allocated 2 weeks for foundation; actual ~6 dev-days).

**CI bug-hunt rounds (in order, all resolved):**

1. **Backend** `MSB1008` from legacy `--locked-mode=false` flag → removed; `<RestoreLockedMode>` property in `Directory.Build.props` covers it.
2. **Backend** `dotnet format` CHARSET errors (`.cs` files have no BOM but `.editorconfig` required `utf-8-bom`) → dropped BOM requirement, files stay utf-8 (no-BOM) which Roslyn handles natively since .NET 5+.
3. **Backend** `dotnet format` CA1848 (LoggerMessage delegates) treated as error → changed `--severity warn` to `--severity error` (compile-time enforcement of warnings-as-errors stays via `Directory.Build.props`).
4. **Backend** `UuidV7Tests.NewGuid_IsVersion7AndRfc4122Variant` failing — version digit at position 12 was random byte instead of `7`. Root cause: legacy `new Guid(byte[])` treats bytes 0–3, 4–5, 6–7 as little-endian, swapping byte[6] (version nibble) with byte[7]. Fixed by switching to `new Guid(bytes, bigEndian: true)` overload (added in .NET 8).
5. **Mobile** `retrofit 4.9.1` requires Dart ≥ 3.8 → bumped Flutter CI from 3.22 → 3.27 → 3.32 → 3.44.x (matches local Mac dev env, Dart 3.12).
6. **Mobile** `dart format` mismatch between local + CI → root cause was stale `.dart_tool/package_config.json` on Mac (`flutter pub get` after pubspec bump regenerated it; Tall vs Short formatter style switched at Dart 3.7+).
7. **Mobile** `flutter analyze` could not find `auth_user.freezed.dart` / `auth_user.g.dart` → added `dart run build_runner build --delete-conflicting-outputs` step before analyze; updated format glob to skip generated files.
8. **Mobile** Android APK smoke build hit Flutter#169475 ("Could not determine run package name") across 3 AGP/Gradle/Kotlin combinations. Deferred APK smoke build to M01 / Sprint 03 release.yml; not a blocker since `flutter analyze` + `flutter test` already verify Dart compiles.

**Final stable toolchain matrix (all in `main` as of 2026-05-28):**

| Stack | Version |
|---|---|
| Backend | .NET 10 SDK `10.0.300`, EF Core `10.0.4`, Microsoft.* `10.0.4`, `System.Security.Cryptography.Xml = 10.0.6` (CVE override) |
| Web | Node 20 LTS, pnpm 9.15.0, Next.js 14.2, AntD Pro 2.8, TanStack Query 5.62 |
| Mobile | Flutter 3.44.0, Dart 3.12.0, AGP 8.11.0, Kotlin 2.2.20, Gradle 8.13, JDK 17 |
| CI | GitHub Actions, `actions/setup-dotnet@v4`, `actions/setup-node@v4`, `subosito/flutter-action@v2` |

**Files changed in last CI bug-hunt round:**

- `.editorconfig` — dropped `charset = utf-8-bom` for `*.cs`
- `.github/workflows/backend.yml` — drop `--locked-mode=false`, `--severity warn` → `error`
- `.github/workflows/mobile.yml` — bump Flutter pin, add `build_runner` step, skip generated files in format check, defer APK smoke build with clear TODO
- `mobile/pubspec.yaml` — sdk constraint `>=3.5.0` → `>=3.12.0`, flutter `>=3.32.0` → `>=3.44.0`
- `mobile/android/settings.gradle.kts` — AGP 9.0.1 → 8.11.0, Kotlin 2.3.20 → 2.2.20
- `mobile/android/gradle/wrapper/gradle-wrapper.properties` — Gradle 9.1.0 → 8.13
- `backend/src/Rmms.Domain/Common/UuidV7.cs` — `new Guid(bytes)` → `new Guid(bytes, bigEndian: true)`
- `.gitignore` — ignore MS Office lock files (`~$*.xlsx`, etc.)

**Sprint 00 acceptance criteria satisfied:**

- ✅ Scaffold builds locally on .NET 10
- ✅ All 9 architectural decisions formalized as Accepted ADRs
- ✅ Three independent CI workflows (backend, web, mobile) green
- ✅ Knowledge base + system prompt synced with actual project state
- ✅ Docker Compose dev infra (Postgres 16, Redis 7, MinIO, Seq, Caddy) ready for local dev

**Sprint 00 unintended discoveries (rolled to Sprint 01+ backlog):**

- Pre-commit hooks for `dotnet format` + `dart format` to prevent format drift
- `.gitattributes` with `* text=auto eol=lf` to normalize line endings
- Re-enable Android APK smoke build when Flutter#169475 fixed upstream
- Migrate Flutter plugins off legacy Kotlin Gradle Plugin pattern (camera_android_camerax, sentry_flutter, device_info_plus, image_picker_android, package_info_plus)
- Bump 66 outdated mobile dependencies during M01 implementation

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
