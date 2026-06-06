# ADR-010 — react-leaflet + OpenStreetMap for the store map view

- **Status:** Accepted
- **Date:** 2026-06-06
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `modules/M03-organization-assignment.md`, `sprints/sprint-02.md` (deferred "Store map view"), `web/src/features/organization/StoreMap.tsx`, ADR-006 (PostGIS deferred), ADR-008 (Tailwind preflight off)

## Context

M03 ships store masters with `latitude` / `longitude` columns and a CRUD ProTable. Sprint 02 deferred the **interactive store map view** because Ant Design Pro has no map component and the CLAUDE.md rule forbids introducing a new UI library without an ADR.

Requirements for the map view (Phase 1, Admin/BUH web only):

- Plot every store as a marker from its `latitude` / `longitude`.
- Distinguish `active` vs `inactive` stores visually (status colors).
- Show a popup with `code` / `name` / `status` on marker click.
- Auto fit-bounds to the visible store set.
- Toggle between the existing table and the map.
- No per-request billing, no mandatory API key (internal tool, single VPS, cost-sensitive 2-dev team).
- Respect `prefers-reduced-motion` and WCAG AA contrast (consistent with the rest of the admin UI).

PostGIS is deferred (ADR-006); coordinates are plain `numeric` columns, so the map is a **pure client-side rendering** concern — no spatial queries are needed.

Three credible options: **react-leaflet + OpenStreetMap**, **Google Maps (`@react-google-maps/api`)**, **Mapbox GL JS**.

## Decision

**Use `react-leaflet` (v4) with `leaflet` (v1.9) rendering OpenStreetMap raster tiles for the store map view.** The map is a client-only component (`next/dynamic` with `ssr: false`), markers are status-colored `L.divIcon` HTML circles (no bundled image assets), and Leaflet's stylesheet (`leaflet/dist/leaflet.css`) is imported in the component.

Concretely:

- New deps: `react-leaflet@^4.2.1`, `leaflet@^1.9.4`, `@types/leaflet@^1.9.x` (dev).
- Tiles: `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png` with the required OSM attribution (`© OpenStreetMap contributors`).
- Markers use `L.divIcon` (CSS circle, green = active, gray = inactive) to sidestep Leaflet's well-known bundler issue with its default marker PNGs and to align with the existing `statusEnum` colors.
- A `FitBounds` child component (`useMap()`) fits the map to all plotted stores; single-store sets get a sensible default zoom.
- The stores page gains an Ant Design `Segmented` table/map toggle. The table remains the source of truth for CRUD.
- Map pan/zoom animation is disabled when `prefers-reduced-motion: reduce` is set.

## Alternatives considered

1. **Google Maps (`@react-google-maps/api`)**
   - Pros: best basemap/POI quality in Vietnam; familiar UX; Street View.
   - Cons: **requires a billed GCP API key**; usage caps and key-leak risk for a public web bundle; ToS restrictions; another secret to manage on the VPS. Overkill for plotting internal store points.
   - **Rejected** on cost + key-management overhead for an internal tool.

2. **Mapbox GL JS**
   - Pros: crisp vector tiles; generous free tier; strong styling control.
   - Cons: requires an access token; heavier WebGL bundle; vector styling is more than this read-only "plot points" use case needs.
   - **Rejected** as over-engineered for the requirement; revisit if heatmaps/clustering/custom cartography become real needs.

3. **react-leaflet + OpenStreetMap**
   - Pros: **no API key, no billing**; OSS (BSD/MIT); tiny mental model; the de-facto React Leaflet binding; raster tiles are trivial; divIcon markers avoid asset bundling pain; works fully client-side with `ssr: false`.
   - Cons: raster tiles look less polished than vector; OSM public tile servers have a fair-use policy (fine for low-volume internal admin traffic); no built-in clustering (not needed at current store counts).
   - **Accepted.**

## Consequences

**Positive**

- Zero recurring cost and no API-key secret to provision/rotate on the VPS.
- Unblocks the Sprint 02 deferred "Store map view" without touching the backend (coords already exist).
- Self-contained client component — does not disturb the working CRUD ProTable.
- divIcon status colors reuse the existing active/inactive semantics → consistent with the table.

**Negative / accepted trade-offs**

- OSM public tiles carry a fair-use policy; acceptable for internal admin volume. If usage grows we add a tile cache/proxy or a paid tile provider — a config change, not a rewrite.
- Raster basemap is less crisp than vector tiles (acceptable for plotting points).
- No marker clustering yet; if store counts reach the thousands we add `leaflet.markercluster` (additive, no ADR churn since Leaflet is already chosen).
- Leaflet manipulates the DOM directly → the component is client-only (`ssr: false`); SSR/SEO is irrelevant for an authenticated admin page.

**Mitigations**

- Attribution control kept visible (OSM ToS compliance).
- Component lazy-loaded via `next/dynamic` so Leaflet is not in the initial admin bundle.
- `prefers-reduced-motion` disables map animations.

## Revisit triggers

- OSM fair-use limits hit → introduce a self-hosted tile server or paid provider.
- Need for spatial queries (radius/within) → revisit ADR-006 (PostGIS) and possibly server-side geofencing rather than client mapping.
- Need for clustering, heatmaps, or custom cartography at scale → evaluate Mapbox/MapLibre vector tiles.
- A mobile map requirement appears (Flutter) → that is a separate stack decision (`flutter_map` vs Google Maps SDK), not covered here.
