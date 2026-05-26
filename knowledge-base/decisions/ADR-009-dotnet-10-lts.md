# ADR-009 — Adopt .NET 10 LTS directly (skip .NET 8)

- **Status:** Accepted
- **Date:** 2026-05-24
- **Deciders:** Tech lead (MotivesVN IT), with input from initial scaffold review session
- **Related:** `02-tech-stack.md`, `backend/global.json`, `Directory.Build.props`, `backend/Directory.Packages.props`, `Dockerfile`s
- **Supersedes / amends:** Original tech-stack decision to use .NET 8 (recorded informally in `02-tech-stack.md` prior to this ADR; this ADR formally amends that choice)

## Context

The initial scaffold (committed 2026-05-24) targeted **.NET 8 LTS** because that was the LTS version when the knowledge base was authored (May 2026 documents referenced ".NET 8.0 LTS support until November 2026, then .NET 10 LTS path"). When the tech lead attempted to build the scaffold locally, the build failed:

```
A compatible .NET SDK was not found.
Requested SDK version: 8.0.100
global.json file: D:\WORKING\SOURCECODE\RMMS\backend\global.json
Installed SDKs:
3.1.426, 5.0.408, 6.0.428, 7.0.410, 9.0.205, 9.0.313
```

The dev machine had .NET 9 SDKs but no .NET 8 SDK. .NET 10 LTS was already released (Nov 2025), bringing the choice into sharper relief:

- Phase 1A finishes ~October 2026; Phase 1B finishes ~February 2027.
- **.NET 8** LTS support ends **November 2026** — mid-Phase 1B. Forces a forced migration during the last phase.
- **.NET 9** is STS (18 months); support ends ~May 2026 — basically already EOL.
- **.NET 10** LTS support ends **November 2028** — covers all of Phase 1 + ~1 year of post-launch operations.

The team has 2 senior generalist devs and no dedicated DevOps. Any framework migration mid-project costs ~3–5 dev-days plus regression risk.

## Decision

**Skip .NET 8 entirely. Build RMMS on .NET 10 LTS from day 1.**

Concretely:
- `backend/global.json` pins SDK `10.0.100`.
- All `.csproj` and `Directory.Build.props` target `net10.0` with `LangVersion=latest`.
- Microsoft-owned NuGet packages (`Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.Extensions.*`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`) pinned at `10.0.0` (bump patches as they ship via `dotnet outdated`).
- Docker base images bump to `mcr.microsoft.com/dotnet/{aspnet,sdk}:10.0`.
- Worker container switches from `runtime:8.0` to `aspnet:10.0` because `Hangfire.AspNetCore` requires the ASP.NET Core shared framework.

Third-party packages (Hangfire, Serilog, Polly, FluentValidation, Mediator, Mapster, SendGrid, MinIO, FirebaseAdmin, NetTopologySuite, QuestPDF, ClosedXML, xUnit, Testcontainers, Bogus, Moq) remain at their current versions — all target `netstandard2.0` or already declare `net10.0` support and need no immediate change.

## Alternatives considered

1. **Install .NET 8 SDK on dev machines, keep scaffold as-is**
   - Pros: zero code change, exactly matches original knowledge-base spec.
   - Cons: Forced migration to .NET 10 around Phase 1A→1B transition; that's 3–5 dev-days + regression test cost during the most critical phase. Two LTS upgrades in 18 months instead of one.
   - **Rejected.**

2. **Update scaffold to .NET 9**
   - Pros: dev already has .NET 9 installed → zero install friction.
   - Cons: .NET 9 is STS, EOL ~May 2026 (i.e., **now**). Would force another migration to .NET 10 within weeks. Worst possible choice.
   - **Rejected hard.**

3. **Adopt .NET 10 LTS now**
   - Pros: single LTS for entire Phase 1; covers operations through Nov 2028; .NET 10 packages stable (6 months since GA); no future forced migration in scope.
   - Cons: ~30 min of file edits + knowledge-base updates; need to install .NET 10 SDK (already done by tech lead in this session); slight risk a third-party package lags .NET 10 support (we audited — none in our stack do).
   - **Accepted.**

## Consequences

**Positive**

- Zero framework migration during Phase 1.
- Access to .NET 10 features (e.g., improved AOT, latest minimal API extensions, BCL improvements) without waiting.
- Docker images smaller and faster on Linux because Ubuntu 24.04 (`noble`) base in modern .NET tags.
- C# `latest` (C# 14) unlocks improved pattern matching, primary constructors elsewhere, etc.

**Negative / accepted trade-offs**

- Slight risk one of the third-party libs (e.g., `Hangfire.PostgreSql`, `Mediator.SourceGenerator`) lags on .NET 10 patch compatibility. Mitigation: pin specific patch versions in `Directory.Packages.props`; track via `dotnet outdated`.
- Knowledge base needed updates (`02-tech-stack.md`, `PROJECT-STATE.md`, `CHANGELOG.md`) — done in the same session as this ADR.
- New devs joining the project must install .NET 10 SDK specifically (cannot reuse a global .NET 8 install).

**Mitigations**

- `global.json` pinned to `10.0.100` with `rollForward: latestFeature` — local builds tolerate patch upgrades, blocked from feature-band upgrades.
- CI workflow (when authored — see Sprint 00 outstanding work) will use `actions/setup-dotnet@v4` with `dotnet-version: 10.0.x`.
- A 6-month dependency audit (run `dotnet list package --outdated` and `dotnet list package --vulnerable`) is added to the Sprint 06 backlog.

## Revisit triggers

- A critical security CVE in .NET 10 with no patch.
- A required third-party library drops .NET 10 support before .NET 11 LTS arrives (highly unlikely; .NET 11 LTS is the next milestone in Nov 2027).
- Microsoft EOL announcement changes for .NET 10 (extremely unlikely — LTS commitment is contractual).
