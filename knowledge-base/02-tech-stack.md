# 02 — Tech Stack

## Quick Reference

> **Note (2026-05-24):** Backend platform was changed from .NET 8 to **.NET 10 LTS** — see [`decisions/ADR-009-dotnet-10-lts.md`](decisions/ADR-009-dotnet-10-lts.md).

| Layer | Technology | Version | Purpose |
|---|---|---|---|
| Backend | .NET | **10.0 (LTS)** | Web API |
| Backend ORM | EF Core | 10.x | Database access |
| Database | PostgreSQL | 16.x | Primary data store |
| Cache/Queue | Redis | 7.x | Cache, Hangfire queue, session |
| Background jobs | Hangfire | 1.8.x | Scheduled jobs, notifications, deadline checks |
| Real-time | SignalR | (built-in) | Live PG Online status |
| Web framework | Next.js | 14.x (App Router) | Admin & BUH web |
| Web language | TypeScript | 5.x | Type safety |
| Web UI library | Ant Design Pro | 5.x | Admin dashboard components |
| Web state (server) | TanStack Query | 5.x | Data fetching, caching |
| Web state (client) | Zustand | 4.x | Local UI state |
| Web forms | React Hook Form + Zod | latest | Form validation |
| Mobile framework | Flutter | 3.x | iOS + Android |
| Mobile state | Riverpod | 2.x | State management |
| Mobile HTTP | Dio + Retrofit | latest | API client |
| Mobile storage | Hive | 2.x | Offline drafts, local cache |
| Auth | Custom JWT | — | Access + Refresh tokens |
| Face Recognition | FPT.AI Face | API | Identity verification |
| Push notification | Firebase Cloud Messaging | — | iOS + Android push |
| Email | SendGrid (start), AWS SES (scale) | — | Transactional email |
| File storage | MinIO (self-host) → S3 | — | Photos, documents |
| Container | Docker + Compose | — | Deployment |
| Reverse proxy | Caddy | 2.x | HTTPS, auto SSL |
| CI/CD | GitHub Actions | — | Build, test, deploy |
| Monitoring | Sentry + Uptime Kuma | — | Errors + uptime |
| Logging | Serilog | — | Structured logs |
| OS | Ubuntu | 22.04 LTS | VPS OS |
| VPS provider | Vultr (Singapore) | — | Production + Staging |

## Backend Stack — Detailed

### Why .NET 10 LTS? (revised 2026-05-24 — see ADR-009)
- **LTS support until November 2028** — covers all of Phase 1 + ~1 year of post-launch operations with no forced framework migration
- Skipping .NET 8 (EOL Nov 2026, mid-Phase 1B) avoids a costly 3–5 dev-day migration during the most critical phase
- .NET 9 was rejected outright: STS only, support ends mid-2026
- Runs native on Linux with same performance as Windows
- Devs already know C# (now using C# 14 via `LangVersion=latest`)
- Hangfire, EF Core, SignalR, all mature; .NET 10 packages stable (6+ months since GA)
- Strong typing reduces runtime bugs
- Memory-efficient (~150MB per API instance baseline)
- Smaller / faster Docker images via Ubuntu 24.04 (Noble) base in modern .NET tags

### Project Structure (Clean Architecture-ish)
```
src/
├── Rmms.Api/                  # ASP.NET Core Web API
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Program.cs
│   └── appsettings.json
├── Rmms.Application/          # Use cases, DTOs, validators
│   ├── Features/
│   │   ├── Attendance/
│   │   ├── Forms/
│   │   └── ...
│   └── Common/
├── Rmms.Domain/               # Entities, value objects, domain events
│   ├── Entities/
│   ├── Enums/
│   └── ValueObjects/
├── Rmms.Infrastructure/       # EF Core, external services, Hangfire jobs
│   ├── Persistence/
│   │   └── Migrations/
│   ├── Services/
│   │   ├── FaceRecognition/
│   │   ├── Notification/
│   │   └── Storage/
│   └── Jobs/
└── Rmms.Shared/               # DTOs shared with FE if needed
tests/
├── Rmms.UnitTests/
└── Rmms.IntegrationTests/     # Uses Testcontainers
```

