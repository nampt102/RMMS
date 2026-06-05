"use client";

import { useQuery } from "@tanstack/react-query";
import { App, Divider, Empty, Select, Space, Spin, Tag, Typography } from "antd";
import { useTranslations } from "next-intl";
import { fetchUsers } from "@/features/users/api";
import { errorCodeFromUnknown } from "@/features/auth/lib/auth-error";
import {
  useAllStores,
  useAssignPgLeader,
  useAssignUserCategory,
  useAssignUserStore,
  useCategories,
  useUnassignUserCategory,
  useUnassignUserStore,
  useUserAssignments,
} from "./api";

type Props = {
  userId: string;
  /** PG users get a Leader assignment row; Leaders/BUH only get store/category. */
  isPg: boolean;
};

/**
 * Assignment management for one user (M03). Embedded in the Users detail Drawer.
 * - PG → Leader (1:1): a Select that assigns / changes the active Leader.
 * - User ↔ Stores / Categories (1:N): closable Tags to remove + a Select to add.
 */
export function UserAssignmentsPanel({ userId, isPg }: Props) {
  const t = useTranslations("assignments");
  const tErrors = useTranslations("errors");
  const { message } = App.useApp();

  const assignments = useUserAssignments(userId);
  const { data: allStores } = useAllStores();
  const { data: categories } = useCategories();

  const leaders = useQuery({
    queryKey: ["admin", "users", "leaders"],
    enabled: isPg,
    queryFn: async () => {
      const res = await fetchUsers({ page: 1, pageSize: 100, role: "leader" });
      return res.data;
    },
  });

  const assignLeader = useAssignPgLeader(userId);
  const assignStore = useAssignUserStore(userId);
  const unassignStore = useUnassignUserStore(userId);
  const assignCategory = useAssignUserCategory(userId);
  const unassignCategory = useUnassignUserCategory(userId);

  const showError = (error: unknown) => {
    const code = errorCodeFromUnknown(error);
    message.error(tErrors.has(code) ? tErrors(code) : tErrors("INTERNAL_ERROR"));
  };

  const run = async (fn: () => Promise<unknown>, successKey: string) => {
    try {
      await fn();
      message.success(t(successKey));
    } catch (error) {
      showError(error);
    }
  };

  const data = assignments.data;
  const assignedStoreIds = new Set((data?.stores ?? []).map((s) => s.id));
  const assignedCategoryIds = new Set((data?.categories ?? []).map((c) => c.id));

  const storeOptions = (allStores ?? [])
    .filter((s) => !assignedStoreIds.has(s.id))
    .map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` }));
  const categoryOptions = (categories ?? [])
    .filter((c) => !assignedCategoryIds.has(c.id))
    .map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }));
  const leaderOptions = (leaders.data ?? []).map((l) => ({ value: l.id, label: `${l.fullName} (${l.email})` }));

  return (
    <>
      <Divider orientation="left">{t("title")}</Divider>

      {assignments.isLoading ? (
        <Spin />
      ) : (
        <Space direction="vertical" size="middle" className="w-full">
          {isPg && (
            <div>
              <Typography.Text type="secondary">{t("leader")}</Typography.Text>
              <Select
                className="w-full"
                placeholder={t("leaderPlaceholder")}
                options={leaderOptions}
                value={data?.leader?.leaderUserId ?? undefined}
                loading={leaders.isLoading || assignLeader.isPending}
                showSearch
                optionFilterProp="label"
                onChange={(value) => run(() => assignLeader.mutateAsync(value), "assignSuccess")}
              />
            </div>
          )}

          <div>
            <Typography.Text type="secondary">{t("stores")}</Typography.Text>
            <div className="mb-2 mt-1 flex flex-wrap gap-1">
              {data && data.stores.length > 0 ? (
                data.stores.map((s) => (
                  <Tag
                    key={s.id}
                    color="geekblue"
                    closable
                    onClose={(e) => {
                      e.preventDefault();
                      run(() => unassignStore.mutateAsync(s.id), "unassignSuccess");
                    }}
                  >
                    {s.code} — {s.name}
                  </Tag>
                ))
              ) : (
                <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t("noStores")} />
              )}
            </div>
            <Select
              className="w-full"
              placeholder={t("storesPlaceholder")}
              options={storeOptions}
              value={null}
              loading={assignStore.isPending}
              showSearch
              optionFilterProp="label"
              onChange={(value) => value && run(() => assignStore.mutateAsync(value), "assignSuccess")}
            />
          </div>

          <div>
            <Typography.Text type="secondary">{t("categories")}</Typography.Text>
            <div className="mb-2 mt-1 flex flex-wrap gap-1">
              {data && data.categories.length > 0 ? (
                data.categories.map((c) => (
                  <Tag
                    key={c.id}
                    color="purple"
                    closable
                    onClose={(e) => {
                      e.preventDefault();
                      run(() => unassignCategory.mutateAsync(c.id), "unassignSuccess");
                    }}
                  >
                    {c.code} — {c.name}
                  </Tag>
                ))
              ) : (
                <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t("noCategories")} />
              )}
            </div>
            <Select
              className="w-full"
              placeholder={t("categoriesPlaceholder")}
              options={categoryOptions}
              value={null}
              loading={assignCategory.isPending}
              showSearch
              optionFilterProp="label"
              onChange={(value) => value && run(() => assignCategory.mutateAsync(value), "assignSuccess")}
            />
          </div>
        </Space>
      )}
    </>
  );
}
