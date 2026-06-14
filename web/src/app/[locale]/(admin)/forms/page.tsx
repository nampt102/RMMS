"use client";

import { PlusOutlined } from "@ant-design/icons";
import { ModalForm, ProFormSelect, ProFormText, ProTable, type ProColumns } from "@ant-design/pro-components";
import { App, Badge, Button, Tag } from "antd";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useForms, useCreateForm } from "@/features/forms/api";
import { EMPTY_SCHEMA, FORM_TYPES, type FormSummary } from "@/features/forms/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

export default function FormsPage() {
  const t = useTranslations("forms");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const router = useRouter();
  const { message } = App.useApp();

  const { data: forms, isLoading, refetch } = useForms();
  const createForm = useCreateForm();

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmtDate = (value: string) => new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN");

  const statusEnum = {
    draft: { text: t("status_draft"), status: "Default" },
    published: { text: t("status_published"), status: "Success" },
    archived: { text: t("status_archived"), status: "Warning" },
  };

  const columns: ProColumns<FormSummary>[] = [
    { title: t("code"), dataIndex: "code", copyable: true, width: 150 },
    { title: t("name"), dataIndex: locale === "en" ? "nameEn" : "nameVi" },
    { title: t("type"), dataIndex: "formType", render: (_, r) => <Tag color="blue">{t(`type_${r.formType}`)}</Tag> },
    {
      title: t("status"),
      dataIndex: "status",
      render: (_, r) => {
        const s = statusEnum[r.status];
        return <Badge status={s.status as never} text={s.text} />;
      },
    },
    {
      title: t("version"),
      key: "version",
      render: (_, r) => (
        <span className="tabular-nums">
          v{r.currentVersion}
          {r.hasDraft && <Tag className="ml-2" color="gold">{t("draftPending")}</Tag>}
        </span>
      ),
    },
    { title: t("createdAt"), dataIndex: "createdAt", render: (_, r) => fmtDate(r.createdAt) },
    {
      title: t("actions"),
      key: "option",
      width: 120,
      render: (_, r) => [
        <a key="edit" onClick={() => router.push(`/${locale}/forms/${r.id}`)}>
          {t("openBuilder")}
        </a>,
      ],
    },
  ];

  return (
    <ProTable<FormSummary>
      headerTitle={t("title")}
      rowKey="id"
      columns={columns}
      search={false}
      loading={isLoading}
      dataSource={forms ?? []}
      pagination={{ pageSize: 20 }}
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
              const res = await createForm.mutateAsync({
                code: values.code as string,
                nameVi: values.nameVi as string,
                nameEn: values.nameEn as string,
                formType: values.formType as string,
                schema: JSON.stringify(EMPTY_SCHEMA),
              });
              message.success(t("createSuccess"));
              refetch();
              router.push(`/${locale}/forms/${res.id}`);
              return true;
            } catch (error) {
              showError(error);
              return false;
            }
          }}
        >
          <ProFormText name="code" label={t("code")} rules={[{ required: true }, { max: 50 }]} />
          <ProFormText name="nameVi" label={t("nameVi")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormText name="nameEn" label={t("nameEn")} rules={[{ required: true }, { max: 255 }]} />
          <ProFormSelect
            name="formType"
            label={t("type")}
            rules={[{ required: true }]}
            options={FORM_TYPES.map((ft) => ({ value: ft, label: t(`type_${ft}`) }))}
          />
        </ModalForm>,
      ]}
    />
  );
}
