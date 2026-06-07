"use client";

import { ProTable, type ProColumns } from "@ant-design/pro-components";
import { App, Tabs, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { fetchAllLeaveRequests, fetchAllOtRequests } from "@/features/requests/api";
import type { LeaveRequest, OtRequest, RequestStatus } from "@/features/requests/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const STATUS_COLOR: Record<RequestStatus, string> = {
  pending: "blue",
  approved: "green",
  rejected: "red",
};

export default function RequestsPage() {
  const t = useTranslations("requests");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (v: string | null) => (v ? new Date(v).toLocaleDateString(locale === "en" ? "en-US" : "vi-VN") : "—");
  const statusEnum = {
    pending: { text: t("status_pending") },
    approved: { text: t("status_approved") },
    rejected: { text: t("status_rejected") },
  };
  const statusTag = (s: RequestStatus) => <Tag color={STATUS_COLOR[s]}>{statusEnum[s]?.text ?? s}</Tag>;

  const leaveColumns: ProColumns<LeaveRequest>[] = [
    { title: t("requester"), dataIndex: "requesterName", search: false },
    {
      title: t("leaveType"),
      dataIndex: "leaveType",
      search: false,
      render: (_, r) => <Tag color={r.leaveType === "emergency" ? "volcano" : "default"}>{t(`leaveType_${r.leaveType}`)}</Tag>,
    },
    { title: t("startDate"), dataIndex: "startDate", search: false, render: (_, r) => fmtDate(r.startDate) },
    { title: t("endDate"), dataIndex: "endDate", search: false, render: (_, r) => fmtDate(r.endDate) },
    { title: t("reason"), dataIndex: "reason", search: false, ellipsis: true },
    { title: t("status"), dataIndex: "status", valueType: "select", valueEnum: statusEnum, render: (_, r) => statusTag(r.status) },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, r) => fmtDate(r.createdAt) },
  ];

  const otColumns: ProColumns<OtRequest>[] = [
    { title: t("requester"), dataIndex: "requesterName", search: false },
    { title: t("otDate"), dataIndex: "otDate", search: false, render: (_, r) => fmtDate(r.otDate) },
    { title: t("time"), search: false, render: (_, r) => `${r.startTime?.slice(0, 5)}–${r.endTime?.slice(0, 5)}` },
    { title: t("reason"), dataIndex: "reason", search: false, ellipsis: true },
    { title: t("status"), dataIndex: "status", valueType: "select", valueEnum: statusEnum, render: (_, r) => statusTag(r.status) },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, r) => fmtDate(r.createdAt) },
  ];

  return (
    <Tabs
      items={[
        {
          key: "leave",
          label: t("tabLeave"),
          children: (
            <ProTable<LeaveRequest>
              headerTitle={t("titleLeave")}
              rowKey="id"
              columns={leaveColumns}
              search={{ labelWidth: "auto" }}
              pagination={{ pageSize: 20, showSizeChanger: true }}
              options={{ reload: true, density: false, setting: false }}
              request={async (params) => {
                try {
                  const res = await fetchAllLeaveRequests({
                    page: params.current ?? 1,
                    pageSize: params.pageSize ?? 20,
                    status: params.status as string | undefined,
                  });
                  return { data: res.data, total: res.meta.total, success: true };
                } catch (error) {
                  showError(error);
                  return { data: [], total: 0, success: false };
                }
              }}
            />
          ),
        },
        {
          key: "ot",
          label: t("tabOt"),
          children: (
            <ProTable<OtRequest>
              headerTitle={t("titleOt")}
              rowKey="id"
              columns={otColumns}
              search={{ labelWidth: "auto" }}
              pagination={{ pageSize: 20, showSizeChanger: true }}
              options={{ reload: true, density: false, setting: false }}
              request={async (params) => {
                try {
                  const res = await fetchAllOtRequests({
                    page: params.current ?? 1,
                    pageSize: params.pageSize ?? 20,
                    status: params.status as string | undefined,
                  });
                  return { data: res.data, total: res.meta.total, success: true };
                } catch (error) {
                  showError(error);
                  return { data: [], total: 0, success: false };
                }
              }}
            />
          ),
        },
      ]}
    />
  );
}
