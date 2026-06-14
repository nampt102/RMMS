"use client";

import { ProTable, type ActionType, type ProColumns } from "@ant-design/pro-components";
import { App, Descriptions, Drawer, Empty, Skeleton, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";
import dynamic from "next/dynamic";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState } from "react";
import { fetchAdminVisitPlans } from "@/features/visit-plans/api";
import type { VisitPlan, VisitPlanItem, VisitPlanStatus } from "@/features/visit-plans/types";
import type { Store } from "@/features/organization/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

// Leaflet touches the DOM directly → load client-side only (ADR-010), mirroring StoreMapView.
const StoreMap = dynamic(() => import("@/features/organization/StoreMap"), {
  ssr: false,
  loading: () => <Skeleton.Node active style={{ width: "100%", height: 260 }} />,
});

const STATUS_PRESENT: Record<VisitPlanStatus, "Default" | "Processing" | "Success" | "Error"> = {
  pending: "Processing",
  approved: "Success",
  rejected: "Error",
  executed: "Default",
};

export default function VisitPlansPage() {
  const t = useTranslations("visitPlans");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();
  const [selected, setSelected] = useState<VisitPlan | null>(null);

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string | null) =>
    value ? new Date(value).toLocaleDateString(locale === "en" ? "en-US" : "vi-VN") : "—";
  const fmtDateTime = (value: string | null) =>
    value ? new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";

  // Color + text (never color alone) — accessible status conveyance.
  const statusEnum = {
    pending: { text: t("status_pending"), status: STATUS_PRESENT.pending },
    approved: { text: t("status_approved"), status: STATUS_PRESENT.approved },
    rejected: { text: t("status_rejected"), status: STATUS_PRESENT.rejected },
    executed: { text: t("status_executed"), status: STATUS_PRESENT.executed },
  } as const;

  const progress = (plan: VisitPlan) => {
    const done = plan.items.filter((i) => i.formSubmissionId).length;
    return `${done}/${plan.items.length}`;
  };

  const columns: ProColumns<VisitPlan>[] = [
    { title: t("leader"), dataIndex: "leaderName", search: false, render: (_, r) => r.leaderName || "—" },
    { title: t("visitDate"), dataIndex: "visitDate", search: false, render: (_, r) => fmtDate(r.visitDate) },
    {
      title: t("stores"),
      dataIndex: "stores",
      search: false,
      align: "center",
      width: 90,
      render: (_, r) => r.items.length,
    },
    {
      title: t("progress"),
      dataIndex: "progress",
      search: false,
      align: "center",
      width: 110,
      render: (_, r) => progress(r),
    },
    {
      title: t("status"),
      dataIndex: "status",
      valueType: "select",
      valueEnum: statusEnum,
      fieldProps: { placeholder: t("filterStatus"), allowClear: true },
    },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, r) => fmtDateTime(r.createdAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      width: 100,
      render: (_, row) => [
        <a key="view" onClick={() => setSelected(row)}>
          {t("view")}
        </a>,
      ],
    },
  ];

  const itemColumns: ColumnsType<VisitPlanItem> = [
    {
      title: "#",
      dataIndex: "ordering",
      width: 48,
      align: "center",
      render: (_, r) => r.ordering + 1,
    },
    { title: t("itemStore"), dataIndex: "storeName", render: (_, r) => r.storeName || r.storeId },
    {
      title: t("itemForm"),
      dataIndex: "formName",
      render: (_, r) => (r.formName ? <Tag color="geekblue">{r.formName}</Tag> : r.formId),
    },
    {
      title: t("itemStatus"),
      dataIndex: "formSubmissionId",
      width: 130,
      render: (_, r) =>
        r.formSubmissionId ? <Tag color="green">{t("itemDone")}</Tag> : <Tag>{t("itemPending")}</Tag>,
    },
    { title: t("executedAt"), dataIndex: "executedAt", width: 170, render: (_, r) => fmtDateTime(r.executedAt) },
  ];

  return (
    <>
      <ProTable<VisitPlan>
        headerTitle={t("title")}
        actionRef={actionRef}
        rowKey="id"
        columns={columns}
        search={{ labelWidth: "auto" }}
        pagination={{ pageSize: 20, showSizeChanger: true }}
        locale={{ emptyText: <Empty description={t("empty")} /> }}
        request={async (params) => {
          try {
            const data = await fetchAdminVisitPlans({ status: params.status as VisitPlanStatus | undefined });
            return { data, total: data.length, success: true };
          } catch (error) {
            showError(error);
            return { data: [], total: 0, success: false };
          }
        }}
      />

      <Drawer
        title={t("detailTitle")}
        width={720}
        open={selected !== null}
        onClose={() => setSelected(null)}
        destroyOnClose
      >
        {selected && (
          <>
            <Descriptions column={1} size="small" bordered className="mb-4">
              <Descriptions.Item label={t("leader")}>{selected.leaderName || "—"}</Descriptions.Item>
              <Descriptions.Item label={t("visitDate")}>{fmtDate(selected.visitDate)}</Descriptions.Item>
              <Descriptions.Item label={t("status")}>
                <Tag
                  color={
                    selected.status === "approved"
                      ? "green"
                      : selected.status === "rejected"
                        ? "red"
                        : selected.status === "executed"
                          ? "blue"
                          : "gold"
                  }
                >
                  {t(`status_${selected.status}`)}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label={t("progress")}>{progress(selected)}</Descriptions.Item>
              <Descriptions.Item label={t("notes")}>{selected.notes || "—"}</Descriptions.Item>
            </Descriptions>

            {(() => {
              const mapStores: Store[] = selected.items
                .filter((i) => i.storeLat != null && i.storeLng != null)
                .map((i) => ({
                  id: i.storeId,
                  code: i.storeName ?? i.storeId,
                  name: i.storeName ?? i.storeId,
                  address: null,
                  latitude: i.storeLat as number,
                  longitude: i.storeLng as number,
                  areaId: null,
                  areaName: null,
                  status: "active",
                  createdAt: "",
                  updatedAt: null,
                }));
              if (mapStores.length === 0) return null;
              return (
                <div style={{ height: 260, marginBottom: 16 }}>
                  <StoreMap stores={mapStores} statusLabel={() => t("itemStore")} />
                </div>
              );
            })()}

            <Table<VisitPlanItem>
              rowKey="id"
              size="small"
              columns={itemColumns}
              dataSource={[...selected.items].sort((a, b) => a.ordering - b.ordering)}
              pagination={false}
              locale={{ emptyText: <Empty description={t("noItems")} /> }}
            />
          </>
        )}
      </Drawer>
    </>
  );
}
