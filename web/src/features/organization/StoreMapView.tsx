"use client";

import { Alert, Card, Empty, Input, Select, Skeleton, Space, Tag } from "antd";
import dynamic from "next/dynamic";
import { useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useAreas, useStoresForMap } from "./api";

// Leaflet touches the DOM directly → load client-side only (ADR-010).
const StoreMap = dynamic(() => import("./StoreMap"), {
  ssr: false,
  loading: () => <Skeleton.Node active style={{ width: "100%", height: 520 }} />,
});

export default function StoreMapView() {
  const t = useTranslations("stores");
  const [search, setSearch] = useState("");
  const [areaId, setAreaId] = useState<string | undefined>(undefined);
  const [status, setStatus] = useState<string | undefined>(undefined);

  const { data: areas } = useAreas();
  const { data: stores, isLoading, isError } = useStoresForMap({ areaId, status, search });

  const areaOptions = useMemo(
    () => (areas ?? []).map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` })),
    [areas],
  );

  const statusLabel = (value: string) =>
    value === "active" ? t("status_active") : t("status_inactive");

  const list = stores ?? [];
  const activeCount = list.filter((s) => s.status === "active").length;

  return (
    <Card
      styles={{ body: { padding: 16 } }}
      title={
        <Space wrap size="middle">
          <Input.Search
            allowClear
            placeholder={t("search")}
            style={{ width: 220 }}
            onSearch={setSearch}
            aria-label={t("search")}
          />
          <Select
            allowClear
            placeholder={t("area")}
            style={{ width: 200 }}
            options={areaOptions}
            value={areaId}
            onChange={setAreaId}
            showSearch
            optionFilterProp="label"
            aria-label={t("area")}
          />
          <Select
            allowClear
            placeholder={t("status")}
            style={{ width: 140 }}
            value={status}
            onChange={setStatus}
            aria-label={t("status")}
            options={[
              { value: "active", label: t("status_active") },
              { value: "inactive", label: t("status_inactive") },
            ]}
          />
        </Space>
      }
    >
      {isError ? (
        <Alert type="error" showIcon message={t("mapLoadError")} />
      ) : (
        <>
          <Space style={{ marginBottom: 12 }} size="small" wrap>
            <Tag color="success">{t("mapLegendActive", { count: activeCount })}</Tag>
            <Tag>{t("mapLegendInactive", { count: list.length - activeCount })}</Tag>
          </Space>
          <div style={{ height: 520, position: "relative" }}>
            {isLoading ? (
              <Skeleton.Node active style={{ width: "100%", height: 520 }} />
            ) : list.length === 0 ? (
              <div style={{ display: "grid", placeItems: "center", height: "100%" }}>
                <Empty description={t("mapEmpty")} />
              </div>
            ) : (
              <StoreMap stores={list} statusLabel={statusLabel} />
            )}
          </div>
        </>
      )}
    </Card>
  );
}
