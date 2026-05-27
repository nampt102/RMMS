# Architecture Decision Records (ADRs)

This folder holds **Architecture Decision Records** — short documents capturing significant technical decisions, the context that led to them, the alternatives considered, and the consequences.

## Why ADRs?

- Lock in decisions so they aren't re-debated every 3 months
- Onboard new devs (and AI tools) faster — they read the ADR instead of asking "why did we…?"
- Make trade-offs explicit so future teams can revisit when context changes

## Format (one ADR per file)

File name: `ADR-NNNN-short-slug.md` (e.g. `ADR-001-modular-monolith.md`).

Content template:

```markdown
# ADR-NNNN — <Short Title>

- **Status:** Proposed | Accepted | Superseded by ADR-NNNN | Deprecated
- **Date:** YYYY-MM-DD
- **Deciders:** <names>
- **Related:** <links to issues, PRs, modules, business rules>

## Context

<What's the problem? What forces are at play? Quote relevant numbers / constraints.>

## Decision

<One paragraph stating what we will do. Plain language.>

## Alternatives considered

1. **Option A** — pros / cons / why rejected
2. **Option B** — pros / cons / why rejected

## Consequences

- **Positive:** …
- **Negative / accepted trade-offs:** …
- **Mitigations:** …

## Revisit triggers

<Conditions under which we should re-open this decision.>
```

## ADR index

| ID | Title | Status | Date |
|---|---|---|---|
| [ADR-001](./ADR-001-modular-monolith.md) | Modular Monolith over Microservices/Aspire | **Accepted** | 2026-05-26 |
| [ADR-002](./ADR-002-mediator-martin-othmar.md) | Mediator (Martin Othmar) replaces MediatR | **Accepted** | 2026-05-26 |
| [ADR-003](./ADR-003-uuid-v7-app-generated.md) | UUID v7 app-generated PK | **Accepted** | 2026-05-26 |
| [ADR-004](./ADR-004-soft-delete-interceptor.md) | Soft delete via EF Core interceptor | **Accepted** | 2026-05-26 |
| [ADR-005](./ADR-005-snake-case-postgres.md) | snake_case PostgreSQL via EFCore.NamingConventions | **Accepted** | 2026-05-26 |
| [ADR-006](./ADR-006-postgis-deferred.md) | PostGIS deferred; NetTopologySuite for geofence | **Accepted** | 2026-05-26 |
| [ADR-007](./ADR-007-caddy-reverse-proxy.md) | Caddy as reverse proxy (auto-SSL) | **Accepted** | 2026-05-26 |
| [ADR-008](./ADR-008-tailwind-preflight-disabled.md) | Tailwind preflight disabled in web (AntD wins) | **Accepted** | 2026-05-26 |
| [ADR-009](./ADR-009-dotnet-10-lts.md) | Adopt .NET 10 LTS directly (skip .NET 8) | **Accepted** | 2026-05-24 |

> **Numbering note:** ADR-009 was authored on 2026-05-24 (during scaffold build verification) before ADR-001..008 (authored 2026-05-26). This is fine — ADR numbers are immutable once assigned and don't have to be authored in order.

## Conventions

- ADR numbers are **strictly sequential**; never reused even if superseded.
- Once **Accepted**, an ADR is **immutable**. To change a decision, write a new ADR that **Supersedes** the old one and update the old one's status field.
- Keep each ADR ≤ 2 pages. Long discussion belongs in design docs, not ADRs.
- Cite specific files / business-rule IDs / acceptance-criterion IDs so cross-references are unambiguous.
