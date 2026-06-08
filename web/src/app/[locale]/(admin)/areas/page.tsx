"use client";

import { PlusOutlined } from "@ant-design/icons";
import { ModalForm, ProForm, ProFormSelect, ProFormText, ProTable, type ProColumns } from "@ant-design/pro-components";
import { App, Button, Popconfirm, Tag } from "antd";
import { useTranslations } from "next-intl";
import {
  useAreas,
  useCreateArea,
  useDeleteArea,
  useUpdateArea,
} from "@/features/organization/api";
import type { Area } from "@/features/organization/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

export default function AreasPage() {
  const t = useTranslations("areas");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const { message } = App.useApp();

  const { data: areas, isLoading } = useAreas();
  const createArea = useCreateArea();
  const updateArea = useUpdateArea();
  const deleteArea = useDeleteArea();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const parentOptions = (areas ?? []).map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` }));

  const columns: ProColumns<Area>[] = [
    { title: t("code"), dataIndex: "code", copyable: true, width: 160 },
    { title: t("name"), dataIndex: "name" },
    {
      title: t("parent"),
      dataIndex: "parentAreaName",
      render: (_, row) => (row.parentAreaName ? <Tag color="geekblue">{row.parentAreaName}</Tag> : "—"),
    },
    {
      title: t("actions"),
      key: "option",
      width: 160,
      render: (_, row) => [
        <EditAreaButton
          key="edit"
          area={row}
          parentOptions={parentOptions.filter((o) => o.value !== row.id)}
          update={updateArea}
          onError={showError}
          t={t}
        />,
        <Popconfirm
          key="delete"
          title={t("deleteConfirm")}
          okButtonProps={{ danger: true }}
          onConfirm={async () => {
            try {
              await deleteArea.mutateAsync(row.id);
              message.success(t("deleteSuccess"));
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
    <ProTable<Area>
      headerTitle={t("title")}
      rowKey="id"
      columns={columns}
      dataSource={areas ?? []}
      loading={isLoading}
      search={false}
      pagination={{ pageSize: 20 }}
      options={{ reload: false, density: false, setting: false }}
      toolBarRender={() => [
        <ModalForm
          key="create"
          title={t("createTitle")}
          trigger={
            <Button type="primary" icon={<PlusOutlined />}>
              {t("create")}
            </Button>
          }
          modalProps={{ destroyOnHidden: true }}
          onFinish={async (values) => {
            try {
              await createArea.mutateAsync({
                code: values.code as string,
                name: values.name as string,
                parentAreaId: (values.parentAreaId as string) || undefined,
              });
              message.success(t("createSuccess"));
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormText name="code" label={t("code")} rules={[{ required: true }, { max: 50 }]} />
          <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormSelect name="parentAreaId" label={t("parent")} options={parentOptions} showSearch allowClear />
        </ModalForm>,
      ]}
    />
  );
}

type EditProps = {
  area: Area;
  parentOptions: { value: string; label: string }[];
  update: ReturnType<typeof useUpdateArea>;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function EditAreaButton({ area, parentOptions, update, onError, t }: EditProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("editTitle")}
      trigger={<a>{t("edit")}</a>}
      modalProps={{ destroyOnHidden: true }}
      initialValues={{ name: area.name, parentAreaId: area.parentAreaId ?? undefined }}
      onFinish={async (values) => {
        try {
          await update.mutateAsync({
            id: area.id,
            payload: { name: values.name as string, parentAreaId: (values.parentAreaId as string) || undefined },
          });
          message.success(t("updateSuccess"));
          return true;
        } catch (error) {
          onError(error);
          return false;
        }
      }}
    >
      <ProForm.Item label={t("code")}>
        <span>{area.code}</span>
      </ProForm.Item>
      <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
      <ProFormSelect name="parentAreaId" label={t("parent")} options={parentOptions} showSearch allowClear />
    </ModalForm>
  );
}
