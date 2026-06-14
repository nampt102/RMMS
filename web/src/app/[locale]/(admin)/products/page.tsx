"use client";

import { PlusOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProForm,
  ProFormSelect,
  ProFormText,
  ProFormTextArea,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import { App, Button, Popconfirm, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef } from "react";
import { useCategories } from "@/features/organization/api";
import {
  fetchProducts,
  useChangeProductStatus,
  useCreateProduct,
  useDeleteProduct,
  useUpdateProduct,
} from "@/features/products/api";
import type { Product } from "@/features/products/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

// Optional JSONB attributes: empty is fine, otherwise must parse as JSON.
const jsonRule = (msg: string) => ({
  validator: (_: unknown, value: string) => {
    if (!value || !value.trim()) return Promise.resolve();
    try {
      JSON.parse(value);
      return Promise.resolve();
    } catch {
      return Promise.reject(new Error(msg));
    }
  },
});

export default function ProductsPage() {
  const t = useTranslations("products");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const { data: categories } = useCategories();
  const createProduct = useCreateProduct();
  const updateProduct = useUpdateProduct();
  const changeStatus = useChangeProductStatus();
  const deleteProduct = useDeleteProduct();

  const categoryOptions = (categories ?? []).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }));

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

  const columns: ProColumns<Product>[] = [
    { title: t("search"), dataIndex: "search", valueType: "text", hideInTable: true },
    { title: t("sku"), dataIndex: "sku", copyable: true, search: false, width: 140 },
    { title: t("name"), dataIndex: "name", search: false },
    { title: t("brand"), dataIndex: "brand", search: false, render: (_, r) => r.brand || "—" },
    {
      title: t("category"),
      dataIndex: "categoryId",
      valueType: "select",
      fieldProps: { options: categoryOptions, allowClear: true, showSearch: true },
      render: (_, r) => (r.categoryName ? <Tag color="geekblue">{r.categoryName}</Tag> : "—"),
    },
    { title: t("status"), dataIndex: "status", valueType: "select", valueEnum: statusEnum, hideInSearch: true },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, r) => fmtDate(r.createdAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      width: 220,
      render: (_, row) => [
        <EditProductButton
          key="edit"
          product={row}
          categoryOptions={categoryOptions}
          update={updateProduct}
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
              await deleteProduct.mutateAsync(row.id);
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
    <ProTable<Product>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={{ labelWidth: "auto" }}
      pagination={{ pageSize: 50, showSizeChanger: true }}
      request={async (params) => {
        try {
          const res = await fetchProducts({
            page: params.current ?? 1,
            pageSize: params.pageSize ?? 50,
            categoryId: params.categoryId as string | undefined,
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
          modalProps={{ destroyOnHidden: true }}
          onFinish={async (values) => {
            try {
              await createProduct.mutateAsync({
                sku: values.sku as string,
                name: values.name as string,
                brand: (values.brand as string) || undefined,
                categoryId: (values.categoryId as string) || undefined,
                attributes: (values.attributes as string) || undefined,
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
          <ProFormText name="sku" label={t("sku")} rules={[{ required: true }, { max: 100 }]} />
          <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormText name="brand" label={t("brand")} rules={[{ max: 255 }]} />
          <ProFormSelect name="categoryId" label={t("category")} options={categoryOptions} showSearch allowClear />
          <ProFormTextArea
            name="attributes"
            label={t("attributes")}
            tooltip={t("attributesHint")}
            fieldProps={{ rows: 3, placeholder: '{"color":"red","size":"M"}' }}
            rules={[jsonRule(t("attributesInvalid"))]}
          />
        </ModalForm>,
      ]}
    />
  );
}

type EditProps = {
  product: Product;
  categoryOptions: { value: string; label: string }[];
  update: ReturnType<typeof useUpdateProduct>;
  onDone: () => void;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function EditProductButton({ product, categoryOptions, update, onDone, onError, t }: EditProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("editTitle")}
      trigger={<a>{t("edit")}</a>}
      modalProps={{ destroyOnHidden: true }}
      initialValues={{
        name: product.name,
        brand: product.brand ?? "",
        categoryId: product.categoryId ?? undefined,
        attributes: product.attributes ?? "",
      }}
      onFinish={async (values) => {
        try {
          await update.mutateAsync({
            id: product.id,
            payload: {
              name: values.name as string,
              brand: (values.brand as string) || undefined,
              categoryId: (values.categoryId as string) || undefined,
              attributes: (values.attributes as string) || undefined,
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
      <ProForm.Item label={t("sku")}>
        <span>{product.sku}</span>
      </ProForm.Item>
      <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
      <ProFormText name="brand" label={t("brand")} rules={[{ max: 255 }]} />
      <ProFormSelect name="categoryId" label={t("category")} options={categoryOptions} showSearch allowClear />
      <ProFormTextArea
        name="attributes"
        label={t("attributes")}
        tooltip={t("attributesHint")}
        fieldProps={{ rows: 3 }}
        rules={[jsonRule(t("attributesInvalid"))]}
      />
    </ModalForm>
  );
}
