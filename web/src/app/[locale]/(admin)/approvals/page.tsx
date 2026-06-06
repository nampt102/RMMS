"use client";

import { ProTable, type ActionType, type ProColumns } from "@ant-design/pro-components";
import { App, Form, Input, Modal, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState } from "react";
import {
  fetchAllApprovals,
  fetchPendingApprovals,
  useApproveApproval,
  useOverrideApproval,
  useRejectApproval,
} from "@/features/approvals/api";
import type { Approval, ApprovalStatus } from "@/features/approvals/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";
import { useAuthStore } from "@/lib/stores/auth-store";

const STATUS_COLOR: Record<ApprovalStatus, string> = {
  pending: "blue",
  approved: "green",
  rejected: "red",
  overridden: "purple",
};

export default function ApprovalsPage() {
  const t = useTranslations("approvals");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();
  const role = useAuthStore((s) => s.user?.role);
  const isAdmin = role === "admin";

  const approve = useApproveApproval();
  const reject = useRejectApproval();
  const override = useOverrideApproval();

  const [reasonModal, setReasonModal] = useState<{
    open: boolean;
    id: string | null;
    mode: "reject" | "override";
  }>({ open: false, id: null, mode: "reject" });
  const [form] = Form.useForm<{ reason: string }>();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string | null) =>
    value ? new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";

  const statusEnum = {
    pending: { text: t("status_pending") },
    approved: { text: t("status_approved") },
    rejected: { text: t("status_rejected") },
    overridden: { text: t("status_overridden") },
  };

  const entityText = (raw: string) => (t.has(`entity_${raw}`) ? t(`entity_${raw}`) : raw);

  const runApprove = async (id: string) => {
    try {
      await approve.mutateAsync(id);
      message.success(t("approveSuccess"));
      actionRef.current?.reload();
    } catch (error) {
      showError(error);
    }
  };

  const submitReason = async () => {
    const { reason } = await form.validateFields();
    const id = reasonModal.id!;
    try {
      if (reasonModal.mode === "reject") {
        await reject.mutateAsync({ id, reason });
        message.success(t("rejectSuccess"));
      } else {
        await override.mutateAsync({ id, reason });
        message.success(t("overrideSuccess"));
      }
      setReasonModal({ open: false, id: null, mode: "reject" });
      form.resetFields();
      actionRef.current?.reload();
    } catch (error) {
      if ((error as { errorFields?: unknown }).errorFields) return; // form validation
      showError(error);
    }
  };

  const columns: ProColumns<Approval>[] = [
    { title: t("requester"), dataIndex: "requesterName", search: false },
    {
      title: t("entityType"),
      dataIndex: "entityType",
      search: false,
      render: (_, row) => entityText(row.entityType),
    },
    {
      title: t("status"),
      dataIndex: "status",
      valueType: "select",
      valueEnum: statusEnum,
      // Status filter only matters for the admin (all-approvals) view.
      hideInSearch: !isAdmin,
      render: (_, row) => <Tag color={STATUS_COLOR[row.status]}>{statusEnum[row.status]?.text ?? row.status}</Tag>,
    },
    {
      title: t("reason"),
      search: false,
      render: (_, row) => row.overrideReason || row.decisionReason || "—",
    },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, row) => fmtDate(row.createdAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      render: (_, row) => {
        if (isAdmin) {
          return [
            <a
              key="override"
              onClick={() => {
                form.resetFields();
                setReasonModal({ open: true, id: row.id, mode: "override" });
              }}
            >
              {t("override")}
            </a>,
          ];
        }
        if (row.status !== "pending") return [<span key="none">—</span>];
        return [
          <a key="approve" onClick={() => runApprove(row.id)}>
            {t("approve")}
          </a>,
          <a
            key="reject"
            onClick={() => {
              form.resetFields();
              setReasonModal({ open: true, id: row.id, mode: "reject" });
            }}
          >
            {t("reject")}
          </a>,
        ];
      },
    },
  ];

  return (
    <>
      <ProTable<Approval>
        headerTitle={isAdmin ? t("titleAdmin") : t("title")}
        actionRef={actionRef}
        rowKey="id"
        columns={columns}
        search={isAdmin ? { labelWidth: "auto" } : false}
        pagination={isAdmin ? { pageSize: 20, showSizeChanger: true } : false}
        options={{ reload: true, density: false, setting: false }}
        request={async (params) => {
          try {
            if (isAdmin) {
              const res = await fetchAllApprovals({
                page: params.current ?? 1,
                pageSize: params.pageSize ?? 20,
                status: params.status as string | undefined,
              });
              return { data: res.data, total: res.meta.total, success: true };
            }
            const rows = await fetchPendingApprovals();
            return { data: rows, total: rows.length, success: true };
          } catch (error) {
            showError(error);
            return { data: [], total: 0, success: false };
          }
        }}
      />

      <Modal
        title={reasonModal.mode === "reject" ? t("rejectTitle") : t("overrideTitle")}
        open={reasonModal.open}
        onOk={submitReason}
        confirmLoading={reject.isPending || override.isPending}
        onCancel={() => {
          setReasonModal({ open: false, id: null, mode: "reject" });
          form.resetFields();
        }}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="reason"
            label={t("reasonLabel")}
            rules={[{ required: true, message: t("reasonRequired") }]}
          >
            <Input.TextArea rows={3} maxLength={500} showCount />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}
