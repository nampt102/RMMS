"use client";

import {
  AlertOutlined,
  AuditOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  LoginOutlined,
  LogoutOutlined,
  ReloadOutlined,
  RightOutlined,
  SolutionOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import { Button, Card, Col, Empty, Row, Spin, Statistic, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import type { ReactNode } from "react";
import { useDashboardSummary } from "@/features/dashboard/api";
import { useTeamToday, type TeamMemberStatus } from "@/features/monitoring/api";

const STATUS_COLOR: Record<string, string> = {
  working: "green",
  checked_out: "blue",
  not_checked_in: "orange",
  on_leave: "purple",
  no_schedule_today: "default",
  pending_review: "red",
};

// Present today = anyone with a check-in (working / checked-out / pending review). The
// dashboard list is a quick glance; the full roster lives on /monitoring.
const PRESENT = new Set(["working", "checked_out", "pending_review"]);

export default function DashboardPage() {
  const t = useTranslations("dashboard");
  const tMon = useTranslations("monitoring");
  const locale = useLocale();
  const { data, isFetching, refetch } = useDashboardSummary();
  const { data: team, isFetching: teamFetching } = useTeamToday();

  const fmtTime = (v: string | null) =>
    v ? new Date(v).toLocaleTimeString(locale === "en" ? "en-US" : "vi-VN", { hour: "2-digit", minute: "2-digit" }) : "—";
  const statusText = (s: string) => (tMon.has(`status_${s}`) ? tMon(`status_${s}`) : s);

  const present = (team?.members ?? []).filter((m) => PRESENT.has(m.status));

  const columns: ColumnsType<TeamMemberStatus> = [
    { title: tMon("name"), dataIndex: "fullName" },
    { title: tMon("role"), dataIndex: "role", render: (r: string) => r.toUpperCase() },
    {
      title: tMon("status"),
      dataIndex: "status",
      render: (s: string) => <Tag color={STATUS_COLOR[s] ?? "default"}>{statusText(s)}</Tag>,
    },
    { title: tMon("checkInAt"), dataIndex: "checkInAt", render: fmtTime },
    { title: tMon("store"), dataIndex: "storeName", render: (v: string | null) => v || "—" },
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <Typography.Title level={4} className="!mb-0">
          {t("title")}
        </Typography.Title>
        <div className="flex items-center gap-3">
          {data?.asOf && (
            <Typography.Text type="secondary">
              {t("asOf")}: {new Date(data.asOf).toLocaleTimeString(locale === "en" ? "en-US" : "vi-VN")}
            </Typography.Text>
          )}
          <Button icon={<ReloadOutlined />} loading={isFetching} onClick={() => refetch()}>
            {t("refresh")}
          </Button>
        </div>
      </div>

      {/* ── Presence (today) ───────────────────────────────────────── */}
      <Typography.Text type="secondary" className="text-xs font-medium uppercase tracking-wide">
        {t("sectionPresence")}
      </Typography.Text>
      <Row gutter={[12, 12]}>
        <Kpi label={t("totalMembers")} value={data?.totalMembers} icon={<TeamOutlined />} loading={isFetching} />
        <Kpi label={t("online")} value={data?.online} icon={<CheckCircleOutlined />} color="#16a34a" loading={isFetching} />
        <Kpi label={t("checkedOut")} value={data?.checkedOutToday} icon={<LogoutOutlined />} color="#2563eb" loading={isFetching} />
        <Kpi label={t("notCheckedIn")} value={data?.notCheckedIn} icon={<LoginOutlined />} color="#d97706" loading={isFetching} />
        <Kpi label={t("onLeave")} value={data?.onLeave} icon={<ClockCircleOutlined />} color="#9333ea" loading={isFetching} />
      </Row>

      {/* ── Actionable backlogs ────────────────────────────────────── */}
      <Typography.Text type="secondary" className="text-xs font-medium uppercase tracking-wide">
        {t("sectionActionable")}
      </Typography.Text>
      <Row gutter={[12, 12]}>
        <ActionKpi
          href={`/${locale}/attendance`}
          label={t("pendingReview")}
          value={data?.pendingReviewAttendance}
          icon={<ClockCircleOutlined />}
          color="#d97706"
          loading={isFetching}
        />
        <ActionKpi
          href={`/${locale}/approvals`}
          label={t("pendingApprovals")}
          value={data?.pendingApprovals}
          icon={<AuditOutlined />}
          color="#2563eb"
          loading={isFetching}
        />
        <ActionKpi
          href={`/${locale}/attendance`}
          label={t("anomaliesToday")}
          value={data?.anomaliesToday}
          icon={<AlertOutlined />}
          color="#dc2626"
          loading={isFetching}
        />
      </Row>

      {/* ── Present today (quick glance) ───────────────────────────── */}
      <div className="mt-2 flex items-center justify-between">
        <Typography.Text type="secondary" className="text-xs font-medium uppercase tracking-wide">
          {t("sectionPresentList")}
        </Typography.Text>
        <Link href={`/${locale}/monitoring`} className="text-sm">
          {t("viewAll")} <RightOutlined className="text-xs" />
        </Link>
      </div>
      {teamFetching && !team ? (
        <div className="flex justify-center py-12">
          <Spin size="large" />
        </div>
      ) : (
        <Table<TeamMemberStatus>
          rowKey="userId"
          size="small"
          columns={columns}
          dataSource={present}
          pagination={present.length > 8 ? { pageSize: 8 } : false}
          locale={{ emptyText: <Empty description={t("emptyPresent")} image={<SolutionOutlined style={{ fontSize: 40 }} />} /> }}
        />
      )}
    </div>
  );
}

/** A presence KPI tile (read-only). */
function Kpi({
  label,
  value,
  icon,
  color,
  loading,
}: {
  label: string;
  value: number | undefined;
  icon: ReactNode;
  color?: string;
  loading: boolean;
}) {
  return (
    <Col xs={12} sm={8} md={8} lg={4} xl={4}>
      <Card size="small">
        <Statistic
          title={
            <span className="inline-flex items-center gap-1.5" style={{ color }}>
              {icon}
              {label}
            </span>
          }
          value={loading ? undefined : (value ?? 0)}
          valueStyle={{ color, fontVariantNumeric: "tabular-nums", fontWeight: 600 }}
        />
      </Card>
    </Col>
  );
}

/** An actionable backlog tile — the whole card links to the relevant queue. */
function ActionKpi({
  href,
  label,
  value,
  icon,
  color,
  loading,
}: {
  href: string;
  label: string;
  value: number | undefined;
  icon: ReactNode;
  color: string;
  loading: boolean;
}) {
  return (
    <Col xs={24} sm={8}>
      <Link href={href} className="block">
        <Card size="small" hoverable className="cursor-pointer">
          <Statistic
            title={
              <span className="inline-flex items-center gap-1.5" style={{ color }}>
                {icon}
                {label}
              </span>
            }
            value={loading ? undefined : (value ?? 0)}
            valueStyle={{ color, fontVariantNumeric: "tabular-nums", fontWeight: 700 }}
            suffix={<RightOutlined className="text-xs text-neutral-400" />}
          />
        </Card>
      </Link>
    </Col>
  );
}
