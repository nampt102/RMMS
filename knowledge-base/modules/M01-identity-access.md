# M01_IDENTITY_ACCESS — Identity & Access

## Quick Reference

| | |
|---|---|
| **Module ID** | M01 |
| **Phase** | 1A |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 18 |
| **Sprints** | S1 |
| **Depends on** | — |
| **Acceptance criteria** | AC-1, AC-2 |

## Purpose

Quản lý đăng ký, đăng nhập, phân quyền cho PG, Leader, Admin, BUH.

## Scope (Phase 1)

- PG đăng ký tài khoản bằng email (có email verification)
- Leader được Admin cấp tài khoản
- BUH được Admin cấp tài khoản
- Admin được cấp tài khoản (seed initial qua CLI)
- Đăng nhập / đăng xuất tất cả roles
- JWT access token (15min) + refresh token (30d) với rotation
- Quản lý trạng thái tài khoản (active/inactive/pending_email_verify)
- Phân quyền theo role (RBAC đơn giản)
- Ghi nhận login history (success/fail, IP, UA)
- Quên mật khẩu / reset mật khẩu qua email

## Data Entities

- `users`
- `refresh_tokens`
- `login_history`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/auth/register — PG email registration`
- `POST /api/v1/auth/verify-email — confirm email`
- `POST /api/v1/auth/login — returns access+refresh`
- `POST /api/v1/auth/refresh — rotate tokens`
- `POST /api/v1/auth/logout — invalidate refresh`
- `POST /api/v1/auth/forgot-password — send reset email`
- `POST /api/v1/auth/reset-password — apply new password`
- `GET /api/v1/auth/me — current user info`
- `POST /api/v1/admin/users — Admin creates Leader/BUH/Admin`
- `PATCH /api/v1/admin/users/:id — update user status`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- Register screen (PG only)
- Login screen
- Forgot password
- Reset password (via email link)

### Web Admin
- Login screen
- User management (CRUD)
- User detail with status toggle

### Web BUH
- Login screen

## Business Rules Applied

- BR-101
- BR-102
- BR-103
- BR-104

See `06-business-rules.md` for rule details.

## Edge Cases

- Email verification link expires (24h) → user can request new link
- User tries to login while pending verification → block with clear message
- Admin deactivates user → existing refresh tokens revoked
- Brute force protection: 5 failed attempts → 15min lockout

## Key Implementation Notes

- Password: bcrypt cost 12
- Email verification token: random 32 bytes, 24h TTL, single-use
- Reset password token: same as verification
- Refresh token rotation: store hashed, mark old as revoked on rotation
- First Admin: seed via CLI command `dotnet run seed-admin --email=...`

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
