"use client";

import { ReloadOutlined } from "@ant-design/icons";
import { Button, Card, Col, Empty, Row, Spin, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import { useLocale, useTranslations } from "next-intl";
import { useTeamToday, type TeamMemberStatus } from "@/features/monitoring/api";

const STATUS_COLOR: Record<string, string> = {
  working: "green",
  checked_out: "blue",
  not_checked_in: "orange",
  on_leave: "purple",
  no_schedule_today: "default",
  pending_review: "red",
};

const SUMMARY_ORDER = ["working", "not_checked_in", "pending_review", "checked_out", "on_leave", "no_schedule_today"];

export default function MonitoringPage() {
  const t = useTranslations("monitoring");
  const locale = useLocale();
  const { data, isFetching, refetch } = useTeamToday();

  const statusText = (s: string) => (t.has(`status_${s}`) ? t(`status_${s}`) : s);
  const fmtTime = (v: string | null) =>
    v ? new Date(v).toLocaleTimeString(locale === "en" ? "en-US" : "vi-VN", { hour: "2-digit", minute: "2-digit" }) : "—";

  const columns: ColumnsType<TeamMemberStatus> = [
    { title: t("name"), dataIndex: "fullName" },
    { title: t("role"), dataIndex: "role", render: (r: string) => statusText(r) === r ? r.toUpperCase() : r.toUpperCase() },
    { title: t("status"), dataIndex: "status", render: (s: string) => <Tag color={STATUS_COLOR[s] ?? "default"}>{statusText(s)}</Tag> },
    { title: t("checkInAt"), dataIndex: "checkInAt", render: fmtTime },
    { title: t("store"), dataIndex: "storeName", render: (v: string | null) => v || "—" },
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

      <Row gutter={[12, 12]}>
        {SUMMARY_ORDER.map((s) => (
          <Col key={s} xs={12} sm={8} md={4}>
            <Card size="small">
              <Typography.Text type="secondary" className="text-xs">
                {statusText(s)}
              </Typography.Text>
              <div className="text-2xl font-semibold">{data?.summary?.[s] ?? 0}</div>
            </Card>
          </Col>
        ))}
      </Row>

      {isFetching && !data ? (
        <div className="flex justify-center py-16">
          <Spin size="large" />
        </div>
      ) : (
        <Table<TeamMemberStatus>
          rowKey="userId"
          columns={columns}
          dataSource={data?.members ?? []}
          pagination={{ pageSize: 20, showSizeChanger: true }}
          locale={{ emptyText: <Empty description={t("empty")} /> }}
        />
      )}
    </div>
  );
}
