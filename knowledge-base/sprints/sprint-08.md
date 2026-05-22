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
- [ ] Team Monitoring query/view
- [ ] audit_logs entity + service
- [ ] EF Core interceptor or explicit logging
- [ ] Audit log query endpoints
- [ ] DB role with INSERT/SELECT only on audit

### Mobile
- [ ] PG Online list (Leader)

### Web
- [ ] Team Monitoring dashboard
- [ ] Audit Log explorer

### QA
- [ ] Status accuracy across edge cases
- [ ] Audit log completeness

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on `M12, M16`