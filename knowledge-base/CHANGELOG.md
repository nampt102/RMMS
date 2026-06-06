# CHANGELOG — RMMS 2026

Append-only chronological log of significant project milestones, decisions, and infrastructure changes.

**Format:** newest entries at the top. Each entry has a date (ISO 8601), short title, and bullet list of what changed. Reference module / business-rule / acceptance-criterion IDs where relevant. Keep entries factual — opinions and rationale belong in ADRs.

---

## 2026-06-07 — Sprint 06: M09 Approval web + mobile clients

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Web typecheck + lint clean; backend build green. Mobile code-only (Mac runs build_runner + flutter analyze).

- **Web — public BUH email-link landing (AC-18):** `/[locale]/approve?token=` (no login, outside the admin/auth layouts; next-intl middleware doesn't gate it). Previews the signed link via the public `email-action` endpoint (friendly expired/used/already-decided states) and approves/rejects (reason) via `email-action/confirm` — uses bare axios, never the auth client.
- **Web — approvals queue (AC-17/19):** `/[locale]/(admin)/approvals`, role-aware: Leader/BUH see their pending queue with approve / reject-reason; Admin sees all approvals (status filter, paginated) with override-reason. Backed by new BE `GET /api/v1/admin/approvals`. Admin nav "Approvals"; messages vi/en (`approvals` + `approveLink`).
- **Mobile (AC-17):** `features/approvals` (Freezed `Approval`, api/repo/`pendingApprovalsProvider`) + `ApprovalsScreen` — Leader pending queue, inline approve + reject-with-reason dialog (`POST /approvals/:id/{approve,reject}`), themed `SoftCard`/`StatusPill`, pull-to-refresh. Home "Approvals" tile gated to Leader role. ARB vi/en.
- **Still deferred:** schedule→approval producer wiring (M07 keeps its own approve/reject), SendGrid (ConsoleEmailSender logs the link in dev).

## 2026-06-07 — Sprint 06: M09 Approval Workflow Engine BE (CompreFace unaffected)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ BE engine done — full solution builds, **208 unit tests green** (+17). Migration `M09_Approvals` applied to the server DB. Web + mobile + schedule-producer wiring pending.

- **Generic engine:** `Approval` aggregate (entity_type / entity_id / requester / approver+role / status pending→approved|rejected|overridden) + `ApprovalEmailToken`; EF configs + migration `M09_Approvals` (`approvals` + `approval_email_tokens`, unique token-hash index). DbSets on `IAppDbContext`/`AppDbContext`.
- **Decisions:** `GET /api/v1/approvals/pending` (approver queue, AC-17) + `GET /approvals/:id` (requester/approver/admin) + `POST /approvals/:id/{approve,reject}` (reason required, BR-404). Admin `POST /admin/approvals/:id/override` (reason + audit, BR-408/AC-19; second override → 409, first wins).
- **BUH email-link (BR-407 / AC-18):** `IApprovalTokenService` issues HMAC-SHA256 signed JWT-like tokens (24h TTL, nonce); only the SHA-256 hash is persisted. Public `[AllowAnonymous]` `GET /approvals/email-action` (friendly preview: expired/used/already-decided) + `POST /approvals/email-action/confirm` (one-time consume, records decision via `email_link`, logs IP/UA). `Approval:SigningKey` from env/user-secrets (dev fallback key).
- **Producer:** `IApprovalService.CreateAsync` enqueues an approval and, for a BUH approver, issues the token + sends a bilingual (vi/en) email with the `{webBase}/approve?token=` link.
- **Audit:** `approval.{requested,approved,rejected,overridden}` (CR-1).
- **Deferred:** wiring M07 schedule submit → approval (M07 has its own approve/reject; avoid regressions), web BUH/Admin UI + public landing page, mobile Leader queue. **Note:** the local dev API (PID 24656) was stopped to generate the migration — restart with `dotnet run` (CompreFace user-secrets persist).

## 2026-06-06 — Sprint 05 closed: M07 Work Schedule — mobile edit + themed status

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Sprint 05 DoD met. M07 backend + versioning (BR-308) were built ahead (16 unit tests); this entry closes the remaining mobile gap.

- **Mobile edit flow:** `RegisterScheduleScreen` now supports an `editSchedule` param (single-day edit mode: prefilled shifts, hidden mode/date selectors) → `PATCH /schedule/{id}` via new `ScheduleApi.edit` / `ScheduleRepository.edit`. Editing an **approved** schedule triggers the BR-308 new-version flow (old stays effective until re-approval). `WorkSchedule.isEditable` (pending/edit_pending/approved) gates an **Edit** action on each schedule card; route passes the schedule via `extra`.
- **Theme polish:** schedule status chips now use the themed `StatusPill` (success/warning/danger/info/neutral tones + icons) instead of hardcoded colors. ARB keys `scheduleEdit` / `registerEditTitle` / `registerEditHint` / `registerEditSaved` (vi default + en).
- **Already complete (built ahead):** BE aggregate + CRUD + approve/reject + Leader scoping; web `/schedules` team overview with approve/reject; mobile register wizard (day/week/month) + multi-shift editor + submit/withdraw.
- **Next:** Sprint 06 — M09 Approval Workflow Engine + BUH email-link (AC-17/18/19).

## 2026-06-06 — M06 CompreFace stack live + admin-side face enrollment (Sprint 04)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ CompreFace stack up (5 containers healthy, gateway 200 on :8000); backend API build + web typecheck/lint green.

- **CompreFace dựng thật:** `docker compose -f infra/compreface up -d` on the dev box. Two image fixes baked into the compose: **core pinned `1.2.0 → 1.1.0`** (the 1.2.0 core ships a broken numpy → `cannot import name 'git_revision'` crash-loop; 1.1.0 is wire-compatible with the 1.2.0 api/admin/fe), and **`PROXY_CONNECT_TIMEOUT=10s`** added to the fe service (its nginx template references the var; unset → `invalid directive` crash). Real engine activates when `CompreFace__ApiKey` is set (host-run backend → `BaseUrl=http://localhost:8000`); until then `DevFaceClient` stays active.
- **Admin-side enrollment:** `POST /api/v1/admin/face/enroll/:userId` (multipart 1..5 photos, AdminOnly) reuses `EnrollFaceCommand`; web Users drawer gains a photo **Upload** + Enroll button (`useAdminEnrollFace`) so an admin can enroll a user's face without the mobile app. Audit `face.enrolled` (actor = admin).
- **One manual step remains:** create a CompreFace account + Application + Recognition service in the :8000 UI and copy the API key into `appsettings.Development.json` (the public signup REST requires internal auth, so this stays a UI step).

## 2026-06-06 — M06 Face Verification Web + Mobile + new mobile theme (Sprint 04)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Web typecheck green, backend build green (0 errors). Mobile code-only (Flutter not on the Windows box → Mac runs `build_runner` + `flutter analyze`).

- **New mobile theme (ui-ux-pro-max guided):** replaced the basic seed-only theme with a modern **flat + soft-depth** system — vibrant **indigo** brand + **emerald** success accent. `core/theme/app_palette.dart` (design tokens + `AppSemantics` ThemeExtension for success/warning/info + brand gradient, light & dark), rewritten `app_theme.dart` (explicit `ColorScheme`, rounded 14–20 components, filled inputs, 52pt controls, navbar/sheet/snackbar themes, tuned `TextTheme`), and a reusable kit `core/widgets/brand_widgets.dart` (`GradientHero`, `IconBadge`, `StatusPill`, `SoftCard`, `FeatureTile`, `SectionLabel`). Home redesigned into a gradient-hero dashboard with a primary check-in card, face-enroll nudge, and a quick-access grid.
- **M06 mobile:** new `features/face/` (Freezed `FaceStatus`, `FaceApi`/`FaceRepository` + `faceStatusProvider`), 3-angle **face enrollment wizard** (`face_enrollment_screen.dart`, front-camera capture → `POST /face/enroll`, reused for re-enroll), home banner when not enrolled. Check-in selfie already feeds server-side verification (M05). ARB keys (vi default + en).
- **M06 web:** Users admin gains a **Face** status column (PG/Leader) + a Face section in the detail Drawer with **force re-enroll** and **remove template** actions (`useReEnrollFace` / `useRemoveFace` → admin endpoints). `AdminUserDto` extended with `faceEnrolled` + `faceEnrolledAt` (projected in `GetUsersQueryHandler`). next-intl messages (vi + en).
- **Deferred (unchanged):** enrolled-photo-vs-selfie side-by-side compare (CompreFace owns the embedding; no stored reference image).

## 2026-06-06 — M06 Face Verification BE (Sprint 04) — CompreFace (ADR-011)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ BE done — full suite **191 unit tests green** (+7 face). Web + mobile pending.

- **Engine:** `IFaceClient` port (Enroll/Verify/Delete, subject model) with `CompreFaceClient` (real, typed HttpClient → CompreFace Recognition REST: `faces?subject=`, `recognize`, `subjects/{id}`) gated by `CompreFace:ApiKey`; `DevFaceClient` deterministic fallback (no service needed for local/CI/tests). `CompreFaceOptions` (threshold 0.85).
- **M05 integration:** `FaceVerificationService` replaces the always-pass stub — resolves the user's enrolled subject + verifies the selfie; **unenrolled → PendingReview** (BR-206), engine unreachable → PendingReview (BR-207). M05 check-in unit tests unaffected (inject a fake port).
- **Use-cases/API:** `GET /api/v1/face/status`, `POST /face/enroll` (multipart, re-enroll replaces subject), `POST /face/verify`; admin `POST /admin/face/re-enroll/:userId` + `DELETE /admin/face/template/:userId`. `User.ClearFaceEnrollment()` + audit `face.enrolled` / `face.removed` (CR-1). No schema change (reuses `users.face_template_external_id` + `face_enrolled_at`).
- **Admin review:** continues to use the M05 status-driven queue (no `admin_reviews` table).
- **Deferred:** web face-status UI + admin re-enroll/remove actions; mobile 3-angle enrollment wizard + capture in check-in (code-only). Enrolled-photo-vs-selfie side-by-side compare deferred (CompreFace owns the embedding; would need a stored reference image column).

## 2026-06-06 — M05 Sprint 03 follow-up: real MinIO storage + photo-retention job

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ BE done — full suite **184 unit tests green** (+1 retention). MinIO container smoke-checked (healthy on :9000).

- **Real photo storage:** `MinioAttendancePhotoStorage` replaces the stub when `MinIO:Endpoint` is configured (DI picks MinIO vs local no-op fallback). `SaveAsync` ensures the bucket + `PutObject` and returns a stable **object key**; `IAttendancePhotoStorage` gained `GetUrlAsync` (short-lived presigned GET, default 1h) + `DeleteAsync`. Records persist keys; read paths (history, admin list, check-in/out responses) mint presigned preview URLs via `AttendanceQueries.PresignAsync`. `MinioOptions` bound from `MinIO` config section.
- **Retention (CR-4):** `IAttendancePhotoRetentionService` + `AttendancePhotoRetentionService` — daily Hangfire job `attendance-photo-retention` purges selfies/store photos older than 90 days (deletes MinIO objects + `AttendanceRecord.PurgePhotos()` clears URL columns; record kept for compliance). Registered in `Rmms.Worker`.
- **EXIF deferred to M13:** server-side EXIF GPS/timestamp verification + mobile EXIF write deferred to the photo-integrity pipeline (M13) — non-blocking audit value, and the mobile `image`-pkg EXIF writer is fragile to ship untested on the Windows box. Face Verification remains a stub → M06.
- Also added `seed-demo` CLI earlier same day (1 admin + 2 leaders + 5 PGs + 3 stores + assignments) and initialized the `rmms` DB on the new server `103.216.116.206` (PostGIS installed, all migrations applied, demo data seeded).

## 2026-06-06 — M05 Attendance core (Sprint 03; Face/MinIO deferred)

**By:** Tech lead (MotivesVN IT), AI-assisted · web + mobile UI via `ui-ux-pro-max`

**Status:** ✅ BE + web done (full suite **183 unit tests green** — +18 attendance; web type-check/lint/build green). Mobile is **code-only** (Flutter not on the Windows box — Mac verifies).

- **Scope:** check-in / check-out with GPS geofence (BR-204), fake-GPS block (BR-205), early-window (BR-202/AC-5) + late marking (BR-203/AC-6), the status state machine (§3.2), history, and the Admin list + review queue (AC-9). **Bound to M07 shifts** via `work_schedule_shift_id`. **Face Verification (M06) and photo storage (M13/MinIO) ship as stubs** this sprint per the Sprint 03 goal ("without Face Recognition yet").
- **Domain:** `AttendanceRecord` aggregate owns the `AttendanceStatus` state machine (valid / late / gps_violation_pending_review / face_fail_pending_review / fake_gps_blocked / admin_approved / admin_rejected). `CheckIn` factory derives status from validated facts (GPS distance wins over face fail); `CheckOut` (BR-206 face at both ends) escalates a clean record to review on a check-out anomaly; `ApproveReview`/`RejectReview` (BR-208/209, reason required). Geofence radius const = 300 m. `FaceVerificationResult` enum.
- **Abstractions (deferred providers):** `IFaceVerificationService` (+ `StubFaceVerificationService` → always Success 0.99, swap at M06) and `IAttendancePhotoStorage` (+ `LocalAttendancePhotoStorage` → deterministic `local://` placeholder URL, swap at M13). Wired in Infrastructure DI.
- **Use-cases/API (`api/v1/attendance`):** `GET today`, `GET check-in/info` (assigned stores + thresholds + today's shifts), `POST check-in` (multipart), `POST :id/check-out` (multipart), `GET history` (paginated). Admin `api/v1/admin/attendance`: `GET` (filter user/store/status/date) + `POST :id/review`. Validation order per M05 spec; VN-local (UTC+7, CR-5) shift-window math; fake-GPS audited and blocked without creating a valid record. Migration `M05_Attendance` applied to dev DB. Audit actions `attendance.*` (CR-1).
- **Web:** `/attendance` admin page (ProTable + filters + status Tag with **icon+text** not color-alone, late badge, tabular distance) + detail/review modal (approve/reject-with-reason) + photo slots (placeholder until M13). Nav item + i18n vi/en.
- **Mobile:** `features/attendance` (Freezed models, Dio multipart api, repo + `todayShiftsProvider`/`attendanceHistoryProvider`), `AttendanceTodayScreen` (today's shifts + check-in/out actions), `CheckInScreen` (geolocator GPS + `isMocked` fake-GPS flag, image_picker selfie + store photo, note), `AttendanceHistoryScreen`. Status chip widget (icon+label). Routes + home button + ARB vi/en. iOS `Info.plist` camera/photo/location usage strings + Android camera/location permissions added. **Awaiting Mac:** `pub get` → `build_runner` (gen `attendance.{freezed,g}.dart`) → `gen-l10n` → `analyze`.
- **Deferred within M05:** real Face match (M06), MinIO photo upload + EXIF GPS/timestamp verification (M13), SignalR team-monitoring push (M16), `admin_reviews` table (status-driven queue used instead).

## 2026-06-06 — M07 Work Schedule (built ahead of M05; sprint reorder)

**By:** Tech lead (MotivesVN IT), AI-assisted · web + mobile UI via `ui-ux-pro-max`

**Status:** ✅ BE + web done (full suite **165 unit tests green**, web type-check/lint/build green). Mobile is **code-only** (Flutter not on the Windows box — Mac verifies).

- **Why before M05:** attendance AC-5 (early ≤60′) / AC-6 (late >5′) need **shift start times**, and `attendance_records.work_schedule_shift_id` FKs to `work_schedule_shifts` (M07). User chose to build M07 fully first rather than a throwaway minimal-shift model. M07 was originally Sprint 05.
- **Domain:** `WorkSchedule` aggregate (UserId, ScheduleDate, Status, Version, PreviousVersionId, Submitted/Approved/RejectReason) + `WorkScheduleShift` modeled as an **EF owned collection** (clean wholesale replace on edit across providers). `WorkScheduleStatus` = pending/approved/rejected/edit_pending/superseded. Overlap + future-date invariants in the aggregate; `ReplaceShifts` reconciles in place (UPDATE, not delete+insert).
- **Versioning (BR-307/BR-308):** editing an approved schedule creates a new `edit_pending` row (previous_version_id → old) while the **old stays approved/effective**; on approval the old is `superseded`. The "one approved per user/day" invariant is enforced in the approve handler (supersede-then-approve) — **not** a partial-unique index (which would trip mid-transition). AC-15 covered by tests.
- **Use-cases/API (`api/v1/schedule`):** `GET me`, `POST me` (day/week/month — client expands to days, all-or-nothing), `POST :id/submit`, `PATCH :id` (edit/version), `DELETE :id` (withdraw), `GET user/:id` + `POST :id/{approve,reject}` (AdminOrLeader, **Leader-scoped** to managed PGs, BR-404 reason required). Store-assignment + past-date validation. Migration `M07_WorkSchedule` applied to dev DB.
- **Web:** `/schedules` admin overview — pick PG/Leader + date range, view each day's shifts + status, approve / reject-with-reason modal. Nav item + i18n vi/en.
- **Mobile:** `features/schedule` (Freezed models, Dio api, repo + `myScheduleProvider`), `MyScheduleScreen` (30-day list, status chips, submit/withdraw), `RegisterScheduleScreen` (day/week/month modes + multi-shift editor from assigned stores). Routes + home button + ARB vi/en. `initializeDateFormatting()` added in `main.dart`. **Server still owns approval routing/email (BUH link) → M09**; full month-grid calendar deferred (list view; no calendar lib without ADR).

---

## 2026-06-06 — Sprint 02 deferred items cleared (store map + mobile FCM)

**By:** Tech lead (MotivesVN IT), AI-assisted · UI via `ui-ux-pro-max`

**Status:** ✅ Both Sprint 02 deferred items delivered. Web type-check + prod build green; mobile is code-only (Flutter not on the Windows box — Mac verifies).

- **Store map view (web)** — [ADR-010](./decisions/ADR-010-store-map-react-leaflet.md) accepts **react-leaflet 4 + OpenStreetMap** (Google Maps / Mapbox rejected on cost + key/over-engineering). `/stores` gains a `Segmented` table/map toggle; `StoreMap.tsx` plots status-colored `divIcon` markers (active=green, inactive=gray) with code/name/status/address/area/GPS popups, fit-bounds, and reduced-motion-aware animation; `StoreMapView.tsx` adds area/status/search filters + legend + empty/error states. Leaflet lazy-loaded via `next/dynamic` (`ssr:false`) → not in initial bundle. New deps: `leaflet`, `react-leaflet`, `@types/leaflet`. i18n keys added (vi/en). (M03, satisfies the Sprint-02 "store management with map" DoD.)
- **Mobile device-change push (client-side)** — new `core/notifications/`: fail-safe `FcmService` (guarded `Firebase.initializeApp`, permission, `getToken`, token-refresh, foreground/opened/initial streams — all no-ops when Firebase unconfigured so the app still launches); `FcmCoordinator` wires streams → `deviceApprovalProvider` + `inAppNotificationProvider`; root `app.dart` shows a foreground `MaterialBanner` (info/success/warning) via `ScaffoldMessenger`; `device_pending_screen` reacts live to a `device_changed` push (approved → "sign in again" CTA, rejected → contact-admin state). FCM token is now actually sent on login (`auth_repository` resolves it from `FcmService` → `auth_api.login(fcmToken:)`). Background handler registered in `main.dart` (guarded). ARB keys added (vi/en). **Server-side push *delivery* stays →M14 Notification.** To emit real tokens the Mac build must run `flutterfire configure` (generates native Firebase config); until then FCM is gracefully disabled. (BR-105/BR-106.)

---

## 2026-06-05 — Sprint 02 CLOSED

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Closed — core goal + AC-2 + AC-3 delivered (M02 + M03 across BE/web/mobile; 149 unit tests green).

- **Done:** Admin CRUD (users/stores/areas/categories), assignment management (+ web panel), single-device enforcement (AC-2), device-change request flow end-to-end incl. Leader-scoped approval (AC-3, BR-106).
- **Deferred (tracked, non-blocking):**
  - Store **map view** (web) — list/CRUD/lat-lon shipped; interactive map needs a map library → **new ADR required** (no new UI lib without ADR).
  - Mobile device-change **push** handling → **M14 Notification** (FCM infra); pending-approval screen already shipped; FCM token captured on login.
  - **CSV bulk assignment** → Phase 2 (spec-marked).
- **Next:** Sprint 03 → **M05 Attendance core** (attendance_records + state machine + history + Admin list; Face/M06, MinIO photos, and shift-binding/M07 deferred within S3).

---

## 2026-06-05 — M03 close-out: macOS mobile verify + tooling/process

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Mobile app verified on macOS dev box (analyze clean, app runs). All changes on `origin/main`.

- **Mobile verified on macOS** — `flutter pub get` → `build_runner` (Freezed/json) → `gen-l10n` → `flutter analyze --fatal-infos --fatal-warnings` clean → app runs; M03 "My assignments" screen (stores + leader) works against `/users/me/*`.
- **Analyzer cross-toolchain fix** — `prefer_initializing_formals` ignored in `mobile/analysis_options.yaml` (newer macOS analyzer flagged pre-existing Sprint-01 constructors; pinned CI Flutter 3.44 did not). M03 code already uses initializing formals. (`5bf9d11`)
- **M03 test-data seed** — `backend/scripts/seed-m03-testdata.sql`: idempotent areas/stores/categories + assignments that bind to any existing PG/Leader; for spinning up demo data on a fresh DB (e.g. macOS box). (`b9efc5e`)
- **UI/UX skill mandate extended to Mobile** — `ui-ux-pro-max` now MANDATORY for Flutter UI work too (was web-only), with App-UI rules (touch targets, safe areas, press feedback, contrast) and stack guardrails (Flutter + Riverpod + Material 3 + existing theme + ARB l10n, no new UI libs w/o ADR). Synced into both `CLAUDE.md` (`7bb4c0b`) and `knowledge-base/prompts/system-prompt.md` (`dc9f5d5`).

---

## 2026-06-05 — Sprint 02 M03: Organization & Assignment (BE + web masters)

**By:** Tech lead (MotivesVN IT), AI-assisted · web designed via `ui-ux-pro-max`

**Status:** ✅ Backend builds clean (0 errors) + runtime smoke test pass (login → create area/store, assign pg-leader/user-store 204, GetUserAssignments OK). Web type-check + lint (0 warnings) + production build all green.

- **Domain (M03)** — 6 aggregates: `Area` (self-ref hierarchy), `Category`, `Store` (GPS + `StoreStatus` enum), `UserLeaderAssignment` (1:1 active, effective-dated), `UserStoreAssignment` (1:N active), `UserCategoryAssignment`. Mirrors M01/M02 pattern (`AuditableEntity` + `IAggregateRoot`, factory methods, invariants).
- **Migration `M03_Organization_Assignment`** — 6 tables + indexes; partial unique indexes enforce *one active Leader per PG* (`effective_to IS NULL`) and *one active store link per user*; unique `code` per master `WHERE deleted_at IS NULL`. Applied to dev DB.
- **Backend CRUD** — `/admin/stores` (list paginated + area/status/search filter, create/update/status/delete), `/admin/areas`, `/admin/categories` (list+CRUD). All `AdminOnly`, soft-delete (ADR-004), audit (CR-1), FluentValidation, code-keyed vi/en errors.
- **Assignments** — `POST /admin/assignments/pg-leader` (re-assign ends previous active), `user-store` (assign + `DELETE` unassign), `user-category` (assign + `DELETE` unassign), `GET /admin/assignments/user/{id}` (leader + stores + categories).
- **New error codes** — `CODE_ALREADY_EXISTS`, `INVALID_REFERENCE`, `INVALID_ASSIGNMENT`, `ASSIGNMENT_EXISTS` (+ vi/en catalog). New `AuditAction` constants for store/area/category/assignment.
- **Web admin pages** — Stores (ProTable code/name/area/status/GPS, lat/lon `ProFormDigit`, activate/deactivate + delete confirmations), Areas (CRUD + optional parent), Categories (CRUD). Admin nav extended: Users · Stores · Areas · Categories · Devices. i18n vi+en.
- **Web assignment panel** — `UserAssignmentsPanel` in the Users detail Drawer: PG→Leader `Select` (1:1), Store/Category closable Tags + add `Select` (1:N), empty states, localized errors, TanStack invalidation per user.
- **Unit tests** — 35 M03 handler tests (xunit + FluentAssertions + EF InMemory); full suite **106 → 141 green**, 0 regressions. Covers create/dup/unknown-ref/notfound/reassign/idempotent/filter paths + GetUserAssignments.

- **Mobile read endpoints** — `GET /users/me/stores` + `/users/me/leader` (AnyAuthenticated, identity from JWT, active assignments only). Flutter `organization` feature: Freezed `AssignedStore`/`AssignedLeader`, Dio api + repository, Riverpod providers, `MyAssignmentsScreen` (+ go_router route + Home button), vi/en ARB keys. (Mobile code-complete, pending macOS `build_runner` + `gen-l10n` + verify.)
- **Leader-scoped device approval (BR-106)** — M02 endpoints `/devices/{pending,approve,reject}` switched AdminOnly → **AdminOrLeader**: Admins act on all; Leaders scoped to PGs they actively manage (`user_leader_assignments`). Non-managing Leader → `403 NOT_APPROVER` (no state change). New `AdminOrLeader` policy; `NOT_APPROVER` vi/en catalog entry.
- **Tests** — total **149 green** (+8 since masters: 4 Me + 4 leader-scoping; device tests updated for new command signatures).

> **M03 CODE-COMPLETE.** Commits: `d93e172` (BE masters), `79464ab` (web masters), `3c1c3b4` (assignment panel), `2645155` (unit tests), `64aac19` (mobile endpoints BE+Flutter), `5e65aac` (Leader-scoped approval), `efa3e23` (dev port 3010). **Next:** macOS mobile verify; then M04 work-schedules.

---

## 2026-05-31 — Sprint 02 M02: Device Requests admin page (web)

**By:** Tech lead (MotivesVN IT), AI-assisted · designed via `ui-ux-pro-max`

**Status:** ✅ Web type-check + lint (0 warnings) + build green; vitest 8/8.

- **`(admin)/devices` page** — AntD ProTable listing `GET /api/v1/devices/pending` (columns: email, full name, role Tag, new device, OS, requested-at; locale-formatted date). Row actions: **Approve** (Popconfirm, primary affordance) → `POST /devices/:id/approve`; **Reject** (danger link → `ModalForm` requiring a reason, max 500, show-count) → `POST /devices/:id/reject`. Friendly **empty state** when no requests. Errors localized via `errorCodeFromUnknown`.
- **`features/devices/`** — `fetchPendingDevices` (single-envelope unwrap) + TanStack `useApproveDevice` / `useRejectDevice`.
- **Admin nav** — added a top `Menu` to `(admin)/layout.tsx` with Users + Devices links (active-route highlight via `usePathname`), so the two admin areas are discoverable.
- **i18n** — `admin.navUsers/navDevices` + `devices.*` keys in `messages/{vi,en}.json`.

> Applied `ui-ux-pro-max` guidance: approve = primary confirm, reject = destructive (danger) + reason modal, color+text role tags, helpful empty state, loading-safe async actions.

---

## 2026-05-31 — Sprint 02 M02: device-change approval flow (BE)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Backend solution builds clean + 106 unit tests green (+10 device tests). AC-3 integration test compiles (runs on CI).

Implements the BR-106 device-change approval flow on top of the Sprint 01 foundation (`user_devices` + pending rows + `UserDevice.Approve/Reject/MarkReplaced`):

- **Endpoints** (`DevicesController`): `GET /api/v1/devices/me` (caller's active + pending device), `GET /api/v1/devices/pending` (Admin — requests awaiting approval), `POST /api/v1/devices/{id}/approve`, `POST /api/v1/devices/{id}/reject` (reason required).
- **Approve** (`ApproveDeviceCommandHandler`): the requested device → Active; the user's current Active device → Replaced and its refresh tokens revoked (old app logs out on next refresh, per BR-106); audit `device.approved`. Guards: not-found → 404, non-pending → 409 `APPROVAL_NOT_PENDING`.
- **Reject** (`RejectDeviceCommandHandler` + validator): pending → Rejected; audit `device.rejected` with reason; empty reason → 422 `REJECT_REASON_REQUIRED`.
- **Queries**: `GetPendingDevicesQuery` (pending list + owner email/name/role), `GetMyDeviceQuery` (active + pending summary for the caller).
- **Tests**: `DeviceApprovalHandlerTests` (approve replaces+revokes+audits, not-found, non-pending, reject, reject-validator), `DeviceQueryHandlerTests` (pending list, my-device active+pending, empty); `DeviceApprovalTests` integration (login device-1 ok → device-2 403 → admin approves → device-2 logs in).

> **Scope note:** approval endpoints are `AdminOnly` this slice — fully satisfies AC-3 via the Admin path. **Leader-scoped approval** (a Leader approving only their assigned PGs) + the Leader mobile/web UI are enabled once M03 `user_leader_assignments` ships; switch the policy to Leader+Admin and add the assignment filter then. FCM push + email notification on new device requests also remain (M02 carry-forward).

---

## 2026-05-30 — Sprint 01 closed (CODE-COMPLETE) — Day 10 skipped

**By:** Tech lead (MotivesVN IT), AI-assisted

**Decision:** Sprint 01 (M01 Auth & Devices) is declared **code-complete** after Day 1–9. **Day 10 is skipped** — its items are manual/operational (UAT on physical Android/iOS devices, formal AC-1/AC-2 sign-off walkthrough, stakeholder demo, Swagger/OpenAPI export review) and are not blocking development. We move on to Sprint 02.

- **Delivered (Day 1–9):** EF migration (M01+M02 foundation), 10 auth endpoints + `/auth/me/device-status`, admin user CRUD + seed CLI, JWT issue/rotate/reuse-detection, BR-105 device check **PG-only**, authz policies, `X-Idempotency-Key` middleware, login rate-limit, Hangfire token-cleanup job, error localization (vi/en) + Serilog enrichment + append-only audit (CR-1); **Web Admin** login + user management (list/create/edit/detail/reset) + client route guard + single-flight 401 refresh; **Mobile** full auth surface (register→verify→login→forgot/reset→device-pending) with `rmms://` deep links + auto-refresh.
- **Quality:** 96 BE unit tests + integration tests (CI) + web vitest 8/8 + mobile widget tests; coverage gate met (Domain 72.6% / Application 90.7%, generated Mediator code excluded); 3 CI workflows green.
- **⚠️ Deferred with Day 10 (NOT performed):** manual UAT on real devices, AC-1/AC-2 formal acceptance sign-off, live stakeholder demo, OpenAPI export review. Mobile FE still pending on-device verification on macOS (Flutter not installed on the Windows dev box). Carry these into Sprint 02 acceptance if formal sign-off is required.

---

## 2026-05-30 — Sprint 01 Day 9: tests + hardening (coverage gate met)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Backend **96 unit tests** green. ✅ Web **vitest 8/8** + type-check + lint + build green. ✅ Coverage gate ≥70% met (generated code excluded): Domain 72.6%, Application 90.7%, combined 81.2%.

### Backend unit tests (now 96, was 54)
- **`LoginCommandHandlerTests`** (9) — BR-105 device check: first device → auto-active, same device → reuse, different device → 403 `DEVICE_NOT_AUTHORIZED` + pending row, non-PG device-less login, PG-without-device rejected; status/credential gates (wrong password, unknown email, pending-verify, inactive).
- **`RegisterUserCommandHandlerTests`**, **`VerifyEmailCommandHandlerTests`**, **`LogoutCommandHandlerTests`**, **`GetMeQueryHandlerTests`** — previously only integration-covered handlers.
- **`CommandValidatorTests`** (21) — all 9 M01 FluentValidation validators (Register/Login/Reset/Forgot/Logout/Refresh/VerifyEmail/CreateAdminUser/UpdateUser): valid + key invalid paths (weak password, bad email, bad OS, invalid role/status/language).

### Web unit tests (vitest, new)
- `vitest.config.ts` (happy-dom, `@`→`src` alias, automatic JSX runtime) + `vitest.setup.ts` (jest-dom).
- `auth-error.test.ts` (envelope code, NETWORK_ERROR, INTERNAL_ERROR fallbacks), `login.test.tsx` (useLoginMutation persists tokens + maps `userId→id`; no store write on failure), `users/api.test.ts` (fetchUsers unwraps the double `data` envelope; filter forwarding).

### Coverage
- `coverlet.runsettings` excludes the source-generated Mediator dispatch/wrapper code (~1.3k lines emitted into `Rmms.Application`) + `[GeneratedCode]`/`[CompilerGenerated]` so the number reflects first-party logic. Run: `dotnet test ... --collect:"XPlat Code Coverage" --settings coverlet.runsettings`. **Result: Domain 72.6% · Application 90.7% · combined 81.2%** (≥70% gate). Without the exclusion the raw Application number is ~57% purely due to the generated Mediator plumbing that unit tests bypass by design.

> Mobile widget tests (login + register) were delivered in the Day 3–9 mobile work; this entry covers the BE + web Day 9 items.

---

## 2026-05-30 — Sprint 01 Day 8: error localization (vi/en) + Serilog enrichment + audit verification

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Backend solution builds clean + 54 unit tests + 5 catalog tests green (catalog runs locally, no container). Localization + audit integration tests compile and run on CI.

- **Error message localization (vi/en)** — `IErrorMessageLocalizer` / `ErrorMessageCatalog` (code-keyed vi/en catalog) + `ErrorLocalizationFilter` (global `IActionFilter`) localize the `ErrorEnvelope.Error.Message` by error code for the active request culture (`Accept-Language` → `UseRequestLocalization`, default vi). Handlers keep one default (vi) message as a fallback; no per-controller changes. **Design note:** we key by `ErrorCodes` in a static catalog rather than authoring `.resx` XML — deterministic, unit-testable, and avoids resx path-convention pitfalls; same outcome the plan intended. Unknown codes pass through untouched.
- **Serilog scope enrichment** — `RequestEnrichmentMiddleware` (after auth) pushes `TraceId` / `UserId` / `DeviceId` / `Role` into the Serilog `LogContext` so every handler log line carries them; the `UseSerilogRequestLogging` completion log is enriched via `EnrichDiagnosticContext` with the same properties (resolved from `ICurrentUser`).
- **Audit verification (CR-1)** — `AuthFlowTests.AuthFlow_EmitsCr1AuditEntries` runs register→verify→login then queries `audit_log` by the user's id and asserts `user.registered` + `user.email_verified` + `auth.login_success` are present (complements existing per-handler `InMemoryAuditLogger` unit assertions).
- **Tests** — `ErrorMessageCatalogTests` (vi default, en, regional culture → base, unknown code → null, unknown culture → vi); `Login_WrongCredentials_LocalizesMessage_ByAcceptLanguage` (en vs vi message via header, CI).
- **i18n status** — mobile ARB (vi/en) for all auth screens and web next-intl strings (auth + admin + users + errors namespaces, vi/en) were already shipped in the Day 3–7 work, satisfying the Day 8 FE i18n items.

---

## 2026-05-30 — Sprint 01 Day 7 follow-up: close gaps (user detail drawer, refresh-reuse integration test, web 401 refresh)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Web `type-check` + `lint` + `build` green. ✅ Backend solution builds clean + 54 unit tests green; the new integration test compiles (runs on CI Linux — Testcontainers host resolution fails on the Windows/Docker dev box).

- **FE-W user detail drawer** (was the missing "user detail page" from Day 7) — right-side AntD `Drawer` (520px) opened from a "Details" row action; `Descriptions` (bordered) for read-only fields (email copyable, full name, phone, role Tag, status, language, email-verified, last-login, created, updated; dates locale-formatted) + divider-separated status toggle (`Switch`) and danger reset-password (`Popconfirm`). Designed via the `ui-ux-pro-max` skill (Descriptions for key-value data, color+text status/role, destructive action separated + confirmed).
- **BE refresh-reuse integration test** (was unit-only on Day 7) — `AuthFlowTests.RefreshTokenReuse_RevokesAllSessions_Returns401`: register→verify→login→rotate (A→B)→replay A ⇒ 401 `REFRESH_TOKEN_REUSED`, and the freshly issued B is also dead (reuse nukes all sessions). Adds `ReadErrorCode` helper.
- **Web 401 refresh (bug fix)** — the admin users page surfaced a generic `INTERNAL_ERROR` toast with an empty table once the 15-min access token expired: an expired JWT yields a **401 with an empty body** (`WWW-Authenticate: Bearer error="invalid_token"`, no error envelope), so `errorCodeFromUnknown` fell back to `INTERNAL_ERROR`. `lib/api/client.ts` now refreshes the token on 401 (single-flight `/auth/refresh`, replay once; on failure clear store + redirect to `/{locale}/login`). This was a real gap — the web client previously had only a TODO for 401.

> Day 7 is now complete (all plan lines implemented). Verified locally where the env allows (web build; backend build + unit tests); the reuse integration test is green on CI.

---

## 2026-05-30 — Sprint 01 Day 7: Web Admin user management + route guard (FE-W) + refresh-reuse tests (BE)

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Web `type-check` + `lint` + `next build` green (`/vi/users` + `/en/users` generated). ✅ Backend 54 unit tests green (+3 refresh-token tests).

### Web (Next.js) — `FE-W-D7` user management

- **User list** — `(admin)/users/page.tsx` uses AntD **ProTable** with server-side pagination + filters (role / status selects + email/name keyword) wired to `GET /api/v1/admin/users`. Role rendered as colored `Tag`; status as ProTable badge enum.
- **Create user** — toolbar `ModalForm` (email / full name / phone / role∈{leader,buh,admin} / language) → `POST /admin/users`; success toast notes the initial password is emailed (Console sink in dev).
- **Edit + status toggle** — per-row `ModalForm` (full name / phone / status active↔inactive / language) → `PATCH /admin/users/:id`.
- **Reset password** — per-row `Popconfirm` → `POST /admin/users/:id/reset-password`.
- **API hooks** — `features/users/api.ts`: `fetchUsers` (drives ProTable.request) + TanStack `useCreateUser` / `useUpdateUser` / `useResetUserPassword` (invalidate on success). Errors localized via the shared `errorCodeFromUnknown` + `errors.*` keys.
- **Route guard** — `(admin)/layout.tsx` is a client-side guard + shell (header with current email + logout). The JWT lives in the Zustand store (localStorage), which the Next.js middleware can't read, so the guard runs on the client: unauthenticated visitors are redirected to `/{locale}/login`; a `hydrated` flag prevents an SSR/client mismatch and premature redirect. Login now redirects to `/{locale}/users`.
- **i18n** — added `admin.*` + `users.*` keys to `messages/vi.json` + `messages/en.json` (roles, statuses, actions, toasts).

### Backend — `BE-D7` refresh-token reuse detection tests

- `RefreshTokenCommandHandlerTests`: reused (revoked) token → all active tokens revoked + `auth.refresh_reused` audit + `REFRESH_TOKEN_REUSED`; valid token → rotates (old revoked + `ReplacedByTokenId` linked, new active) + `auth.refresh_rotated`; unknown token → `REFRESH_TOKEN_REVOKED`.

> **Note on the route guard vs. sprint plan:** the Day 7 plan said "middleware checks JWT". Because tokens are stored client-side (Zustand/localStorage) rather than in an httpOnly cookie, a server middleware can't see them — the guard is implemented client-side instead. Revisit if/when we move tokens to httpOnly cookies (would also need a CSRF strategy).

---

## 2026-05-30 — Sprint 01 Day 6 (BE): Hangfire token-cleanup job + `/auth/me/device-status` skeleton

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Backend builds clean (Worker + Api), 51 unit tests green (+6: token cleanup + device-status). Recurring job verified to register; run against the dev DB by the Worker.

- **Token cleanup job** — `ITokenCleanupService` / `TokenCleanupService` (Application/Maintenance) hard-deletes (a) email-verification + (b) password-reset tokens that are used or past their 24h TTL, and (c) refresh tokens past their 30d expiry. **Revoked-but-unexpired** refresh rows are intentionally KEPT so reuse detection still works inside their window. Registered as a Hangfire **hourly** recurring job (`auth-token-cleanup`, `low` queue) in `Rmms.Worker` via `IRecurringJobManager` (avoids `JobStorage.Current` init-timing pitfalls). Tokens are plain `Entity` (not soft-deleted), so `RemoveRange` truly deletes; load-then-remove is fine at the 24h/30d hourly cadence (switch to `ExecuteDelete` only if volumes grow).
- **`GET /auth/me/device-status`** — `GetDeviceStatusQuery`/Handler/`DeviceStatusDto` (placed in `Auth.Me` to avoid colliding with the `DeviceStatus` enum namespace). Authenticated read returning the snake_case status of the device bound to the access token: `active` (PG), `none` (web/`Guid.Empty` token), or `unknown` (missing row). The pending-device polling variant (callable by a not-yet-approved device) + push/in-app notification stays **Sprint 02** with the approval workflow.
- **Tests** — `TokenCleanupServiceTests` (deletes used/expired, keeps live + revoked-unexpired; zero-case) + `GetDeviceStatusQueryHandlerTests` (web→none, active device→active, unknown).

---

## 2026-05-30 — Sprint 01 W1 close-out: device check scoped to PG (BR-105) + Web Admin login wired

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Backend builds clean (warnings-as-errors) + 45 unit tests green. ✅ Web `type-check` + `lint` + `next build` green. ⏳ Backend integration tests not re-run locally (Testcontainers host-resolution issue on the Windows/Docker Desktop dev box — fails in `RmmsApiFactory.InitializeAsync` before any test logic; unaffected on the Linux CI runner). This closes the last open **Week 1 (Day 1–5)** item, `FE-W-D5`.

### Backend — `/auth/login` device check is now PG-only (correct per BR-105)

- **Problem:** the login contract forced a `device` object on *every* login and ran the single-active-device check for *all* roles. BR-105/BR-106 are explicitly **PG-scoped**, and the OS validator only accepts `ios`/`android` — so Leader/BUH/Admin (web) could not authenticate. The demo script (#7) requires an Admin to log into Web Admin with no device ceremony.
- **Change:** `device` is now **optional** at the transport + command level (`LoginRequest.Device` / `LoginCommand.Device` nullable). In `LoginCommandHandler`, the BR-105 device resolution runs **only when `user.Role == Pg`** (a PG with no device → `403 DEVICE_NOT_AUTHORIZED`). Non-PG users skip device entirely; their tokens carry `device_id = Guid.Empty` and create no `user_devices` row. `LoginHistory.RecordSuccess` now takes a nullable `deviceId`; `GetMeQueryHandler` treats `Guid.Empty` as "no device".
- **No schema change** — `refresh_tokens.device_id` / `login_history.device_id` have no FK to `user_devices`, so the empty/null sentinel is safe.
- **Tests** — `AdminAuthorizationTests.WebUser_LogsInWithoutDevice_AndCanCallApi` (web admin logs in device-less → `/me` + `/admin/users` 200); `AuthFlowTests` now asserts a verified PG is rejected (403) when logging in without device. New `ApiHelpers.LoginNoDeviceAsync` helper.

### Web (Next.js) — `FE-W-D5` Admin login screen wired

- `useLoginMutation` calls `POST /api/v1/auth/login` with **no device payload**, maps the response `user.userId → AuthUser.id`, and persists tokens + user to the Zustand store (`persist` → localStorage, survives reload).
- `(auth)/login/page.tsx` wired to the mutation: submit → on success `router.replace('/{locale}')`; on failure surfaces a localized AntD `message.error`. Button shows a loading state; the form is disabled while pending.
- `errorCodeFromUnknown` extracts the backend `{ error: { code } }` envelope (no-response → `NETWORK_ERROR`); added `errors.*` keys (`EMAIL_NOT_VERIFIED`, `ACCOUNT_INACTIVE`, `ACCOUNT_LOCKED`, `DEVICE_NOT_AUTHORIZED` → "PG must use the mobile app", `RATE_LIMIT_EXCEEDED`, `PERMISSION_DENIED`, `NETWORK_ERROR`) to both `messages/vi.json` + `messages/en.json`.

> Week 1 (Day 1–5) is now functionally complete across BE + mobile FE + web FE. Remaining Sprint 01 work is **Week 2**: BE Day 6 (Hangfire token-cleanup job + `/auth/me/device-status` skeleton), FE-W Day 7 (user-management UI + route guard), Day 8 i18n/resx + Serilog enrichment, Day 9 tests, Day 10 UAT/demo.

---

## 2026-05-30 — Sprint 01 Day 5–6 (mobile FE): M01 auth flow wired end-to-end on Flutter

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ⏳ Code complete, awaiting verification on macOS (Flutter not installed on the Windows dev box). Needs `flutter pub get` → `build_runner build` → `dart format .` → `flutter analyze` → `flutter test`.

- **Login flow** — `LoginScreen` now calls `POST /auth/login` via `AuthRepository`, persists the issued access/refresh tokens in `flutter_secure_storage`, and routes on the resulting auth state. `DEVICE_NOT_AUTHORIZED` (BR-105) transitions to a dedicated **device-pending screen** instead of surfacing as a form error.
- **Session state + router guard** — `AuthController` (`Notifier<AuthState>`: unknown/unauthenticated/authenticated/deviceNotAuthorized) drives a `GoRouter` `redirect` via a `ValueNotifier` `refreshListenable`; silent session restore on launch calls `/auth/me`.
- **Device fingerprint (R-7)** — install-scoped UUID v4 generated once and stored in secure storage (survives updates, wiped on uninstall). `DeviceInfoService` is the single source for both the `X-Device-Id` header and the `/auth/login` body, keeping them consistent.
- **Auto-refresh interceptor** — `AuthInterceptor` rotates the refresh token once on 401 and replays the original request through a token-less Dio; `TokenRefresher` uses a single-flight guard so concurrent 401s trigger exactly one `/auth/refresh` (R-3, avoids reuse-detection revoke storms).
- **Forgot / Reset password screens** — `forgot-password` (neutral confirmation) + `reset-password` (token from `rmms://reset-password?token=` deep-link query, with manual-code fallback per R-4). Mutation calls send a per-request `X-Idempotency-Key`.
- **Register + Email-verify (Day 3–4 FE)** — `RegisterScreen` (BR-101 PG self-register, client-side password rule ≥8 + 1 letter + 1 digit mirroring the server) → routes to `VerifyEmailScreen`, which auto-verifies a deep-link token or accepts a manually pasted code.
- **Deep links wired at OS level** — added `app_links` dep + `rmms` custom scheme to `AndroidManifest.xml` (intent-filter) and iOS `Info.plist` (`CFBundleURLTypes`). `DeepLinkService` routes `rmms://verify-email?token=` and `rmms://reset-password?token=` to the right screen. https Universal/App Links remain deferred to Sprint 02 (R-4).
- **Errors + i18n** — `ApiException.fromDio` decodes the `{ error: {...} }` envelope to a typed code; `authErrorText` maps codes (incl. `EMAIL_ALREADY_REGISTERED`, `PASSWORD_TOO_WEAK`, `EMAIL_TOKEN_EXPIRED/USED`) to bilingual copy. All new strings added to `app_vi.arb` + `app_en.arb`.
- **Tests** — `auth_logic_test.dart` (envelope parsing, role mapping, UUID v4) + `login_screen_test.dart` + `register_screen_test.dart` (validation gating + repository call, mocktail).

> This completes the **M01 mobile FE surface for Sprint 01** (register → verify → login → device-pending → forgot/reset, with deep-link + auto-refresh). Pure-Dart backend is unchanged. Native Universal/App Links and the device-approval polling UI remain Sprint 02.

---

## 2026-06-01 — Sprint 01 Day 5: Authorization policies + middleware hardening + /auth/me + integration tests

**By:** Tech lead (MotivesVN IT), AI-assisted

**Status:** ✅ Built clean (warnings-as-errors) + `dotnet test` green, including Testcontainers integration tests (PostGIS + Redis).

- **Authorization policies** — `AuthorizationPolicies` (`PgOnly`/`LeaderOnly`/`BuhOnly`/`AdminOnly`/`PgOrLeader`/`AnyAuthenticated`) via `AddRmmsAuthorization()`. `AdminUsersController` now uses `[Authorize(Policy = AdminOnly)]`.
- **Idempotency** — `IdempotencyMiddleware` (Redis): caches 2xx responses 24h keyed by user+method+path+`X-Idempotency-Key`, replays on repeat, `409 IDEMPOTENCY_KEY_REUSED` on concurrent duplicate, fails open.
- **Login rate limit** — `ILoginRateLimiter` + `RedisLoginRateLimiter`: 5 failed logins / 15 min per (email+IP) → `429 RATE_LIMIT_EXCEEDED`; resets on success.
- **`GET /auth/me`** — `GetMeQuery`/`GetMeQueryHandler`/`MeDto`: current user profile + active device, identity sourced from JWT claims only.
- **Integration tests** — `RmmsApiFactory` (Testcontainers PostGIS + Redis, applies migrations, capturing email sender) + `AuthFlowTests` (full happy path) + `AdminAuthorizationTests` (admin 200 / leader 403 / no-token 401 — regression for the MapInboundClaims auth bug).

**Build/test notes:** resolved analyzer-as-error issues (`CA1822`/`ASP0026`/`CA1859`/`CA1861`); integration fixture needs `Microsoft.AspNetCore.TestHost` and a non-`Collection`-suffixed collection class (`CA1711`). Discovered that `Program.cs` reads `Jwt:SigningKey` eagerly, so integration tests must rely on the `appsettings.json` key rather than overriding it (otherwise issuance/validation key mismatch → 401).

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
