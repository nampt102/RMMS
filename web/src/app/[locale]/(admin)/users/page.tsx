"use client";

import { CheckCircleFilled, MinusCircleOutlined, PlusOutlined, UploadOutlined } from "@ant-design/icons";
import {
  ModalForm,
  ProForm,
  ProFormSelect,
  ProFormText,
  ProTable,
  type ActionType,
  type ProColumns,
} from "@ant-design/pro-components";
import {
  App,
  Button,
  Descriptions,
  Divider,
  Drawer,
  Popconfirm,
  Space,
  Switch,
  Tag,
  Tooltip,
  Typography,
  Upload,
  type UploadFile,
} from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRef, useState } from "react";
import {
  fetchUsers,
  useAdminEnrollFace,
  useCreateUser,
  useReEnrollFace,
  useRemoveFace,
  useResetUserPassword,
  useUpdateUser,
} from "@/features/users/api";
import type { AdminUser } from "@/features/users/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";
import { UserAssignmentsPanel } from "@/features/organization/UserAssignmentsPanel";

const ROLE_COLORS: Record<string, string> = {
  pg: "blue",
  leader: "geekblue",
  buh: "purple",
  admin: "red",
};

export default function UsersPage() {
  const t = useTranslations("users");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();
  const [detail, setDetail] = useState<AdminUser | null>(null);

  const createUser = useCreateUser();
  const updateUser = useUpdateUser();
  const resetPassword = useResetUserPassword();
  const reEnrollFace = useReEnrollFace();
  const removeFace = useRemoveFace();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string | null) =>
    value ? new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";

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

  const roleTag = (role: AdminUser["role"]) => (
    <Tag color={ROLE_COLORS[role] ?? "default"}>{roleEnum[role]?.text ?? role}</Tag>
  );

  // Face Verification only applies to field users (PG/Leader) who check in.
  const usesFace = (role: AdminUser["role"]) => role === "pg" || role === "leader";

  const faceTag = (row: AdminUser) => {
    if (!usesFace(row.role)) return <Typography.Text type="secondary">—</Typography.Text>;
    return row.faceEnrolled ? (
      <Tooltip title={row.faceEnrolledAt ? fmtDate(row.faceEnrolledAt) : undefined}>
        <Tag icon={<CheckCircleFilled />} color="success">
          {t("face_enrolled")}
        </Tag>
      </Tooltip>
    ) : (
      <Tag icon={<MinusCircleOutlined />} color="default">
        {t("face_notEnrolled")}
      </Tag>
    );
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
      render: (_, row) => roleTag(row.role),
    },
    {
      title: t("status"),
      dataIndex: "status",
      valueType: "select",
      valueEnum: statusEnum,
    },
    {
      title: t("face"),
      dataIndex: "faceEnrolled",
      search: false,
      render: (_, row) => faceTag(row),
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
        <a key="detail" onClick={() => setDetail(row)}>
          {t("viewDetail")}
        </a>,
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

  const toggleStatus = async (checked: boolean) => {
    if (!detail) return;
    const status = checked ? "active" : "inactive";
    try {
      await updateUser.mutateAsync({ id: detail.id, payload: { status } });
      setDetail({ ...detail, status });
      message.success(t("updateSuccess"));
      actionRef.current?.reload();
    } catch (error) {
      showError(error);
    }
  };

  const resetForDetail = async () => {
    if (!detail) return;
    try {
      await resetPassword.mutateAsync(detail.id);
      message.success(t("resetSuccess"));
    } catch (error) {
      showError(error);
    }
  };

  const reEnrollForDetail = async () => {
    if (!detail) return;
    try {
      await reEnrollFace.mutateAsync(detail.id);
      setDetail({ ...detail, faceEnrolled: false, faceEnrolledAt: null });
      message.success(t("face_reEnrollSuccess"));
      actionRef.current?.reload();
    } catch (error) {
      showError(error);
    }
  };

  const removeFaceForDetail = async () => {
    if (!detail) return;
    try {
      await removeFace.mutateAsync(detail.id);
      setDetail({ ...detail, faceEnrolled: false, faceEnrolledAt: null });
      message.success(t("face_removeSuccess"));
      actionRef.current?.reload();
    } catch (error) {
      showError(error);
    }
  };

  return (
    <>
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

    <Drawer
      title={t("detailTitle")}
      width={520}
      open={detail !== null}
      onClose={() => setDetail(null)}
      destroyOnClose
    >
      {detail && (
        <>
          <Descriptions column={1} bordered size="small" styles={{ label: { width: 160 } }}>
            <Descriptions.Item label={t("email")}>
              <Typography.Text copyable>{detail.email}</Typography.Text>
            </Descriptions.Item>
            <Descriptions.Item label={t("fullName")}>{detail.fullName}</Descriptions.Item>
            <Descriptions.Item label={t("phone")}>{detail.phone || "—"}</Descriptions.Item>
            <Descriptions.Item label={t("role")}>{roleTag(detail.role)}</Descriptions.Item>
            <Descriptions.Item label={t("status")}>
              {statusEnum[detail.status as keyof typeof statusEnum]?.text ?? detail.status}
            </Descriptions.Item>
            <Descriptions.Item label={t("language")}>
              {detail.preferredLanguage === "en" ? "English" : "Tiếng Việt"}
            </Descriptions.Item>
            <Descriptions.Item label={t("emailVerifiedAt")}>
              {detail.emailVerifiedAt ? fmtDate(detail.emailVerifiedAt) : t("notVerified")}
            </Descriptions.Item>
            <Descriptions.Item label={t("lastLogin")}>{fmtDate(detail.lastLoginAt)}</Descriptions.Item>
            <Descriptions.Item label={t("createdAt")}>{fmtDate(detail.createdAt)}</Descriptions.Item>
            <Descriptions.Item label={t("updatedAt")}>{fmtDate(detail.updatedAt)}</Descriptions.Item>
          </Descriptions>

          <Divider />

          <Space align="center">
            <span>{t("statusToggle")}</span>
            <Switch
              checked={detail.status === "active"}
              loading={updateUser.isPending}
              onChange={toggleStatus}
            />
            <span>{detail.status === "active" ? t("status_active") : t("status_inactive")}</span>
          </Space>

          <Divider />

          <Popconfirm title={t("resetConfirm")} onConfirm={resetForDetail}>
            <Button danger loading={resetPassword.isPending}>
              {t("resetPassword")}
            </Button>
          </Popconfirm>

          {usesFace(detail.role) && (
            <>
              <Divider />
              <Typography.Title level={5} style={{ marginTop: 0 }}>
                {t("face")}
              </Typography.Title>
              <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                <Space align="center">
                  {faceTag(detail)}
                  {detail.faceEnrolled && detail.faceEnrolledAt && (
                    <Typography.Text type="secondary">
                      {t("face_enrolledAt", { date: fmtDate(detail.faceEnrolledAt) })}
                    </Typography.Text>
                  )}
                </Space>
                <FaceEnrollUpload
                  userId={detail.id}
                  onDone={() => {
                    setDetail({ ...detail, faceEnrolled: true });
                    actionRef.current?.reload();
                  }}
                  onError={showError}
                  t={t}
                />
                <Space>
                  <Popconfirm title={t("face_reEnrollConfirm")} onConfirm={reEnrollForDetail}>
                    <Button loading={reEnrollFace.isPending}>{t("face_reEnroll")}</Button>
                  </Popconfirm>
                  <Popconfirm title={t("face_removeConfirm")} onConfirm={removeFaceForDetail}>
                    <Button danger disabled={!detail.faceEnrolled} loading={removeFace.isPending}>
                      {t("face_remove")}
                    </Button>
                  </Popconfirm>
                </Space>
              </Space>
            </>
          )}

          <UserAssignmentsPanel userId={detail.id} isPg={detail.role === "pg"} />
        </>
      )}
    </Drawer>
    </>
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

type FaceEnrollProps = {
  userId: string;
  onDone: () => void;
  onError: (e: unknown) => void;
  t: ReturnType<typeof useTranslations>;
};

/** Admin-side face enrollment: pick 1..5 face photos and push them to the engine. */
function FaceEnrollUpload({ userId, onDone, onError, t }: FaceEnrollProps) {
  const { message } = App.useApp();
  const enroll = useAdminEnrollFace();
  const [fileList, setFileList] = useState<UploadFile[]>([]);

  const submit = async () => {
    const files = fileList
      .map((f) => f.originFileObj)
      .filter((f): f is NonNullable<typeof f> => Boolean(f));
    if (files.length === 0) return;
    try {
      await enroll.mutateAsync({ id: userId, files });
      message.success(t("face_enrollSuccess"));
      setFileList([]);
      onDone();
    } catch (error) {
      onError(error);
    }
  };

  return (
    <Space direction="vertical" size="small" style={{ width: "100%" }}>
      <Upload
        listType="picture"
        accept="image/*"
        multiple
        maxCount={5}
        beforeUpload={() => false}
        fileList={fileList}
        onChange={({ fileList: list }) => setFileList(list.slice(0, 5))}
      >
        <Button icon={<UploadOutlined />}>{t("face_pickPhotos")}</Button>
      </Upload>
      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
        {t("face_enrollHint")}
      </Typography.Text>
      <Button
        type="primary"
        disabled={fileList.length === 0}
        loading={enroll.isPending}
        onClick={submit}
      >
        {t("face_enroll")}
      </Button>
    </Space>
  );
}
