"use client";

import { Badge, Tag } from "antd";
import { useTranslations } from "next-intl";

/** Role → AntD tag color. Distinct + accessible; never color-only (always paired with the label). */
export const ROLE_COLOR: Record<string, string> = {
  buh: "purple",
  leader: "blue",
  pg: "green",
  admin: "gold",
};

export function RoleTag({ role }: { role: string }) {
  const t = useTranslations("users");
  const key = `role_${role}`;
  const label = t.has(key) ? t(key) : role.toUpperCase();
  return (
    <Tag color={ROLE_COLOR[role] ?? "default"} style={{ marginInlineEnd: 0 }}>
      {label}
    </Tag>
  );
}

/** Compact status indicator (dot + label). Only rendered when not active to reduce clutter. */
export function StatusBadge({ status }: { status: string }) {
  const t = useTranslations("users");
  if (status === "active") return null;
  const map: Record<string, { key: string; status: "default" | "warning" | "error" }> = {
    inactive: { key: "status_inactive", status: "default" },
    pending_email_verify: { key: "status_pending", status: "warning" },
  };
  const cfg = map[status] ?? { key: "", status: "default" as const };
  const label = cfg.key && t.has(cfg.key) ? t(cfg.key) : status;
  return <Badge status={cfg.status} text={<span className="text-xs text-neutral-500">{label}</span>} />;
}
