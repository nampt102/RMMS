# ADR-011 — Self-hosted CompreFace for Face Verification (replaces FPT.AI Face)

- **Status:** Accepted
- **Date:** 2026-06-06
- **Deciders:** Tech lead (MotivesVN IT)
- **Supersedes:** the "Face Recognition = FPT.AI Face" choice in `02-tech-stack.md`
- **Related:** `modules/M06-face-verification.md`, `modules/M05-attendance-antifraud.md`, `sprints/sprint-04.md`, `06-business-rules.md` (BR-206..BR-209, CR-4), ADR-007 (Caddy), `docker-compose.yml`

## Context

M06 requires mandatory Face Verification at check-in/out (BR-206) with enrollment + 1:N/1:1 matching and an Admin review flow on failure (BR-207..209). The original stack named **FPT.AI Face** (a paid cloud API).

Re-evaluation for Phase 1A with the real constraints:

- **Privacy / compliance (CR-4):** check-in selfies + face biometrics are sensitive. A cloud API ships biometric data to a third party; self-hosting keeps it on our own VPS.
- **Cost:** FPT.AI bills per call; attendance runs many verifications/day → recurring per-call cost + a vendor account/key to manage and rotate.
- **Vendor lock-in / availability:** an external dependency on every check-in (latency + uptime risk; M06 edge case "Face API down → pending_review").
- **Team shape:** 2 senior generalist devs, no ML/DevOps. We want a ready-made service, not a model to train/operate.
- **Architecture already abstracted:** M05 introduced `IFaceVerificationService`; M06 adds an `IFaceClient` port. The vendor sits behind a port, so swapping it is localized.

Options surveyed (GitHub popularity + fit):

| Option | Form | Notes |
|---|---|---|
| **CompreFace** (exadel, ~8k★, Apache-2.0) | Ready REST **service** (Docker Compose) | Built on InsightFace + FaceNet; enroll/recognize/verify endpoints + API-key/role mgmt; 99.7% LFW; CPU or GPU |
| InsightFace (deepinsight, ~28k★) | Models/SDK | SOTA embeddings but **no** matching/REST service — we'd build the microservice + storage ourselves |
| DeepFace (serengil, ~22k★) | Python lib (+basic REST) | Flexible multi-model wrapper; still need to operate a service + subject mgmt |
| face_recognition (ageitgey, ~54k★) | dlib lib | Simplest API but older/weaker accuracy, unmaintained-ish, build everything — unfit for anti-fraud |

## Decision

**Adopt CompreFace, self-hosted as a Docker service alongside the existing stack on the Vultr VPS, as the Face Verification engine for M06.** The .NET backend calls CompreFace's REST API through the `IFaceClient` port; biometric embeddings ("templates") live inside CompreFace, never in our PostgreSQL.

Concretely:

- **Deployment:** CompreFace `docker-compose` services (UI/API/core + its own Postgres) added to our compose under a `face` profile; reachable only on the internal Docker network (not publicly exposed — Caddy does not proxy it). CPU model by default (no GPU on the VPS); revisit GPU if latency misses the <2s budget.
- **Enrollment** (`POST /api/v1/recognition/faces?subject=<userId>`, multipart, `x-api-key`): the PG's 3 angle photos are added under **subject = the user's id**. We store only the subject id in `users.face_template_external_id` + stamp `face_enrolled_at` (unchanged columns).
- **Verification** at check-in/out (`POST /api/v1/recognition/recognize`, multipart selfie): match when the top returned `subject == userId` **and** `similarity ≥ threshold` (default **0.85**, tunable). Maps to `FaceVerificationResult` success/fail; fail → `FaceFailPendingReview` (BR-207, existing M05 state machine).
- **Admin remove / re-enroll** (`DELETE /api/v1/recognition/subjects/<userId>`): clears the subject; user must re-enroll.
- **Raw selfies** continue to be stored in **MinIO** (90-day TTL, ADR/CR-4) for the Admin review "selfie vs enrolled" comparison; CompreFace holds the embeddings.
- **Config-gated client** (same pattern as SendGrid/FCM): a real `CompreFaceClient` is used when `CompreFace:ApiKey` is configured; otherwise a deterministic **dev face client** keeps the whole enroll→verify flow exercisable locally and in tests without running the service.
- **Face API down** → verification returns `pending_review` (BR-207 / M06 edge case), never a hard check-in failure.

## Alternatives considered

1. **FPT.AI Face (status quo)** — Rejected: per-call cost, sends biometrics off-prem, vendor key + uptime dependency on every check-in.
2. **InsightFace-REST (self-build)** — Rejected for Phase 1: SOTA accuracy but we'd own a custom matching/identity service + embedding storage; CompreFace already wraps InsightFace, so we get the accuracy without the build.
3. **DeepFace microservice** — Rejected: viable but more service-operation + subject-management code than CompreFace's ready API.
4. **face_recognition (dlib)** — Rejected: weaker accuracy for anti-fraud, build-everything, low maintenance.

## Consequences

**Positive**

- Biometric data stays on our infrastructure (privacy/CR-4); no per-call cost; no third-party key on the hot path.
- Ready REST service → minimal build; the `IFaceClient` port means only one Infrastructure class (`CompreFaceClient`) plus deployment.
- InsightFace-grade accuracy (99.7% LFW) without operating models ourselves.
- Dev/CI keep working via the deterministic dev face client (no service required for tests).

**Negative / accepted trade-offs**

- New runtime service to operate: **~3–4 GB RAM** for the CPU model + its own Postgres → VPS must have headroom (verify before enabling the `face` profile; otherwise size up the VPS or use a lighter model).
- CompreFace's last tagged release is 2023 (still maintained, Apache-2.0); we pin a known-good image tag.
- Self-hosted CPU inference latency must stay within the <2s budget; if not, enable GPU or scale the core service.
- One more service in the compose topology + an internal API key to manage (kept in `.env`, never committed — see `.env.example`).

## Revisit triggers

- VPS can't fit CompreFace's memory footprint → lighter model, dedicated face node, or reconsider a cloud API.
- CPU latency misses <2s at load → GPU model / horizontal scale of the core service.
- Need liveness/anti-spoofing beyond CompreFace plugins → evaluate a dedicated liveness vendor (separate ADR).
- CompreFace abandonment → the `IFaceClient` port lets us swap to DeepFace/InsightFace-REST without touching handlers.
