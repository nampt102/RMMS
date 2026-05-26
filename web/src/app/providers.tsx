"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { ConfigProvider, App as AntdApp } from "antd";
import viVN from "antd/locale/vi_VN";
import enUS from "antd/locale/en_US";
import { useMemo } from "react";
import { makeQueryClient } from "@/lib/api/query-client";

type Props = {
  children: React.ReactNode;
  locale: string;
};

export function Providers({ children, locale }: Props) {
  const queryClient = useMemo(() => makeQueryClient(), []);
  const antdLocale = locale === "en" ? enUS : viVN;

  return (
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        locale={antdLocale}
        theme={{
          token: { colorPrimary: "#1677ff", borderRadius: 6 },
        }}
      >
        <AntdApp>{children}</AntdApp>
      </ConfigProvider>
      {process.env.NODE_ENV === "development" && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  );
}
