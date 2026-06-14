# Ops Runbook — RMMS 2026 Phase 1A

> Quy trình vận hành tối thiểu cho bản phát hành nội bộ Phase 1A. Bổ trợ checklist trong [RELEASE-NOTES-Phase-1A.md §6](./RELEASE-NOTES-Phase-1A.md#6-danh-sách-việc-vận-hành-còn-lại-trước-nghiệm-thu).
>
> **Cập nhật:** 2026-06-14 · **Server prod:** `103.216.116.206` (Ubuntu 22.04 + Docker Compose)

---

## 1. Production smoke test (sau mỗi lần deploy)

Script **chỉ đọc** (read-only), an toàn chạy thẳng trên prod — một lần login admin + các GET đại diện cho từng module Phase 1A đã giao.

```powershell
# Từ máy Windows (PowerShell 5.1 hoặc 7+):
$env:RMMS_SMOKE_ADMIN_PWD = "<admin password>"   # giữ pwd ngoài lịch sử lệnh
.\scripts\smoke-prod.ps1 -BaseUrl https://<prod-domain>
# hoặc trực tiếp qua IP:port API
.\scripts\smoke-prod.ps1 -BaseUrl http://103.216.116.206:5080 -AdminEmail admin@motivesvn.com
```

**Kiểm tra:** `/health/{live,ready}`, admin login → JWT, `/auth/me`, rồi 1 GET cho mỗi module: M15 dashboard, M12 monitoring, M01 users, M05/M16 attendance, M09 approvals, M16 audit, M03 stores; cuối cùng 1 ca **âm tính RBAC** (gọi admin endpoint không token → 401).

**Kết quả:** exit code `0` = tất cả pass, `1` = có lỗi (in danh sách FAIL). Không tạo/sửa dữ liệu.

---

## 2. Backup + restore-verify (Postgres + object storage)

> ⚠️ **Luôn `backup` TRƯỚC khi recreate container Postgres.** Bind-mount `${POSTGRES_DATA_DIR:-./data/postgres}` chỉ được entrypoint khởi tạo khi thư mục **rỗng**; recreate với dir rỗng → cluster trắng. Giữ một bản dump đã verify **ngoài máy chủ**.

Chạy **trên VPS Ubuntu** (nơi có docker compose):

```bash
# nạp biến môi trường thật trước (chứa POSTGRES_PASSWORD)
set -a; source .env; set +a

./scripts/backup-restore.sh            # backup + verify (mặc định)
./scripts/backup-restore.sh backup     # chỉ dump + tar
./scripts/backup-restore.sh verify ./backups/<stamp>/rmms.dump   # chỉ verify
```

**`backup`** → `./backups/<stamp>/`:
- `rmms.dump` — `pg_dump -Fc` (custom format, nén, restore bằng `pg_restore`)
- `minio.tar.gz` — ảnh selfie / ảnh điểm bán (bind-mount `./data/minio`)
- `compreface-pgdata.tar.gz` — embedding khuôn mặt (bind-mount `./data/compreface-pgdata`)

**`verify`** → restore dump vào **container Postgres tạm** (`--tmpfs`, không đụng prod), đợi ready, `pg_restore`, rồi đếm bảng + `users` / `attendance_records` / `audit_log`. Thất bại nếu DB restore không có bảng nào. Container tự dọn khi xong.

> 💡 Khuyến nghị: cron hằng ngày chạy `backup`, rsync `./backups` sang lưu trữ ngoài; chạy `verify` định kỳ (tuần) để đảm bảo dump thực sự restore được.

---

## 3. Checklist phát hành (tóm tắt)

| Mục | Công cụ | Trạng thái |
|---|---|---|
| Production smoke test | [`scripts/smoke-prod.ps1`](../scripts/smoke-prod.ps1) | ✅ script sẵn sàng |
| Backup verified | [`scripts/backup-restore.sh`](../scripts/backup-restore.sh) | ✅ script sẵn sàng |
| Functional smoke (auth/admin) | [`scripts/smoke-day4.ps1`](../scripts/smoke-day4.ps1) | ✅ (dev) |
| Bug bash thiết bị thật | thủ công | ⏳ cần thiết bị |
| UAT sign-off 23 AC | [07-acceptance-criteria.md](./07-acceptance-criteria.md) | ⏳ cần stakeholder |
| Performance baseline (p95) | — | ⏳ |
| FCM end-to-end | thiết bị thật + creds prod | ⏳ |

---

## 4. Tham chiếu

- Release: [RELEASE-NOTES-Phase-1A.md](./RELEASE-NOTES-Phase-1A.md)
- Hướng dẫn người dùng: [USER-GUIDE-Phase-1A.md](./USER-GUIDE-Phase-1A.md)
- Trạng thái dự án: [PROJECT-STATE.md](./PROJECT-STATE.md)
