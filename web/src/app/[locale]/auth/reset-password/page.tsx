"use client";

import { App, Button, Card, Form, Input, Result, Typography } from "antd";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { useResetPasswordMutation } from "@/features/auth/api/reset-password";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const { Title, Paragraph } = Typography;

type FormValues = { newPassword: string; confirm: string };

function ResetPasswordForm({ locale }: { locale: string }) {
  const t = useTranslations("auth.resetPassword");
  const tErrors = useTranslations("errors");
  const router = useRouter();
  const { message } = App.useApp();
  const params = useSearchParams();
  const token = params.get("token") ?? "";
  const reset = useResetPasswordMutation();
  const [done, setDone] = useState(false);

  if (!token) {
    return (
      <Result
        status="warning"
        title={t("missingToken")}
        extra={
          <Link href={`/${locale}/login`}>
            <Button type="primary">{t("toLogin")}</Button>
          </Link>
        }
      />
    );
  }

  if (done) {
    return (
      <Result
        status="success"
        title={t("success")}
        extra={
          <Link href={`/${locale}/login`}>
            <Button type="primary">{t("toLogin")}</Button>
          </Link>
        }
      />
    );
  }

  const onFinish = async (values: FormValues) => {
    if (values.newPassword !== values.confirm) {
      message.warning(t("mismatch"));
      return;
    }
    try {
      await reset.mutateAsync({ token, newPassword: values.newPassword });
      setDone(true);
      setTimeout(() => router.replace(`/${locale}/login`), 2500);
    } catch (error) {
      const code = errorCodeFromUnknown(error);
      message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
    }
  };

  return (
    <>
      <Title level={3} className="!mb-2 text-center">
        {t("title")}
      </Title>
      <Paragraph type="secondary" className="text-center">
        {t("subtitle")}
      </Paragraph>
      <Form<FormValues> layout="vertical" onFinish={onFinish} disabled={reset.isPending}>
        <Form.Item label={t("newPassword")} name="newPassword" rules={[{ required: true, min: 8 }]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item label={t("confirm")} name="confirm" rules={[{ required: true, min: 8 }]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={reset.isPending}>
          {t("submit")}
        </Button>
      </Form>
    </>
  );
}

export default function ResetPasswordPage({ params: { locale } }: { params: { locale: string } }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-100 p-4">
      <Card className="w-full max-w-sm">
        <Suspense fallback={null}>
          <ResetPasswordForm locale={locale} />
        </Suspense>
      </Card>
    </div>
  );
}
