# M11_VISIT_PLAN — Visit Plan & Execution

## Quick Reference

| | |
|---|---|
| **Module ID** | M11 |
| **Phase** | 1B |
| **Priority** | P0 |
| **Complexity** | Medium |
| **Est. dev-days** | 20 |
| **Sprints** | S14 |
| **Depends on** | M1, M3, M9, M10 |
| **Acceptance criteria** | AC-28, AC-29, AC-30 |

## Purpose

Leader lập kế hoạch viếng thăm cửa hàng, BUH duyệt, Leader thực hiện và báo cáo.

## Scope (Phase 1)

- Leader tạo Visit Plan: date + stores + per-store tasks/forms
- Gửi BUH duyệt (Approval Workflow)
- BUH duyệt qua web hoặc email link
- Leader nhận notification kết quả
- Sau visit: Leader nộp post-visit report cho mỗi store
- Report dùng Form Engine (form template 'Visit Report')
- Lịch sử Visit Plan
- Status: pending / approved / rejected / executed

## Data Entities

- `visit_plans`
- `visit_plan_items`

See `04-data-model.md` for column-level definitions.

## API Endpoints

- `POST /api/v1/visit-plans — create`
- `PATCH /api/v1/visit-plans/:id — edit pending`
- `GET /api/v1/visit-plans/me — Leader's plans`
- `POST /api/v1/visit-plans/:id/items/:itemId/execute — link to form submission`
- (approval via M9)
- `GET /api/v1/admin/visit-plans — Admin view all`

See `05-api-conventions.md` for shared request/response patterns.

## Screens

### Mobile (Leader)
- Visit Plans list
- Create Visit Plan wizard
- Visit Plan detail with stores+tasks
- Visit execution: per-store form filling
- Visit history

### Web BUH
- Visit Plans approval list
- Visit Plan detail with map

### Web Admin
- All visit plans view

## Business Rules Applied

- (No specific decision tables; standard CRUD logic)

See `06-business-rules.md` for rule details.

## Edge Cases

- Edit plan after approved → typically not allowed; document policy
- Visit not executed by date → mark as 'missed' (Phase 2)
- Stores added after approval → re-approval needed (Phase 2)

## Key Implementation Notes

- Re-use M9 Approval Workflow for BUH approval flow
- Visit Report form linked via `form_submission_id` in visit_plan_items
- Status `executed` set when all items have form submissions

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
