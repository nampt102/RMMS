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
      { protocol: "https", hostname: "**.motivesvn.com" },
      { protocol: "http", hostname: "localhost" },
    ],
  },
  async rewrites() {
    return [
      // Proxy API calls in dev so we don't fight CORS
      {
        source: "/api/proxy/:path*",
        destination: `${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080"}/api/:path*`,
      },
    ];
  },
};

export default withNextIntl(nextConfig);
