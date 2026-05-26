# ADR-001 — Modular Monolith over Microservices / .NET Aspire

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `00-overview.md` (team & scope), `02-tech-stack.md`, `03-architecture.md`, `backend/Rmms.sln`

## Context

RMMS 2026 is an internal product with the following constraints (from `00-overview.md`):

- **2 senior generalist devs** + 1 dev kiêm PM at −20% capacity → effective ~2.0 FTE.
- **No dedicated DevOps, SRE, QA, or designer.**
- **Single customer** (own company, no multi-tenancy in Phase 1) — no cross-tenant traffic to isolate.
- **Single VPS** target (Vultr Singapore) for Phase 1 — no Kubernetes cluster, no service mesh.
- **Phase 1A budget ~200 dev-days, Phase 1B ~180 dev-days.** Every operational overhead has a direct cost.
- Domain not yet stable; modules M01..M16 are likely to be refactored as feedback arrives.

During scaffold review the team evaluated three options including the `nampt102/microservice-patterns` template (.NET Aspire-based) suggested by a previous proposal.

## Decision

**Build RMMS as a Modular Monolith** organized by **Clean Architecture** layers and feature folders that match module IDs (`M01_Auth`, `M02_Stores`, …).

Concretely:

- One deployable `Rmms.Api` host + one `Rmms.Worker` (Hangfire) host. No service-per-module split.
- Module boundaries enforced at the **folder / namespace** level, not at the process boundary.
- Cross-module calls go through **MediatR-style** in-process commands/queries (see ADR-002), never via HTTP.
- Shared kernel lives in `Rmms.Domain` + `Rmms.Shared`. No "shared DTO library across services" anti-pattern because there's only one service.
- Selectively borrow patterns from `.NET Aspire` templates (Mediator, Outbox, CircuitBreaker) **as library code**, not as the application skeleton.

Database is a single Postgres instance with all module tables in one schema. Module isolation is enforced via EF Core configuration owned by each module (e.g., `M01_Auth/Infrastructure/UserConfiguration.cs`).

## Alternatives considered

1. **Microservices (one service per module)**
   - Pros: textbook scaling story; tech-stack flexibility per service.
   - Cons: requires service mesh / API gateway / distributed tracing / per-service CI/CD / cross-service DB strategy — every one of these is dev work the team cannot afford. Single customer + single VPS makes most "scale" benefits theoretical. Distributed transactions across Attendance + Form Engine + Audit Log would dominate the architecture cost.
   - **Rejected.**

2. **`.NET Aspire` template (`nampt102/microservice-patterns`) as base**
   - Pros: comes with Mediator, Outbox, CircuitBreaker patterns prewired; modern orchestration story.
   - Cons: Aspire is designed for **multi-service orchestration**, which we don't have. Adopting it as the base forces us to either fight the template (single-service Aspire app) or implement microservices anyway. Patterns we want are extractable as libraries without the orchestration overhead.
   - **Rejected as base**, **borrowed as patterns**.

3. **Modular Monolith with module-per-folder + Mediator + Outbox patterns**
   - Pros: single deploy unit; in-process calls are 1000× faster than HTTP; team size matches operational complexity; modules can be extracted later if scaling demands. Maps cleanly to Phase 1 module decomposition.
   - Cons: must enforce module boundaries by convention/review; risk of "big ball of mud" if discipline slips.
   - **Accepted.**

## Consequences

**Positive**

- One CI/CD pipeline, one Docker image, one observability target — feasible for a 2-dev team.
- All EF Core migrations live in one `Rmms.Infrastructure/Migrations/` folder → single source of truth for schema.
- In-process Mediator calls give us strong consistency for cross-module operations (e.g., Check-in writes Attendance + AuditLog in one DbContext.SaveChangesAsync).
- "Future-proof" without paying upfront cost: if a module truly needs independent scaling later, we can extract it then.

**Negative / accepted trade-offs**

- Cannot deploy modules independently — entire API redeploys for any module fix.
- Database is a single point of failure — accepted; Phase 1 has a single customer with weekday-business-hours SLA.
- Team must enforce module boundaries via PR review (no compiler-enforced isolation between modules). Mitigation: feature-folder structure + ArchUnitNET rules (post-Phase-1A backlog).

**Mitigations**

- Use Mediator handlers as the **only** cross-module call surface. Direct constructor injection of another module's services across module boundaries is a PR-blocker.
- Each module's EF Core configuration goes into a dedicated `*Configuration.cs` file inside that module's Infrastructure folder.
- If a single module's release cadence diverges sharply from the rest (e.g., Form Engine needs daily deploys while everything else is weekly), revisit this ADR.

## Revisit triggers

- Phase 2 expansion to multi-tenant or multi-customer deployment.
- A single module's load profile diverges by >10× from the rest (e.g., Attendance receives sustained >1000 req/s while other modules are <50).
- Team size grows past ~6 developers and module ownership becomes a coordination problem.
- Module isolation discipline breaks down in code review and "big ball of mud" symptoms appear (circular dependencies, shared mutable state across modules).
