"use client";

import { App, Button, Card, Form, Input, Typography } from "antd";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useLoginMutation, type LoginRequest } from "@/features/auth/api/login";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const { Title } = Typography;

export default function LoginPage({ params: { locale } }: { params: { locale: string } }) {
  const t = useTranslations("auth.login");
  const tErrors = useTranslations("errors");
  const router = useRouter();
  const { message } = App.useApp();
  const login = useLoginMutation();

  const onFinish = async (values: LoginRequest) => {
    try {
      await login.mutateAsync(values);
      // Land on the Admin user-management page (the Sprint 01 admin home).
      router.replace(`/${locale}/users`);
    } catch (error) {
      const code = errorCodeFromUnknown(error);
      message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-100">
      <Card className="w-full max-w-sm">
        <Title level={3} className="!mb-6 text-center">
          {t("title")}
        </Title>
        <Form<LoginRequest> layout="vertical" onFinish={onFinish} disabled={login.isPending}>
          <Form.Item label={t("email")} name="email" rules={[{ required: true, type: "email" }]}>
            <Input autoComplete="email" />
          </Form.Item>
          <Form.Item label={t("password")} name="password" rules={[{ required: true, min: 8 }]}>
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block loading={login.isPending}>
            {t("submit")}
          </Button>
        </Form>
      </Card>
    </div>
  );
}
