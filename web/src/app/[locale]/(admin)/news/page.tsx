"use client";

import { PlusOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProFormSelect,
  ProFormSwitch,
  ProFormText,
  ProFormTextArea,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import { App, Button, Popconfirm, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef } from "react";
import {
  fetchAdminNews,
  useAssignNews,
  useCreateNews,
  useDeleteNews,
  usePublishNews,
  useUpdateNews,
} from "@/features/news/api";
import type { AdminNewsItem } from "@/features/news/types";
import { fetchUsers } from "@/features/users/api";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

export default function NewsPage() {
  const t = useTranslations("news");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const create = useCreateNews();
  const update = useUpdateNews();
  const assign = useAssignNews();
  const publish = usePublishNews();
  const del = useDeleteNews();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };
  const reload = () => actionRef.current?.reload();
  const fmtDate = (v: string | null) => (v ? new Date(v).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—");

  const columns: ProColumns<AdminNewsItem>[] = [
    {
      title: t("title_col"),
      dataIndex: "titleVi",
      search: false,
      render: (_, r) => (
        <div className="flex items-center gap-2">
          <span>{locale === "en" ? r.titleEn : r.titleVi}</span>
          {r.isImportant && <Tag color="red">{t("important")}</Tag>}
        </div>
      ),
    },
    { title: t("category"), dataIndex: "category", search: false, render: (_, r) => r.category || "—" },
    {
      title: t("status"),
      dataIndex: "isPublished",
      search: false,
      render: (_, r) =>
        r.isPublished ? <Tag color="green">{t("status_published")}</Tag> : <Tag>{t("status_draft")}</Tag>,
    },
    { title: t("publishedAt"), dataIndex: "publishedAt", search: false, render: (_, r) => fmtDate(r.publishedAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      width: 260,
      render: (_, row) => [
        <NewsFormButton key="edit" trigger={<a>{t("edit")}</a>} initial={row} t={t} onError={showError}
          onFinish={async (payload) => {
            await update.mutateAsync({ id: row.id, payload });
            message.success(t("updateSuccess"));
            reload();
          }}
        />,
        <AssignNewsButton key="assign" newsId={row.id} assign={assign} onError={showError} t={t} />,
        !row.isPublished ? (
          <Popconfirm
            key="publish"
            title={t("publishConfirm")}
            onConfirm={async () => {
              try {
                await publish.mutateAsync(row.id);
                message.success(t("publishSuccess"));
                reload();
              } catch (error) {
                showError(error);
              }
            }}
          >
            <a>{t("publish")}</a>
          </Popconfirm>
        ) : (
          <span key="published" className="text-neutral-400">
            {t("status_published")}
          </span>
        ),
        <Popconfirm
          key="delete"
          title={t("deleteConfirm")}
          okButtonProps={{ danger: true }}
          onConfirm={async () => {
            try {
              await del.mutateAsync(row.id);
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
    <ProTable<AdminNewsItem>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={false}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      request={async () => {
        try {
          const data = await fetchAdminNews();
          return { data, total: data.length, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
      toolBarRender={() => [
        <NewsFormButton
          key="create"
          trigger={
            <Button type="primary" icon={<PlusOutlined />}>
              {t("create")}
            </Button>
          }
          t={t}
          onError={showError}
          onFinish={async (payload) => {
            await create.mutateAsync(payload);
            message.success(t("createSuccess"));
            reload();
          }}
        />,
      ]}
    />
  );
}

type NewsFormProps = {
  trigger: React.ReactElement;
  initial?: AdminNewsItem;
  t: ReturnType<typeof useTranslations>;
  onError: (e: unknown) => void;
  onFinish: (payload: {
    titleVi: string;
    titleEn: string;
    contentVi: string;
    contentEn: string;
    category?: string;
    isImportant: boolean;
  }) => Promise<void>;
};

function NewsFormButton({ trigger, initial, t, onError, onFinish }: NewsFormProps) {
  return (
    <ModalForm
      title={initial ? t("editTitle") : t("createTitle")}
      trigger={trigger}
      width={640}
      modalProps={{ destroyOnHidden: true }}
      initialValues={
        initial
          ? {
              titleVi: initial.titleVi,
              titleEn: initial.titleEn,
              contentVi: initial.contentVi,
              contentEn: initial.contentEn,
              category: initial.category ?? "",
              isImportant: initial.isImportant,
            }
          : { isImportant: false }
      }
      onFinish={async (values) => {
        try {
          await onFinish({
            titleVi: values.titleVi as string,
            titleEn: values.titleEn as string,
            contentVi: (values.contentVi as string) ?? "",
            contentEn: (values.contentEn as string) ?? "",
            category: (values.category as string) || undefined,
            isImportant: Boolean(values.isImportant),
          });
          return true;
        } catch (error) {
          onError(error);
          return false;
        }
      }}
    >
      <ProFormText name="titleVi" label={t("titleVi")} rules={[{ required: true }, { max: 255 }]} />
      <ProFormText name="titleEn" label={t("titleEn")} rules={[{ required: true }, { max: 255 }]} />
      <ProFormTextArea name="contentVi" label={t("contentVi")} fieldProps={{ rows: 4 }} />
      <ProFormTextArea name="contentEn" label={t("contentEn")} fieldProps={{ rows: 4 }} />
      <ProFormText name="category" label={t("category")} rules={[{ max: 50 }]} />
      <ProFormSwitch name="isImportant" label={t("important")} tooltip={t("importantHint")} />
    </ModalForm>
  );
}

type AssignNewsProps = {
  newsId: string;
  assign: ReturnType<typeof useAssignNews>;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function AssignNewsButton({ newsId, assign, onError, t }: AssignNewsProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("assignTitle")}
      trigger={<a>{t("assign")}</a>}
      width={420}
      modalProps={{ destroyOnHidden: true }}
      onFinish={async (values) => {
        const role = (values.role as string) || undefined;
        const userId = (values.userId as string) || undefined;
        if (!role && !userId) {
          message.warning(t("assignTargetRequired"));
          return false;
        }
        try {
          await assign.mutateAsync({ id: newsId, payload: { role, userId } });
          message.success(t("assignSuccess"));
          return true;
        } catch (error) {
          onError(error);
          return false;
        }
      }}
    >
      <ProFormSelect
        name="role"
        label={t("assignRole")}
        options={[
          { value: "pg", label: "PG" },
          { value: "leader", label: "Leader" },
        ]}
        allowClear
      />
      <ProFormSelect
        name="userId"
        label={t("assignUser")}
        showSearch
        debounceTime={300}
        request={async ({ keyWords }) => {
          const res = await fetchUsers({ page: 1, pageSize: 20, search: keyWords });
          return res.data.map((u) => ({ value: u.id, label: `${u.fullName} (${u.email})` }));
        }}
        fieldProps={{ allowClear: true, filterOption: false }}
      />
    </ModalForm>
  );
}
