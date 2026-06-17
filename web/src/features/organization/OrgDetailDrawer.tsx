"use client";

import { Descriptions, Divider, Drawer, Empty, Skeleton, Space, Tag } from "antd";
import { useTranslations } from "next-intl";
import { useUserAssignments } from "./api";
import { RoleTag, StatusBadge } from "./org-ui";

export type OrgDetailTarget = {
  id: string;
  fullName: string;
  role: string;
  status: string;
  email?: string;
};

/** Read-only person detail: identity + active assignments (leader / stores / categories). */
export function OrgDetailDrawer({
  target,
  onClose,
}: {
  target: OrgDetailTarget | null;
  onClose: () => void;
}) {
  const t = useTranslations("organization");
  const tA = useTranslations("assignments");
  const { data, isLoading } = useUserAssignments(target?.id ?? null);

  return (
    <Drawer
      open={target !== null}
      onClose={onClose}
      width={420}
      destroyOnHidden
      title={target?.fullName ?? ""}
    >
      {target && (
        <>
          <Space size={8} wrap>
            <RoleTag role={target.role} />
            <StatusBadge status={target.status} />
          </Space>

          <Descriptions column={1} size="small" className="mt-4">
            {target.email && <Descriptions.Item label={t("detailEmail")}>{target.email}</Descriptions.Item>}
          </Descriptions>

          <Divider orientation="left" plain>
            {t("detailAssignments")}
          </Divider>

          {isLoading ? (
            <Skeleton active paragraph={{ rows: 3 }} />
          ) : (
            <Descriptions column={1} size="small" layout="vertical">
              {target.role === "pg" && (
                <Descriptions.Item label={tA("leader")}>
                  {data?.leader ? data.leader.fullName : <span className="text-neutral-400">{t("detailNone")}</span>}
                </Descriptions.Item>
              )}
              <Descriptions.Item label={tA("stores")}>
                {data && data.stores.length > 0 ? (
                  <Space size={[4, 4]} wrap>
                    {data.stores.map((s) => (
                      <Tag key={s.id} color="geekblue">
                        {s.code} · {s.name}
                      </Tag>
                    ))}
                  </Space>
                ) : (
                  <span className="text-neutral-400">{t("detailNone")}</span>
                )}
              </Descriptions.Item>
              <Descriptions.Item label={tA("categories")}>
                {data && data.categories.length > 0 ? (
                  <Space size={[4, 4]} wrap>
                    {data.categories.map((c) => (
                      <Tag key={c.id}>{c.name}</Tag>
                    ))}
                  </Space>
                ) : (
                  <span className="text-neutral-400">{t("detailNone")}</span>
                )}
              </Descriptions.Item>
            </Descriptions>
          )}

          {!isLoading && !data && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} />}
        </>
      )}
    </Drawer>
  );
}
