# ADR-005 — snake_case Postgres naming via `EFCore.NamingConventions`

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `02-tech-stack.md` (Database section, lines 218–222), `08-coding-standards.md` (lines 22–25, 323–324), `backend/src/Rmms.Infrastructure/Persistence/AppDbContext.cs`, `backend/Directory.Packages.props` (`EFCore.NamingConventions`)

## Context

C# code uses **PascalCase** for class names, properties, and (usually) DB column properties: `User`, `CreatedAt`, `PhoneNumber`. PostgreSQL convention is **snake_case** for identifiers: `users`, `created_at`, `phone_number` — and importantly, **unquoted identifiers in PostgreSQL are folded to lowercase**, so `"CreatedAt"` and `"createdat"` are different objects.

`02-tech-stack.md` already documents the chosen convention:

| Item | Pattern | Example |
|---|---|---|
| Tables | snake_case, plural | `users`, `attendance_records` |
| Columns | snake_case | `created_at`, `user_id` |
| PK column | always `id` | `id` |

Three approaches to enforce this:

1. Manually annotate every entity with `[Table("...")]` and every property with `[Column("...")]`.
2. Centrally configure each entity in `IEntityTypeConfiguration<T>` with `.ToTable("users")`, `.Property(x => x.CreatedAt).HasColumnName("created_at")`.
3. Use a naming convention plugin that transforms all PascalCase identifiers to snake_case at model-build time.

## Decision

**Use `EFCore.NamingConventions` package with `UseSnakeCaseNamingConvention()` applied to `AppDbContext`.**

Concretely:

- `Rmms.Infrastructure.csproj` references `EFCore.NamingConventions`.
- `AppDbContext.OnConfiguring` (or its DI registration in `Rmms.Infrastructure/DependencyInjection.cs`) calls `.UseSnakeCaseNamingConvention()` on the `DbContextOptionsBuilder` chain after `.UseNpgsql(...)`.
- Entity classes stay PascalCase as idiomatic C#; the convention plugin handles the translation to snake_case for table names, column names, index names, FK names, and check-constraint names.
- Manual `.ToTable("xxx")` / `.HasColumnName("xxx")` is reserved for **exceptions** — e.g., entity name that's an SQL reserved word, or matching an externally-defined table name.

## Alternatives considered

1. **Manual `[Table]` / `[Column]` annotations on every entity**
   - Pros: explicit; visible in the entity definition.
   - Cons: tedious; bug-prone (one missed annotation produces `"CreatedAt"` in DB, surprises everyone); pollutes the domain model with persistence concerns.
   - **Rejected.**

2. **Per-entity `IEntityTypeConfiguration<T>` with explicit `.HasColumnName(...)` calls**
   - Pros: centralized per entity; lives in Infrastructure layer (not Domain).
   - Cons: still hundreds of lines of boilerplate; same risk of forgetting; doesn't transform index names or FK names automatically.
   - **Rejected** as the *primary* mechanism (kept as the escape hatch for exceptions).

3. **`EFCore.NamingConventions` plugin (`UseSnakeCaseNamingConvention()`)**
   - Pros: one line of code; transforms tables + columns + indexes + FKs uniformly; works retroactively when new entities are added (zero per-entity work); maintained by the EF Core community; tracks EF Core major versions closely (10.0.0 supports EF Core 10).
   - Cons: third-party dependency (small, MIT-licensed); applies to **all** entities — opting one entity out requires manual override.
   - **Accepted.**

## Consequences

**Positive**

- New entities automatically get snake_case translation with zero per-entity work.
- Migration files use snake_case names, which match what a DBA querying the database expects.
- Query logs (Serilog SQL output, pg_stat_statements) show snake_case → readable for ops / DBA / non-.NET team members.
- Domain layer stays clean — no `[Table]` / `[Column]` attribute clutter on classes that should be pure POCOs.

**Negative / accepted trade-offs**

- One additional dependency (`EFCore.NamingConventions`) tracked in CPM; bump version when EF Core majors bump.
- If we ever need to **override** a single column name (e.g., to match an external schema), it's a manual `.HasColumnName(...)` that fights the convention — slightly more confusing than if conventions never applied. Acceptable for the rare exception.
- Migration scripts written by hand (raw SQL) must remember to use snake_case manually — no plugin help.

**Mitigations**

- Document the override pattern in `08-coding-standards.md` (when to use `.HasColumnName(...)` and `.ToTable(...)`).
- Periodic audit: `grep -r "HasColumnName\|ToTable" Rmms.Infrastructure/` and review each occurrence's justification during code review.
- CI workflow runs `dotnet ef migrations bundle` (or equivalent script) on PRs that touch entities — diffs surface unexpected naming.

## Revisit triggers

- `EFCore.NamingConventions` stops releasing for a new EF Core LTS major (no `10.0.x` package for EF Core 10, for example).
- Project pivots to integrating heavily with an externally-controlled PostgreSQL schema that doesn't follow snake_case (e.g., third-party data warehouse) — partial override might exceed plugin's flexibility.
- A future PostgreSQL feature (e.g., case-insensitive identifier collation) makes the snake_case-vs-PascalCase distinction irrelevant (extremely unlikely).
