"use client";

import { Button, Card, Input, Result, Spin } from "antd";
import { useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import {
  confirmEmailAction,
  previewEmailAction,
} from "@/features/approvals/api";
import type { EmailActionPreview } from "@/features/approvals/types";

type Phase = "loading" | "preview" | "rejecting" | "submitting" | "done" | "error";

/**
 * Public BUH email-link landing page (M09, BR-407 / AC-18). No login required.
 * Renders a friendly state for expired / used / already-decided links and lets the
 * BUH approve or reject (reason) in one click. Uses the public, no-auth endpoints.
 */
function ApproveLandingContent() {
  const t = useTranslations("approveLink");
  const token = useSearchParams().get("token") ?? "";

  const [phase, setPhase] = useState<Phase>("loading");
  const [preview, setPreview] = useState<EmailActionPreview | null>(null);
  const [reason, setReason] = useState("");
  const [resultStatus, setResultStatus] = useState<string | null>(null);
  const [errorText, setErrorText] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    if (!token) {
      setPhase("error");
      setErrorText(t("invalid"));
      return;
    }
    previewEmailAction(token)
      .then((p) => {
        if (!active) return;
        setPreview(p);
        setPhase("preview");
      })
      .catch(() => {
        if (!active) return;
        setPhase("error");
        setErrorText(t("invalid"));
      });
    return () => {
      active = false;
    };
  }, [token, t]);

  const submit = async (action: "approve" | "reject") => {
    if (action === "reject" && phase !== "rejecting") {
      setPhase("rejecting");
      return;
    }
    setPhase("submitting");
    try {
      const res = await confirmEmailAction(token, action, action === "reject" ? reason : undefined);
      setResultStatus(res.status);
      setPhase("done");
    } catch {
      setPhase("error");
      setErrorText(t("failed"));
    }
  };

  const center = (node: React.ReactNode) => (
    <main className="flex min-h-dvh items-center justify-center bg-neutral-50 p-4">
      <Card className="w-full max-w-md shadow-sm">{node}</Card>
    </main>
  );

  if (phase === "loading") {
    return center(
      <div className="flex flex-col items-center gap-3 py-8">
        <Spin size="large" />
        <span className="text-neutral-500">{t("loading")}</span>
      </div>,
    );
  }

  if (phase === "error") {
    return center(<Result status="warning" title={t("title")} subTitle={errorText ?? t("invalid")} />);
  }

  if (phase === "done") {
    const ok = resultStatus === "approved";
    return center(
      <Result
        status={ok ? "success" : "info"}
        title={ok ? t("approved") : t("rejected")}
        subTitle={t("recorded")}
      />,
    );
  }

  // Not actionable (expired / used / already decided)
  if (preview && !preview.valid) {
    const subTitle = preview.expired
      ? t("expired")
      : preview.used
        ? t("used")
        : preview.alreadyDecided
          ? t("alreadyDecided")
          : t("invalid");
    return center(<Result status="warning" title={t("title")} subTitle={subTitle} />);
  }

  // Valid + pending → actionable
  return center(
    <div className="flex flex-col gap-4 py-2">
      <div>
        <h1 className="text-xl font-semibold">{t("title")}</h1>
        <p className="mt-1 text-sm text-neutral-500">{t("prompt")}</p>
      </div>

      {phase === "rejecting" && (
        <Input.TextArea
          rows={3}
          maxLength={500}
          showCount
          placeholder={t("reasonPlaceholder")}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
      )}

      <div className="flex gap-3">
        {phase === "rejecting" ? (
          <Button
            danger
            type="primary"
            block
            disabled={!reason.trim()}
            loading={false}
            onClick={() => submit("reject")}
          >
            {t("confirmReject")}
          </Button>
        ) : (
          <>
            <Button type="primary" block onClick={() => submit("approve")}>
              {t("approve")}
            </Button>
            <Button danger block onClick={() => submit("reject")}>
              {t("reject")}
            </Button>
          </>
        )}
      </div>
      <p className="text-center text-xs text-neutral-400">{t("note")}</p>
    </div>,
  );
}

/**
 * useSearchParams() forces client-side rendering, so Next.js requires a Suspense
 * boundary around it (otherwise static prerender of this page fails). The fallback
 * mirrors the in-component "loading" state.
 */
export default function ApproveLandingPage() {
  return (
    <Suspense
      fallback={
        <main className="flex min-h-dvh items-center justify-center bg-neutral-50 p-4">
          <Card className="w-full max-w-md shadow-sm">
            <div className="flex flex-col items-center gap-3 py-8">
              <Spin size="large" />
            </div>
          </Card>
        </main>
      }
    >
      <ApproveLandingContent />
    </Suspense>
  );
}
