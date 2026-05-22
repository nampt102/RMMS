# 03 — Architecture

## System Architecture (High Level)

```
┌──────────────────────────────────────────────────────────────────┐
│                       CLIENTS                                    │
├───────────────────────────┬──────────────────────────────────────┤
│   Mobile (Flutter)        │   Web Admin/BUH (Next.js)            │
│   ├─ PG screens           │   ├─ Admin screens                   │
│   └─ Leader screens       │   └─ BUH screens                     │
└───────────────┬───────────┴──────────────────┬───────────────────┘
                │ HTTPS                        │ HTTPS
                │ JWT in Authorization header  │
                ▼                              ▼
        ┌────────────────────────────────────────┐
        │     Caddy (Reverse proxy + SSL)        │
        └─────────────┬──────────────────────────┘
                      │
        ┌─────────────▼──────────────────────────┐
        │     .NET 8 Web API                     │
        │     ├─ Controllers (REST endpoints)    │
        │     ├─ SignalR Hub (PG Online status)  │
        │     ├─ Middleware (auth, logging,      │
        │     │   rate limit, CORS, i18n)        │
        │     └─ Hangfire (background jobs)      │
        └──┬─────────┬─────────┬─────────┬───────┘
           │         │         │         │
           ▼         ▼         ▼         ▼
        ┌─────┐  ┌─────┐  ┌──────┐  ┌─────────┐
        │ PG  │  │Redis│  │MinIO │  │External │
        │ SQL │  │     │  │      │  │APIs     │
        └─────┘  └─────┘  └──────┘  └─────────┘
        Data     Cache+   Files,    FPT.AI,
                 Queue    photos    FCM,
                                    SendGrid,
                                    Sentry
```

## Backend Layered Architecture

```
┌──────────────────────────────────────────────┐
│  Rmms.Api (Presentation)                     │
│  - Controllers                               │
│  - Middlewares                               │
│  - DTOs (request/response)                   │
│  - Auth setup                                │
└────────────────┬─────────────────────────────┘
                 │ depends on
                 ▼
┌──────────────────────────────────────────────┐
│  Rmms.Application (Use cases)                │
│  - Commands / Queries (MediatR)              │
│  - Handlers                                  │
│  - Validators (FluentValidation)             │
│  - Mappers (Mapster)                         │
│  - Interfaces (ports)                        │
└────────────────┬─────────────────────────────┘
                 │ depends on
                 ▼
┌──────────────────────────────────────────────┐
│  Rmms.Domain (Core)                          │
│  - Entities                                  │
│  - Value Objects                             │
│  - Domain Events                             │
│  - Domain Services                           │
│  - Enums                                     │
└──────────────────────────────────────────────┘
                 ▲
                 │ depends on
┌────────────────┴─────────────────────────────┐
│  Rmms.Infrastructure (Adapters)              │
│  - EF Core DbContext                         │
│  - Migrations                                │
│  - External API clients (Face, Email, FCM)   │
│  - Hangfire jobs                             │
│  - File storage adapters                     │
│  - SignalR hubs                              │
└──────────────────────────────────────────────┘
```

## Request Flow (typical)

1. Client sends HTTP request with JWT in `Authorization: Bearer xxx`
2. Caddy terminates SSL, forwards to .NET API
3. Middleware pipeline:
   - CORS
   - Request logging (Serilog)
   - Rate limiting (Redis-backed)
   - JWT auth (validates token)
   - Authorization (role check)
   - i18n (Accept-Language header → IStringLocalizer)
4. Controller receives DTO, validates with FluentValidation
5. Controller sends Command/Query via MediatR to Application
6. Handler executes business logic, calls Domain entities
7. Repository (in Infrastructure) reads/writes via EF Core
8. Response DTO returned to client

## Critical Flows

### Flow 1: Check-in with Face Recognition

