# Sprint 9 — Phase 1A (W19-W20)

**Goal:** Admin Review actions + Notification (basic)

**Modules touched:** M14, M16

## Deliverables (Definition of Done)

- [ ] Admin Review actions with notifications
- [ ] In-app + push + email notifications (basic events)
- [ ] FCM working end-to-end

## User Stories / Key outcomes

- AC-12, AC-13 fully wired: Admin decision → user notified
- Approval events trigger notifications

## Tasks by Discipline

### BE
- [ ] notifications entity
- [ ] INotificationService with In-App, FCM, Email adapters
- [ ] Hangfire jobs for sending
- [ ] Email templates VI+EN
- [ ] FCM token registration endpoint
- [ ] Wire all approval events to notifications

### Mobile
- [ ] Notification list + badge
- [ ] FCM setup + handler
- [ ] Deep link from notification

### Web
- [ ] Admin Review detail with actions

### QA
- [ ] Notification delivery testing
- [ ] Multi-channel correctness

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M14, M16`