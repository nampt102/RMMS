# M06_FACE_VERIFICATION — Face Verification

## Quick Reference

| | |
|---|---|
| **Module ID** | M06 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | High |
| **Est. dev-days** | 22 |
| **Sprints** | S4 |
| **Depends on** | M1, M5 |
| **Acceptance criteria** | AC-7, AC-8, AC-11, AC-12, AC-13 |

## Purpose

Xác định đúng PG/Leader khi chấm công, tránh check-in hộ.

> **Engine: self-hosted CompreFace (ADR-011), replacing FPT.AI.** Biometric embeddings live
> inside CompreFace on our own VPS; our DB stores only the CompreFace `subject` id. A
> config-gated dev face client keeps the flow exercisable without the service (tests/CI/local).

## Scope (Phase 1)

- PG tự enroll khuôn mặt lần đầu (3 ảnh: front, left, right) → đẩy vào CompreFace dưới `subject = userId`
- Embedding (template) lưu trong **CompreFace self-hosted**; DB chỉ giữ `subject` id (`users.face_template_external_id`)
- Face verification BẮT BUỘC tại check-in/out (CompreFace `recognize`: khớp khi `subject==userId` và `similarity ≥ ngưỡng 0.85`)
- Capture selfie HD (1080p min) tại thời điểm verify; selfie gốc lưu MinIO (TTL 90 ngày, CR-4)
- Nếu fail → attendance `FaceFailPendingReview` (Admin Review queue, không tạo bảng riêng — dùng status-driven của M05)
- Admin xem selfie + so sánh với enrolled photo (lấy từ CompreFace/MinIO)
- Admin action: confirm correct → attendance success / confirm wrong → no attendance
- Re-enroll face khi cần (Admin trigger → xoá subject, buộc enroll lại)

## Data Entities

- (uses `users.face_template_external_id` = CompreFace subject id, `users.face_enrolled_at`)
- No new table — embeddings are owned by CompreFace; review uses the M05 status-driven queue.

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/face/enroll — multipart, 3 photos, returns template id`
- `POST /api/v1/face/verify — multipart, 1 photo, returns match result`
- `POST /api/v1/admin/face/re-enroll/:userId — trigger user to re-enroll`
- `DELETE /api/v1/admin/face/template/:userId — remove enrollment`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Face enrollment wizard (3 angles)
- Face verify during check-in/out (auto)
- Re-enrollment prompt if requested by Admin

### Web Admin
- Face fail review screen (selfie vs enrolled)
- User face status overview

## Business Rules Applied

- BR-206
- BR-207
- BR-208
- BR-209

See `06-business-rules.md` for rule details.

## Edge Cases

- Face API down → attendance goes to pending_review with note
- PG wears mask → likely fail → Admin review picks up
- Low light (dark store) → fail → Admin review
- PG enrolled with different appearance (haircut, glasses) → re-enroll option
- Twins / siblings (rare) → confidence threshold needed, fall to Admin review

## Key Implementation Notes

- **Engine: CompreFace (self-hosted, ADR-011)** — Docker service on the VPS, internal network only (not proxied by Caddy). Backend calls it via the `IFaceClient` port; a deterministic **dev face client** is used when `CompreFace:ApiKey` is unset (tests/CI/local), real `CompreFaceClient` when configured.
- Endpoints used: enroll `POST /api/v1/recognition/faces?subject=<userId>`; verify `POST /api/v1/recognition/recognize`; remove `DELETE /api/v1/recognition/subjects/<userId>` (header `x-api-key`, multipart `file`).
- Confidence threshold: **0.85** default — tune during S4. Match requires top `subject == userId`.
- Latency budget: <2s (CPU model). If missed → enable GPU model or scale the CompreFace core.
- Privacy: face **embeddings stored inside CompreFace** on our infra, NOT in our PostgreSQL (DB keeps only the subject id). Raw selfies in MinIO, 90-day TTL (CR-4).
- Resource: CompreFace CPU model ≈ 3–4 GB RAM + its own Postgres → verify VPS headroom before enabling the `face` compose profile.
- Face API down → attendance → `pending_review` (never a hard failure).
- Liveness/anti-spoofing: CompreFace plugins if needed; else manual via Admin review.

## Definition of Done

This module is considered DONE when:
- [ ] All endpoints implemented and documented in Swagger
- [ ] Unit tests cover happy path + error cases (≥70%)
- [ ] Integration tests via Testcontainers for critical flows
- [ ] Mobile/Web screens implemented per spec
- [ ] i18n strings present for both `vi` and `en`
- [ ] Acceptance criteria listed above pass manual verification
- [ ] Audit log entries for relevant actions (see CR-1)
- [ ] PR reviewed and merged
- [ ] Deployed to staging and smoke-tested
