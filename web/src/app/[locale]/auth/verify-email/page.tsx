"use client";

import { Button, Card, Result, Spin } from "antd";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useRef, useState } from "react";
import { useVerifyEmailMutation } from "@/features/auth/api/verify-email";

function VerifyEmailInner({ locale }: { locale: string }) {
  const t = useTranslations("auth.verifyEmail");
  const params = useSearchParams();
  const token = params.get("token") ?? "";
  const verify = useVerifyEmailMutation();
  const [state, setState] = useState<"pending" | "ok" | "error">("pending");
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    if (!token) {
      setState("error");
      return;
    }
    verify
      .mutateAsync(token)
      .then(() => setState("ok"))
      .catch(() => setState("error"));
  }, [token, verify]);

  const toLogin = (
    <Link href={`/${locale}/login`}>
      <Button type="primary">{t("toLogin")}</Button>
    </Link>
  );

  if (state === "pending") {
    return (
      <div className="flex flex-col items-center gap-4 py-6">
        <Spin size="large" />
        <span className="text-neutral-500">{t("verifying")}</span>
      </div>
    );
  }
  if (state === "ok") {
    return <Result status="success" title={t("success")} extra={toLogin} />;
  }
  return <Result status="error" title={t("failed")} subTitle={t("failedHint")} extra={toLogin} />;
}

export default function VerifyEmailPage({ params: { locale } }: { params: { locale: string } }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-100 p-4">
      <Card className="w-full max-w-sm">
        <Suspense fallback={null}>
          <VerifyEmailInner locale={locale} />
        </Suspense>
      </Card>
    </div>
  );
}
