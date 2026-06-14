# Release Notes — RMMS 2026 Phase 1A (Internal Release)

> **Bản phát hành nội bộ** (internal release) khép lại Phase 1A. Tài liệu này tổng hợp những gì **đã giao**, những gì **chuyển sang Phase 1B**, và **danh sách việc vận hành còn lại** trước khi nghiệm thu chính thức (formal acceptance) ở cuối Phase 1B.
>
> - **Phạm vi:** Phase 1A — Sprint 00 → Sprint 10
> - **Ngày phát hành nội bộ:** 2026-06-14
> - **Đối tượng:** team nội bộ + stakeholder (BUH/Admin)
> - **Nguồn sự thật:** [PROJECT-STATE.md](./PROJECT-STATE.md), [CHANGELOG.md](./CHANGELOG.md), [07-acceptance-criteria.md](./07-acceptance-criteria.md)

---

## 1. Tóm tắt (TL;DR)

Phase 1A giao **lõi vận hành hằng ngày** của RMMS: định danh người dùng + thiết bị, chấm công có **Face Verification + GPS + chống fake-GPS**, lịch làm việc có versioning, đơn nghỉ/OT, luồng phê duyệt (kể cả BUH duyệt qua email-link không cần đăng nhập), giám sát đội nhóm theo thời gian thực và dashboard tổng quan. Notification đa kênh (in-app + push FCM + realtime SignalR) và audit log append-only đã hoạt động.

**Chưa nằm trong 1A (để Phase 1B):** Form Engine, Product Master, Visit Plan, Documents/Payslip, báo cáo đầy đủ + export Excel/CSV, và module News (hạ tầng notification đã sẵn, phần nội dung News + xác nhận đã đọc giao ở 1B).

---

## 2. Đã giao trong Phase 1A — theo module

| Module | Nội dung | AC liên quan |
|---|---|---|
| **M01 Auth & Devices** | Đăng ký qua email + verify, login JWT, refresh-rotation + reuse-detection, forgot/reset, Admin user CRUD, rate-limit login, idempotency | AC-1 |
| **M02 Devices** | 1 thiết bị active/PG (BR-105), đổi thiết bị cần Leader/Admin duyệt (BR-106, Leader-scoped) | AC-2, AC-3 |
| **M03 Organization & Assignment** | Area / Store (GPS) / Category + 3 loại assignment (PG↔Leader, User↔Store, User↔Category); store map (react-leaflet + OSM, ADR-010) | — |
| **M05 Attendance** | Check-in/out với geofence (BR-204), chặn fake-GPS (BR-205), cửa sổ sớm 60' (AC-5) + trễ (AC-6), state machine, lịch sử, lưu ảnh selfie (MinIO), Admin list + review queue, photo-retention job 90 ngày (CR-4) | AC-4, AC-5, AC-6, AC-8, AC-9, AC-10 |
| **M06 Face Verification** | CompreFace self-hosted (ADR-011), enroll 3 góc + admin enroll, verify khi check-in/out; engine-down → PendingReview (BR-207); **liveness chủ động** (ML Kit blink/smile/turn, ADR-013) | AC-7 |
| **M07 Work Schedule** | Đăng ký lịch day/week/month, versioning BR-308 (sửa lịch đã duyệt → bản mới, bản cũ vẫn hiệu lực tới khi duyệt), Leader duyệt | AC-14, AC-15 |
| **M08 Leave & OT** | Đơn nghỉ thường/khẩn cấp + OT; nghỉ khẩn cấp gắn với check-in đang mở | AC-16 |
| **M09 Approval Engine** | Hàng đợi phê duyệt generic; Leader duyệt/từ chối (AC-17); **BUH duyệt qua email-link, HMAC 24h, một lần dùng** (AC-18, BR-407); Admin override có audit (AC-19) | AC-17, AC-18, AC-19 |
| **M12 Team Monitoring** | Trạng thái hôm nay theo từng thành viên (Leader = PG quản lý, Admin/BUH = tất cả); PG online realtime | AC-26 |
| **M15 Dashboard (basic)** | Dashboard tổng quan: KPI hiện diện hôm nay (total/online/checked-out/not-checked-in/on-leave) + backlog cần xử lý (attendance chờ review, approval chờ duyệt, anomaly hôm nay) | AC-27 (cơ bản) |
| **M14 Notification** | In-app (durable) + push (FCM HTTP v1, ADR) + realtime (SignalR); CR-3 channel matrix (approval → in-app+push+email; AttendanceInReview → in-app only) | hạ tầng cho AC-33/34 |
| **M16 Admin Review + Audit** | Admin review Face-fail/GPS-violation → approve (AC-12)/reject (AC-13); audit log append-only ở mức DB (AC-35) | AC-11, AC-12, AC-13, AC-35 |

**Acceptance Criteria đã phủ trong 1A:** AC-1 → AC-19, AC-26, AC-27 (cơ bản), AC-35 — **23/35**.

