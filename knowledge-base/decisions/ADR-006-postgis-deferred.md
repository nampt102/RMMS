# ADR-006 — PostGIS deferred; NetTopologySuite handles geofence for Phase 1

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `06-business-rules.md` (BR-201..BR-210 attendance geofence; specifically BR-204 "GPS distance > 300m from store → Admin Review"), `02-tech-stack.md`, `backend/src/Rmms.Domain/ValueObjects/GpsCoordinate.cs`, `infra/postgres/init/01-extensions.sql`

## Context

The system's only spatial computation in Phase 1 is the **distance check between a PG's check-in GPS reading and the assigned Store's GPS coordinates** (BR-204: if distance > 300m, route to Admin Review). The store has `latitude`/`longitude` columns; the check-in event includes `latitude`/`longitude` from the device. We need to compute distance reliably and consistently between:

- The mobile client (Flutter, for client-side hint UI: "you appear to be 450m from store X").
- The backend API (.NET, for the authoritative decision per BR-204 — client-side checks are advisory only because the device's GPS can be falsified).

We do **not** need (in Phase 1):

- Spatial joins (e.g., "find all stores within 2km of point P") — every check-in is tied to a specific assigned store, no nearest-neighbor query needed.
- Polygon containment (geofence shapes beyond a circle).
- Heatmaps, route tracing, complex geometries.

PostGIS is the canonical PostgreSQL extension for geospatial work. It is powerful but adds operational cost: installation, version compatibility tracking, larger Docker images, and one more thing for managed-Postgres compatibility.

## Decision

**Defer PostGIS adoption. Use `NetTopologySuite` (NTS) in .NET to compute distances in application code; store GPS as `(double precision, double precision)` columns (or a single `geography(point, 4326)` later if PostGIS is enabled).**

Concretely:

- `Rmms.Domain.ValueObjects.GpsCoordinate` is a `record struct` that holds `(double Latitude, double Longitude)` with validation (lat ∈ [−90, 90], lon ∈ [−180, 180]).
- `GpsCoordinate` exposes a `DistanceMetersTo(GpsCoordinate other)` method that uses the **Haversine formula** (great-circle distance on a sphere; accuracy ±0.5% — good enough for a 300m geofence with ±20m GPS error budget).
- The Haversine implementation lives in `Rmms.Domain.Common` so attendance handlers can call it without a database round trip.
- `NetTopologySuite` is referenced for any future spatial value-object helpers (Point creation, WKT serialization for logs); we **do not** currently use NTS's PostgreSQL plugin (no spatial columns yet).
- `infra/postgres/init/01-extensions.sql` contains `-- CREATE EXTENSION IF NOT EXISTS postgis;` (commented out). Enabling PostGIS is a single-line change when Phase 2 brings spatial queries.
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` is **already referenced** in `Directory.Packages.props` to make Phase 2 adoption a config flip — but the type mapping is not yet activated in `AppDbContext`.

## Alternatives considered

1. **Adopt PostGIS now**
   - Pros: future-proof; correct for arbitrary spatial queries; NTS↔PostGIS round-trip is well-supported.
   - Cons: PostGIS is a heavy extension (~50MB), bloats the dev/CI Postgres image; some managed-Postgres providers limit extensions; the only Phase 1 spatial operation is a one-line Haversine that doesn't need a SQL function; Phase 2 adoption is trivial when needed.
   - **Deferred to Phase 2.**

2. **Use raw SQL `earthdistance` extension (PostgreSQL contrib)**
   - Pros: built-in to standard Postgres contribs; cheap; sufficient for great-circle distance.
   - Cons: locks the computation into SQL → cannot reuse on mobile; less precise than NTS; one more extension to enable; non-standard outside Postgres.
   - **Rejected.**

3. **NetTopologySuite (Haversine in app code) — DEFER PostGIS**
   - Pros: zero Postgres extension dependency; Haversine works identically on mobile (Flutter has Haversine in `latlong2` or a 10-line implementation); accuracy ±0.5% — well within GPS error budget; computation is sub-microsecond per call; testable in unit tests without a database. Migration to PostGIS later is mechanical because NTS types map 1:1.
   - Cons: cannot do "find stores within radius R of point P" in SQL — would require loading candidate stores into memory first (acceptable: each PG is assigned a small handful of stores).
   - **Accepted.**

## Consequences

**Positive**

- Dev / CI / prod Postgres images stay small and standard (`postgres:16-alpine` works as-is).
- Same Haversine formula used by mobile, web, and backend → no rounding-difference disputes.
- Distance check runs in <10µs per call → no DB round-trip for the BR-204 decision.
- Unit tests for `GpsCoordinate.DistanceMetersTo` need no Postgres container → fast and deterministic.
- `audit_log` records the computed distance value at decision time → forensic clarity if a PG disputes a routing.

**Negative / accepted trade-offs**

- Cannot run "list stores within 1km of this point" in SQL (Phase 2 nice-to-have, not in scope). When needed, pull candidate set by city/district then filter in code.
- If Phase 2 brings polygon-based geofences (non-circular shapes), Haversine is insufficient and PostGIS becomes mandatory — that's the planned trigger to revisit.
- Haversine assumes spherical Earth (~0.5% max error on intercontinental distances; <0.1% over 1km) — adequate for store geofences, inadequate for surveying-grade work (not in scope).

**Mitigations**

- Keep `NetTopologySuite` and `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` referenced in `Directory.Packages.props` — Phase 2 enablement is just `o.UseNetTopologySuite()` on the Npgsql options builder + the `CREATE EXTENSION` uncomment.
- Document the Phase 2 enablement steps in `infra/postgres/init/01-extensions.sql` as a comment.
- Store GPS as two `double precision` columns now → migrating to `geography(point, 4326)` later is a single ALTER TABLE migration.

## Revisit triggers

- Phase 2 introduces non-circular geofences (polygons, multi-store coverage zones).
- A new feature requires spatial joins (e.g., "all check-ins within 1km of address X" for fraud investigation).
- Managed-Postgres provider migration where PostGIS is included by default with no extra cost (then there's no longer a reason to defer).
- Haversine accuracy is demonstrably insufficient (would require GPS device improvements beyond consumer-grade — not foreseeable in scope).
