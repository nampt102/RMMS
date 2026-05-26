"use client";

import { Button, Card, Form, Input, Typography } from "antd";
import { useTranslations } from "next-intl";

const { Title } = Typography;

type LoginFormValues = {
  email: string;
  password: string;
};

export default function LoginPage() {
  const t = useTranslations("auth.login");

  const onFinish = async (values: LoginFormValues) => {
    // TODO(M01): call POST /api/v1/auth/login via lib/api/client.ts
    // For now, just log so the scaffold is wired end-to-end.
    // eslint-disable-next-line no-console
    console.log("login submit", values);
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-100">
      <Card className="w-full max-w-sm">
        <Title level={3} className="!mb-6 text-center">
          {t("title")}
        </Title>
        <Form<LoginFormValues> layout="vertical" onFinish={onFinish}>
          <Form.Item
            label={t("email")}
            name="email"
            rules={[{ required: true, type: "email" }]}
          >
            <Input autoComplete="email" />
          </Form.Item>
          <Form.Item
            label={t("password")}
            name="password"
            rules={[{ required: true, min: 8 }]}
          >
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block>
            {t("submit")}
          </Button>
        </Form>
      </Card>
    </div>
  );
}
