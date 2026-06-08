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
2. Add **one** UI dependency: **`google_fonts ^6.2.1`** — to provide Space Grotesk
   (display) + Plus Jakarta Sans (body). This is the only UI package added; no
   other design/UI libraries are introduced.
3. **Bundle the fonts offline** (no runtime fetch): the `.ttf` files live under
   `mobile/google_fonts/` (declared as an asset in `pubspec.yaml`) and
   `main.dart` sets `GoogleFonts.config.allowRuntimeFetching = false`. Same
   `google_fonts` API at call sites — it just resolves from bundled assets.

## Consequences

- **+** Consistent, modern UI; feature code composes from a typed kit instead of
  ad-hoc styling; light/dark + reduced-motion handled centrally.
- **+** Stays within Flutter/Material 3; web (AntD Pro, ADR-008) untouched.
- **+** Fonts render with **no network dependency** — works on corp/VPN networks
  with SSL inspection and on physical devices where the runtime HTTPS fetch failed.
  No first-paint font flash, fully offline.
- **−** App bundle grows by ~10 `.ttf` files (~0.6 MB). Acceptable; the asset list
  must be kept in sync if weights change (filenames must match google_fonts'
  expected variant naming, e.g. `PlusJakartaSans-Regular.ttf`).
- **Follow-up:** legacy `brand_widgets.dart` (used by Approvals + Team Monitoring)
  is deprecated — migrate those screens to `app_widgets.dart` when next touched.

## Alternatives considered

- **Runtime font fetching (`google_fonts` default)** — initially chosen, then
  reversed: the HTTPS fetch fails behind SSL-inspecting corp networks and on some
  physical devices, causing fallback fonts. Bundling the `.ttf` assets fixed it
  with no call-site change.
- **Stay on platform/Material default fonts** — rejected: no distinct brand identity.
