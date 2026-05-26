# ADR-008 — Tailwind preflight disabled in Web app (Ant Design wins)

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `02-tech-stack.md`, `08-coding-standards.md` (Frontend section, lines 186–188), `web/tailwind.config.ts`, `web/src/app/globals.css`

## Context

The Web app (Admin + BUH portal) uses **Ant Design Pro 5** as the primary component library. Ant Design ships its own CSS reset / normalization aligned with its visual language: form-element baseline, heading margins, button reset, link color, etc. The team also added **Tailwind CSS** to handle utility-class layout for sections AntD doesn't cover well (one-off spacing, custom dashboards, marketing-style hero blocks).

Tailwind, **by default**, injects a CSS reset called **Preflight** that:

- Removes default heading sizes (`h1..h6` all become unstyled).
- Removes list bullets (`ul`, `ol`).
- Strips form-element styling that the browser provides.
- Resets margins on all elements.

Preflight is designed to make a blank-slate Tailwind project look consistent. **Combining Preflight with another CSS reset (Ant Design's) produces fights:**

- AntD's `<Typography.Title>` no longer renders with the expected heading sizes (Preflight resets `h1..h6` after AntD's styles apply).
- AntD's `<List>` loses bullet styling.
- AntD `<Button>` icons misalign in some browsers because Preflight resets `img` and `svg` baselines.

The team observed multiple visual regressions during scaffold review attributable to Preflight interfering with AntD.

## Decision

**Disable Tailwind Preflight in the Web app. Ant Design's reset is the sole CSS reset / normalization.**

Concretely:

```ts
// web/tailwind.config.ts
export default {
  content: [...],
  corePlugins: {
    preflight: false,   // ← AntD reset wins
  },
  theme: { ... },
  plugins: [],
}
```

- Tailwind **utility classes** remain available (`flex`, `gap-4`, `text-sm`, `space-y-2`, `grid-cols-3`, etc.) — those are unaffected by Preflight.
- AntD components are styled by AntD's own ConfigProvider theme tokens (see `web/src/app/providers.tsx`).
- For pages that intentionally need a Tailwind-style "blank slate" (rare), scope Preflight rules to a wrapper class manually rather than re-enabling globally.

## Alternatives considered

1. **Don't use Tailwind; rely solely on AntD + CSS Modules**
   - Pros: zero reset-fight; one styling system.
   - Cons: utility-first layout (gap, flex, responsive breakpoints) is verbose in CSS Modules; team productivity drops on dashboard layouts AntD doesn't cover; Tailwind utilities are highly portable across pages.
   - **Rejected.**

2. **Use Tailwind with Preflight enabled and "patch" AntD overrides**
   - Pros: get the full Tailwind base styling.
   - Cons: dozens of overrides; new AntD versions can shift the override surface; debug burden every time a heading or list renders wrong. Anti-pattern: fighting two resets indefinitely.
   - **Rejected.**

3. **Use Tailwind utilities only with Preflight disabled — AntD reset is canonical**
   - Pros: best of both: utility-first layout via Tailwind + consistent component styling via AntD; no reset fight; AntD's design language is preserved.
   - Cons: developers familiar with Tailwind "blank slate" pages need to know that headings are AntD-styled, not Tailwind-reset; documented in `08-coding-standards.md`.
   - **Accepted.**

## Consequences

**Positive**

- Visual consistency across AntD components is preserved without per-component overrides.
- Tailwind utilities (`flex`, `grid`, spacing, typography utilities like `text-sm`) remain fully usable for custom layouts.
- No CSS reset war → fewer "why does this heading look weird?" tickets.
- AntD ConfigProvider tokens (brand color, border-radius) drive the design system; Tailwind is a layout / one-off utility tool, not a styling system.

**Negative / accepted trade-offs**

- A page that wants Tailwind's blank-slate look (e.g., a marketing landing page with `Typography.h1` that should have no default styles) can't have it globally — must scope manually.
- Devs new to the project must learn the convention: "AntD for components, Tailwind for utilities" — documented in `08-coding-standards.md` and ADR-008 itself.
- Some Tailwind documentation examples assume Preflight is on — copy-paste of those snippets may produce unexpected results until the developer adapts.

**Mitigations**

- `web/README.md` calls out this convention prominently in its Styling section.
- `08-coding-standards.md` (Frontend / Tailwind / AntD section) reiterates the rule.
- In code review, a heading that uses `<h1>` directly (instead of `<Typography.Title>`) is a smell — AntD `Typography` components ensure the design-system styling.

## Revisit triggers

- Project pivots away from Ant Design (e.g., to shadcn/ui or Mantine) — then Tailwind Preflight may become useful again; this ADR is superseded.
- Tailwind v5+ changes the structure of Preflight in a way that makes opt-in per-rule possible — partial Preflight could be evaluated.
- A clear majority of pages are non-AntD custom layouts (currently AntD covers ~80%+) — revisit the cost-benefit.
