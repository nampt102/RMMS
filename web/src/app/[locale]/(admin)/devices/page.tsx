"use client";

import { CheckOutlined } from "@ant-design/icons";
import { ModalForm, ProFormTextArea, ProTable, type ActionType, type ProColumns } from "@ant-design/pro-components";
import { App, Button, Empty, Popconfirm, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef } from "react";
import { fetchPendingDevices, useApproveDevice, useRejectDevice } from "@/features/devices/api";
import type { PendingDevice } from "@/features/devices/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const ROLE_COLORS: Record<string, string> = {
  pg: "blue",
  leader: "geekblue",
  buh: "purple",
  admin: "red",
};

export default function DeviceRequestsPage() {
  const t = useTranslations("devices");
  const tErrors = useTranslations("errors");
  const tUsers = useTranslations("users");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const approve = useApproveDevice();
  const reject = useRejectDevice();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string) => new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN");

  const columns: ProColumns<PendingDevice>[] = [
    { title: t("userEmail"), dataIndex: "userEmail", copyable: true },
    { title: t("userFullName"), dataIndex: "userFullName" },
    {
      title: t("role"),
      dataIndex: "userRole",
      render: (_, row) => (
        <Tag color={ROLE_COLORS[row.userRole] ?? "default"}>{tUsers(`role_${row.userRole}`)}</Tag>
      ),
    },
    { title: t("deviceName"), dataIndex: "deviceName" },
    { title: t("os"), dataIndex: "os" },
    { title: t("requestedAt"), dataIndex: "requestedAt", render: (_, row) => fmtDate(row.requestedAt) },
    {
      title: t("actions"),
      key: "option",
      render: (_, row) => [
        <Popconfirm
          key="approve"
          title={t("approveConfirm")}
          okText={t("approve")}
          onConfirm={async () => {
            try {
              await approve.mutateAsync(row.deviceId);
              message.success(t("approveSuccess"));
              actionRef.current?.reload();
            } catch (error) {
              showError(error);
            }
          }}
        >
          <Button type="link" icon={<CheckOutlined />}>
            {t("approve")}
          </Button>
        </Popconfirm>,
        <ModalForm
          key="reject"
          title={t("rejectTitle")}
          trigger={
            <Button type="link" danger>
              {t("reject")}
            </Button>
          }
          width={420}
          modalProps={{ destroyOnClose: true }}
          onFinish={async (values) => {
            try {
              await reject.mutateAsync({ deviceId: row.deviceId, reason: values.reason as string });
              message.success(t("rejectSuccess"));
              actionRef.current?.reload();
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormTextArea
            name="reason"
            label={t("rejectReason")}
            rules={[{ required: true, message: t("rejectReasonRequired") }]}
            fieldProps={{ rows: 3, maxLength: 500, showCount: true }}
          />
        </ModalForm>,
      ],
    },
  ];

  return (
    <ProTable<PendingDevice>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="deviceId"
      columns={columns}
      search={false}
      pagination={false}
      options={{ reload: true, setting: false, density: false }}
      locale={{ emptyText: <Empty description={t("empty")} /> }}
      request={async () => {
        try {
          const data = await fetchPendingDevices();
          return { data, total: data.length, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
    />
  );
}
