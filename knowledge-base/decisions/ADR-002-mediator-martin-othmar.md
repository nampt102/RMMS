# ADR-002 — Mediator (Martin Othmar fork) replaces MediatR

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `02-tech-stack.md`, `08-coding-standards.md`, `backend/src/Rmms.Application/`, `backend/Directory.Packages.props`

## Context

The Application layer needs an **in-process request/notification mediator** to decouple controllers/Hangfire jobs from handler implementations. This is a load-bearing component — most endpoints follow the `Controller → Mediator.Send(command) → Handler → Repository` flow.

Original scaffold notes referenced **MediatR** (the de-facto .NET mediator library since ~2014). In **MediatR v12 (released 2024)**, the maintainer (Jimmy Bogard) introduced commercial licensing for organizations above a revenue threshold, with paid tiers for production use. While Phase 1 MotivesVN likely falls under the free tier, the project has a **multi-year horizon**, and license risk on a load-bearing dependency is a structural problem we want to avoid now rather than during a future audit.

Constraints:

- The replacement must be API-compatible enough that migrating to/from MediatR later costs <1 dev-day.
- Source-generator-based mediators are 10–100× faster than reflection-based dispatch; the team is willing to pay the compile-time cost.
- MIT or Apache-2.0 license preferred; copyleft (GPL/AGPL) rejected.

## Decision

**Use `Mediator` by Martin Othmar (NuGet packages `Mediator.Abstractions` + `Mediator.SourceGenerator`) as the application mediator.**

Concretely:

- `Rmms.Application.csproj` references both packages; `Mediator.SourceGenerator` set to `<PrivateAssets>all</PrivateAssets>` (analyzer only, no transitive consumption).
- `IRequest<TResponse>`, `INotification`, `IRequestHandler<TRequest, TResponse>`, `INotificationHandler<TNotification>` interfaces match MediatR's surface closely → handler bodies port over with minimal change.
- Pipeline behaviors (`IPipelineBehavior<TRequest, TResponse>`) wire `ValidationBehavior` (FluentValidation) and `LoggingBehavior` (Serilog scopes) — same shape as MediatR.
- The source generator emits a sealed `Mediator` class at compile time; no reflection-based handler lookup at runtime.

## Alternatives considered

1. **MediatR v12 (commercial license)**
   - Pros: most mature; best documentation; widest community examples.
   - Cons: commercial license adds legal/budget overhead; revenue threshold may shift; dependency on a single maintainer's commercial decisions. Performance trails source-generator mediators.
   - **Rejected on license risk.**

2. **MediatR v11 (last MIT-licensed version)**
   - Pros: free, mature, MIT.
   - Cons: no security patches going forward; eventually incompatible with future .NET versions; team-debt that grows over time.
   - **Rejected.**

3. **`Mediator` by Martin Othmar (source-generator-based, MIT)**
   - Pros: MIT license; source-generator → faster + AOT-friendly; near-identical API surface to MediatR (re-namespace migration possible); active maintainer; .NET 10 compatible.
   - Cons: smaller community → fewer Stack Overflow answers; minor API differences (e.g., `Send` vs `Send`+`Publish` distinctions); requires recompile to pick up new handlers.
   - **Accepted.**

4. **Roll our own minimal mediator (~200 LOC interface + DI registration)**
   - Pros: zero external dependency.
   - Cons: every pipeline behavior, every cancellation/exception edge case becomes our debt. The team would write the bug-fixed versions of features Martin Othmar's package already ships. Not worth the savings.
   - **Rejected.**

## Consequences

**Positive**

- No licensing cost or compliance review needed.
- Source generator emits handler dispatch code visible in `obj/` → debuggable, no reflection black box.
- ~10× lower per-call overhead vs MediatR reflection dispatch (matters at scale for Hangfire job-heavy workloads).
- Application layer code is highly portable: if Martin Othmar's package is abandoned, migrating back to MediatR (or to a fork) costs ~1 dev-day of namespace renames.

**Negative / accepted trade-offs**

- Smaller community; team will sometimes solve issues with no Stack Overflow answer to copy from.
- New handlers require a full rebuild before they're discoverable (source generator runs at compile time).
- Some MediatR conveniences (e.g., the `IRequestExceptionHandler<T>` pipeline) have slightly different shapes; team needs to consult Mediator docs once per pattern.

**Mitigations**

- Pin `Mediator.SourceGenerator` and `Mediator.Abstractions` to the same minor version in `Directory.Packages.props` to avoid analyzer/abstraction skew.
- Keep handler files small and free of mediator-specific tricks → portability stays high.
- If the package's maintenance stalls (no commit for >12 months), revisit and consider a fork or migration to a maintained alternative.

## Revisit triggers

- Martin Othmar's `Mediator` package becomes unmaintained (>12 months no commits or no .NET-N support after .NET 11 ships).
- A critical bug in the source generator that blocks production use.
- MediatR pivots back to a permissive license OR a clearly-better source-generator mediator emerges (e.g., one maintained by Microsoft) with low migration cost.
