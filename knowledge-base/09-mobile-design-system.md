# 09 — Mobile Design System (Redesign 2026)

> **Scope:** Flutter mobile app (PG + Leader). This is the **source of truth** for
> the mobile visual language. Web Admin/BUH keeps its own system (AntD Pro +
> Tailwind, ADR-008) — do **not** copy these tokens to web.
>
> **Status:** Adopted (Redesign 2026). Recorded in **ADR-012**.

---

## 1. Visual language

Bold **indigo → violet** brand with **mesh-gradient** hero blocks, rounded
"bento" cards (radius 26–28), large display typography, soft layered shadows,
and tasteful transform-only micro-motion. Goal: modern, trend-forward, high
perceived quality — replacing the previous flat/basic look.

Tokens live in `lib/core/theme/` and are consumed via `ColorScheme` or the
`AppSemantics` ThemeExtension — **never** as ad-hoc hex in widgets.

| Concern | File |
|---|---|
| Raw tokens (colors, gradients, shadows, dark variants) | `lib/core/theme/app_palette.dart` |
| Material 3 `ThemeData` (light + dark), text theme | `lib/core/theme/app_theme.dart` |
| Design-system widgets (the component kit) | `lib/core/widgets/app_widgets.dart` |
| Context helpers | `context.scheme` / `context.semantics` / `context.texts` |

---

## 2. Color tokens (`AppPalette`)

**Brand:** `indigo #5B5BF0` (primary) · `indigoDeep #4338CA` (gradient end / pressed) ·
`indigoBright #7C7CFF` (gradient start / secondary) · `violet #8B5CF6` (mesh accent).

**Semantic accents:** `emerald #10B981` (success / checked-in) · `amber #F59E0B`
(warning) · `rose #F43F5E` (danger) · `sky #0EA5E9` (info).

**Neutrals:** `ink #161528` (titles) · `body #45445C` · `muted #8A89A3` ·
`faint #B6B5CC` · `line #ECECF3` (borders) · `surface #FFFFFF` ·
`surface2 #F5F5FB` (input fill / chips / soft buttons) · `bg #F1F1F8` (scaffold).

**Status chip pairs** (`chip*Bg`/`chip*Fg`) and **icon-tile pairs**
(`tile*Bg`/`tile*Fg`) exist for neutral/indigo/violet/emerald/amber/rose/sky —
use the `AppTone` enum + `chipColors()`/`tileColors()` helpers, not the raw pairs.

**Dark neutrals:** `darkBg #0F1117` · `darkSurface #171A21` · `darkSurfaceHi #1F2430` ·
`darkInk #E7E9F1` · `darkMuted #94A3B8` · `darkBorder #2A2F3A`.

> Legacy aliases (`indigoLight`, `red`, `border`, `surfaceMuted`, `indigoTintBg`,
> `emeraldLight`) are kept for back-compat — prefer the canonical names in new code.

### Gradients & shadows (`AppSemantics` ThemeExtension)

- `brandGradient` — 135°, `indigoBright → indigo → indigoDeep` (buttons, FAB).
- `meshGradient` — 135°, `violet → indigo → indigoDeep` (hero / header blocks).
- `emeraldGradient` — 135°, `emeraldBright → emeraldDeep` (success / checked-in).
- Shadow scale: `shadowSm` · `shadow` (= `cardShadow`) · `shadowLg` · `shadowBrand`
  (glow under gradient buttons). Adapts automatically to light/dark.
- Semantic surfaces: `success/warning/info/danger` + their `*Container` + `on*`.

---

## 3. Typography

`google_fonts ^6.2.1` (per ADR-012):

- **Space Grotesk** — display roles: page titles, hero numbers, the live clock
  (weights 700–800, negative letter-spacing). Use `AppTheme.display(...)`.
- **Plus Jakarta Sans** — all body / UI / labels / buttons. Use `AppTheme.body(...)`
  or the `TextTheme` roles.

Material 3 text roles are remapped in `AppTheme._textTheme`: display/headline →
Space Grotesk; title/body/label → Plus Jakarta Sans with weight hierarchy
(headings 700–800, body 500, labels 700–800). Body line-height 1.45.

---

