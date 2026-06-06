# Hướng dẫn migrate database (local dev)

Tài liệu này mô tả cách **kiểm tra** và **chạy EF Core migration** cho RMMS trên máy dev (macOS / Linux / Windows với Docker Desktop).

> **Phạm vi:** Postgres + Redis local qua `docker-compose.yml` ở **gốc repo** (`RMMS/`), không phải trong `backend/`.

---

## Yêu cầu

| Thành phần | Ghi chú |
|------------|---------|
| Docker Desktop | Postgres + Redis |
| .NET SDK **10.0.100** | Xem `backend/global.json` |
| `dotnet-ef` | Cài qua `dotnet tool restore` hoặc global tool |

```bash
# macOS — nếu dotnet báo thiếu SDK 10
export PATH="$HOME/.dotnet:$PATH"
dotnet --version   # kỳ vọng: 10.0.100
```

---

## Connection string (local)

Docker Compose expose Postgres trên **port 5432**:

```
Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true
```

**Lưu ý:** `appsettings.Development.json` trên máy dev có thể dùng port khác (ví dụ `5433`). Khi chạy `dotnet ef` hoặc `dotnet run`, API và EF **phải trỏ cùng một Postgres**. Khuyến nghị: dùng `--connection` (EF) hoặc biến môi trường khi chạy API:

```bash
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

---

## 1. Khởi động hạ tầng

Chạy từ **gốc repo**:

```bash
cd /path/to/RMMS

cp .env.example .env   # lần đầu (tuỳ chọn)

docker compose up -d postgres redis
docker compose ps
docker compose exec postgres pg_isready -U rmms -d rmms
```

Postgres dùng image **PostGIS** (`postgis/postgis:16-3.4`) — bắt buộc vì migration có annotation extension `postgis`.

---

## 2. Kiểm tra trạng thái DB

### Migration đã apply

```bash
docker compose exec postgres psql -U rmms -d rmms -c \
  'SELECT migration_id FROM "__EFMigrationsHistory" ORDER BY migration_id;'
```

### Danh sách bảng

```bash
docker compose exec postgres psql -U rmms -d rmms -c "\dt"
```

### Extension

```bash
docker compose exec postgres psql -U rmms -d rmms -c "\dx"
```

Kỳ vọng có `postgis`, `pgcrypto`, `citext`, `pg_trgm` (từ `infra/postgres/init/01-extensions.sql`).

---

## 3. Xem migration pending (EF Core)

```bash
export PATH="$HOME/.dotnet:$PATH"   # macOS nếu cần
cd backend

dotnet restore

dotnet ef migrations list \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

Dòng có `(Pending)` = chưa apply.

Migration hiện có (cập nhật khi thêm migration mới):

| Migration ID | Module |
|--------------|--------|
| `20260529022602_Init_M01_M02_Foundation` | M01 Auth, users, audit_log, … |
| `20260605034359_M03_Organization_Assignment` | M03 areas, stores, assignments |
| `20260606042427_M07_WorkSchedule` | M07 lịch làm việc |

---

## 4. Chạy migration

```bash
cd backend

dotnet ef database update \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

### Nếu build fail do analyzer trên file migration

Một số file migration auto-generated có thể vi phạm rule `IDE0161`. Build tạm tắt analyzer rồi update với `--no-build`:

```bash
dotnet build src/Rmms.Api/Rmms.Api.csproj \
  -p:EnforceCodeStyleInBuild=false \
  -p:TreatWarningsAsErrors=false

dotnet ef database update \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --no-build \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

### Migrate về migration cụ thể (rollback)

```bash
dotnet ef database update <MigrationName> \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

Ví dụ rollback về foundation only:

```bash
dotnet ef database update 20260529022602_Init_M01_M02_Foundation \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

---

## 5. Post-migration SQL (bắt buộc sau migration foundation)

Script **append-only** cho `audit_log` nằm ngoài EF (REVOKE/GRANT theo role DB). **Idempotent** — chạy lại an toàn.

Từ **gốc repo**:

