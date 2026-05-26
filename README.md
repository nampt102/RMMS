# RMMS 2026 — Retail Merchandiser Management System

Hệ thống quản lý PG/Leader làm việc tại các điểm bán lẻ.

## Cấu trúc Repo (Monorepo)

```
RMMS/
├── knowledge-base/      # Tài liệu spec (overview, architecture, modules, sprints…)
├── backend/             # .NET 8 modular monolith (API + Worker)
├── web/                 # Next.js 14 (Admin + BUH)
├── mobile/              # Flutter 3.x (PG + Leader)
├── infra/               # docker-compose, Caddyfile, scripts
├── docker-compose.yml   # Local dev orchestration
├── .editorconfig
└── README.md
```

## Tech Stack (tóm tắt — xem chi tiết `knowledge-base/02-tech-stack.md`)

| Layer | Stack |
|---|---|
| Backend | .NET 8 + EF Core 8 + PostgreSQL 16 + Redis 7 + Hangfire + SignalR |
| Web | Next.js 14 (App Router) + TypeScript + Ant Design Pro + TanStack Query + Zustand |
| Mobile | Flutter 3.x + Riverpod 2 + Dio + Hive + Freezed |
| Infra | Docker Compose + Caddy 2 + Ubuntu 22.04 + Vultr Singapore |

## Yêu cầu môi trường dev

- **.NET SDK 8.0** (LTS) — backend
- **Node.js 20 LTS** + **pnpm** (hoặc npm) — web
- **Flutter SDK 3.22+** — mobile
- **Docker Desktop** (hoặc Docker Engine + Compose) — DB/Redis/MinIO local
- **PostgreSQL 16 client** (`psql`) — query nhanh khi cần
- **Git** + **EditorConfig plugin** trên IDE

## Quickstart

```bash
# 1) Khởi động hạ tầng (Postgres, Redis, MinIO, Caddy)
docker compose up -d postgres redis minio

# 2) Backend
cd backend
dotnet restore
dotnet ef database update --project src/Rmms.Infrastructure --startup-project src/Rmms.Api
dotnet run --project src/Rmms.Api

# 3) Web (terminal mới)
cd web
pnpm install
pnpm dev

# 4) Mobile (terminal mới)
cd mobile
flutter pub get
dart run build_runner build --delete-conflicting-outputs
flutter run
# Platform folders: mobile/android, mobile/ios, … (org com.rmms)
```

## Quy ước

- Branch: `feature/<sprint>-<slug>`, `fix/<issue>-<slug>`
- Commit: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`)
- PR: cần ≥1 review, CI xanh, link tới module/AC/BR ID
- Strings: bilingual `vi` (default) + `en` — không hardcode tiếng Việt trực tiếp
- Error envelope: theo `knowledge-base/05-api-conventions.md`
- Business logic: SOURCE OF TRUTH là `knowledge-base/06-business-rules.md`

## Phase 1A (5 tháng đầu)

Modules ưu tiên: M01 Auth/Device → M02 Stores → M03 Shifts → M04 Attendance (face + GPS) → M05 Form Engine. Xem `knowledge-base/sprints/` để biết chi tiết.

## Liên hệ

- Tech Lead / PM: (cập nhật)
- Email: nampt
