# ADR-014 — @dnd-kit for the web Form Builder drag-and-drop

- **Status:** Accepted
- **Date:** 2026-06-14
- **Deciders:** Tech lead, PM
- **Context module:** M10 Form Engine — Form Builder (web, Sprint 12)

## Context

The Form Builder (web Admin) lets Admin compose a form by adding fields and **reordering**
them (M10 spec: "Drag-drop reorder (Phase 1) or simple list (fallback)"). Ant Design Pro
has no first-class drag-and-drop list primitive for this. Per project rules, **no new UI
library is adopted without an ADR**.

## Decision

Adopt **`@dnd-kit`** (`@dnd-kit/core` + `@dnd-kit/sortable`) for drag-and-drop reordering
of fields (and option lists) in the Form Builder only.

- Used strictly for interaction (sortable lists); all rendered chrome stays **Ant Design Pro
  + Tailwind utilities** (ADR-008). @dnd-kit ships no visual components, so it does not
  conflict with the AntD design system or the Tailwind-preflight-off decision.
- **Fallback remains valid:** if Builder UX slips, ship the list-based reorder (up/down
  buttons) first; @dnd-kit is the enhancement, not a hard dependency for AC-20.

## Consequences

- **+** Modern, accessible (keyboard + screen-reader support built in), zero-dependency,
  actively maintained; far lighter and more flexible than `react-beautiful-dnd` (deprecated)
  or full grid/layout builders.
- **+** Headless — no styling to reconcile with AntD; we keep full visual control.
- **−** One more frontend dependency to track for security updates.
- **−** Slightly more wiring than a styled drag list (we provide the DOM/markup), acceptable
  given the headless flexibility.

## Alternatives considered

- **react-beautiful-dnd** — popular but no longer maintained / React 18 friction. Rejected.
- **AntD `Table` row drag / `List` only** — limited, not designed for nested field+option
  reordering. Insufficient.
- **List-based reorder (no DnD)** — the accepted **fallback**; functional for AC-20 but poorer
  UX. We adopt @dnd-kit to exceed it, keeping this as the safety net.
