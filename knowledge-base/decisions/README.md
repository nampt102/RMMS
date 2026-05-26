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
| _ADR-001_ | _Modular Monolith over Microservices/Aspire_ | _Planned_ | _—_ |
| _ADR-002_ | _Mediator (Martin Othmar) replaces MediatR_ | _Planned_ | _—_ |
| _ADR-003_ | _UUID v7 app-generated PK_ | _Planned_ | _—_ |
| _ADR-004_ | _Soft delete via EF Core interceptor_ | _Planned_ | _—_ |
| _ADR-005_ | _snake_case PostgreSQL via EFCore.NamingConventions_ | _Planned_ | _—_ |
| _ADR-006_ | _PostGIS deferred; NetTopologySuite for geofence_ | _Planned_ | _—_ |
| _ADR-007_ | _Caddy as reverse proxy (auto-SSL)_ | _Planned_ | _—_ |
| _ADR-008_ | _Tailwind preflight disabled in web (AntD wins)_ | _Planned_ | _—_ |

When an ADR is authored, replace its row with a real link and date.

## Conventions

- ADR numbers are **strictly sequential**; never reused even if superseded.
- Once **Accepted**, an ADR is **immutable**. To change a decision, write a new ADR that **Supersedes** the old one and update the old one's status field.
- Keep each ADR ≤ 2 pages. Long discussion belongs in design docs, not ADRs.
- Cite specific files / business-rule IDs / acceptance-criterion IDs so cross-references are unambiguous.
