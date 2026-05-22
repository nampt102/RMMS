# Sprint 0 — Setup (W1-W2)

**Goal:** Foundation & infrastructure ready for development

**Modules touched:** Foundation

## Deliverables (Definition of Done)

- [ ] Monorepo (or 3 repos) initialized with .NET API, Next.js web, Flutter mobile skeletons
- [ ] GitHub Actions CI pipelines for each
- [ ] VPS (production + staging) provisioned with Docker Compose stack
- [ ] PostgreSQL 16 + Redis 7 + MinIO running
- [ ] Caddy reverse proxy with auto SSL
- [ ] Apple Developer + Google Play accounts purchased & configured
- [ ] Domain DNS + Cloudflare proxy setup
- [ ] Sentry project created, SDK integrated
- [ ] Face Recognition vendor decision (PoC FPT.AI + AWS Rekognition)
- [ ] SendGrid account + sender verification
- [ ] FCM project created
- [ ] Definition of Done agreed by team

## User Stories / Key outcomes

- As a dev, I can clone the repo and run all 3 apps locally with one command
- As a dev, my push triggers CI checks and a build artifact is produced
- As a dev, I have a staging URL where my branch's build is deployed automatically

## Tasks by Discipline

### DevOps
- [ ] Provision Vultr SG VPS (8vCPU/16GB) for prod
- [ ] Provision smaller VPS for staging
- [ ] Install Docker + Docker Compose
- [ ] Setup Caddy with auto SSL
- [ ] Create Docker Compose stack: postgres, redis, minio, caddy
- [ ] Setup backup script (pg_dump → S3 cron)
- [ ] Setup monitoring: Sentry + Uptime Kuma

### BE (.NET)
- [ ] Init solution structure (Api, Application, Domain, Infrastructure projects)
- [ ] Add NuGet packages from tech stack list
- [ ] Configure Serilog + Sentry
- [ ] Setup EF Core with PostgreSQL provider
- [ ] First migration creating initial schema
- [ ] Health check endpoints
- [ ] Swagger setup
- [ ] Dockerfile + multi-stage build

### Web (Next.js)
- [ ] Init Next.js 14 with App Router + TypeScript
- [ ] Install Ant Design Pro v5 template
- [ ] Setup next-intl with vi+en locales
- [ ] Configure TanStack Query, Zustand, React Hook Form
- [ ] Dockerfile
- [ ] ESLint + Prettier config

### Mobile (Flutter)
- [ ] Init Flutter 3.x project
- [ ] Add dependencies: Riverpod, Dio, Hive, Firebase, etc.
- [ ] Setup l10n (vi + en) with ARB files
- [ ] Setup theme & routing (go_router)
- [ ] Build iOS + Android signing setup
- [ ] Configure Firebase for both platforms
- [ ] Setup integration_test scaffolding

### CI/CD
- [ ] GitHub Actions workflows for each repo
- [ ] Lint + test + build per push
- [ ] Deploy staging on `develop` push
- [ ] Manual deploy to prod from `main`

### Documentation
- [ ] Each repo README with setup steps
- [ ] Architecture Decision Record (ADR) folder
- [ ] Knowledge base initial commit

## Sprint-specific Risks

- Mac for iOS builds — buy or rent decision needs to be made early
- App store account approval may take days for Apple
- FPT.AI account setup may need business documents

## Sprint DoD

- [ ] All 3 apps build and run locally
- [ ] CI green on all 3 repos
- [ ] Staging environment accessible at https://staging.rmms.example.com
- [ ] Knowledge base reviewed and committed

## Demo

End-of-sprint demo to stakeholder showing the deliverables above.

## Notes

- See `00-overview.md` for project context
- See `02-tech-stack.md` for technology decisions
- See respective module docs in `modules/` for details on ``