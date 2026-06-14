# PROJECT STATE — RMMS 2026

> **READ THIS FIRST.** This file is the single source of truth for "where the project is right now". Every AI session and every new dev should open this file before doing anything else. Update on every significant milestone.

**Last updated:** 2026-06-14 — **Phase 1A code DONE (awaiting your UAT/sign-off); Phase 1B kicked off — M10 Form Engine design + ADR-014/015/016.** Go-live infra ready ([docker-compose.prod.yml](../docker-compose.prod.yml) pulls GHCR images via `release.yml` tag build; ops scripts `smoke-prod.ps1` + `backup-restore.sh`). ⚠️ Known go-live gap: image is runtime-only + API doesn't auto-migrate + no `migrate` CLI verb → prod migrations applied manually via `dotnet ef` for now. **Phase 1B design landed:** [M10 Form Engine design](./modules/M10-form-engine-design.md) (schema-driven JSONB registry + factory renderer + offline/idempotency + versioning BR-505) + [ADR-016](./decisions/ADR-016-form-engine-schema-driven.md) (engine) + [ADR-014](./decisions/ADR-014-web-form-builder-dnd-kit.md) (@dnd-kit) + [ADR-015](./decisions/ADR-015-web-reports-recharts.md) (Recharts). **M10 Form Engine Flutter renderer ⏳ code-only (AC-22/23).** `features/forms` — plain dynamic models, `FormsApi`/repo (`/forms/me`, `/forms/:id`, `/submit`), **`DynamicField` factory renderer** (text/number/choice/dropdown/multi/datetime/section; media+entity pickers = placeholder, deferred), **offline draft via Hive** (`FormDraftStore`, stable clientKey → server dedup), `FormsListScreen` + `FormFillScreen`, `/forms` routes + home tile, i18n vi/en (parity 309=309). No build_runner (plain models) — **Mac runs `gen-l10n` + `analyze`**. _(Deferred: media/entity widgets, scoring display, reconnect auto-retry, server visible_if/scoring.)_

**M10 Form Engine fill/submit + assignment BE ✅ (AC-22/23).** `FormAssignment` (OR-logic targets) + `FormSubmission` (version snapshot, jsonb answers, client-idempotency dedup); CQRS AssignForm / GetMyForms (resolve by role/user/store/category, published-only) / GetFormForFill / SubmitForm (**idempotent offline retry** + required-field validation; visible_if/scoring deferred); `FormsController` (`GET /forms/me`, `GET /forms/:id`, `POST /forms/:id/submit`) + admin `POST /forms/:id/assignments`; migration `M10_FormFill`; **251 unit tests green** (+6). _(Next: Flutter dynamic renderer + offline Hive + submit/idempotency + scoring; server visible_if/scoring.)_

**M10 Form Engine web Form Builder ✅ (AC-20/21).** `/forms` list + `/forms/[id]` Builder: drag-drop field list (`@dnd-kit`, ADR-014 installed), field editor (id/label vi-en/required + options for choice types), rules panel, save-draft + publish (save→publish) + version-history drawer; schema round-trips losslessly. i18n `forms.*` + `navForms` (parity 438=438); type-check+lint+`next build` green. _(Next: S13 mobile renderer/offline/submit + form assignment endpoints.)_

**M10 Form Engine BE skeleton ✅ (AC-20/21).** `Rmms.Domain.Forms` — `Form` (code/name+desc vi-en/FormType/CurrentVersion/FormStatus) + `FormVersion` (schema jsonb, immutable once published); schema validator by input-type registry (ADR-016, not generic JSON-Schema); CQRS Create/UpdateDraft/Publish/GetForms/GetForm/GetFormVersions with **BR-505 versioning** (edit published → new draft v(n+1), publish freezes + sets current_version); `AdminFormsController`; migration `M10_Forms`; **245 unit tests green** (+7). `@dnd-kit`+Recharts approved (install S12/S16). _(Migration not applied to prod; web Form Builder S12 + mobile renderer/offline S13 next.)_

