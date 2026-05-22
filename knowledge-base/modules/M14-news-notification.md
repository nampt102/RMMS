# M14_NEWS_NOTIFICATION — News & Notification

## Quick Reference

| | |
|---|---|
| **Module ID** | M14 |
| **Phase** | 1A (basic), 1B (full) |
| **Priority** | P0 (basic) / P1 (full) |
| **Complexity** | Medium |
| **Est. dev-days** | 22 |
| **Sprints** | S9, S15 |
| **Depends on** | M1 |
| **Acceptance criteria** | AC-33, AC-34 |

## Purpose

Gửi tin tức, thông báo và nhắc việc cho người dùng.

## Scope (Phase 1)

**News:**
  - Admin tạo tin tức (bilingual title + content)
  - Phân loại (category)
  - Gán cho user / role
  - Đánh dấu đã đọc
  - Important news cần xác nhận read (user click confirm)
  - Badge trong app khi có unread
**Notification channels:**
  - In-app notification (DB + badge)
  - Push notification (FCM)
  - Email notification (SendGrid)
**Events triggering notifications (see business rules CR-2):**
  - New form / form deadline / form past due
  - Approval needed / approval decision
  - Device change request
  - New document / payslip
  - New news / important news
  - Attendance sent to Admin Review

## Data Entities

- `news`
- `news_assignments`
- `news_reads`
- `notifications`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/admin/news`
- `GET /api/v1/news/me — assigned news (paginated)`
- `POST /api/v1/news/:id/read`
- `POST /api/v1/news/:id/confirm — for important news`
- `GET /api/v1/notifications/me — paginated notifications`
- `POST /api/v1/notifications/:id/read`
- `POST /api/v1/notifications/read-all`
- `PUT /api/v1/users/me/fcm-token — register FCM token`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (PG/Leader)
- News list (with category filter)
- News detail (with confirm button for important)
- Notifications list
- Notification badge in tab bar

### Web Admin
- News editor (bilingual)
- Notification settings

## Business Rules Applied

- CR-2
- CR-3

See `06-business-rules.md` for rule details.

## Edge Cases

- User without FCM token (e.g., notifications disabled) → in-app only
- Email delivery failure → retry via Hangfire (3 attempts)
- Important news: cannot dismiss until confirmed
- Notification spam protection: batch by Hangfire if >5 in 10min for same user

## Key Implementation Notes

- Notification service: abstract `INotificationService` with adapters (InApp, Push, Email)
- Channel routing per CR-3 table
- Bilingual email templates: pick by user's preferred_language
- Push payload includes deep link (e.g., `rmms://attendance/123`)
- Background job processes notification queue from Hangfire
- Phase 1A basic: in-app + approval push only. Phase 1B full: all events all channels.

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
