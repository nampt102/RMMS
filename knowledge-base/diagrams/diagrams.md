# Diagrams

Mermaid source for architecture diagrams. Render at https://mermaid.live or in Markdown viewers that support Mermaid (GitHub, GitLab, Obsidian, Notion).

## 1. System Architecture

```mermaid
flowchart TB
    subgraph "Clients"
        Mobile["Mobile App<br/>(Flutter)<br/>PG + Leader"]
        WebAdmin["Web Admin<br/>(Next.js)<br/>Admin"]
        WebBUH["Web BUH<br/>(Next.js)<br/>BUH"]
    end

    Cloudflare["Cloudflare DNS + Proxy"]
    
    subgraph VPS["VPS (Vultr SG, Ubuntu 22.04)"]
        Caddy["Caddy<br/>(Reverse Proxy + SSL)"]
        
        subgraph DockerCompose["Docker Compose Stack"]
            API[".NET 8 API<br/>+ SignalR Hub<br/>+ Hangfire"]
            Web["Next.js App"]
            Postgres["PostgreSQL 16"]
            Redis["Redis 7<br/>(Cache + Queue + Session)"]
            MinIO["MinIO<br/>(File Storage)"]
            Kuma["Uptime Kuma"]
        end
    end
    
    subgraph External["External Services"]
        FPT["FPT.AI Face"]
        FCM["Firebase FCM"]
        SendGrid["SendGrid<br/>(Email)"]
        Sentry["Sentry<br/>(Errors)"]
        S3["S3 / Vultr Object<br/>(Backups)"]
    end
    
    Mobile --> Cloudflare
    WebAdmin --> Cloudflare
    WebBUH --> Cloudflare
    Cloudflare --> Caddy
    
    Caddy --> API
    Caddy --> Web
    
    API <--> Postgres
    API <--> Redis
    API <--> MinIO
    
    API --> FPT
    API --> FCM
    API --> SendGrid
    API --> Sentry
    
    Postgres -.daily backup.-> S3
```

## 2. Backend Layered Architecture

```mermaid
flowchart TB
    Controllers["Rmms.Api<br/>Controllers + Middleware"]
    AppLayer["Rmms.Application<br/>Commands / Queries / Handlers<br/>Validators / Mappers"]
    Domain["Rmms.Domain<br/>Entities / Value Objects / Domain Events"]
    Infra["Rmms.Infrastructure<br/>EF Core / External Services / Jobs"]
    
    Controllers --> AppLayer
    AppLayer --> Domain
    Infra --> Domain
    AppLayer -.uses interfaces from.-> Infra
```

## 3. Check-in Sequence

```mermaid
sequenceDiagram
    autonumber
    participant PG as PG Mobile App
    participant API as .NET API
    participant Face as FPT.AI Face
    participant DB as PostgreSQL
    participant MinIO

    PG->>API: GET /api/v1/attendance/check-in/info
    API->>DB: Get assigned store + shift
    DB-->>API: Store + shift data
    API-->>PG: Store info, GPS coords, shift time

    Note over PG: Capture selfie<br/>Get GPS<br/>Capture store photo (EXIF)

    PG->>API: POST /api/v1/attendance/check-in (multipart)
    Note over API: 1. Check fake GPS<br/>2. Check store assigned<br/>3. Calc distance to store<br/>4. Late marking
    
    API->>Face: POST verify (selfie + user_face_id)
    Face-->>API: { match: true, confidence: 0.92 }
    
    API->>MinIO: Upload selfie + store photo
    MinIO-->>API: file URLs
    
    API->>DB: INSERT attendance_records (status)
    
    alt GPS > 300m OR face fail
        API->>DB: INSERT admin_reviews
    end
    
    API->>DB: INSERT audit_logs
    
    API-->>PG: 201 { id, status }
    
    Note over API: Background:<br/>Push SignalR event<br/>Send notification
```

## 4. Approval Workflow

