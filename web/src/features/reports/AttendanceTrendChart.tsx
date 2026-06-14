"use client";

import { Card, Empty, Skeleton } from "antd";
import { useLocale, useTranslations } from "next-intl";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useAttendanceTrend } from "./api";

// WCAG-safe categorical colors (also distinguishable for common color blindness).
const COLORS = { valid: "#16a34a", late: "#d97706", anomaly: "#dc2626" };

export default function AttendanceTrendChart({ days = 14 }: { days?: number }) {
  const t = useTranslations("reports");
  const locale = useLocale();
  const { data, isLoading } = useAttendanceTrend(days);

  const fmtDay = (iso: string) => {
    const d = new Date(iso);
    return d.toLocaleDateString(locale === "en" ? "en-US" : "vi-VN", { day: "2-digit", month: "2-digit" });
  };

  const chartData = (data ?? []).map((p) => ({
    date: fmtDay(p.date),
    valid: p.valid,
    late: p.late,
    anomaly: p.anomaly,
  }));

  return (
    <Card size="small" title={t("trendTitle")}>
      {isLoading ? (
        <Skeleton active paragraph={{ rows: 4 }} />
      ) : chartData.length === 0 ? (
        <Empty description={t("trendEmpty")} />
      ) : (
        <div style={{ width: "100%", height: 280 }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#eee" vertical={false} />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
              <Tooltip />
              <Legend />
              <Bar dataKey="valid" name={t("legendValid")} stackId="a" fill={COLORS.valid} radius={[0, 0, 0, 0]} />
              <Bar dataKey="late" name={t("legendLate")} stackId="a" fill={COLORS.late} />
              <Bar dataKey="anomaly" name={t("legendAnomaly")} stackId="a" fill={COLORS.anomaly} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </Card>
  );
}
