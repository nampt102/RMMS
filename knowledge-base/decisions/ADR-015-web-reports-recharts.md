# ADR-015 — Recharts for web Reports & Dashboard charts

- **Status:** Accepted
- **Date:** 2026-06-14
- **Deciders:** Tech lead, PM
- **Context module:** M15 Dashboard & Reports (web, Sprint 16) — also future dashboard widgets

## Context

Sprint 16 builds the full Reports surface (PRD 6.15) and richer dashboard widgets with
charts (trends, distributions, per-store/area breakdowns). The Phase 1A basic dashboard
(M15) uses only Ant Design Pro `StatisticCard` KPIs — no real charting. Ant Design Pro's
chart layer historically pulls `@ant-design/charts` (AntV/G2), a heavier stack. Per project
rules, a charting library needs an ADR. Decide once, early, so all charts are consistent.

## Decision

Adopt **Recharts** as the single charting library for the web app (Reports + dashboard
widgets).

- React-native, composable SVG charts (line/bar/area/pie/stacked) — enough for all Phase 1
  report types. Colors driven by our existing semantic tokens to match the AntD theme.
- Charts live inside AntD Pro `Card`/`StatisticCard` containers; Recharts adds **no global
  CSS**, so ADR-008 (Tailwind preflight off, AntD reset wins) is unaffected.
- Server does the heavy aggregation (indexed report queries + Hangfire async export for
  large sets, Sprint 16); Recharts only renders already-aggregated series.

## Consequences

- **+** Lightweight, ubiquitous, simple declarative API; fast to build standard reports; large
  community and examples.
- **+** Pure SVG + React — no canvas/global-style surprises, composes cleanly with AntD.
- **−** Less specialized than AntV/G2 or ECharts for exotic/large-scale visualizations; fine
  for Phase 1 report types, revisit only if a chart need exceeds it.
- **−** One more frontend dependency to track.

## Alternatives considered

- **@ant-design/charts (AntV/G2)** — native to AntD Pro but heavier bundle and a different
  mental model; overkill for Phase 1 report set. Rejected for weight/complexity.
- **ECharts (echarts-for-react)** — very powerful (huge datasets, rich types) but heavier and
  more imperative; revisit if Phase 2 needs it. Rejected for now.
- **Chart.js (react-chartjs-2)** — canvas-based; fine but less idiomatic in React and harder to
  theme to match AntD tokens. Rejected.
