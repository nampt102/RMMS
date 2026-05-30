"use client";

import { PlusOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProForm,
  ProFormSelect,
  ProFormText,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import { App, Button, Popconfirm, Tag } from "antd";
import { useTranslations } from "next-intl";
import { useRef } from "react";
import {
  fetchUsers,
  useCreateUser,
  useResetUserPassword,
  useUpdateUser,
} from "@/features/users/api";
import type { AdminUser } from "@/features/users/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const ROLE_COLORS: Record<string, string> = {
  pg: "blue",
  leader: "geekblue",
  buh: "purple",
  admin: "red",
};

export default function UsersPage() {
  const t = useTranslations("users");
  const tErrors = useTranslations("errors");
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const resetPassword = useResetUserPassword();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const roleEnum = {
    pg: { text: t("role_pg") },
    leader: { text: t("role_leader") },
    buh: { text: t("role_buh") },
    admin: { text: t("role_admin") },
  };

  const statusEnum = {
    active: { text: t("status_active"), status: "Success" },
    inactive: { text: t("status_inactive"), status: "Default" },
    pending_email_verify: { text: t("status_pending"), status: "Warning" },
  };

  const columns: ProColumns<AdminUser>[] = [
    {
      title: t("search"),
      dataIndex: "search",
      valueType: "text",
      hideInTable: true,
    },
    { title: t("email"), dataIndex: "email", copyable: true, search: false },
    { title: t("fullName"), dataIndex: "fullName", search: false },
    {
      title: t("role"),
      dataIndex: "role",
      valueType: "select",
      valueEnum: roleEnum,
      render: (_, row) => <Tag color={ROLE_COLORS[row.role] ?? "default"}>{roleEnum[row.role]?.text ?? row.role}</Tag>,
    },
    {
      title: t("status"),
      dataIndex: "status",
      valueType: "select",
      valueEnum: statusEnum,
    },
    {
      title: t("lastLogin"),
      dataIndex: "lastLoginAt",
      valueType: "dateTime",
      search: false,
    },
    {
      title: t("actions"),
      valueType: "option",
      key: "option",
      render: (_, row) => [
        <EditUserButton key="edit" user={row} onDone={() => actionRef.current?.reload()} onError={showError} update={updateUser} t={t} />,
        <Popconfirm
          key="reset"
          title={t("resetConfirm")}
          onConfirm={async () => {
            try {
              await resetPassword.mutateAsync(row.id);
              message.success(t("resetSuccess"));
            } catch (error) {
              showError(error);
            }
          }}
        >
          <a>{t("resetPassword")}</a>
        </Popconfirm>,
      ],
    },
  ];

  return (
    <ProTable<AdminUser>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={{ labelWidth: "auto" }}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      request={async (params) => {
        try {
          const res = await fetchUsers({
            page: params.current ?? 1,
            pageSize: params.pageSize ?? 20,
            role: params.role as string | undefined,
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
              await createUser.mutateAsync({
                email: values.email as string,
                fullName: values.fullName as string,
                phone: (values.phone as string) || undefined,
                role: values.role as "leader" | "buh" | "admin",
                preferredLanguage: (values.preferredLanguage as "vi" | "en") ?? "vi",
              });
              message.success(t("createSuccess"));
              actionRef.current?.reload();
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormText
            name="email"
            label={t("email")}
            rules={[{ required: true, type: "email" }]}
          />
          <ProFormText name="fullName" label={t("fullName")} rules={[{ required: true }]} />
          <ProFormText name="phone" label={t("phone")} />
          <ProFormSelect
            name="role"
            label={t("role")}
            rules={[{ required: true }]}
            options={[
              { value: "leader", label: t("role_leader") },
              { value: "buh", label: t("role_buh") },
              { value: "admin", label: t("role_admin") },
            ]}
          />
          <ProFormSelect
            name="preferredLanguage"
            label={t("language")}
            initialValue="vi"
            options={[
              { value: "vi", label: "Tiếng Việt" },
              { value: "en", label: "English" },
            ]}
          />
        </ModalForm>,
      ]}
    />
  );
}

type EditProps = {
  user: AdminUser;
  onDone: () => void;
  onError: (e: unknown) => void;
  update: ReturnType<typeof useUpdateUser>;
  t: ReturnType<typeof useTranslations>;
};

function EditUserButton({ user, onDone, onError, update, t }: EditProps) {
  const { message } = App.useApp();
  return (
    <ModalForm
      title={t("editTitle")}
      trigger={<a>{t("edit")}</a>}
      modalProps={{ destroyOnClose: true }}
      initialValues={{
        fullName: user.fullName,
        phone: user.phone ?? "",
        status: user.status,
        preferredLanguage: user.preferredLanguage,
      }}
      onFinish={async (values) => {
        try {
          await update.mutateAsync({
            id: user.id,
            payload: {
              fullName: values.fullName as string,
              phone: (values.phone as string) || undefined,
              status: values.status as "active" | "inactive",
              preferredLanguage: values.preferredLanguage as "vi" | "en",
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
      <ProForm.Item label={t("email")}>
        <span>{user.email}</span>
      </ProForm.Item>
      <ProFormText name="fullName" label={t("fullName")} rules={[{ required: true }]} />
      <ProFormText name="phone" label={t("phone")} />
      <ProFormSelect
        name="status"
        label={t("status")}
        options={[
          { value: "active", label: t("status_active") },
          { value: "inactive", label: t("status_inactive") },
        ]}
      />
      <ProFormSelect
        name="preferredLanguage"
        label={t("language")}
        options={[
          { value: "vi", label: "Tiếng Việt" },
          { value: "en", label: "English" },
        ]}
      />
    </ModalForm>
  );
}
