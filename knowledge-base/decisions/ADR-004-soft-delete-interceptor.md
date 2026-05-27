# ADR-004 — Soft delete enforced by EF Core SaveChangesInterceptor

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `06-business-rules.md` (CR-1 audit log, user-deletion rule), `08-coding-standards.md`, `backend/src/Rmms.Domain/Common/AuditableEntity.cs`, `backend/src/Rmms.Infrastructure/Persistence/AuditableEntityInterceptor.cs`

## Context

`06-business-rules.md` is unambiguous:

> User deletion: soft delete + retain attendance/audit records for compliance.

CR-1 also requires that "delete" actions (form delete, schedule delete, …) appear in the audit log — i.e., we need an after-the-fact record that the row existed. Hard deletes destroy that evidence.

The pragmatic question is **where** to enforce soft delete. Three layers are candidates:

1. Per-aggregate domain logic — every `Delete()` method on every aggregate sets `DeletedAt`.
2. Repository layer — repositories rewrite `Remove` calls into updates.
3. EF Core interceptor — single `SaveChangesInterceptor` rewrites `EntityState.Deleted` → `EntityState.Modified` with `DeletedAt = DateTimeOffset.UtcNow`.

We also need:

- A `IsDeleted` query filter so soft-deleted rows are invisible by default in every query.
- An escape hatch for legitimate hard-delete cases (e.g., GDPR right-to-erasure, future Phase 2).

## Decision

**Implement soft delete via a single EF Core `SaveChangesInterceptor` (`AuditableEntityInterceptor`) that rewrites `EntityState.Deleted` to `EntityState.Modified` and stamps `DeletedAt` on any entity that inherits from `AuditableEntity`.**

Concretely:

- `AuditableEntity` (in `Rmms.Domain.Common`) exposes `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `DeletedAt` (nullable), `DeletedBy` (nullable).
- `AuditableEntityInterceptor` overrides `SavingChangesAsync` and:
  - For `EntityState.Added` → stamps `CreatedAt`/`CreatedBy`.
  - For `EntityState.Modified` → stamps `UpdatedAt`/`UpdatedBy`.
  - For `EntityState.Deleted` on `AuditableEntity` → flips to `Modified`, stamps `DeletedAt`/`DeletedBy`.
- `AppDbContext.OnModelCreating` applies a global query filter `e => e.DeletedAt == null` to every entity implementing `IAuditableEntity`.
- A deliberate hard delete uses `dbContext.Database.ExecuteSqlAsync(...)` or a dedicated `IHardDeleteService.Force(...)` API — never accidentally via `Remove()`.

Tables that **must not** participate in soft delete (e.g., `audit_log` — append-only at DB level per `06-business-rules.md`; `refresh_tokens` — rotation discards the prior token) inherit from plain `Entity<TGuid>` instead of `AuditableEntity`.

## Alternatives considered

1. **Per-aggregate domain method (`User.SoftDelete(actor)` etc.)**
   - Pros: explicit; domain-driven; no "magic" in infrastructure.
   - Cons: a developer can still call `_db.Users.Remove(user)` and bypass the rule — no enforcement. Audit-log requirement (CR-1) means "missed soft-delete" is a compliance bug, not just a feature bug. Repetition across every aggregate.
   - **Rejected** as the sole mechanism (kept as an optional convenience layer).

2. **Repository-layer rewrite (every repo's `Delete` method updates instead of removes)**
   - Pros: one place per aggregate.
   - Cons: every repo is its own enforcement point; a future PR can add a new repo and forget the rewrite. Also requires us to have per-aggregate repositories at all (we use `DbContext` directly in handlers for simple cases).
   - **Rejected.**

3. **EF Core `SaveChangesInterceptor` (single enforcement point)**
   - Pros: one piece of code enforces the rule for every aggregate that inherits `AuditableEntity` — opt-in by inheritance; impossible to "forget" without explicitly removing the inheritance. Combined with the global query filter, soft-deleted rows are invisible by default. Auditable stamps (CreatedAt/UpdatedAt/DeletedAt) also flow through the same interceptor, reducing duplication.
   - Cons: bulk operations via `ExecuteDelete()` / `ExecuteUpdate()` bypass interceptors — those calls must be reviewed manually. Interceptors run inside `SaveChangesAsync` → small per-call overhead (negligible at our scale).
   - **Accepted.**

## Consequences

**Positive**

- Single source of truth for soft-delete semantics → low risk of regression.
- `CreatedAt`/`UpdatedAt`/`DeletedAt`/`*By` stamps all flow through the same interceptor → no scattered timestamp logic.
- Global query filter means controllers/handlers/Mediator pipelines never have to remember "filter out deleted" — that's the default.
- Easy to write integration tests: assert that `Remove(...)` on `AuditableEntity` results in a row with `DeletedAt` set, not a missing row.

**Negative / accepted trade-offs**

- `ExecuteDelete()` and `ExecuteUpdate()` (EF Core 7+ bulk operations) bypass `SaveChangesAsync` and therefore the interceptor → bulk operations must be reviewed for soft-delete compliance.
- Soft-deleted rows continue to consume table space; periodic GDPR/retention purges are a separate background job (deferred to post-Phase-1A).
- Global query filter can produce surprising query plans on large tables (e.g., `IsDeleted` predicate hidden in every query). Mitigation: add partial indexes `WHERE deleted_at IS NULL` for hot tables.
- Filtered indexes interact with EF Core query filters; this requires careful migration script review.

**Mitigations**

- Add a **roslyn analyzer** (or PR-template check) that flags any `ExecuteDelete()` / `ExecuteUpdate()` calls and requires explicit ADR-004 acknowledgment in PR description.
- Document the hard-delete escape hatch in `08-coding-standards.md` (currently `IHardDeleteService` is a placeholder; flesh out in M01 hardening sprint).
- For high-volume tables (`audit_log`, `attendance_records`) where soft-delete is undesired, do not inherit from `AuditableEntity` — `audit_log` is already excluded by design (append-only).

## Revisit triggers

- GDPR / Vietnamese personal-data-protection law requires hard-delete for specific entities — then add a per-entity carve-out, not a system-wide change.
- Bulk delete operations become a measured hot path AND the interceptor's per-row overhead becomes meaningful (currently it isn't).
- Query plans on large tables show repeated penalty from the `DeletedAt IS NULL` filter → introduce partial indexes (does not require revising this ADR).
