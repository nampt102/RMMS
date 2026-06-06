"use client";

import { Button, Layout, Menu, Space, Typography } from "antd";
import {
  AppstoreOutlined,
  ApartmentOutlined,
  AuditOutlined,
  CalendarOutlined,
  CheckSquareOutlined,
  LaptopOutlined,
  LogoutOutlined,
  ShopOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
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
  const pathname = usePathname();
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

  const navItems = [
    { key: `/${locale}/users`, icon: <TeamOutlined />, label: <Link href={`/${locale}/users`}>{t("navUsers")}</Link> },
    { key: `/${locale}/stores`, icon: <ShopOutlined />, label: <Link href={`/${locale}/stores`}>{t("navStores")}</Link> },
    { key: `/${locale}/areas`, icon: <ApartmentOutlined />, label: <Link href={`/${locale}/areas`}>{t("navAreas")}</Link> },
    { key: `/${locale}/categories`, icon: <AppstoreOutlined />, label: <Link href={`/${locale}/categories`}>{t("navCategories")}</Link> },
    { key: `/${locale}/schedules`, icon: <CalendarOutlined />, label: <Link href={`/${locale}/schedules`}>{t("navSchedules")}</Link> },
    { key: `/${locale}/attendance`, icon: <CheckSquareOutlined />, label: <Link href={`/${locale}/attendance`}>{t("navAttendance")}</Link> },
    { key: `/${locale}/approvals`, icon: <AuditOutlined />, label: <Link href={`/${locale}/approvals`}>{t("navApprovals")}</Link> },
    { key: `/${locale}/devices`, icon: <LaptopOutlined />, label: <Link href={`/${locale}/devices`}>{t("navDevices")}</Link> },
  ];
  const selectedKey = navItems.find((i) => pathname.startsWith(i.key))?.key;

  return (
    <Layout className="min-h-screen">
      <Header className="flex items-center justify-between gap-6">
        <div className="flex min-w-0 items-center gap-6">
          <Text strong className="whitespace-nowrap !text-white">
            {t("appTitle")}
          </Text>
          <Menu
            theme="dark"
            mode="horizontal"
            selectedKeys={selectedKey ? [selectedKey] : []}
            items={navItems}
            className="min-w-0 flex-1"
            disabledOverflow
          />
        </div>
        <Space>
          {user?.email && <Text className="whitespace-nowrap !text-white/80">{user.email}</Text>}
          <Button type="text" icon={<LogoutOutlined />} className="!text-white" onClick={onLogout}>
            {t("logout")}
          </Button>
        </Space>
      </Header>
      <Content className="p-6">{children}</Content>
    </Layout>
  );
}
