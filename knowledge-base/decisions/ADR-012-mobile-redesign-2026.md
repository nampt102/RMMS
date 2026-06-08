# ADR-012 — Mobile Redesign 2026 visual language + `google_fonts`

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Mobile lead, PM
- **Context module:** Mobile app (PG + Leader); see `09-mobile-design-system.md`

## Context

The original mobile theme was flat and basic. We want a modern, trend-forward
look with a strong brand identity, while staying inside the agreed stack
(Flutter + Riverpod + Material 3) and the "no extra UI packages without an ADR"
rule from `CLAUDE.md`.

Two needs drove a decision:

1. A cohesive **visual language** (brand, gradients, radii, motion) and a reusable
   **component kit**, so feature screens stop hand-rolling containers/hex/shadows.
2. **Custom typography** (Space Grotesk display + Plus Jakarta Sans body), which
   is not available from the bundled platform fonts.

## Decision

1. Adopt the **Redesign 2026** visual language and component kit documented in
   `09-mobile-design-system.md`:
   - Indigo→violet brand, mesh-gradient heroes, bento cards (r26–28), soft layered
     shadows, transform-only micro-motion.
   - Tokens in `lib/core/theme/{app_palette,app_theme}.dart`; component kit in
     `lib/core/widgets/app_widgets.dart`; semantic extension `AppSemantics`.
2. Add **one** UI dependency: **`google_fonts ^6.2.1`** — to load Space Grotesk
   (display) + Plus Jakarta Sans (body). This is the only UI package added; no
   other design/UI libraries are introduced.

## Consequences

- **+** Consistent, modern UI; feature code composes from a typed kit instead of
  ad-hoc styling; light/dark + reduced-motion handled centrally.
- **+** Stays within Flutter/Material 3; web (AntD Pro, ADR-008) untouched.
- **−** `google_fonts` fetches fonts at runtime on first use (cached). Acceptable;
  if offline-first font loading becomes a requirement, bundle the `.ttf` assets and
  drop the network path (no API change to call sites).
- **Follow-up:** legacy `brand_widgets.dart` (used by Approvals + Team Monitoring)
  is deprecated — migrate those screens to `app_widgets.dart` when next touched.

## Alternatives considered

- **Bundle fonts as assets instead of `google_fonts`** — more setup/maintenance for
  weight variants now; revisit only if offline font loading is required.
- **Stay on platform/Material default fonts** — rejected: no distinct brand identity.
