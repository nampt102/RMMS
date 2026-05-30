"use client";

import { Button, Layout, Space, Typography } from "antd";
import { LogoutOutlined } from "@ant-design/icons";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore } from "@/lib/stores/auth-store";

const { Header, Content } = Layout;
const { Text } = Typography;

/**
 * Authenticated shell + client-side route guard for the Admin area.
 *
 * The JWT lives in the Zustand store (persisted to localStorage), which the Next.js
 * middleware (server-side) cannot read — so the guard runs on the client: unauthenticated
 * visitors are redirected to the login page. The `hydrated` flag avoids an SSR/client
 * mismatch and a premature redirect before the persisted store rehydrates.
 */
export default function AdminLayout({
  children,
  params: { locale },
}: {
  children: React.ReactNode;
  params: { locale: string };
}) {
  const t = useTranslations("admin");
  const router = useRouter();
  const token = useAuthStore((s) => s.accessToken);
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);

  const [hydrated, setHydrated] = useState(false);
  useEffect(() => setHydrated(true), []);

  useEffect(() => {
    if (hydrated && !token) {
      router.replace(`/${locale}/login`);
    }
  }, [hydrated, token, locale, router]);

  if (!hydrated || !token) {
    return null;
  }

  const onLogout = () => {
    clear();
    router.replace(`/${locale}/login`);
  };

  return (
    <Layout className="min-h-screen">
      <Header className="flex items-center justify-between">
        <Text strong className="!text-white">
          {t("appTitle")}
        </Text>
        <Space>
          {user?.email && <Text className="!text-white/80">{user.email}</Text>}
          <Button type="text" icon={<LogoutOutlined />} className="!text-white" onClick={onLogout}>
            {t("logout")}
          </Button>
        </Space>
      </Header>
      <Content className="p-6">{children}</Content>
    </Layout>
  );
}