---

## 3. Chuyển sang Phase 1B (chưa làm)

| Module | Nội dung | AC liên quan |
|---|---|---|
| **M10 Form Engine** | Form builder động, versioning bản publish, fill form, offline draft, edit-after-submit | AC-20, AC-21, AC-22, AC-23, AC-24 |
| **M04 Product Master** | Danh mục sản phẩm read-only trong app | AC-25 |
| **M11 Visit Plan** | Leader lập kế hoạch viếng thăm, BUH duyệt, báo cáo sau viếng thăm qua Form Engine | AC-28, AC-29, AC-30 |
| **M13 Documents** | Tài liệu public/private, gửi payslip dạng file | AC-31, AC-32 |
| **M14 News (nội dung)** | Admin gửi News + push; News quan trọng cần xác nhận đã đọc | AC-33, AC-34 |
| **M15 Reports (đầy đủ)** | Báo cáo theo khoảng ngày + filter + export Excel/CSV (Sprint 16) | mở rộng AC-27 |

**Còn lại:** AC-20 → AC-25, AC-28 → AC-34 — **12/35** (Phase 1B).

---

## 4. Hạ tầng & vận hành đã thiết lập

- **Backend:** .NET 10 LTS modular monolith (Clean Architecture, Mediator/Othamar), EF Core 10 + PostgreSQL 16 (snake_case), Redis 7, Hangfire, SignalR. **231 unit test xanh**; integration test (Testcontainers) xanh trên CI Linux.
- **Web Admin/BUH:** Next.js 14 (App Router) + Ant Design Pro + TanStack Query + Zustand + next-intl (vi/en). Type-check + lint sạch.
- **Mobile:** Flutter + Riverpod + Dio + Hive + ARB l10n (vi mặc định + en). Verify trên macOS (`flutter analyze`).
- **Infra:** Docker Compose (Postgres/Redis/MinIO/CompreFace/Seq/Caddy) trên Vultr Singapore VPS (`103.216.116.206`); MinIO + CompreFace pgdata + Postgres data **bind-mount ra host `./data`** để backup.
- **External:** CompreFace (face), SMTP/SendGrid (email), Firebase FCM (push), MinIO (object storage).
- **CI:** GitHub Actions — backend / web / mobile workflows xanh trên `main`.
- **13 ADR** (ADR-001 → ADR-013) đều ở trạng thái Accepted.

---

## 5. Giới hạn đã biết (Known limitations)

- **Không có check-in/out offline** (BR-210) — Phase 1 chỉ cho phép offline draft của form (sẽ có ở 1B cùng Form Engine).
- **Phát hiện fake-GPS không 100%** (AC-10) — Android dùng mock-location flag; iOS dựa jailbreak-detection. Admin Review là lưới an toàn.
- **Push gửi best-effort, đồng bộ** — chưa đẩy qua Hangfire async (chuyển 1B); email notification generic dùng HTML tối giản (email approval dùng template đầy đủ).
- **Push chỉ tới PG** — non-PG (Leader/BUH/Admin) chưa có device-row/fcm_token nên không nhận push (đúng BR-105 phạm vi thiết bị PG).
- **Mobile chỉ verify trên macOS** — không build được trên Windows; mọi thay đổi mobile cần Mac chạy `flutter pub get` + `build_runner` + `gen-l10n` + `analyze`.

---

## 6. Danh sách việc vận hành còn lại (trước nghiệm thu)

> Cần thiết bị thật / môi trường prod / stakeholder — **chưa hoàn tất**:

- [ ] **Bug bash** trên thiết bị thật (Android + iOS) — full luồng auth → check-in/out → approval.
- [ ] **UAT thủ công** từng AC (AC-1..AC-19, AC-26, AC-27, AC-35) với sign-off (mẫu ở [07-acceptance-criteria.md](./07-acceptance-criteria.md)).
- [ ] **Performance baseline** — đo p95 các endpoint chính trên prod.
- [ ] **Production smoke test** — sau deploy, xác nhận health + login + check-in mẫu.
- [ ] **Backup verify** — kiểm tra restore từ `./data` (Postgres + MinIO + CompreFace).
- [ ] **FCM end-to-end** — push tới thiết bị thật (cần APNs key + creds prod).
- [ ] **User guide** — hướng dẫn cho PG / Leader / Admin / BUH.

---

## 7. Bản ghi & truy vết

- Lịch sử chi tiết theo sprint: [CHANGELOG.md](./CHANGELOG.md)
- Trạng thái hiện tại: [PROJECT-STATE.md](./PROJECT-STATE.md)
- Tiêu chí nghiệm thu + mẫu sign-off: [07-acceptance-criteria.md](./07-acceptance-criteria.md)
- Quyết định kiến trúc: [decisions/ADR-001..013](./decisions/)