```mermaid
sequenceDiagram
    autonumber
    participant PG as PG Mobile
    participant API
    participant DB
    participant Leader as Leader Mobile
    participant Email

    PG->>API: POST /api/v1/ot-requests
    API->>DB: INSERT ot_requests
    API->>DB: INSERT approvals (assignee=Leader)
    
    Note over API: Background: send notifications
    API->>Leader: Push notification (FCM)
    API->>Email: Send email to Leader
    
    Leader->>API: GET /api/v1/approvals/pending
    API->>DB: Query pending for current user
    API-->>Leader: List

    Leader->>API: POST /api/v1/approvals/:id/approve
    API->>DB: UPDATE approvals.status = approved
    API->>DB: UPDATE ot_requests.status = approved
    API->>DB: INSERT audit_logs
    
    Note over API: Notify requester
    API->>PG: Push notification
```

## 5. BUH Email-Link Approval

```mermaid
sequenceDiagram
    autonumber
    participant Leader
    participant API
    participant DB
    participant Email as SendGrid
    participant BUH as BUH (any browser)

    Leader->>API: POST /api/v1/visit-plans
    API->>DB: Create plan + approval
    
    Note over API: Generate signed token<br/>HMAC(approval_id + buh_id + nonce)<br/>TTL 24h
    
    API->>DB: INSERT approval_email_tokens (hashed)
    API->>Email: Send email with link
    Email->>BUH: Email arrives
    
    BUH->>API: GET /approve?token=xxx&action=approve
    API->>DB: Validate token (exists, not expired, not used, signature OK)
    API-->>BUH: HTML confirm page
    
    BUH->>API: POST /approve/confirm (with reason if reject)
    API->>DB: UPDATE approval
    API->>DB: Mark token used
    API->>DB: INSERT audit_logs (with IP/UA)
    
    Note over API: Notify Leader
    
    API-->>BUH: Success page
```

## 6. Form Engine Architecture

```mermaid
flowchart LR
    subgraph Admin Web
        Builder["Form Builder UI<br/>(drag-drop or list)"]
        Rules["Rules Config Panel"]
        Assign["Assignment Matrix"]
    end
    
    subgraph DB
        Forms["forms<br/>(meta)"]
        Versions["form_versions<br/>(JSONB schema)"]
        Assignments["form_assignments<br/>(role/user/store/area/category)"]
        Submissions["form_submissions<br/>(answers JSONB)"]
    end
    
    subgraph Mobile
        List["Form List<br/>(assigned only)"]
        Renderer["Dynamic Renderer<br/>(factory pattern)"]
        Draft["Hive Offline Draft"]
        Submit["Submit with<br/>Idempotency Key"]
    end
    
    Builder --> Forms
    Builder --> Versions
    Rules --> Versions
    Assign --> Assignments
    
    List -.read assigned.-> Forms
    List -.read assigned.-> Assignments
    
    Renderer -.read schema.-> Versions
    Renderer --> Draft
    Draft --> Submit
    Submit --> Submissions
    Submissions -.FK.-> Versions
```

## 7. Entity Relationship (simplified)

```mermaid
erDiagram
    USERS ||--o{ USER_DEVICES : "has"
    USERS ||--o{ ATTENDANCE_RECORDS : "creates"
    USERS ||--o{ WORK_SCHEDULES : "has"
    USERS ||--o| USER_LEADER_ASSIGNMENT : "is PG of"
    USERS ||--o{ USER_STORE_ASSIGNMENTS : "assigned to"
    USERS ||--o{ FORM_SUBMISSIONS : "submits"
    
    STORES ||--o{ ATTENDANCE_RECORDS : "location of"
    STORES ||--o{ WORK_SCHEDULE_SHIFTS : "work at"
    STORES }|--|| AREAS : "in"
    
    WORK_SCHEDULES ||--o{ WORK_SCHEDULE_SHIFTS : "contains"
    WORK_SCHEDULE_SHIFTS ||--o{ ATTENDANCE_RECORDS : "tracks"
    
    FORMS ||--|{ FORM_VERSIONS : "versioned as"
    FORMS ||--o{ FORM_ASSIGNMENTS : "assigned via"
    FORM_VERSIONS ||--o{ FORM_SUBMISSIONS : "submitted against"
    
    APPROVALS }o--|| USERS : "approver"
    APPROVALS }o--|| USERS : "requester"
    
    VISIT_PLANS ||--|{ VISIT_PLAN_ITEMS : "contains"
    VISIT_PLAN_ITEMS }o--|| STORES : "at"
    VISIT_PLAN_ITEMS }o--|| FORMS : "uses"
    
    ATTENDANCE_RECORDS ||--o| ADMIN_REVIEWS : "may need"
    USER_DEVICES ||--o| ADMIN_REVIEWS : "may need"
    
    USERS ||--o{ AUDIT_LOGS : "actor of"
```

