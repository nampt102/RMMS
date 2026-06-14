# ADR-017 — Mobile: `url_launcher` for opening document signed URLs

**Status:** Accepted · 2026-06-15 · Phase 1B (Sprint 15, M13)

## Context

M13 Document Center lets PG/Leader open documents (payslips, policies, handbooks) from
mobile. The backend serves each file as a short-lived **signed URL** (`GET /documents/:id/download`),
never a stable path. The module spec calls for: "Document viewer (PDF in-app, image in-app,
others via system viewer)".

Flutter has no built-in way to hand a URL to the OS. Options considered:

1. **`url_launcher`** — tiny, first-party (flutter.dev), opens the signed URL in the system
   browser / default viewer (Chrome Custom Tab on Android, SFSafariViewController on iOS).
   Handles every MIME the device can render (PDF, image, docx…) with zero per-format code.
2. **In-app PDF renderer** (`flutter_pdfview`, `syncfusion_flutter_pdfviewer`) — heavier, native
   build surface, per-format; only solves PDF.
3. **Download + `open_filex`** — extra storage handling + a viewer dependency anyway.

## Decision

Adopt **`url_launcher` ^6.3.1**. The mobile Documents viewer fetches the signed URL and opens it
with `launchUrl(uri, mode: LaunchMode.externalApplication)` — the OS viewer handles PDF/image/other.
This is the lowest-footprint way to satisfy "others via system viewer" and covers PDF + image too.

No new **UI** library is introduced (url_launcher is a platform-channel utility), consistent with
the Material-only constraint. In-app rich PDF rendering (annotations, search) is deferred to a
future ADR if product asks for it.

## Consequences

- `+ url_launcher` in `pubspec.yaml`. Mac runs `flutter pub get`. No iOS/Android manifest changes
  needed for `https` external launches (default schemes are allowed).
- Signed URLs are short-lived; if the OS viewer is slow to open, the user re-taps to mint a fresh
  URL — acceptable for Phase 1 (matches the 5-min expiry note in M13).
- Private payslips open in the system viewer like any URL; the download itself is access-checked +
  audited server-side (CR-1), so no extra client gating is required.