```
PG App                  API                Face API        DB
  │                      │                    │             │
  │ 1. Get assigned store│                    │             │
  ├─────────────────────>│                    │             │
  │                      ├───────────────────────────────-->│
  │                      │<────────────────────────────-────│
  │<─────────────────────│                    │             │
  │                      │                    │             │
  │ 2. Capture selfie    │                    │             │
  │ 3. Get GPS coords    │                    │             │
  │ 4. Capture store photo (with EXIF)        │             │
  │                      │                    │             │
  │ 5. POST /attendance/check-in              │             │
  │    (selfie, gps, store_photo, store_id)   │             │
  ├─────────────────────>│                    │             │
  │                      │ 6. Check fake GPS  │             │
  │                      │ 7. Calc distance to store        │
  │                      │                    │             │
  │                      │ 8. Verify face     │             │
  │                      ├───────────────────>│             │
  │                      │<───────────────────│             │
  │                      │                    │             │
  │                      │ 9. Determine status:             │
  │                      │    - Valid                       │
  │                      │    - Late (>5min)                │
  │                      │    - GpsViolation                │
  │                      │    - FaceFail                    │
  │                      │    - FakeGpsBlocked              │
  │                      │                    │             │
  │                      │ 10. Save attendance              │
  │                      ├───────────────────────────────-->│
  │                      │                    │             │
  │                      │ 11. Upload photos to MinIO       │
  │                      │                    │             │
  │                      │ 12. If review needed,            │
  │                      │     queue Admin Review           │
  │                      │                    │             │
  │                      │ 13. Audit log entry              │
  │                      │                    │             │
  │<─────────────────────│                    │             │
  │ 14. Show result      │                    │             │
```

### Flow 2: Approval Workflow (PG requests, Leader approves)

```
PG App           API            DB           Leader App      Email
  │              │              │                │             │
  │ 1. Create OT request        │                │             │
  ├─────────────>│              │                │             │
  │              ├─────────────>│                │             │
  │              │              │                │             │
  │              │ 2. Find assigned Leader       │             │
  │              │              │                │             │
  │              │ 3. Create approval task       │             │
  │              ├─────────────>│                │             │
  │              │              │                │             │
  │              │ 4. Push notif to Leader       │             │
  │              ├──────────────────────────────>│             │
  │              │ 5. Email to Leader            │             │
  │              ├──────────────────────────────────────────-->│
  │              │              │                │             │
  │              │              │  6. Leader opens app         │
  │              │              │<───────────────│             │
  │              │              │                │             │
  │              │              │  7. Approve (inline or detail)
  │              │              ├───────────────>│             │
  │              │              │                │             │
  │              │ 8. Update OT status to Approved             │
  │              │              │                │             │
  │              │ 9. Audit log │                │             │
  │              │              │                │             │
  │              │ 10. Notify PG│                │             │
  │              │<─────────────┤                │             │
  │ 11. Notif    │              │                │             │
```

### Flow 3: BUH approves via Email Link (no login)

```
Leader App      API         DB         BUH Email      BUH (any browser)
  │              │           │              │                │
  │ 1. Create Visit Plan     │              │                │
  ├─────────────>│           │              │                │
  │              ├──────────>│              │                │
  │              │           │              │                │
  │              │ 2. Generate signed token  │                │
  │              │    HMAC(approval_id +     │                │
  │              │           buh_id + 24h_expiry)             │
  │              │           │              │                │
  │              │ 3. Send email with link  │                │
  │              │    https://api.rmms/approve?token=xxx&action=approve
  │              ├──────────────────────────>│                │
  │              │           │              │                │
  │              │           │              │ 4. BUH clicks link
  │              │           │              ├───────────────>│
  │              │           │              │                │
  │              │           │              │                │ 5. GET /approve?token=xxx
  │              │<──────────────────────────────────────────│
  │              │           │              │                │
  │              │ 6. Validate token (HMAC, not expired,      │
  │              │    not already used)                       │
  │              │           │              │                │
  │              │ 7. Show simple approve/reject page         │
  │              ├─────────────────────────────────────────-->│
  │              │           │              │                │
  │              │           │              │ 8. BUH confirms action
  │              │<──────────────────────────────────────────│
  │              │           │              │                │
  │              │ 9. Mark approval done    │                │
  │              │    Burn token (one-use)  │                │
  │              ├──────────>│              │                │
  │              │           │              │                │
  │              │ 10. Notify Leader        │                │
```

