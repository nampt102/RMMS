"use client";

import { ProTable, type ProColumns } from "@ant-design/pro-components";
import { App, Tag, Tooltip, Typography } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { fetchAuditLogs, type AuditLog } from "@/features/audit/api";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

export default function AuditLogsPage() {
  const t = useTranslations("audit");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const columns: ProColumns<AuditLog>[] = [
    {
      title: t("time"),
      dataIndex: "createdAt",
      search: false,
      width: 180,
      render: (_, r) => new Date(r.createdAt).toLocaleString(locale === "en" ? "en-US" : "vi-VN"),
    },
    { title: t("actor"), dataIndex: "actorName", search: false, render: (_, r) => r.actorName || "—" },
    { title: t("action"), dataIndex: "action", render: (_, r) => <Tag>{r.action}</Tag> },
    { title: t("entity"), dataIndex: "targetEntity" },
    { title: t("ip"), dataIndex: "ipAddress", search: false, render: (_, r) => r.ipAddress || "—" },
    {
      title: t("metadata"),
      dataIndex: "metadata",
      search: false,
      ellipsis: true,
      render: (_, r) =>
        r.metadata && r.metadata !== "{}" ? (
          <Tooltip title={<pre className="m-0 whitespace-pre-wrap text-xs">{r.metadata}</pre>}>
            <Typography.Text className="text-xs" code>
              {r.metadata.length > 40 ? `${r.metadata.slice(0, 40)}…` : r.metadata}
            </Typography.Text>
          </Tooltip>
        ) : (
          "—"
        ),
    },
  ];

  return (
    <ProTable<AuditLog>
      headerTitle={t("title")}
      rowKey="id"
      columns={columns}
      search={{ labelWidth: "auto" }}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      options={{ reload: true, density: false, setting: false }}
      request={async (params) => {
        try {
          const res = await fetchAuditLogs({
            page: params.current ?? 1,
            pageSize: params.pageSize ?? 20,
            action: params.action as string | undefined,
            targetEntity: params.targetEntity as string | undefined,
          });
          return { data: res.data, total: res.meta.total, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
    />
  );
}