### Key NuGet Packages
- `Microsoft.EntityFrameworkCore.PostgreSQL` — Npgsql provider
- `Hangfire.AspNetCore` + `Hangfire.PostgreSql` — background jobs
- `MediatR` — CQRS pattern
- `FluentValidation` — request validation
- `Mapster` — object mapping (lighter than AutoMapper)
- `Serilog.AspNetCore` — structured logging
- `Sentry.AspNetCore` — error tracking
- `Swashbuckle.AspNetCore` — OpenAPI/Swagger
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT auth
- `BCrypt.Net-Next` — password hashing
- `ClosedXML` — Excel export
- `QuestPDF` — PDF generation (if needed)
- `Polly` — retry/circuit breaker for external APIs

## Web Frontend Stack — Detailed

### Why Next.js 14?
- App Router with React Server Components → fast initial loads
- Built-in i18n routing
- Mature ecosystem
- TypeScript first-class
- Easy deployment (Docker)

### Project Structure
```
web-admin/
├── app/
│   ├── [locale]/              # i18n routing (vi, en)
│   │   ├── (auth)/            # Login layout
│   │   │   └── login/
│   │   ├── (dashboard)/       # Authenticated layout
│   │   │   ├── attendance/
│   │   │   ├── users/
│   │   │   ├── forms/
│   │   │   ├── reports/
│   │   │   └── ...
│   │   └── layout.tsx
│   └── api/                   # Next.js API routes (proxy if needed)
├── components/
│   ├── ui/                    # Reusable components
│   └── features/              # Feature-specific
├── lib/
│   ├── api/                   # API client (axios/fetch wrappers)
│   ├── hooks/                 # Custom hooks
│   └── utils/
├── locales/
│   ├── vi.json
│   └── en.json
├── types/
└── public/
```

### Key Packages
- `next` 14
- `antd` ^5, `@ant-design/pro-components` — Pro UI
- `@tanstack/react-query` — server state
- `zustand` — client state
- `react-hook-form`, `zod`, `@hookform/resolvers` — forms
- `next-intl` — i18n
- `axios` or `ky` — HTTP client
- `recharts` — charts
- `@dnd-kit/core`, `@dnd-kit/sortable` — drag-drop for Form Builder
- `dayjs` — dates
- `lodash-es` — utils
- `vitest`, `@testing-library/react` — unit tests
- `playwright` — E2E tests

## Mobile Stack — Detailed

### Why Flutter?
- Single codebase iOS + Android
- Performance close to native
- Mature plugins for camera, GPS, biometrics
- Hot reload speeds up dev
- Devs already chose this

### Project Structure
```
mobile/
├── lib/
│   ├── main.dart
│   ├── core/
│   │   ├── api/               # Dio client, interceptors
│   │   ├── storage/           # Hive
│   │   ├── theme/
│   │   ├── router/            # go_router
│   │   └── utils/
│   ├── features/
│   │   ├── auth/
│   │   │   ├── data/
│   │   │   ├── domain/
│   │   │   └── presentation/
│   │   ├── attendance/
│   │   ├── schedule/
│   │   ├── forms/
│   │   └── ...
│   ├── shared/
│   │   ├── widgets/
│   │   └── providers/
│   └── l10n/
│       ├── app_en.arb
│       └── app_vi.arb
├── ios/
├── android/
└── test/
```

### Key Packages
- `flutter_riverpod` ^2 — state
- `dio` ^5 — HTTP
- `retrofit` + `retrofit_generator` — typed API client
- `hive`, `hive_flutter`, `hive_generator` — local DB
- `geolocator` — GPS
- `trust_location` — fake GPS detection (Android)
- `flutter_jailbreak_detection` — root/jailbreak check
- `camera`, `image_picker` — photo capture
- `image` — EXIF manipulation
- `native_exif` — read/write EXIF
- `firebase_messaging`, `flutter_local_notifications` — push
- `flutter_secure_storage` — token storage
- `go_router` — routing
- `intl`, `flutter_localizations` — i18n
- `freezed`, `json_serializable` — data classes
- `flutter_test`, `integration_test`, `mocktail` — testing

## Database — PostgreSQL 16