## 8. Phase 1A vs Phase 1B Module Map

```mermaid
flowchart TB
    subgraph "Phase 1A (Sprints 1-10) — Internal Release"
        direction LR
        M1["M1 Identity & Access"]
        M2["M2 Device Mgmt"]
        M3["M3 Org & Assignment"]
        M5["M5 Attendance"]
        M6["M6 Face Verif"]
        M7["M7 Schedule"]
        M8["M8 Leave & OT"]
        M9["M9 Approval"]
        M12["M12 Team Monitor"]
        M14a["M14 Notif (basic)"]
        M16["M16 Audit & Review"]
    end
    
    subgraph "Phase 1B (Sprints 11-18) — Final Acceptance"
        direction LR
        M4["M4 Product Master"]
        M10["M10 Form Engine"]
        M11["M11 Visit Plan"]
        M13["M13 Documents"]
        M14b["M14 Notif (full)"]
        M15["M15 Dashboards (full)"]
    end
    
    M1 --> M2
    M1 --> M3
    M3 --> M5
    M1 --> M6
    M5 --> M6
    M3 --> M7
    M7 --> M9
    M8 --> M9
    M5 --> M12
    M5 --> M16
    M1 --> M14a
    
    M3 --> M4
    M4 --> M10
    M10 --> M11
    M9 --> M11
    M14a --> M14b
    M14a --> M13
    M5 --> M15
    
    style M5 fill:#ffeb3b
    style M6 fill:#ffeb3b
    style M9 fill:#ff9800
    style M10 fill:#f44336
```

## 9. Attendance Status State Machine

```mermaid
stateDiagram-v2
    [*] --> CheckInRequested
    CheckInRequested --> FakeGpsBlocked : Fake GPS detected
    FakeGpsBlocked --> [*]
    
    CheckInRequested --> Valid : GPS OK + Face OK + On time
    CheckInRequested --> Late : GPS OK + Face OK + >5min late
    CheckInRequested --> GpsViolationPendingReview : GPS >300m
    CheckInRequested --> FaceFailPendingReview : Face fails
    
    GpsViolationPendingReview --> AdminApproved : Admin: right person
    GpsViolationPendingReview --> AdminRejected : Admin: wrong person
    FaceFailPendingReview --> AdminApproved : Admin: right person
    FaceFailPendingReview --> AdminRejected : Admin: wrong person
    
    Valid --> CheckedOut : check-out
    Late --> CheckedOut : check-out
    AdminApproved --> CheckedOut : check-out
    
    CheckedOut --> [*]
    AdminRejected --> [*]
```

## 10. Form Version Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Draft : Admin creates
    Draft --> Published_v1 : publish
    Published_v1 --> InUse : assigned + filled
    
    Published_v1 --> Editing_v2 : Admin edits
    Editing_v2 --> Published_v2 : publish v2
    
    Published_v2 --> InUse
    
    note right of Published_v1
        v1 submissions
        immutable, kept
    end note
    
    note right of Published_v2
        New submissions
        use v2 schema
    end note
    
    Published_v2 --> Archived : Admin archives form
    Archived --> [*]
```

## How to Render

### GitHub / GitLab
Just commit `.md` files; they render automatically.

### Local preview
```bash
npx @mermaid-js/mermaid-cli -i diagrams.md -o output.png
```

### Online
Copy each ` ```mermaid ... ``` ` block to https://mermaid.live

### Export to SVG
```bash
mmdc -i diagram.mmd -o diagram.svg
```