## Data Storage Strategy

| Data type | Where | Retention |
|---|---|---|
| User accounts, roles | PostgreSQL | Forever (soft delete) |
| Attendance records | PostgreSQL | 5 years, then archive |
| Selfie photos | MinIO | 90 days, then delete |
| Store photos | MinIO | 90 days, then delete |
| Face templates | FPT.AI (cloud) | Until user inactive |
| Form submissions | PostgreSQL | 5 years |
| Form attachments | MinIO | 5 years |
| Documents | MinIO | Until Admin deletes |
| Audit logs | PostgreSQL | 12 months hot, then S3 archive |
| Notifications | PostgreSQL | 90 days |
| Push notifications | Firebase | Per FCM defaults |
| Email logs | SendGrid | Per SendGrid retention |
| Session/cache | Redis | TTL-based |

## Background Jobs (Hangfire)

| Job | Schedule | Purpose |
|---|---|---|
| `ExpireOldDrafts` | Daily 2am | Cleanup form drafts older than 30d |
| `SendDeadlineReminders` | Hourly | Notify users of upcoming form deadlines |
| `ArchiveOldNotifications` | Daily 3am | Move notifications older than 90d |
| `ProcessNotificationQueue` | Triggered | Send batched email/push |
| `GenerateDailyReports` | Daily 1am | Pre-aggregate dashboard data |
| `CleanupExpiredApprovalTokens` | Daily 4am | Remove BUH email tokens > 24h |
| `RetryFailedFaceVerifications` | Every 15min | Retry transient API failures |
| `BackupDatabase` | Daily 3am | pg_dump → upload to S3 |

## Real-time (SignalR)

### Hubs
- `/hubs/team-monitoring` — broadcast PG online status changes

### Events Pushed
- `OnPgCheckedIn` — when a PG checks in
- `OnPgCheckedOut` — when a PG checks out
- `OnPgStatusChanged` — schedule change, leave approved, etc.

### Subscribers
- Leader app — sees own PGs
- Admin web — sees all
- BUH web — sees by area/category

## Security Architecture

### Auth Flow
```
1. Login (email + password)
   POST /auth/login
   → returns access_token (15min) + refresh_token (30d)

2. Store tokens:
   - Mobile: flutter_secure_storage
   - Web: httpOnly cookie OR localStorage (TBD)

3. API calls:
   Authorization: Bearer {access_token}

4. Token expiry:
   - 401 → call /auth/refresh with refresh_token
   - Get new access + refresh (rotation)
   - Old refresh invalidated

5. Single-device (PG only):
   - On login, check device_id
   - If different from registered → device change flow
   - Other sessions invalidated when new device approved
```

### Authorization
- Role-based: PG, Leader, Admin, BUH
- Each endpoint declares required role(s) via `[Authorize(Roles="...")]`
- Resource-level: e.g., Leader can only see own PGs (filtered in queries)

## Deployment Architecture

### Containers (Docker Compose)
```yaml
services:
  caddy:       # Reverse proxy + SSL
  api:         # .NET 8 API
  web:         # Next.js app
  postgres:    # DB
  redis:       # Cache + Hangfire queue
  minio:       # File storage
  uptime-kuma: # Monitoring
```

### CI/CD Flow
```
Developer push to feature branch
  ↓
GitHub Actions:
  - Lint
  - Run unit tests
  - Run integration tests (Testcontainers)
  ↓
PR review by other dev
  ↓
Merge to `develop`
  ↓
Auto-deploy to Staging VPS
  ↓
Manual testing
  ↓
Merge to `main` (or release branch)
  ↓
Auto-build Docker images, push to registry
  ↓
Manual approve to deploy Production
  ↓
Production VPS pulls images, runs migrations, restarts containers
```

## Disaster Recovery

- Daily PostgreSQL backup → S3 (Vultr Object Storage or AWS)
- MinIO data → daily snapshot
- Backup retention: 30 days daily, 12 months monthly
- Recovery target: RPO 24h, RTO 4h
- Runbook documented separately
