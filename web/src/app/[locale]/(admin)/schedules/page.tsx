"use client";

import { App, Card, DatePicker, Empty, Input, Modal, Popconfirm, Select, Space, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";
import dayjs, { type Dayjs } from "dayjs";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import {
  useApproveSchedule,
  useRejectSchedule,
  useSchedulableUsers,
  useUserSchedule,
} from "@/features/schedule/api";
import type { ScheduleStatus, WorkSchedule } from "@/features/schedule/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const STATUS_TAG: Record<ScheduleStatus, string> = {
  pending: "processing",
  approved: "success",
  rejected: "error",
  edit_pending: "warning",
  superseded: "default",
};

const PENDING_STATES: ScheduleStatus[] = ["pending", "edit_pending"];

export default function SchedulesPage() {
  const t = useTranslations("schedules");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();

  const [userId, setUserId] = useState<string | null>(null);
  const [range, setRange] = useState<[Dayjs, Dayjs]>([dayjs(), dayjs().add(30, "day")]);
  const [rejecting, setRejecting] = useState<WorkSchedule | null>(null);
  const [reason, setReason] = useState("");

  const from = range[0].format("YYYY-MM-DD");
  const to = range[1].format("YYYY-MM-DD");

  const { data: users } = useSchedulableUsers();
  const { data: schedules, isLoading } = useUserSchedule(userId, from, to);
  const approve = useApproveSchedule(userId);
  const reject = useRejectSchedule(userId);

  const userOptions = useMemo(
    () => (users ?? []).map((u) => ({ value: u.id, label: `${u.fullName} (${u.email})` })),
    [users],
  );

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const statusLabel = (s: ScheduleStatus) => t(`status_${s}`);

  const columns: ColumnsType<WorkSchedule> = [
    {
      title: t("date"),
      dataIndex: "scheduleDate",
      width: 130,
      render: (v: string) => new Date(v).toLocaleDateString(locale === "en" ? "en-US" : "vi-VN"),
    },
    {
      title: t("shifts"),
      key: "shifts",
      render: (_, row) => (
        <Space size={[4, 4]} wrap>
          {row.shifts.map((s) => (
            <Tag key={s.id} color="geekblue" className="tabular-nums">
              {s.startTime}–{s.endTime} · {s.storeCode}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: t("status"),
      dataIndex: "status",
      width: 140,
      render: (s: ScheduleStatus) => <Tag color={STATUS_TAG[s]}>{statusLabel(s)}</Tag>,
    },
    { title: t("version"), dataIndex: "version", width: 80 },
    {
      title: t("rejectReason"),
      dataIndex: "rejectReason",
      render: (v: string | null) => v ?? "—",
    },
    {
      title: t("actions"),
      key: "actions",
      width: 180,
      render: (_, row) =>
        PENDING_STATES.includes(row.status) ? (
          <Space>
            <Popconfirm
              title={t("approveConfirm")}
              onConfirm={async () => {
                try {
                  await approve.mutateAsync(row.id);
                  message.success(t("approveSuccess"));
                } catch (error) {
                  showError(error);
                }
              }}
            >
              <a>{t("approve")}</a>
            </Popconfirm>
            <a
              className="text-red-600"
              onClick={() => {
                setRejecting(row);
                setReason("");
              }}
            >
              {t("reject")}
            </a>
          </Space>
        ) : (
          "—"
        ),
    },
  ];

  return (
    <Card
      title={
        <Space wrap size="middle">
          <Select
            showSearch
            allowClear
            style={{ width: 320 }}
            placeholder={t("pickUser")}
            options={userOptions}
            value={userId ?? undefined}
            onChange={(v) => setUserId(v ?? null)}
            optionFilterProp="label"
          />
          <DatePicker.RangePicker
            value={range}
            allowClear={false}
            onChange={(v) => {
              if (v && v[0] && v[1]) setRange([v[0], v[1]]);
            }}
          />
        </Space>
      }
    >
      {userId === null ? (
        <Empty description={t("pickUserHint")} />
      ) : (
        <Table<WorkSchedule>
          rowKey="id"
          loading={isLoading}
          columns={columns}
          dataSource={schedules ?? []}
          pagination={false}
          locale={{ emptyText: <Empty description={t("noSchedules")} /> }}
        />
      )}

      <Modal
        title={t("rejectTitle")}
        open={rejecting !== null}
        okText={t("reject")}
        okButtonProps={{ danger: true, disabled: reason.trim().length === 0 }}
        onCancel={() => setRejecting(null)}
        onOk={async () => {
          if (!rejecting) return;
          try {
            await reject.mutateAsync({ scheduleId: rejecting.id, reason: reason.trim() });
            message.success(t("rejectSuccess"));
            setRejecting(null);
          } catch (error) {
            showError(error);
          }
        }}
      >
        <Input.TextArea
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder={t("rejectReasonPlaceholder")}
          maxLength={1000}
        />
      </Modal>
    </Card>
  );
}
