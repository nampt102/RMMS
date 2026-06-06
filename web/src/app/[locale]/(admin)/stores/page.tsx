"use client";

import { PlusOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProForm,
  ProFormDigit,
  ProFormSelect,
  ProFormText,
  ProFormTextArea,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import { App, Button, Popconfirm, Segmented, Tag } from "antd";
import { EnvironmentOutlined, TableOutlined } from "@ant-design/icons";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState } from "react";
import {
  fetchStores,
  useAreas,
  useChangeStoreStatus,
  useCreateStore,
  useDeleteStore,
  useUpdateStore,
} from "@/features/organization/api";
import type { Store } from "@/features/organization/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";
import StoreMapView from "@/features/organization/StoreMapView";

export default function StoresPage() {
  const t = useTranslations("stores");
  const [view, setView] = useState<"table" | "map">("table");

  return (
    <div>
      <Segmented
        value={view}
        onChange={(v) => setView(v as "table" | "map")}
        style={{ marginBottom: 16 }}
        options={[
          { value: "table", label: t("viewTable"), icon: <TableOutlined /> },
          { value: "map", label: t("viewMap"), icon: <EnvironmentOutlined /> },
        ]}
      />
      {view === "table" ? <StoresTable /> : <StoreMapView />}
    </div>
  );
}

function StoresTable() {
  const t = useTranslations("stores");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const { data: areas } = useAreas();
  const createStore = useCreateStore();
  const updateStore = useUpdateStore();
  const changeStatus = useChangeStoreStatus();
  const deleteStore = useDeleteStore();

  const areaOptions = (areas ?? []).map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` }));

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string | null) =>
    value ? new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";

  const statusEnum = {
    active: { text: t("status_active"), status: "Success" },
    inactive: { text: t("status_inactive"), status: "Default" },
  };

  const reload = () => actionRef.current?.reload();

  const columns: ProColumns<Store>[] = [
    { title: t("search"), dataIndex: "search", valueType: "text", hideInTable: true },
    { title: t("code"), dataIndex: "code", copyable: true, search: false, width: 120 },
    { title: t("name"), dataIndex: "name", search: false },
    {
      title: t("area"),
      dataIndex: "areaId",
      valueType: "select",
      fieldProps: { options: areaOptions, allowClear: true },
      render: (_, row) => (row.areaName ? <Tag color="geekblue">{row.areaName}</Tag> : "—"),
    },
    {
      title: t("status"),
      dataIndex: "status",
      valueType: "select",
      valueEnum: statusEnum,
    },
    {
      title: t("gps"),
      key: "gps",
      search: false,
      render: (_, row) => (
        <span className="tabular-nums">
          {row.latitude.toFixed(5)}, {row.longitude.toFixed(5)}
        </span>
      ),
    },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, row) => fmtDate(row.createdAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      width: 220,
      render: (_, row) => [
        <EditStoreButton
          key="edit"
          store={row}
          areaOptions={areaOptions}
          update={updateStore}
          onDone={reload}
          onError={showError}
          t={t}
        />,
        <Popconfirm
          key="status"
          title={row.status === "active" ? t("deactivateConfirm") : t("activateConfirm")}
          onConfirm={async () => {
            try {
              await changeStatus.mutateAsync({ id: row.id, status: row.status === "active" ? "inactive" : "active" });
              message.success(t("statusSuccess"));
              reload();
            } catch (error) {
              showError(error);
            }
          }}
        >
          <a>{row.status === "active" ? t("deactivate") : t("activate")}</a>
        </Popconfirm>,
        <Popconfirm
          key="delete"
          title={t("deleteConfirm")}
          okButtonProps={{ danger: true }}
          onConfirm={async () => {
            try {
              await deleteStore.mutateAsync(row.id);
              message.success(t("deleteSuccess"));
              reload();
            } catch (error) {
              showError(error);
            }
          }}
        >
          <a className="text-red-600">{tc("delete")}</a>
        </Popconfirm>,
      ],
    },
  ];

  return (
    <ProTable<Store>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={{ labelWidth: "auto" }}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      request={async (params) => {
        try {
          const res = await fetchStores({
            page: params.current ?? 1,
            pageSize: params.pageSize ?? 20,
            areaId: params.areaId as string | undefined,
            status: params.status as string | undefined,
            search: params.search as string | undefined,
          });
          return { data: res.data, total: res.meta.total, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
      toolBarRender={() => [
        <ModalForm
          key="create"
          title={t("createTitle")}
          trigger={
            <Button type="primary" icon={<PlusOutlined />}>
              {t("create")}
            </Button>
          }
          modalProps={{ destroyOnClose: true }}
          onFinish={async (values) => {
            try {
              await createStore.mutateAsync({
                code: values.code as string,
                name: values.name as string,
                address: (values.address as string) || undefined,
                latitude: Number(values.latitude),
                longitude: Number(values.longitude),
                areaId: (values.areaId as string) || undefined,
              });
              message.success(t("createSuccess"));
              reload();
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormText name="code" label={t("code")} rules={[{ required: true }, { max: 50 }]} />
          <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormTextArea name="address" label={t("address")} fieldProps={{ rows: 2 }} />
          <ProForm.Group>
            <ProFormDigit
              name="latitude"
              label={t("latitude")}
              rules={[{ required: true }]}
              min={-90}
              max={90}
              fieldProps={{ precision: 7, step: 0.000001 }}
              width="sm"
            />
            <ProFormDigit
              name="longitude"
              label={t("longitude")}
              rules={[{ required: true }]}
              min={-180}
              max={180}
              fieldProps={{ precision: 7, step: 0.000001 }}
              width="sm"
            />
          </ProForm.Group>
          <ProFormSelect name="areaId" label={t("area")} options={areaOptions} showSearch allowClear />
        </ModalForm>,
      ]}
    />
  );
}

type EditProps = {
  store: Store;
  areaOptions: { value: string; label: string }[];
  update: ReturnType<typeof useUpdateStore>;
  onDone: () => void;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function EditStoreButton({ store, areaOptions, update, onDone, onError, t }: EditProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("editTitle")}
      trigger={<a>{t("edit")}</a>}
      modalProps={{ destroyOnClose: true }}
      initialValues={{
        name: store.name,
        address: store.address ?? "",
        latitude: store.latitude,
        longitude: store.longitude,
        areaId: store.areaId ?? undefined,
      }}
      onFinish={async (values) => {
        try {
          await update.mutateAsync({
            id: store.id,
            payload: {
              name: values.name as string,
              address: (values.address as string) || undefined,
              latitude: Number(values.latitude),
              longitude: Number(values.longitude),
              areaId: (values.areaId as string) || undefined,
            },
          });
          message.success(t("updateSuccess"));
          onDone();
          return true;
        } catch (error) {
          onError(error);
          return false;
        }
      }}
    >
      <ProForm.Item label={t("code")}>
        <span>{store.code}</span>
      </ProForm.Item>
      <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
      <ProFormTextArea name="address" label={t("address")} fieldProps={{ rows: 2 }} />
      <ProForm.Group>
        <ProFormDigit name="latitude" label={t("latitude")} rules={[{ required: true }]} min={-90} max={90} fieldProps={{ precision: 7, step: 0.000001 }} width="sm" />
        <ProFormDigit name="longitude" label={t("longitude")} rules={[{ required: true }]} min={-180} max={180} fieldProps={{ precision: 7, step: 0.000001 }} width="sm" />
      </ProForm.Group>
      <ProFormSelect name="areaId" label={t("area")} options={areaOptions} showSearch allowClear />
    </ModalForm>
  );
}