### Why PostgreSQL?
- JSONB for Form Engine dynamic schema
- Mature, free, well-supported
- Good performance for our workload
- Strong type system

### Naming Conventions
- Tables: `snake_case`, plural (`users`, `attendance_records`)
- Columns: `snake_case` (`created_at`, `user_id`)
- Indexes: `ix_table_column` (`ix_attendance_records_user_id_check_in_at`)
- Foreign keys: `fk_table_column` 
- Primary keys: `id` (UUID v7 — time-sortable)

### Critical Indexes (will add as we go)
- `attendance_records (user_id, check_in_at DESC)` — history queries
- `attendance_records (status, created_at)` — Admin Review queue
- `notifications (user_id, is_read, created_at DESC)` — unread badge
- `form_submissions (form_id, user_id)` — submission lookup
- `audit_logs (entity_type, entity_id, created_at)` — audit trail

### Partitioning Strategy (later, when volume grows)
- `attendance_records` partitioned by month
- `audit_logs` partitioned by month, archive after 12 months
- `notifications` partitioned by month, archive after 3 months

## Infrastructure

### Production Setup (initial)
```
                    Internet
                       │
                  Cloudflare (DNS + proxy, free tier)
                       │
                  VPS (Vultr SG, 8 vCPU / 16GB / 320GB SSD, ~$80/mo)
                       │
                  Caddy (auto SSL, reverse proxy)
                  ├──> .NET API (Docker container)
                  ├──> Next.js Admin Web (Docker container)
                  ├──> PostgreSQL 16 (Docker, with daily backup to S3)
                  ├──> Redis 7 (Docker)
                  ├──> MinIO (Docker, for file storage)
                  └──> Uptime Kuma (Docker)

                  External:
                  ├──> FPT.AI Face API
                  ├──> SendGrid (email)
                  ├──> FCM (push)
                  └──> Sentry (errors)
```

### Staging
- Separate smaller VPS (~$20/mo)
- Same stack but smaller
- Auto-deploy from `develop` branch
- Production auto-deploys from `main` (or manual)

### Scaling Plan (post Phase 1)
1. Separate DB to own VPS
2. Add read replica for reporting queries
3. Multiple API containers behind load balancer
4. Move MinIO data to S3
5. CDN for static assets

## Security Choices

- HTTPS only (HSTS enabled)
- JWT in `Authorization` header (NOT cookies, for mobile compat)
- Refresh token rotation (revoke old on use)
- Password hashing: bcrypt with cost 12
- Rate limiting: per-IP and per-user (via Redis)
- Input validation: FluentValidation on BE, Zod on FE
- SQL injection: prevented by EF Core parameterized queries
- XSS: React/Next.js auto-escape; Content-Security-Policy header
- CSRF: not applicable (no cookies, JWT in header)
- Sensitive data encryption: at rest via column encryption for selfies + face templates
- BUH email-link tokens: HMAC-signed, 24h expiry
- File uploads: virus scan (ClamAV optional), type/size validation, store outside web root
- Audit log: append-only, no UPDATE/DELETE allowed at DB level

## Performance Targets

| Metric | Target |
|---|---|
| API p50 response time | <100ms |
| API p99 response time | <500ms |
| Mobile cold start | <3s |
| Mobile check-in flow | <8s (incl. face API + GPS) |
| Web TTI (Time to Interactive) | <2s |
| DB query (single record) | <50ms |
| DB query (report) | <2s |
| Concurrent users supported | 500+ initially |

## Localization (i18n)

- **Languages**: Vietnamese (default), English
- **BE**: `IStringLocalizer` with .resx files for error messages + email templates
- **Web**: `next-intl` with `locales/vi.json` and `locales/en.json`
- **Mobile**: Flutter's `intl` package with `app_vi.arb` and `app_en.arb`
- **Forms / User-generated content**: NOT auto-translated. Admin enters bilingual content manually if needed (e.g., news title_vi + title_en).
- **Date format**: dd/MM/yyyy for VN, MM/dd/yyyy for EN
- **Number format**: 1.234,56 for VN, 1,234.56 for EN
- **Time zone**: All timestamps stored as UTC, displayed in user's local TZ (Asia/Ho_Chi_Minh default)
