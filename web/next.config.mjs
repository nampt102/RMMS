import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/lib/i18n/request.ts");

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  // Standalone is required for Docker (see web/Dockerfile). Opt-in locally — Windows
  // dev machines often lack symlink permission (EPERM) during `next build`.
  output: process.env.NEXT_OUTPUT_STANDALONE === "true" ? "standalone" : undefined,
  experimental: {
    typedRoutes: true,
  },
  images: {
    remotePatterns: [
      { protocol: "https", hostname: "**.gmail.com" },
      { protocol: "http", hostname: "localhost" },
    ],
  },
  async rewrites() {
    // Dev convenience: proxy /api/proxy/* → backend to avoid CORS. Next requires an
    // absolute (http/https) destination, so only emit the rewrite when a valid base
    // URL is configured. In prod the web image is built with NEXT_PUBLIC_API_BASE_URL
    // empty (browser calls relative /api/* and Caddy path-routes it) → no rewrite.
    const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
    if (!apiBase || !/^https?:\/\//.test(apiBase)) {
      return [];
    }
    return [
      {
        source: "/api/proxy/:path*",
        destination: `${apiBase}/api/:path*`,
      },
    ];
  },
};

export default withNextIntl(nextConfig);