**M04 Product Master ✅ (AC-25) — BE + web.** `products` (sku/name/brand/category_id soft-ref/`attributes` jsonb/status) + migration `M04_ProductMaster`; CQRS Create/Update/ChangeStatus/Delete + paginated search (`GetProductsQuery`, ActiveOnly for mobile); `AdminProductsController` + read-only `ProductsController`; **238 unit tests green** (+7). Web `/products` admin (ProTable search+category filter+status, create/edit modal w/ JSON-validated attributes, activate/deactivate/delete) + `navProducts`; i18n vi/en parity 364=364; type-check+lint clean. _(Migration not yet applied to prod; mobile `product_selector` → Form Engine M10/S13, read endpoint ready.)_ **Next: Sprint 11/12 Form CRUD skeleton (M10).**

**Earlier (2026-06-14) — Sprint 10 ✅ — M15 basic Dashboard (AC-27).** BE `GET /api/v1/admin/dashboard/summary` (`GetDashboardSummaryQuery`, role-scoped like M12: Admin/BUH all, Leader managed PGs) → KPIs: presence today (total/online/checked-out/not-checked-in/on-leave) + actionable backlogs (attendance pending review, pending approvals, today anomalies); **+2 unit tests → 231 green.** Web `/dashboard` landing (StatisticCard KPI rows + "present today" quick list, links to /attendance + /approvals + /monitoring); new `navDashboard` nav item (admin/leader/buh), **login now lands on `/dashboard`** (was `/users`), non-admin fallback → `/dashboard`. ui-ux-pro-max: Data-Dense Dashboard, status colors green/amber/red, tabular nums. Web typecheck+lint clean. **i18n full pass ✅** — key parity verified (web 336=336, mobile ARB 299=299, no missing keys); fixed 3 hardcoded strings (mobile home quick-access + role chip via existing keys; removed web dev scaffold note). **Phase 1A release notes + user guide drafted** → [RELEASE-NOTES-Phase-1A.md](./RELEASE-NOTES-Phase-1A.md) (shipped AC-1..19/26/27-basic/35 = 23/35; 1B = Form Engine/Product Master/Visit Plan/Documents/News/full reports = 12/35) + [USER-GUIDE-Phase-1A.md](./USER-GUIDE-Phase-1A.md) (per-role vi walkthrough + 5 Mermaid flows). _(Full reports + Excel/CSV export → 1B/Sprint 16. Sprint 10 code done; remaining = ops checklist needing real devices/stakeholder.)_

**Earlier (2026-06-14) — Mobile Redesign 2026 follow-ups.** Self-service **face removal** (`DELETE /api/v1/face` → `RemoveMyFaceCommand`, deletes CompreFace subject + clears enrollment + audit `FaceRemoved self=true`, re-enroll required BR-206) wired to the mobile "Xóa khuôn mặt" button; **layered mesh gradient** (`MeshRadialOverlay` over the base linear `meshGradient` on Home/History/Assignment per ADR-012); legacy **`brand_widgets.dart` retired** — Approvals + Team-monitoring migrated to the canonical `app_widgets.dart` kit. Backend builds (0 errors); mobile awaits Mac `flutter analyze`. _(Sprint 10 still next.)_

**Earlier (2026-06-08) — Sprint 09 ✅ CLOSED (M14 Notification + M16 Admin Review).** M14: in-app `Notification` aggregate + `INotificationService` (best-effort push/email/realtime), **real FCM** (`FcmPushSender`, Firebase Admin HTTP v1, behind `Push:Provider=fcm`; default `LoggingPushSender`), **SignalR realtime** (`NotificationsHub` `/hubs/notifications`, JWT via `access_token` query, per-user `HubUserIdProvider`; web `@microsoft/signalr` hook → live toast + query invalidation; mobile on FCM). CR-2 events / CR-3 channel matrix (approval→in-app+push+email; `AttendanceInReview`→in-app only). M16: web Admin Review actions wired; attendance "Chi tiết" modal fixed (was a ProTable child). **Anti-spoof:** check-in selfie now via `LivenessCaptureScreen` (front camera + `google_mlkit_face_detection` randomised blink/smile/turn challenge, ADR-013) → still → CompreFace match. **Infra:** MinIO + CompreFace pgdata bind-mounted to host `./data` (backup); `MinioOptions.PublicEndpoint` for browser-reachable presigned URLs. **Fixes:** attendance timer counts elapsed-since-check-in; GPS multipart parsed with `InvariantCulture` (vi-VN `.`-as-thousands bug); 11 `destroyOnClose`→`destroyOnHidden`; ML Kit `format.raw as int`. **229 unit tests green**; web typecheck+lint clean; mobile `flutter analyze` clean on Mac. **Next: Sprint 10.** _(history below)_

