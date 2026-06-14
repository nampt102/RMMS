"use client";

import { DownloadOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { App, Button, Card, DatePicker, Empty, Segmented, Select, Space, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs, { type Dayjs } from "dayjs";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { downloadReportExcel, fetchAttendanceReport } from "@/features/reports/api";
import type { AttendanceReportRow } from "@/features/reports/types";
import { useStoresForMap } from "@/features/organization/api";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

type Kind = "attendance" | "anomalies";

const STATUS_COLOR: Record<string, string> = {
  valid: "green",
  late: "gold",
  admin_approved: "green",
  admin_rejected: "default",
  gps_violation_pending_review: "orange",
  face_fail_pending_review: "orange",
  fake_gps_blocked: "red",
};

export default function ReportsPage() {
  const t = useTranslations("reports");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();

  const [kind, setKind] = useState<Kind>("attendance");
  const [range, setRange] = useState<[Dayjs, Dayjs]>([dayjs().subtract(29, "day"), dayjs()]);
  const [storeId, setStoreId] = useState<string | undefined>(undefined);
  const [exporting, setExporting] = useState(false);

  const filters = {
    from: range[0].format("YYYY-MM-DD"),
    to: range[1].format("YYYY-MM-DD"),
    storeId,
  };

  const { data: stores } = useStoresForMap({});
  const storeOptions = (stores ?? []).map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` }));

  const { data, isFetching, isError } = useQuery({
    queryKey: ["reports", kind, filters.from, filters.to, storeId],
    queryFn: () => fetchAttendanceReport(kind, filters),
  });

  const fmtDateTime = (v: string | null) =>
    v ? new Date(v).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";
  const statusText = (s: string) => (t.has(`status_${s}`) ? t(`status_${s}`) : s);

  const onExport = async () => {
    setExporting(true);
    try {
      await downloadReportExcel(kind, filters);
    } catch (error) {
      const code = errorCodeFromUnknown(error);
      message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
    } finally {
      setExporting(false);
    }
  };

  const columns: ColumnsType<AttendanceReportRow> = [
    { title: t("colDate"), dataIndex: "date", width: 110 },
    { title: t("colUser"), dataIndex: "userName" },
    { title: t("colStore"), dataIndex: "storeName" },
    { title: t("colCheckIn"), dataIndex: "checkInAt", render: fmtDateTime },
    { title: t("colCheckOut"), dataIndex: "checkOutAt", render: fmtDateTime },
    {
      title: t("colStatus"),
      dataIndex: "status",
      render: (s: string) => <Tag color={STATUS_COLOR[s] ?? "default"}>{statusText(s)}</Tag>,
    },
  ];

  return (
    <Card
      title={t("title")}
      extra={
        <Button type="primary" icon={<DownloadOutlined />} loading={exporting} onClick={onExport}>
          {t("export")}
        </Button>
      }
    >
      <Space wrap size="middle" style={{ marginBottom: 16 }}>
        <Segmented
          value={kind}
          onChange={(v) => setKind(v as Kind)}
          options={[
            { value: "attendance", label: t("kindAttendance") },
            { value: "anomalies", label: t("kindAnomalies") },
          ]}
        />
        <DatePicker.RangePicker
          value={range}
          allowClear={false}
          onChange={(v) => {
            if (v && v[0] && v[1]) setRange([v[0], v[1]]);
          }}
        />
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 240 }}
          placeholder={t("filterStore")}
          options={storeOptions}
          value={storeId}
          onChange={setStoreId}
        />
      </Space>

      <Table<AttendanceReportRow>
        rowKey={(r) => `${r.userName}-${r.checkInAt}`}
        size="small"
        loading={isFetching}
        columns={columns}
        dataSource={isError ? [] : (data ?? [])}
        pagination={{ pageSize: 20, showSizeChanger: true }}
        locale={{ emptyText: <Empty description={t("empty")} /> }}
      />
    </Card>
  );
}