```bash
docker compose exec -T postgres psql -U rmms -d rmms \
  < backend/src/Rmms.Infrastructure/Persistence/Migrations/PostMigrationScripts/001_audit_log_append_only.sql
```

Kỳ vọng output: `NOTICE: audit_log permissions hardened for role: rmms`

---

## 6. Verify sau migrate

```bash
# Lịch sử migration
docker compose exec postgres psql -U rmms -d rmms -c \
  'SELECT migration_id FROM "__EFMigrationsHistory" ORDER BY migration_id;'

# Bảng chính (tuỳ sprint)
docker compose exec postgres psql -U rmms -d rmms -c "\dt"
```

Chạy API và health check:

```bash
cd backend
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw" \
  dotnet run --project src/Rmms.Api

curl -s http://localhost:5080/health/ready
```

---

## 7. Seed dữ liệu test (tuỳ chọn)

Script M03: areas, stores, categories, assignments — **idempotent**.

**Điều kiện:** đã có ít nhất **1 PG** và **1 Leader** (tạo qua app/admin). Nếu chưa có user, script vẫn seed masters nhưng **bỏ qua** block assignment.

```bash
# Từ gốc repo
docker compose exec -T postgres psql -U rmms -d rmms \
  < backend/scripts/seed-m03-testdata.sql
```

Tạo admin bootstrap (lần đầu):

```bash
cd backend
ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw" \
  dotnet run --project src/Rmms.Api -- seed-admin \
    --email=admin@rmms.local \
    --password=Admin123 \
    --full-name="System Admin"
```

---

## 8. Reset database từ đầu

Dùng khi volume Postgres cũ thiếu PostGIS hoặc schema lệch không sửa được:

```bash
cd /path/to/RMMS

docker compose down postgres
docker volume rm rmms_postgres-data

docker compose up -d postgres redis

# Chờ healthy, rồi lặp lại: bước 4 → 5 → 7
docker compose exec postgres pg_isready -U rmms -d rmms
```

---

## 9. Tạo migration mới (dev)

```bash
cd backend

dotnet ef migrations add <TenMigration_MoTa> \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --output-dir Persistence/Migrations
```

Sau khi review code migration:

```bash
dotnet ef database update \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"
```

Quy ước đặt tên: `YYYYMMDD_<Module>_<MoTa>` (ví dụ `M07_WorkSchedule`).

---

## Troubleshooting

| Triệu chứng | Nguyên nhân | Cách xử lý |
|-------------|-------------|------------|
| `extension "postgis" is not available` | Volume/image cũ không có PostGIS | Reset volume (mục 8); image hiện tại: `postgis/postgis:16-3.4` |
| `Failed to connect to 127.0.0.1:5433` | API/EF trỏ sai port | Dùng port **5432** hoặc sửa `appsettings.Development.json` |
| `Requested SDK version: 10.0.100` | PATH thiếu .NET 10 | `export PATH="$HOME/.dotnet:$PATH"` |
| `Generated.xcconfig must exist` | Chạy `pod install` sau `flutter clean` | `flutter pub get` trước (Flutter); không liên quan EF |
| Login API 500, Npgsql retry | Postgres down hoặc sai connection | `docker compose ps`, kiểm tra port |

---

## Tóm tắt one-liner (happy path)

```bash
cd /path/to/RMMS
docker compose up -d postgres redis

export PATH="$HOME/.dotnet:$PATH"
cd backend
dotnet restore
dotnet ef database update \
  --project src/Rmms.Infrastructure \
  --startup-project src/Rmms.Api \
  --connection "Host=localhost;Port=5432;Database=rmms;Username=rmms;Password=rmms_dev_pw;Include Error Detail=true"

cd ..
docker compose exec -T postgres psql -U rmms -d rmms \
  < backend/src/Rmms.Infrastructure/Persistence/Migrations/PostMigrationScripts/001_audit_log_append_only.sql

docker compose exec postgres psql -U rmms -d rmms -c \
  'SELECT migration_id FROM "__EFMigrationsHistory" ORDER BY migration_id;'
```
