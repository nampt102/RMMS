"use client";

import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  CloseCircleOutlined,
  EnvironmentOutlined,
  FrownOutlined,
  StopOutlined,
} from "@ant-design/icons";
import { ProTable, type ActionType, type ProColumns } from "@ant-design/pro-components";
import {
  App,
  Button,
  DatePicker,
  Descriptions,
  Image,
  Input,
  Modal,
  Select,
  Space,
  Tag,
} from "antd";
import type { ReactNode } from "react";
import { type Dayjs } from "dayjs";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useMemo, useRef, useState } from "react";
import {
  fetchAttendance,
  useAttendanceStores,
  useAttendanceUsers,
  useReviewAttendance,
} from "@/features/attendance/api";
import {
  REVIEWABLE_STATUSES,
  type AttendanceRecord,
  type AttendanceStatus,
} from "@/features/attendance/types";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";

const STATUS_META: Record<AttendanceStatus, { color: string; icon: ReactNode }> = {
  valid: { color: "success", icon: <CheckCircleOutlined /> },
  late: { color: "gold", icon: <ClockCircleOutlined /> },
  gps_violation_pending_review: { color: "orange", icon: <EnvironmentOutlined /> },
  face_fail_pending_review: { color: "volcano", icon: <FrownOutlined /> },
  fake_gps_blocked: { color: "red", icon: <StopOutlined /> },
  admin_approved: { color: "cyan", icon: <CheckCircleOutlined /> },
  admin_rejected: { color: "default", icon: <CloseCircleOutlined /> },
};

const ALL_STATUSES = Object.keys(STATUS_META) as AttendanceStatus[];

