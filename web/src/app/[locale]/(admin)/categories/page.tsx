"use client";

import { PlusOutlined } from "@ant-design/icons";
import { ModalForm, ProForm, ProFormText, ProTable, type ProColumns } from "@ant-design/pro-components";
import { App, Button, Popconfirm } from "antd";
import { useTranslations } from "next-intl";
import {
  useCategories,
  useCreateCategory,
  useDeleteCategory,
  useUpdateCategory,
} from "@/features/organization/api";
import type { Category } from "@/features/organization/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

export default function CategoriesPage() {
  const t = useTranslations("categories");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const { message } = App.useApp();

  const { data: categories, isLoading } = useCategories();
  const createCategory = useCreateCategory();
  const updateCategory = useUpdateCategory();
  const deleteCategory = useDeleteCategory();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const columns: ProColumns<Category>[] = [
    { title: t("code"), dataIndex: "code", copyable: true, width: 160 },
    { title: t("name"), dataIndex: "name" },
    {
      title: t("actions"),
      key: "option",
      width: 160,
      render: (_, row) => [
        <EditCategoryButton key="edit" category={row} update={updateCategory} onError={showError} t={t} />,
        <Popconfirm
          key="delete"
          title={t("deleteConfirm")}
          okButtonProps={{ danger: true }}
          onConfirm={async () => {
            try {
              await deleteCategory.mutateAsync(row.id);
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
    <ProTable<Category>
      headerTitle={t("title")}
      rowKey="id"
      columns={columns}
      dataSource={categories ?? []}
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
          modalProps={{ destroyOnClose: true }}
          onFinish={async (values) => {
            try {
              await createCategory.mutateAsync({ code: values.code as string, name: values.name as string });
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
        </ModalForm>,
      ]}
    />
  );
}

type EditProps = {
  category: Category;
  update: ReturnType<typeof useUpdateCategory>;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function EditCategoryButton({ category, update, onError, t }: EditProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("editTitle")}
      trigger={<a>{t("edit")}</a>}
      modalProps={{ destroyOnClose: true }}
      initialValues={{ name: category.name }}
      onFinish={async (values) => {
        try {
          await update.mutateAsync({ id: category.id, payload: { name: values.name as string } });
          message.success(t("updateSuccess"));
          return true;
        } catch (error) {
          onError(error);
          return false;
        }
      }}
    >
      <ProForm.Item label={t("code")}>
        <span>{category.code}</span>
      </ProForm.Item>
      <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
    </ModalForm>
  );
}
