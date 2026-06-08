# ADR-013 — Active liveness for check-in selfie (Google ML Kit)

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Tech lead, PM
- **Context module:** M05 Attendance anti-fraud / M06 Face Verification (mobile)

## Context

Check-in/out captures a selfie that CompreFace (ADR-011) matches against the enrolled
face. CompreFace verifies **identity only — not liveness**. The capture used
`ImagePicker(source: camera)`, which blocks picking from the gallery but does **not**
stop a user from pointing the front camera at a printed photo or another screen
("photo-of-photo" spoof) to fake another person's (or their own) presence.

We want an anti-spoof step that is on-device (no per-call cost, works offline at the
camera step), inside the agreed Flutter stack, and does not send extra biometrics to a
third party.

## Decision

Add **active liveness** before the selfie is accepted, using
**`google_mlkit_face_detection`** (on-device, free) driven by the existing **`camera`**
package:

1. Open the front camera as a live preview and stream frames to ML Kit's face detector
   (classification + head-angle enabled).
2. Issue a **randomised challenge** per attempt — blink / smile / turn head — and only
   capture the still frame once the challenge is satisfied. A static photo cannot blink,
   smile, or turn, so photo-of-photo and screen-replay of a still are rejected.
3. The captured frame then continues to the existing CompreFace identity match unchanged.

This replaces `ImagePicker` for the **selfie** only; the store photo keeps `ImagePicker`
(rear camera, no liveness needed). `google_mlkit_face_detection` is the only package added.

## Consequences

- **+** Rejects printed/photo-on-screen spoofs with a live, on-device check; no extra
  network call, no biometric leaves the device (frames processed locally, discarded).
- **+** Reuses the bundled `camera` package; identity match (CompreFace) is untouched.
- **−** Adds a native ML Kit dependency (APK/IPA size + iOS needs `NSCameraUsageDescription`,
  already required by the camera flow). First-run model download on some devices.
- **−** Active liveness still doesn't stop a **video replay** of the person performing the
  challenge — acceptable for Phase 1; passive liveness / depth is a future ADR if needed.
- **Edge handling:** challenge timeout → retry; camera permission denied → block check-in
  with a clear message; low light / no face → guidance overlay.

## Alternatives considered

- **In-app `CameraController` only (no liveness)** — controls the capture source but a live
  camera pointed at a photo still passes; does not address the actual spoof. Rejected as
  insufficient on its own (this ADR's approach already includes a direct camera stream).
- **Commercial liveness SDK (FPT.AI, AWS Face Liveness)** — stronger (passive/3D) but adds
  cost + per-call dependency. Revisit only if active liveness proves inadequate.
- **CompreFace liveness** — not available in the OSS edition (ADR-011).
