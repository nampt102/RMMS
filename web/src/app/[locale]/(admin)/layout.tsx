"use client";

import { Avatar, Button, Drawer, Grid, Layout, Menu, Typography } from "antd";
import {
  AppstoreOutlined,
  ApartmentOutlined,
  AuditOutlined,
  BarChartOutlined,
  CalendarOutlined,
  DashboardOutlined,
  FileTextOutlined,
  CheckSquareOutlined,
  LaptopOutlined,
  MenuFoldOutlined,
  MenuOutlined,
  MenuUnfoldOutlined,
  SafetyCertificateOutlined,
  LogoutOutlined,
  ShopOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useRealtimeNotifications } from "@/lib/realtime/useRealtimeNotifications";

const { Header, Content, Sider } = Layout;
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

  // Live notifications (SignalR) — toast + refetch approvals/monitoring on push.
  useRealtimeNotifications();

  const screens = Grid.useBreakpoint();
  const isDesktop = screens.lg ?? false; // sidebar ≥992px, drawer below (adaptive-navigation)
  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // next-intl uses `localePrefix: "as-needed"`, so the default locale (vi) has NO `/vi`
  // prefix in the URL. Normalise so route matching always compares against `/${locale}/...`.
  const fullPath =
    pathname === `/${locale}` || pathname.startsWith(`/${locale}/`) ? pathname : `/${locale}${pathname}`;

  useEffect(() => {
    if (hydrated && !token) {
      router.replace(`/${locale}/login`);
    }
  }, [hydrated, token, locale, router]);

  // Non-admins (Leader/BUH) can only use the approval queue + team monitoring on web —
  // keep them off AdminOnly pages (which would 403) and land them on /approvals.
  useEffect(() => {
    if (!hydrated || !token || !user || user.role === "admin") return;
    const allowed = [`/${locale}/dashboard`, `/${locale}/approvals`, `/${locale}/monitoring`];
    if (!allowed.some((p) => fullPath.startsWith(p))) {
      router.replace(`/${locale}/dashboard`);
    }
  }, [hydrated, token, user, fullPath, locale, router]);

  if (!hydrated || !token) {
    return null;
  }

  const onLogout = () => {
    clear();
    router.replace(`/${locale}/login`);
  };

  // Role-scoped navigation. Most admin pages call AdminOnly endpoints, so a Leader/BUH
  // only sees what they can actually use (the approval queue). `approvals` is the shared
  // landing for non-admins.
  const role = user?.role ?? "";
  const allItems = [
    { key: `/${locale}/dashboard`, icon: <BarChartOutlined />, label: <Link href={`/${locale}/dashboard`}>{t("navDashboard")}</Link>, roles: ["admin", "leader", "buh"] },
    { key: `/${locale}/users`, icon: <TeamOutlined />, label: <Link href={`/${locale}/users`}>{t("navUsers")}</Link>, roles: ["admin"] },
    { key: `/${locale}/stores`, icon: <ShopOutlined />, label: <Link href={`/${locale}/stores`}>{t("navStores")}</Link>, roles: ["admin"] },
    { key: `/${locale}/areas`, icon: <ApartmentOutlined />, label: <Link href={`/${locale}/areas`}>{t("navAreas")}</Link>, roles: ["admin"] },
    { key: `/${locale}/categories`, icon: <AppstoreOutlined />, label: <Link href={`/${locale}/categories`}>{t("navCategories")}</Link>, roles: ["admin"] },
    { key: `/${locale}/schedules`, icon: <CalendarOutlined />, label: <Link href={`/${locale}/schedules`}>{t("navSchedules")}</Link>, roles: ["admin"] },
    { key: `/${locale}/attendance`, icon: <CheckSquareOutlined />, label: <Link href={`/${locale}/attendance`}>{t("navAttendance")}</Link>, roles: ["admin"] },
    { key: `/${locale}/monitoring`, icon: <DashboardOutlined />, label: <Link href={`/${locale}/monitoring`}>{t("navMonitoring")}</Link>, roles: ["admin", "leader", "buh"] },
    { key: `/${locale}/approvals`, icon: <AuditOutlined />, label: <Link href={`/${locale}/approvals`}>{t("navApprovals")}</Link>, roles: ["admin", "leader", "buh"] },
    { key: `/${locale}/requests`, icon: <FileTextOutlined />, label: <Link href={`/${locale}/requests`}>{t("navRequests")}</Link>, roles: ["admin"] },
    { key: `/${locale}/audit-logs`, icon: <SafetyCertificateOutlined />, label: <Link href={`/${locale}/audit-logs`}>{t("navAudit")}</Link>, roles: ["admin"] },
    { key: `/${locale}/devices`, icon: <LaptopOutlined />, label: <Link href={`/${locale}/devices`}>{t("navDevices")}</Link>, roles: ["admin"] },
  ];
  const navItems = allItems.filter((i) => i.roles.includes(role));
  const selectedKey = navItems.find((i) => fullPath.startsWith(i.key))?.key;
  const canViewCurrent = navItems.some((i) => fullPath.startsWith(i.key));

  // forSider: logo + collapse toggle always share one row. When collapsed the wordmark
  // hides and padding tightens so both still fit in the narrow rail.
  const brand = (forSider: boolean) => (
    <div className={`flex h-16 items-center gap-1 ${forSider && collapsed ? "px-1" : "gap-2 px-3"}`}>
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-[#1677ff] font-bold text-white">
        R
      </div>
      {!(forSider && collapsed) && (
        <Text strong className="flex-1 truncate text-base">
          {t("appTitle")}
        </Text>
      )}
      {forSider && (
        <Button
          type="text"
          size="small"
          aria-label="toggle menu"
          icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          onClick={() => setCollapsed((c) => !c)}
        />
      )}
    </div>
  );

  const nav = (
    <Menu
      mode="inline"
      selectedKeys={selectedKey ? [selectedKey] : []}
      items={navItems.map((i) => ({ key: i.key, icon: i.icon, label: i.label }))}
      style={{ borderInlineEnd: "none" }}
      onClick={() => setDrawerOpen(false)}
    />
  );

  return (
    <Layout className="min-h-screen">
      {isDesktop ? (
        <Sider
          theme="light"
          width={236}
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          trigger={null}
          className="border-r border-neutral-200"
          style={{ position: "sticky", top: 0, height: "100vh", overflow: "auto" }}
        >
          {brand(true)}
          {nav}
        </Sider>
      ) : (
        <Drawer
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          placement="left"
          width={260}
          styles={{ body: { padding: 0 }, header: { display: "none" } }}
        >
          {brand(false)}
          {nav}
        </Drawer>
      )}

      <Layout>
        <Header
          className="sticky top-0 z-10 flex items-center justify-between border-b border-neutral-200 px-4"
          style={{ background: "#fff", paddingInline: 16 }}
        >
          <div className="flex min-w-0 items-center gap-3">
            {!isDesktop && (
              <>
                <Button type="text" icon={<MenuOutlined />} onClick={() => setDrawerOpen(true)} aria-label="menu" />
                <Text strong className="truncate">
                  {t("appTitle")}
                </Text>
              </>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Avatar size="small" className="!bg-[#1677ff]">
              {(user?.email?.[0] ?? "?").toUpperCase()}
            </Avatar>
            {user?.email && <Text className="hidden whitespace-nowrap text-neutral-500 sm:inline">{user.email}</Text>}
            <Button type="text" icon={<LogoutOutlined />} onClick={onLogout}>
              <span className="hidden sm:inline">{t("logout")}</span>
            </Button>
          </div>
        </Header>
        <Content className="p-4 md:p-6">{canViewCurrent ? children : null}</Content>
      </Layout>
    </Layout>
  );
}
