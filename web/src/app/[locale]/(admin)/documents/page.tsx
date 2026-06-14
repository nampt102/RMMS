"use client";

import { PlusOutlined, UploadOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProFormSelect,
  ProFormText,
  ProFormTextArea,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import { App, Button, Popconfirm, Tag, Upload, type UploadFile } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState } from "react";
import { fetchAdminDocuments, useAssignDocument, useDeleteDocument, useUploadDocument } from "@/features/documents/api";
import type { DocumentItem } from "@/features/documents/types";
import { fetchUsers } from "@/features/users/api";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export default function DocumentsPage() {
  const t = useTranslations("documents");
  const tErrors = useTranslations("errors");
  const tc = useTranslations("common");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const upload = useUploadDocument();
  const assign = useAssignDocument();
  const del = useDeleteDocument();

  const [fileList, setFileList] = useState<UploadFile[]>([]);

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };
  const reload = () => actionRef.current?.reload();
  const fmtDate = (v: string) => new Date(v).toLocaleString(locale === "en" ? "en-US" : "vi-VN");

  const folderEnum = {
    public: { text: t("folder_public"), status: "Default" },
    private: { text: t("folder_private"), status: "Warning" },
  };

  const columns: ProColumns<DocumentItem>[] = [
    { title: t("name"), dataIndex: "name", search: false },
    {
      title: t("folder"),
      dataIndex: "folderType",
      valueType: "select",
      valueEnum: folderEnum,
      render: (_, r) => (
        <Tag color={r.folderType === "private" ? "gold" : "default"}>
          {r.folderType === "private" ? t("folder_private") : t("folder_public")}
        </Tag>
      ),
    },
    { title: t("size"), dataIndex: "fileSizeBytes", search: false, render: (_, r) => formatBytes(r.fileSizeBytes) },
    { title: t("mime"), dataIndex: "mimeType", search: false, render: (_, r) => <span className="text-xs text-neutral-500">{r.mimeType}</span> },
    { title: t("createdAt"), dataIndex: "createdAt", search: false, render: (_, r) => fmtDate(r.createdAt) },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      width: 160,
      render: (_, row) => [
        <AssignButton key="assign" doc={row} assign={assign} onError={showError} t={t} />,
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
    <ProTable<DocumentItem>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={{ labelWidth: "auto" }}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      request={async (params) => {
        try {
          const data = await fetchAdminDocuments(params.folderType as string | undefined);
          return { data, total: data.length, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
      toolBarRender={() => [
        <ModalForm
          key="upload"
          title={t("uploadTitle")}
          trigger={
            <Button type="primary" icon={<PlusOutlined />}>
              {t("upload")}
            </Button>
          }
          modalProps={{ destroyOnHidden: true }}
          onOpenChange={(open) => {
            if (!open) setFileList([]);
          }}
          onFinish={async (values) => {
            const file = fileList[0]?.originFileObj as File | undefined;
            if (!file) {
              message.warning(t("fileRequired"));
              return false;
            }
            try {
              await upload.mutateAsync({
                name: values.name as string,
                description: (values.description as string) || undefined,
                folderType: values.folderType as string,
                file,
              });
              message.success(t("uploadSuccess"));
              setFileList([]);
              reload();
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormText name="name" label={t("name")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormTextArea name="description" label={t("description")} fieldProps={{ rows: 2 }} />
          <ProFormSelect
            name="folderType"
            label={t("folder")}
            initialValue="public"
            options={[
              { value: "public", label: t("folder_public") },
              { value: "private", label: t("folder_private") },
            ]}
            rules={[{ required: true }]}
          />
          <Upload
            beforeUpload={() => false}
            maxCount={1}
            fileList={fileList}
            onChange={({ fileList: list }) => setFileList(list.slice(-1))}
          >
            <Button icon={<UploadOutlined />}>{t("pickFile")}</Button>
          </Upload>
        </ModalForm>,
      ]}
    />
  );
}

type AssignProps = {
  doc: DocumentItem;
  assign: ReturnType<typeof useAssignDocument>;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

function AssignButton({ doc, assign, onError, t }: AssignProps) {
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
          await assign.mutateAsync({ id: doc.id, payload: { role, userId } });
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
        tooltip={t("assignUserHint")}
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