## 4. Radius & spacing scale (`AppTheme`)

| Token | Value | Use |
|---|---|---|
| `radiusSm` | 14 | small inner elements |
| `radiusMd` | 17 | primary buttons, inputs |
| `radiusLg` | 22 | list rows / quick cards |
| `radiusXl` | 28 | hero / feature cards |
| `controlHeight` | 54 | buttons & inputs height |

Bottom sheets: top radius **30**. Glass bottom-nav bar: radius **26**.
Spacing follows a 4/8 rhythm (gaps of 4 / 8 / 12 / 14 / 16 / 18 / 22).

---

## 5. Component kit (`app_widgets.dart`)

The canonical kit (mirrors the prototype `ds.jsx`). **Build screens from these** —
do not hand-roll containers with raw hex/shadows.

| Widget | Notes |
|---|---|
| `AppChip` | pill h28, `AppTone`, tonal or `solid`; optional leading icon |
| `AppIconTile` | rounded square (48, r16), tone-tinted bg + centred stroke icon |
| `AppCard` | surface, r28, `shadowSm`, padding 18; `onTap` → press-scale; supports `gradient` |
| `AppButton.{primary,emerald,soft,destructiveSoft}` | h54 r17 16/700, optional icon, `loading` spinner |
| `AppTopBar` | 40×40 back chip + title 19/700 + trailing actions (place at top of body) |
| `showAppSheet` / `AppSheet` | top r30, grab handle, slide-up, scrim rgba(20,19,40,.4) |
| `showAppToast` / `AppToastKind` | floating dark pill, status icon, auto-dismiss ~1.9s |
| `AppBottomNav` / `AppNavItem` | glass bar (blur20, h70), **exactly 5 items**, index 2 = raised gradient "Chấm công" FAB |
| `AppRiseIn` / `AppStaggerColumn` | transform-only Y-rise entrance (16→0, ~450ms, 60ms stagger) |
| `SectionEyebrow` | UPPERCASE 12/800 section label, optional trailing |
| `PressScale` | press feedback (scale 0.965, 140ms easeOutCubic) — used by all tappables |

---

## 6. Interaction & accessibility rules (mandatory)

- **Press feedback:** every tappable wraps in `PressScale` (0.965 / 140ms).
- **Reduced motion:** all animation honors `MediaQuery.disableAnimations`
  (`AppRiseIn`, `PressScale`, toast collapse to no-op / instant).
- **Touch targets:** buttons 54, nav items h70, FAB 58 — all ≥44pt. `AppTopBar`
  back chip is 40×40 (acceptable with opaque hit area; do not shrink further).
- **Tabular figures** for the clock / numeric columns to avoid layout shift.
- **Motion is transform-only** (translate/scale/opacity) — never animate
  width/height/layout (no CLS / reflow).
- **Dark mode** is first-class: `AppTheme.dark` + `AppSemantics.dark` ship desaturated
  tonal variants; never invert colors. Test contrast separately.

---

## 7. Conventions & guardrails

- **No new UI packages** beyond `google_fonts` without a new ADR (CLAUDE.md rule).
  Material 3 + the kit above covers all needs.
- **No raw hex / shadows in feature widgets** — read tokens via `context.scheme` /
  `context.semantics` or use a kit widget.
- **i18n:** all user-visible strings stay ARB-keyed (`app_vi.arb` default + `app_en.arb`).
  Never hardcode copy.
- **`brand_widgets.dart` is legacy** (pre-redesign: `GradientHero`, `IconBadge`,
  `StatusPill`, `SoftCard`, `FeatureTile`, `BrandTone`). Still used by
  `approvals_screen.dart` and `team_monitoring_screen.dart` — **migrate these to
  `app_widgets.dart`** when next touched; do not add new usages.

---

## 8. Screen coverage (Redesign 2026 rollout)

Migrated to the new kit: Login, Home (mesh hero + today card + quick grid),
Schedule, Register-device, Requests (Leave/OT), Attendance, Face, History,
Assignment. Pending migration off `brand_widgets`: **Approvals**, **Team Monitoring**.

See `lib/core/widgets/app_widgets.dart` (kit) and `lib/core/theme/` (tokens) for
the authoritative implementation.
