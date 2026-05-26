import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "RMMS 2026",
  description: "Retail Merchandiser Management System",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  // Locale-aware layout lives at [locale]/layout.tsx — this is the bare HTML shell.
  return children;
}