**Earlier (2026-06-06) — M05 Attendance core (Sprint 03) — BE + web done, mobile code-only.** Check-in/out with GPS geofence (BR-204), fake-GPS block (BR-205), early-window (AC-5) + late (AC-6), status state machine (§3.2), history, Admin list + review queue (AC-9). Bound to M07 shifts (`work_schedule_shift_id`). **Face (M06) + photo storage (M13/MinIO) ship as stubs** (`IFaceVerificationService`/`IAttendancePhotoStorage`) per Sprint 03 goal. Endpoints `api/v1/attendance/{today,check-in/info,check-in,:id/check-out,history}` + `api/v1/admin/attendance/{list,:id/review}`; migration `M05_Attendance` applied; **+18 unit tests → full suite 183 green**. Web `/attendance` (ProTable filters + status Tag icon+text + review modal + photo placeholders) — type-check/lint/build green. Mobile `features/attendance` (today/capture-with-geolocator+camera/history + iOS/Android permissions) **code-only, awaiting Mac verify**. _(Earlier same day: M07 Work Schedule built ahead of M05 — full suite was 165; Sprint 02 deferred items cleared — store map ADR-010 + mobile FCM client-side.)_
**Sprint 03 follow-up (2026-06-06):** real **MinIO photo storage** (`MinioAttendancePhotoStorage`, object keys + presigned preview URLs; local no-op fallback when unconfigured) replaced the stub; **photo-retention** daily Hangfire job (CR-4, purge >90d). **184 unit tests green.** EXIF verify (server) + EXIF write (mobile) **deferred to M13** (photo-integrity pipeline); Face stays stub → M06. New server `103.216.116.206`: `rmms` DB initialized (PostGIS + all migrations) + demo data seeded via `seed-demo` CLI (1 admin / 2 leaders / 5 PGs / 3 stores / assignments).
**Sprint 04 (2026-06-06) — M06 Face Verification BE done (CompreFace, ADR-011):** `IFaceClient` port + `CompreFaceClient` (gated by API key) + `DevFaceClient` fallback; `FaceVerificationService` replaces the M05 stub (unenrolled→PendingReview BR-206, engine-down→PendingReview BR-207); endpoints `face/{status,enroll,verify}` + `admin/face/{re-enroll,template}`; audit `face.*`; **191 unit tests green**; no schema change. FPT.AI dropped per ADR-011.
**Sprint 04 follow-up (2026-06-06) — M06 web + mobile + new mobile theme:** Web Users admin gains a **Face status column** (PG/Leader) + Drawer actions **re-enroll / remove template** (`AdminUserDto` +`faceEnrolled`/`faceEnrolledAt`); web typecheck + backend build green. Mobile **M06**: `features/face/` (Freezed `FaceStatus`, api/repo/`faceStatusProvider`) + **3-angle enrollment wizard** + home enroll-nudge. **New modern mobile theme** (ui-ux-pro-max): indigo+emerald flat-modern — `app_palette.dart` (+`AppSemantics` ThemeExtension), rewritten `app_theme.dart`, `brand_widgets.dart` kit, redesigned home dashboard. Mobile code-only → **Mac runs `build_runner` + `flutter analyze`**. Enrolled-photo-vs-selfie compare still deferred (CompreFace owns embedding).
**Sprint 04 ✅ CLOSED (2026-06-06) — M06 Face Verification:** CompreFace stack live (5 containers, gateway :8000; core pinned 1.1.0; fe `PROXY_CONNECT_TIMEOUT`); real engine wired via user-secrets (`CompreFace:ApiKey`); admin-side enrollment (`POST /admin/face/enroll/:id` + web Upload); **verified end-to-end** (pg1 enrolled → recognize matched pg1 @ 0.995 ≥ 0.85 → Success).
**Sprint 05 ✅ CLOSED (2026-06-06) — M07 Work Schedule + versioning (AC-14/15):** BE aggregate + CRUD + BR-308 version chain (16 unit tests) and web `/schedules` team overview were built ahead; closed the mobile gap — **edit flow** (`PATCH /schedule/{id}` via RegisterScheduleScreen edit mode; approved-edit → new edit_pending version, old stays effective) + themed `StatusPill`. **Next: Sprint 06 → M09 Approval Workflow Engine + BUH email-link (AC-17/18/19).**
**Sprint 06 in progress (2026-06-07) — M09 Approval Workflow Engine BE done:** generic `approvals` + `approval_email_tokens` (migration `M09_Approvals` applied to server DB); endpoints `approvals/{pending,:id,:id/approve,:id/reject}` + admin `:id/override` (BR-408) + public `email-action`/`email-action/confirm` (BR-407 BUH email-link, HMAC HS256 24h one-time, logs IP/UA); `IApprovalTokenService` + `IApprovalService` producer (bilingual email); audit `approval.*`; **208 unit tests green**. Deferred: schedule→approval producer wiring (M07 keeps its own approve/reject), web BUH/Admin UI + public landing page, mobile Leader queue. _(Dev API PID 24656 was stopped to gen the migration — restart `dotnet run`; CompreFace user-secrets persist.)_
**Sprint 06 web+mobile (2026-06-07):** Web public BUH **email-link landing** `/approve?token=` (AC-18, no login) + role-aware `/approvals` queue (Leader/BUH approve/reject AC-17; Admin all+override AC-19, backed by new `GET /admin/approvals`). Mobile `features/approvals` + `ApprovalsScreen` (Leader inline approve/reject, themed, home tile gated to leader). Web typecheck+lint green; mobile code-only (Mac build_runner+analyze). Remaining M09: schedule→approval producer wiring + SendGrid.
**Sprint 06 schedule wiring (2026-06-07):** PG schedule submit → creates M09 approval routed to Leader (BR-405, idempotent); two-way `ScheduleApprovalSync` (M09 queue/email-link actuates schedule incl. BR-308; M07 `/schedules` decisions clear the queue). **210 unit tests green.** AC-17 now end-to-end (PG submit → Leader queue → approve → schedule approved). Remaining M09: Leader→BUH routing (needs Leader↔BUH assignment) + SendGrid.
**Sprint 07 (2026-06-07) — M08 Leave & OT BE done:** `LeaveRequest` (regular/emergency + `linked_attendance_id`) + `OtRequest`; endpoints `leave-requests/{·,emergency,me,:id}` + `ot-requests/{·,me}`; migration `M08_LeaveOt` applied to server DB. Wired into M09: create routes to Leader (`IApprovalService`, links `approval_id`); generalized `ApprovalActuation` drives schedule/leave/ot status on decision; emergency needs open check-in (409 else). **215 unit tests green.** Web admin all-requests + mobile forms/history pending.
**Sprint 07 web+mobile (2026-06-07):** Web `/requests` admin (tabs Leave/OT, `GET /admin/{leave,ot}-requests`). Mobile `features/requests` — history (tabs), leave + OT forms, **emergency leave from check-out flow**, home tile. **AC-16 end-to-end.** Web green; mobile code-only (Mac build_runner+analyze).
**Sprint 08 BE (2026-06-07) — M12 Team Monitoring + M16 Audit viewer:** `GET /team-monitoring/today` (per-member today status from attendance+schedule+leave; Leader=managed PGs, Admin/BUH=all) + `GET /admin/audit-logs` (filter+paginate over the M01 append-only audit). **220 unit tests green**, no new migration. Web dashboard/explorer + mobile Leader list pending.
**Sprint 08 web+mobile (2026-06-07):** Web `/monitoring` (summary + member table) + `/audit-logs` (admin explorer); role nav allows non-admin on approvals+monitoring. Mobile `features/monitoring` + `TeamMonitoringScreen` (leader home tile). AC-26/27/35 covered.
**Current phase:** Phase 1A — **Sprint 09 (M14 Notification + M16 Admin Review) CLOSED; next: Sprint 10.** _(Dev API may be stopped — `dotnet run` to restart; creds in appsettings.Development.json.)_ ✅ M02 device-approval BE + web done. ✅ **M03 DONE:** domain (Area/Store+GPS/Category + 3 assignment aggregates), migration `M03_Organization_Assignment` (6 tables + partial unique: 1 active Leader/PG, 1 active store/user); backend CRUD `/admin/{stores,areas,categories}` + assignments `/admin/assignments/{pg-leader,user-store,user-category}` + `GET assignments/user/{id}`; mobile read endpoints `GET /users/me/{stores,leader}`; **Leader-scoped device approval** (M02 endpoints now AdminOrLeader — Leaders scoped to managed PGs, BR-106); web Stores/Areas/Categories pages + nav + assignment panel; mobile Flutter `organization` feature (models/api/repo/providers + MyAssignmentsScreen) verified on macOS; idempotent `backend/scripts/seed-m03-testdata.sql`; 149 unit tests green. **Sprint 02 ✅ CLOSED** (M02+M03, AC-2+AC-3) — deferred: store map view (needs map-lib ADR), mobile FCM push (→M14), CSV bulk assign (Phase 2). **Next: Sprint 03 → M05 Attendance core** (attendance_records + state machine + history + admin list; Face/M06 + MinIO photos + shift-binding/M07 deferred within S3). _(Sprint 01 (M01) closed; Day 10 manual UAT/demo skipped.)_
**Sprint 02 status:** ✅ **CLOSED** (2026-06-05; 2 deferred items cleared 2026-06-06) — M02 device approval (incl. Leader-scoped BR-106) + M03 Organization & Assignment (BE + web + mobile read + assignment panel), 149 unit tests green. **Store map view** delivered via react-leaflet + OSM ([ADR-010](./decisions/ADR-010-store-map-react-leaflet.md), table/map toggle on `/stores`). **Mobile FCM** client-side delivered (guarded Firebase init, token-on-login, foreground banner, device-change push → pending-screen reaction); server push *delivery* still →M14. Still deferred: CSV bulk assignment (Phase 2).
**M07 Work Schedule status (2026-06-06):** ⏳ **BE + web DONE, mobile code-only (awaiting Mac verify).** Built ahead of M05 (sprint reorder) because attendance AC-5/AC-6 need shift times. Tables `work_schedules` + `work_schedule_shifts` (owned collection); versioning BR-308 (edit approved → new `edit_pending`, old stays effective until approved, then `superseded`); Leader-scoped approve/reject (BR-404 reason required). Endpoints `api/v1/schedule/{me,me POST,:id/submit,:id PATCH,:id DELETE,user/:id,:id/approve,:id/reject}`. Web `/schedules` admin overview. Mobile day/week/month register + list/submit/withdraw. **165 unit tests green.** Remaining: BUH email-link approval + generic approvals table → M09; full mobile month-grid calendar (used list view, no calendar lib per no-new-UI-lib rule).
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
- ✅ **All 11 ADRs authored** — `decisions/ADR-001..011.md` covers Modular Monolith, Mediator (Othmar), UUID v7, soft-delete interceptor, snake_case naming, PostGIS-deferred, Caddy reverse proxy, Tailwind preflight-off, .NET 10 LTS, react-leaflet+OSM store map, and self-hosted CompreFace face verification
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

All 11 ADRs are **Accepted** and live in `knowledge-base/decisions/`:

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
| [ADR-010](./decisions/ADR-010-store-map-react-leaflet.md) | react-leaflet + OpenStreetMap for store map view (Google Maps / Mapbox rejected) | 2026-06-06 |
| [ADR-011](./decisions/ADR-011-compreface-self-hosted-face.md) | Self-hosted CompreFace for Face Verification — replaces FPT.AI (privacy + no per-call cost) | 2026-06-06 |
| [ADR-012](./decisions/ADR-012-mobile-redesign-2026.md) | Mobile Redesign 2026 — indigo+emerald flat-modern theme + `google_fonts` bundled offline | 2026-06-08 |
| [ADR-013](./decisions/ADR-013-mobile-liveness-mlkit.md) | Active liveness via `google_mlkit_face_detection` (on-device challenge) for check-in anti-spoof | 2026-06-08 |

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
