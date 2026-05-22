# Sprint 1 — Phase 1A (W3-W4)

**Goal:** Identity & Access foundation

**Modules touched:** M1

**Acceptance criteria targeted:** AC-1

## Deliverables (Definition of Done)

- [ ] PG can register with email and verify
- [ ] All roles can login and receive JWT tokens
- [ ] Refresh token rotation working
- [ ] Password reset via email
- [ ] Login history captured

## User Stories / Key outcomes

- AC-1: PG đăng ký bằng email và đăng nhập được mobile app
- Admin can be seeded via CLI
- Logged-in users see their profile via /auth/me

## Tasks by Discipline

### BE
- [ ] User entity + DbContext
- [ ] Register endpoint with email verification
- [ ] Login endpoint
- [ ] JWT issue + validation middleware
- [ ] Refresh token endpoint with rotation
- [ ] Forgot/reset password flow
- [ ] Login history logging
- [ ] Admin seed CLI command

### Mobile
- [ ] Register screen (PG only)
- [ ] Email verification screen (open from link)
- [ ] Login screen
- [ ] Forgot password screen
- [ ] Reset password screen (from email link deep linking)
- [ ] Token storage in flutter_secure_storage
- [ ] Auto-refresh interceptor in Dio

### Web
- [ ] Login screen for Admin/BUH
- [ ] Logged-in route guard

### QA
- [ ] Auth flow test cases
- [ ] Manual test on real devices

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M1`