import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/app/**/*.{ts,tsx}",
    "./src/components/**/*.{ts,tsx}",
    "./src/features/**/*.{ts,tsx}",
    "./src/pages/**/*.{ts,tsx}",
  ],
  // Antd uses its own reset; disable Tailwind's preflight to avoid double-reset.
  corePlugins: { preflight: false },
  important: true, // Tailwind utility wins over AntD specificity when applied.
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#1677ff", // AntD primary blue
          50: "#e6f4ff",
          500: "#1677ff",
          600: "#0958d9",
          700: "#003eb3",
        },
      },
    },
  },
  plugins: [],
};

export default config;