export default function AttendancePage() {
  const t = useTranslations("attendance");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const { message } = App.useApp();
  const actionRef = useRef<ActionType>();

  const [userId, setUserId] = useState<string | undefined>();
  const [storeId, setStoreId] = useState<string | undefined>();
  const [status, setStatus] = useState<AttendanceStatus | undefined>();
  const [range, setRange] = useState<[Dayjs, Dayjs] | null>(null);

  const [detail, setDetail] = useState<AttendanceRecord | null>(null);
  const [reason, setReason] = useState("");

  const { data: users } = useAttendanceUsers();
  const { data: stores } = useAttendanceStores();
  const review = useReviewAttendance();

  const userName = useMemo(() => {
    const map = new Map<string, string>();
    (users ?? []).forEach((u) => map.set(u.id, `${u.fullName} (${u.email})`));
    return map;
  }, [users]);

  // Re-query whenever a filter changes.
  useEffect(() => {
    actionRef.current?.reload();
  }, [userId, storeId, status, range]);

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const fmt = (value: string | null) =>
    value ? new Date(value).toLocaleString(locale === "en" ? "en-US" : "vi-VN") : "—";

  const statusTag = (s: AttendanceStatus) => (
    <Tag color={STATUS_META[s].color} icon={STATUS_META[s].icon}>
      {t(`status_${s}`)}
    </Tag>
  );

  const isReviewable = (s: AttendanceStatus) => REVIEWABLE_STATUSES.includes(s);

  const submitReview = async (approve: boolean) => {
    if (!detail) return;
    if (!approve && reason.trim().length === 0) return;
    try {
      await review.mutateAsync({
        id: detail.id,
        approve,
        note: approve ? reason.trim() || undefined : reason.trim(),
      });
      message.success(approve ? t("approveSuccess") : t("rejectSuccess"));
      setDetail(null);
      setReason("");
    } catch (error) {
      showError(error);
    }
  };

  const columns: ProColumns<AttendanceRecord>[] = [
    {
      title: t("checkInAt"),
      dataIndex: "checkInAt",
      width: 170,
      render: (_, row) => fmt(row.checkInAt),
    },
    {
      title: t("user"),
      dataIndex: "userId",
      ellipsis: true,
      render: (_, row) => userName.get(row.userId) ?? row.userId,
    },
    {
      title: t("store"),
      dataIndex: "storeName",
      render: (_, row) => (
        <span>
          <Tag>{row.storeCode}</Tag>
          {row.storeName}
        </span>
      ),
    },
    {
      title: t("status"),
      dataIndex: "status",
      width: 230,
      render: (_, row) => (
        <Space size={4} wrap>
          {statusTag(row.status)}
          {row.isLate && <Tag color="gold">{t("late")}</Tag>}
        </Space>
      ),
    },
    {
      title: t("distance"),
      dataIndex: "checkInDistanceMeters",
      width: 110,
      align: "right",
      render: (_, row) => (
        <span className="tabular-nums">{Math.round(row.checkInDistanceMeters)} m</span>
      ),
    },
    {
      title: t("checkOutAt"),
      dataIndex: "checkOutAt",
      width: 170,
      render: (_, row) => fmt(row.checkOutAt),
    },
    {
      title: t("actions"),
      key: "actions",
      width: 120,
      fixed: "right",
      render: (_, row) => (
        <a
          onClick={() => {
            setDetail(row);
            setReason("");
          }}
        >
          {isReviewable(row.status) ? t("review") : t("detail")}
        </a>
      ),
    },
  ];

  const userOptions = (users ?? []).map((u) => ({ value: u.id, label: `${u.fullName} (${u.email})` }));
  const storeOptions = (stores ?? []).map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` }));

  return (
    <>
    <ProTable<AttendanceRecord>
      headerTitle={t("title")}
      actionRef={actionRef}
      rowKey="id"
      columns={columns}
      search={false}
      scroll={{ x: 1100 }}
      pagination={{ pageSize: 20, showSizeChanger: true }}
      toolBarRender={() => [
        <Select
          key="user"
          showSearch
          allowClear
          style={{ width: 240 }}
          placeholder={t("filterUser")}
          options={userOptions}
          value={userId}
          onChange={(v) => setUserId(v)}
          optionFilterProp="label"
        />,
        <Select
          key="store"
          showSearch
          allowClear
          style={{ width: 220 }}
          placeholder={t("filterStore")}
          options={storeOptions}
          value={storeId}
          onChange={(v) => setStoreId(v)}
          optionFilterProp="label"
        />,
        <Select
          key="status"
          allowClear
          style={{ width: 220 }}
          placeholder={t("filterStatus")}
          value={status}
          onChange={(v) => setStatus(v)}
          options={ALL_STATUSES.map((s) => ({ value: s, label: t(`status_${s}`) }))}
        />,
        <DatePicker.RangePicker
          key="range"
          value={range}
          onChange={(v) => setRange(v && v[0] && v[1] ? [v[0], v[1]] : null)}
        />,
      ]}
      request={async (params) => {
        try {
          const res = await fetchAttendance({
            page: params.current ?? 1,
            pageSize: params.pageSize ?? 20,
            userId,
            storeId,
            status,
            from: range?.[0]?.format("YYYY-MM-DD"),
            to: range?.[1]?.format("YYYY-MM-DD"),
          });
          return { data: res.data, total: res.meta.total, success: true };
        } catch (error) {
          showError(error);
          return { data: [], total: 0, success: false };
        }
      }}
    />
      {/* Detail / review modal — sibling of ProTable (ProTable does NOT render children). */}
      <Modal
        title={detail ? `${t("detailTitle")} · ${fmt(detail.checkInAt)}` : t("detailTitle")}
        open={detail !== null}
        width={720}
        onCancel={() => setDetail(null)}
        footer={
          detail && isReviewable(detail.status)
            ? [
                <Button key="cancel" onClick={() => setDetail(null)}>
                  {t("close")}
                </Button>,
                <Button
                  key="reject"
                  danger
                  loading={review.isPending}
                  disabled={reason.trim().length === 0}
                  onClick={() => submitReview(false)}
                >
                  {t("reject")}
                </Button>,
                <Button
                  key="approve"
                  type="primary"
                  loading={review.isPending}
                  onClick={() => submitReview(true)}
                >
                  {t("approve")}
                </Button>,
              ]
            : [
                <Button key="cancel" onClick={() => setDetail(null)}>
                  {t("close")}
                </Button>,
              ]
        }
      >
        {detail && <AttendanceDetail record={detail} />}
        {detail && isReviewable(detail.status) && (
          <div style={{ marginTop: 16 }}>
            <div style={{ marginBottom: 4 }}>{t("reviewNoteLabel")}</div>
            <Input.TextArea
              rows={3}
              value={reason}
              maxLength={1000}
              onChange={(e) => setReason(e.target.value)}
              placeholder={t("rejectReasonPlaceholder")}
            />
          </div>
        )}
      </Modal>
    </>
  );

  function AttendanceDetail({ record }: { record: AttendanceRecord }) {
    return (
      <>
        <Descriptions size="small" column={2} bordered>
          <Descriptions.Item label={t("user")} span={2}>
            {userName.get(record.userId) ?? record.userId}
          </Descriptions.Item>
          <Descriptions.Item label={t("store")} span={2}>
            <Tag>{record.storeCode}</Tag>
            {record.storeName}
          </Descriptions.Item>
          <Descriptions.Item label={t("status")}>{statusTag(record.status)}</Descriptions.Item>
          <Descriptions.Item label={t("late")}>
            {record.isLate ? t("yes") : t("no")}
          </Descriptions.Item>
          <Descriptions.Item label={t("checkInAt")}>{fmt(record.checkInAt)}</Descriptions.Item>
          <Descriptions.Item label={t("checkOutAt")}>{fmt(record.checkOutAt)}</Descriptions.Item>
          <Descriptions.Item label={t("distance")}>
            <span className="tabular-nums">{Math.round(record.checkInDistanceMeters)} m</span>
          </Descriptions.Item>
          <Descriptions.Item label={t("faceResult")}>
            {t(`face_${record.checkInFaceResult}`)}
          </Descriptions.Item>
          {record.reviewNote && (
            <Descriptions.Item label={t("reviewNoteLabel")} span={2}>
              {record.reviewNote}
            </Descriptions.Item>
          )}
        </Descriptions>

        <div style={{ marginTop: 16, display: "flex", gap: 24, flexWrap: "wrap" }}>
          <PhotoSlot label={t("checkInSelfie")} url={record.checkInSelfieUrl} />
          <PhotoSlot label={t("checkInStorePhoto")} url={record.checkInStorePhotoUrl} />
          {record.checkOutAt && (
            <>
              <PhotoSlot label={t("checkOutSelfie")} url={record.checkOutSelfieUrl} />
              <PhotoSlot label={t("checkOutStorePhoto")} url={record.checkOutStorePhotoUrl} />
            </>
          )}
        </div>
      </>
    );
  }

  function PhotoSlot({ label, url }: { label: string; url: string | null }) {
    // Photo storage is stubbed until M13/MinIO — `local://` URLs are placeholders.
    const pending = !url || url.startsWith("local://");
    return (
      <div style={{ width: 140 }}>
        <div style={{ fontSize: 12, color: "#64748b", marginBottom: 4 }}>{label}</div>
        {pending ? (
          <div
            style={{
              width: 140,
              height: 140,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              border: "1px dashed #cbd5e1",
              borderRadius: 8,
              color: "#94a3b8",
              fontSize: 12,
              textAlign: "center",
              padding: 8,
            }}
          >
            {t("photoPending")}
          </div>
        ) : (
          <Image src={url} alt={label} width={140} height={140} style={{ objectFit: "cover", borderRadius: 8 }} />
        )}
      </div>
    );
  }
}
