# Sprint 8 — Phase 1A (W17-W18)

**Goal:** Team Monitoring + Audit Log infrastructure

**Modules touched:** M12, M16

**Acceptance criteria targeted:** AC-26, AC-27, AC-35

## Deliverables (Definition of Done)

- [ ] PG/Leader Online status calculation + list
- [ ] Audit log captures critical actions
- [ ] Audit log viewer for Admin

## User Stories / Key outcomes

- AC-26: Leader views PG Online
- AC-27: Admin/BUH see online list and check-in today
- AC-35: Audit log captures critical actions

## Tasks by Discipline

### BE
- [x] Team Monitoring query — `GET /api/v1/team-monitoring/today` (status per member + summary counts; scope Admin/BUH=all, Leader=managed PGs); status from attendance+schedule+leave (working/checked_out/not_checked_in/on_leave/no_schedule_today/pending_review)
- [x] audit_logs entity + service — **already shipped in M01** (`AuditLog` append-only + `DbAuditLogger`, used across all modules per CR-1)
- [x] EF logging — explicit `IAuditLogger.RecordAsync` per handler (atomic with the business change)
- [x] Audit log query endpoint — `GET /api/v1/admin/audit-logs` (filter action/entity/actor/date, paginated, actor-name join)
- [x] DB append-only — `REVOKE UPDATE, DELETE ON audit_log` applied in the initial migration (M01)
- [x] 5 unit tests (team status + audit filter/order) → suite **220 green**

### Mobile
- [ ] PG Online list (Leader)

### Web
- [x] Team Monitoring dashboard — `/monitoring` (summary cards + member table + manual refresh + as-of; visible to Admin/Leader/BUH)
- [x] Audit Log explorer — `/audit-logs` (Admin-only ProTable: filter action/entity, actor name, metadata tooltip)
- [x] Role-scoped nav + redirect updated (Leader/BUH can reach /approvals + /monitoring)

### QA
- [ ] Status accuracy across edge cases
- [ ] Audit log completeness

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M12, M16`