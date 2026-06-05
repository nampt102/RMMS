# RMMS Web (Admin & BUH)

Next.js 14 App Router · TypeScript · Ant Design Pro · TanStack Query · Zustand · next-intl (vi/en).

## Quickstart

```bash
cp .env.example .env.local
pnpm install            # or `npm install`
pnpm dev                # http://localhost:3010
```

> Dev runs on port **3010** (not 3000): some editors — e.g. Cursor/VS Code auto port-forward — hijack `localhost:3000` and return empty replies. Production (`next start`, Docker/Caddy per ADR-007) stays on 3000.

The dev server proxies `/api/proxy/*` → `NEXT_PUBLIC_API_BASE_URL` (default `http://localhost:5080`).

## Project Layout

```
src/
├── app/
│   ├── [locale]/        # i18n-routed pages (vi default + en)
│   │   ├── layout.tsx
│   │   ├── page.tsx     # home
│   │   └── (auth)/login/page.tsx
│   ├── layout.tsx
│   ├── providers.tsx    # QueryClient + AntD ConfigProvider
│   └── globals.css
├── features/<module>/
│   ├── api/             # TanStack Query hooks (useQuery/useMutation)
│   ├── components/      # feature-scoped components
│   ├── hooks/
│   └── types/
├── lib/
│   ├── api/             # axios client, query-client
│   ├── i18n/            # next-intl request config
│   └── stores/          # Zustand stores
├── types/api.ts         # API response shapes (mirrors backend/Rmms.Shared)
└── middleware.ts        # next-intl locale routing
messages/
├── vi.json
└── en.json
```

## Conventions

See `../knowledge-base/08-coding-standards.md` (Frontend section).

- Function components only; named exports
- React Hook Form + Zod for forms
- Tailwind utilities + AntD components — no inline styles
- All user-visible strings via `useTranslations()` — never hardcode
- Server state → TanStack Query; client state → Zustand; form state → React Hook Form
