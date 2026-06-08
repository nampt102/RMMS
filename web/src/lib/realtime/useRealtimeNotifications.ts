"use client";

import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from "@microsoft/signalr";
import { App } from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";
import { useAuthStore } from "@/lib/stores/auth-store";

/** Server `"notification"` event payload (mirrors RealtimeNotification). */
interface RealtimePayload {
  type: string;
  title: string;
  body: string;
}

/**
 * Connects the signed-in user to the M14 notifications hub (SignalR). On a `"notification"`
 * event it shows a toast and invalidates the queues that may have changed (approvals,
 * team monitoring) so the UI refreshes live — useful for Leader/BUH approvers on web,
 * which has no FCM. No-op until an access token is present; reconnects automatically.
 */
export function useRealtimeNotifications() {
  const token = useAuthStore((s) => s.accessToken);
  const qc = useQueryClient();
  const { notification } = App.useApp();
  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!token) return;

    const baseURL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";
    const connection = new HubConnectionBuilder()
      .withUrl(`${baseURL}/hubs/notifications`, {
        // Token may rotate — read the freshest value on each (re)connect.
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connRef.current = connection;

    connection.on("notification", (payload: RealtimePayload) => {
      notification.info({
        message: payload.title,
        description: payload.body,
        placement: "topRight",
        duration: 4,
      });
      // Refresh anything a new notification might have changed.
      qc.invalidateQueries({ queryKey: ["approvals"] });
      qc.invalidateQueries({ queryKey: ["team-monitoring"] });
    });

    connection.start().catch(() => {
      // Hub unreachable / unauthorized — realtime is best-effort; the app works without it.
    });

    return () => {
      connection.off("notification");
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
      connRef.current = null;
    };
  }, [token, qc, notification]);
}
