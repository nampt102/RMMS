# ADR-007 — Caddy as reverse proxy with automatic HTTPS

- **Status:** Accepted
- **Date:** 2026-05-26
- **Deciders:** Tech lead (MotivesVN IT)
- **Related:** `02-tech-stack.md` (Reverse proxy row), `03-architecture.md`, `docker-compose.yml`, `infra/caddy/Caddyfile`

## Context

The Phase 1 deployment target is a single Ubuntu 22.04 VPS on Vultr Singapore. A reverse proxy is required to:

- Terminate TLS for the public API endpoint and the web app.
- Route `/api/*` to `Rmms.Api`, `/` to the Next.js web app, `/hangfire/*` to the Worker's dashboard (admin-gated), `/swagger/*` to the API in dev environments.
- Apply per-route auth (Hangfire dashboard restricted to admin IPs / basic-auth).
- Auto-renew TLS certificates so the team isn't paged at 3am for an expired cert.

Constraints:

- No dedicated SRE — operational simplicity wins over flexibility.
- Configuration must live in the repo (declarative, reviewable in PRs).
- Should run as a Docker Compose service alongside the API and Worker — no system-wide Nginx install on the VPS.

Three credible options: **Caddy**, **Traefik**, **Nginx**.

## Decision

**Use Caddy 2.x as the reverse proxy. Configuration lives in `infra/caddy/Caddyfile`. Caddy runs as a Docker Compose service and obtains TLS certificates via Let's Encrypt automatically.**

Concretely:

- Single `Caddyfile` per environment (dev = HTTP only on `:80`, prod = HTTPS with auto-SSL on `:443`).
- Caddy proxies:
  - `/api/*` → `rmms-api:8080` (Docker network name).
  - `/hangfire/*` → `rmms-worker:8080/hangfire` (with HTTP basic auth scoped to admin team).
  - `/swagger/*` → `rmms-api:8080/swagger` (Development profile only — blocked in prod via Caddyfile guard).
  - Everything else → `rmms-web:3000` (Next.js).
- TLS certificates are stored in a Docker volume (`caddy_data`) that persists across redeploys.
- HTTP → HTTPS upgrade is automatic in prod; no manual `Strict-Transport-Security` config needed (Caddy handles it).

## Alternatives considered

1. **Nginx + Certbot**
   - Pros: industry-standard; ubiquitous documentation; lowest-level control.
   - Cons: TLS renewal requires Certbot configured separately and reload hooks wired to nginx — 3+ moving parts vs Caddy's 1. Config syntax verbose; per-environment overrides require templating. Manual HTTP/2 / HTTP/3 enablement.
   - **Rejected** on operational complexity.

2. **Traefik**
   - Pros: also has auto-SSL; first-class Docker label-based routing; popular in K8s.
   - Cons: configuration spread across `traefik.yml` + per-container labels — harder to review in one place; sharper learning curve for non-DevOps reviewers; opinionated middleware system.
   - **Rejected** because a single Caddyfile is more readable for a 2-dev team.

3. **Caddy 2.x**
   - Pros: declarative, single-file Caddyfile; HTTPS-by-default with zero-config Let's Encrypt; HTTP/2 and HTTP/3 out of the box; sane defaults (HSTS, secure ciphers, OCSP stapling); JSON config API for advanced cases; small binary, small Docker image.
   - Cons: smaller community than Nginx (still plenty of docs); some advanced routing patterns require JSON config rather than Caddyfile; ecosystem of plugins is younger.
   - **Accepted.**

## Consequences

**Positive**

- TLS certificate renewal is automatic — one less ops failure mode.
- Caddyfile is short and reviewable (a single PR shows the full routing layout).
- Sane security defaults (HSTS, modern ciphers, OCSP stapling) without explicit configuration.
- Dev environment uses the same Caddyfile structure with HTTP on `:80` and the prod env adds HTTPS — minimal divergence.

**Negative / accepted trade-offs**

- Caddy's plugin ecosystem is smaller than Nginx's; some niche features (e.g., advanced WAF) may require additional tooling later.
- The `caddy_data` Docker volume must be **backed up** — losing it means re-requesting certificates, which is rate-limited by Let's Encrypt (50 certs per registered domain per week).
- Caddy logs are JSON by default → require a small log-aggregation glue if we want Seq/Loki integration with the API logs.

**Mitigations**

- `caddy_data` volume is in the daily VPS snapshot policy.
- HTTP basic-auth password for the Hangfire dashboard is sourced from `Caddy_HangfireBasicAuth` environment variable (set via `.env`), not committed to the repo.
- A second Caddy instance is **not** required for HA in Phase 1 (single-VPS) — accept that the proxy is a single point of failure same as the VPS itself. Phase 2 HA may introduce a load balancer in front of multiple Caddy instances.
- Document the renewal-volume backup requirement in `docs/ops/deployment.md` (to be created in Sprint 17).

## Revisit triggers

- Phase 2 introduces multiple VPS / load-balancer HA — may bring an external L7 LB (e.g., Cloudflare, AWS ALB) that subsumes Caddy's role.
- Need for a Web Application Firewall (WAF) beyond Caddy's rate-limit features.
- Compliance requirement that forces a specific reverse proxy (e.g., enterprise audit demands Nginx Plus).
- Caddy becomes unmaintained or pivots to a paid-only model.
