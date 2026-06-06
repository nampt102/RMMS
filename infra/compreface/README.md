# CompreFace — self-hosted Face Verification (ADR-011)

RMMS uses **CompreFace** (Apache-2.0, InsightFace/FaceNet under the hood) as the M06 face
recognition engine, replacing FPT.AI. Biometric embeddings live inside CompreFace on our own
VPS; our DB stores only the CompreFace `subject` id (`users.face_template_external_id`).

## Bring it up

```bash
# rmms-net is the shared network created by the root docker-compose.
docker network create rmms-net   # only if it doesn't exist yet
docker compose -f infra/compreface/docker-compose.yml up -d
```

Pinned to **v1.2.0**. CPU model needs **~3–4 GB RAM** + its own Postgres — confirm VPS headroom.
Not proxied by Caddy; reachable only on the internal `rmms-net`.

## One-time setup (get the API key)

1. Open the UI: `http://<host>:8000` → sign up (local admin account).
2. Create an **Application**, then add a **Recognition** service to it.
3. Copy that service's **API key** into the root `.env`:
   ```
   CompreFace__ApiKey=<recognition-service-api-key>
   CompreFace__BaseUrl=http://compreface-fe:8080
   ```
   With a blank `CompreFace__ApiKey`, the backend falls back to the deterministic **dev face
   client** (no service required) — used for local dev / CI / unit tests.

## Endpoints the backend uses (`x-api-key` header, multipart `file`)

| Purpose | Call |
|---|---|
| Enroll (per angle) | `POST /api/v1/recognition/faces?subject=<userId>` |
| Verify at check-in/out | `POST /api/v1/recognition/recognize` → match if top `subject == userId` and `similarity ≥ 0.85` |
| Admin remove / re-enroll | `DELETE /api/v1/recognition/subjects/<userId>` |

See `knowledge-base/decisions/ADR-011-compreface-self-hosted-face.md` and
`knowledge-base/modules/M06-face-verification.md`.
