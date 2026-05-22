# 00 — Project Overview

## What is RMMS 2026?

**RMMS 2026** (Retail Merchandiser Management System, 2026 rewrite) is an internal operational management system for managing **PG (Promotion Girls/Boys)** and **Leaders** working at retail stores. It consists of:

- **Mobile app** (Flutter, iOS + Android) — for PG and Leader
- **Web app** (Next.js) — for Admin and BUH

This is a **rewrite/v2** of an existing system, not a greenfield product.

## Goals — Phase 1

Build a complete operational system (NOT a minimal MVP) covering:

1. Remote attendance via check-in/check-out
2. Identity verification via Face Recognition
3. GPS validation with anti-fraud
4. Work schedule, OT, leave management
5. Approval workflow (PG → Leader → BUH)
6. Form Engine for Admin to deploy reports, surveys, tests, training, checklists
7. PG management, online status, visit planning
8. Dashboard/reports for Admin/BUH
9. Document, news, notification management

## Users (Roles)

| Role | Platform | Description |
|---|---|---|
| **PG** | Mobile | Promotion Girl/Boy — works at stores, registers via email |
| **Leader** | Mobile | Manages PGs in their region, account provisioned by Admin |
| **Admin** | Web | System administrator, full control |
| **BUH** | Web | Business Unit Head, approves Leader requests, views reports |

## Phase 1 Release Strategy

Phase 1 is split into **Phase 1A** and **Phase 1B** for staged internal release. However, **formal acceptance gate is at the end of Phase 1B** — full 35 acceptance criteria must pass.

### Phase 1A (10 sprints / ~5 months) — Core Attendance
Internal release for early UAT. Modules:
- M1 Identity & Access
- M2 Device Management
- M3 Organization & Assignment
- M5 Attendance & Anti-Fraud
- M6 Face Verification
- M7 Work Schedule
- M8 Leave & OT
- M9 Approval Workflow
- M12 Team Monitoring (basic)
- M14 News & Notification (basic)
- M16 Admin Review & Audit Log

### Phase 1B (8 sprints / ~4 months) — Forms, Visits, Documents
Builds on 1A, formal acceptance at end. Modules:
- M4 Product Master
- M10 Form Engine
- M11 Visit Plan & Execution
- M13 Document Center
- M14 News & Notification (full)
- M15 Dashboard & Reports (full)

## Out of Scope Phase 1

These are **explicitly excluded** to keep scope manageable:

- Salary calculation (payslip is delivered as file in Document Center instead)
- Beacon technology
- Target / KPI tracking
- Gifts / Promotions
- Invoice information
- Old sale report / sale revenue (if only for invoice/target/KPI)
- Migration of historical data from old system
- Full app offline mode
- Offline check-in/check-out
- Mandatory Excel import in Phase 1

## Team

- **2 Senior Developers** (generalist)
- 1 dev kiêm PM/Product Owner (-20% dev capacity)
- Dev tự design UI (UI đơn giản)
- Dev tự QA và DevOps
- No dedicated designer / QA / DevOps in Phase 1

## Tech Stack (high-level)

- **BE**: .NET 8 + EF Core + PostgreSQL 16 + Redis + Hangfire + SignalR
- **Web**: Next.js 14 + TypeScript + Ant Design Pro
- **Mobile**: Flutter 3.x + Riverpod + Dio + Hive
- **Infra**: Ubuntu 22.04 + Docker Compose + Caddy on Vultr Singapore VPS

See `02-tech-stack.md` for full details.

## Business Model

**Internal product, single customer (own company).** Therefore:
- No multi-tenancy
- No subscription/billing logic
- No SaaS pricing tiers
- Scaling concerns are minimal initially

## Success Criteria

Phase 1 is considered successful when **all 35 acceptance criteria** are met. See `07-acceptance-criteria.md`.

## Key Constraints

| Constraint | Value |
|---|---|
| Team size | 2 senior devs (1 kiêm PM) |
| Languages | Vietnamese + English (i18n required) |
| Mobile distribution | App Store + Play Store |
| Infra | Linux VPS (single VPS initially) |
| Deadline | No hard deadline (optimize for scope) |
| Budget | TBD — see Excel `06_Cost` sheet |

## Reference Documents

- **Source PRD**: AppRMMS2026.pdf (v1.0)
- **Excel plan**: `RMMS-Phase1-Plan.xlsx`
- **Architecture diagrams**: `diagrams/`

## Stakeholders

- **Product Owner**: TBD (likely 1 of the 2 devs)
- **Business sponsor**: Internal company stakeholders
- **End users**: PGs (~100s expected initially), Leaders, Admins, BUHs
