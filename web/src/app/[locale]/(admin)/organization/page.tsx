"use client";

import { useMemo, useState } from "react";
import {
  ApartmentOutlined,
  CrownOutlined,
  DownOutlined,
  ShopOutlined,
  TeamOutlined,
  UserOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import { Alert, Button, Card, Empty, Input, Skeleton, Space, Tabs, Tag, Tree, type TreeDataNode } from "antd";
import { useTranslations } from "next-intl";
import {
  useOrgAreaTree,
  useOrgHierarchy,
  type OrgAreaNode,
  type OrgHierarchy,
  type OrgStoreNode,
} from "@/features/organization/orgViews";
import { OrgDetailDrawer, type OrgDetailTarget } from "@/features/organization/OrgDetailDrawer";
import { RoleTag, StatusBadge } from "@/features/organization/org-ui";

type Meta =
  | { kind: "group"; label: string; icon: "mgmt" | "warn" | "arealess"; count: number }
  | { kind: "person"; person: OrgDetailTarget }
  | { kind: "leader"; person: OrgDetailTarget; count: number }
  | { kind: "store"; code: string; name: string; status: string; count: number }
  | { kind: "area"; code: string; name: string };

type OrgNode = TreeDataNode & { meta: Meta; searchText: string; children?: OrgNode[] };

function Highlight({ text, q }: { text: string; q: string }) {
  if (!q) return <>{text}</>;
  const i = text.toLowerCase().indexOf(q.toLowerCase());
  if (i < 0) return <>{text}</>;
  return (
    <>
      {text.slice(0, i)}
      <mark className="bg-amber-200 px-0.5">{text.slice(i, i + q.length)}</mark>
      {text.slice(i + q.length)}
    </>
  );
}

const Count = ({ n }: { n: number }) => <span className="ml-1 text-xs text-neutral-400">({n})</span>;

/** Collect ancestor keys of every node whose searchText matches `q` (for search auto-expand). */
function ancestorsOfMatches(nodes: OrgNode[], q: string): string[] {
  const out = new Set<string>();
  const walk = (node: OrgNode, parents: string[]): boolean => {
    const selfMatch = node.searchText.toLowerCase().includes(q);
    let childMatch = false;
    for (const c of (node.children ?? []) as OrgNode[]) {
      if (walk(c, [...parents, String(node.key)])) childMatch = true;
    }
    if ((selfMatch || childMatch) && node.children?.length) out.add(String(node.key));
    if (selfMatch) parents.forEach((p) => out.add(p));
    return selfMatch || childMatch;
  };
  nodes.forEach((n) => walk(n, []));
  return [...out];
}

function allParentKeys(nodes: OrgNode[]): string[] {
  const out: string[] = [];
  const walk = (n: OrgNode) => {
    if (n.children?.length) {
      out.push(String(n.key));
      (n.children as OrgNode[]).forEach(walk);
    }
  };
  nodes.forEach(walk);
  return out;
}

/** Reusable searchable / expandable tree wrapper shared by both tabs. */
function OrgTree({ nodes, onPick }: { nodes: OrgNode[]; onPick: (t: OrgDetailTarget) => void }) {
  const t = useTranslations("organization");
  const [q, setQ] = useState("");
  const [expanded, setExpanded] = useState<React.Key[]>(() => allParentKeys(nodes));
  const [autoExpand, setAutoExpand] = useState(true);

  const onSearch = (value: string) => {
    setQ(value);
    if (value) {
      setExpanded(ancestorsOfMatches(nodes, value.toLowerCase()));
      setAutoExpand(true);
    }
  };

  const titleRender = (raw: TreeDataNode) => {
    const node = raw as OrgNode;
    const m = node.meta;
    switch (m.kind) {
      case "group":
        return (
          <span className="inline-flex items-center gap-2 font-semibold text-neutral-700">
            {m.icon === "mgmt" && <CrownOutlined className="text-purple-500" />}
            {m.icon === "warn" && <WarningOutlined className="text-amber-500" />}
            {m.icon === "arealess" && <ShopOutlined className="text-neutral-400" />}
            {m.label}
            <Count n={m.count} />
          </span>
        );
      case "leader":
        return (
          <span className="inline-flex items-center gap-2">
            <UserOutlined className="text-neutral-400" />
            <span className="font-medium">
              <Highlight text={m.person.fullName} q={q} />
            </span>
            <RoleTag role="leader" />
            <StatusBadge status={m.person.status} />
            <Count n={m.count} />
          </span>
        );
      case "person":
        return (
          <span className="inline-flex items-center gap-2">
            <UserOutlined className="text-neutral-400" />
            <Highlight text={m.person.fullName} q={q} />
            <RoleTag role={m.person.role} />
            <StatusBadge status={m.person.status} />
          </span>
        );
      case "store":
        return (
          <span className="inline-flex items-center gap-2">
            <ShopOutlined className="text-blue-400" />
            <span>
              <span className="font-mono text-xs text-neutral-500">{m.code}</span>{" "}
              <Highlight text={m.name} q={q} />
            </span>
            {m.status !== "active" && <Tag color="default">{t("storeInactive")}</Tag>}
            <Count n={m.count} />
          </span>
        );
      case "area":
        return (
          <span className="inline-flex items-center gap-2 font-medium">
            <ApartmentOutlined className="text-neutral-500" />
            <span className="font-mono text-xs text-neutral-500">{m.code}</span>
            <Highlight text={m.name} q={q} />
          </span>
        );
    }
  };

  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <Input.Search
          allowClear
          placeholder={t("searchPlaceholder")}
          onChange={(e) => onSearch(e.target.value)}
          className="max-w-xs"
        />
        <Button size="small" onClick={() => setExpanded(allParentKeys(nodes))}>
          {t("expandAll")}
        </Button>
        <Button size="small" onClick={() => setExpanded([])}>
          {t("collapseAll")}
        </Button>
      </div>
      <Tree<OrgNode>
        treeData={nodes}
        titleRender={titleRender}
        switcherIcon={<DownOutlined />}
        showLine={{ showLeafIcon: false }}
        blockNode
        selectable
        expandedKeys={expanded}
        autoExpandParent={autoExpand}
        onExpand={(keys) => {
          setExpanded(keys);
          setAutoExpand(false);
        }}
        onSelect={(_keys, info) => {
          const m = (info.node as OrgNode).meta;
          if (m.kind === "person" || m.kind === "leader") onPick(m.person);
        }}
      />
    </div>
  );
}

// ---------------- Tree builders ----------------

function buildHierarchy(data: OrgHierarchy, labels: { mgmt: string; unassigned: string }): OrgNode[] {
  const nodes: OrgNode[] = [];

  if (data.buhs.length > 0) {
    nodes.push({
      key: "group:buh",
      meta: { kind: "group", label: labels.mgmt, icon: "mgmt", count: data.buhs.length },
      searchText: labels.mgmt,
      children: data.buhs.map((p) => ({
        key: `person:${p.id}`,
        meta: { kind: "person", person: { id: p.id, fullName: p.fullName, role: p.role, status: p.status, email: p.email } },
        searchText: `${p.fullName} ${p.email}`,
        isLeaf: true,
      })),
    });
  }

  for (const l of data.leaders) {
    nodes.push({
      key: `leader:${l.id}`,
      meta: {
        kind: "leader",
        person: { id: l.id, fullName: l.fullName, role: "leader", status: l.status, email: l.email },
        count: l.pgs.length,
      },
      searchText: `${l.fullName} ${l.email}`,
      children: l.pgs.map((p) => ({
        key: `person:${p.id}`,
        meta: { kind: "person", person: { id: p.id, fullName: p.fullName, role: p.role, status: p.status, email: p.email } },
        searchText: `${p.fullName} ${p.email}`,
        isLeaf: true,
      })),
    });
  }

  if (data.unassignedPgs.length > 0) {
    nodes.push({
      key: "group:unassigned",
      meta: { kind: "group", label: labels.unassigned, icon: "warn", count: data.unassignedPgs.length },
      searchText: labels.unassigned,
      children: data.unassignedPgs.map((p) => ({
        key: `person:${p.id}`,
        meta: { kind: "person", person: { id: p.id, fullName: p.fullName, role: p.role, status: p.status, email: p.email } },
        searchText: `${p.fullName} ${p.email}`,
        isLeaf: true,
      })),
    });
  }

  return nodes;
}

function storeNode(s: OrgStoreNode): OrgNode {
  return {
    key: `store:${s.id}`,
    meta: { kind: "store", code: s.code, name: s.name, status: s.status, count: s.employees.length },
    searchText: `${s.code} ${s.name}`,
    children: s.employees.map((e) => ({
      key: `person:${e.id}`,
      meta: { kind: "person", person: { id: e.id, fullName: e.fullName, role: e.role, status: e.status } },
      searchText: e.fullName,
      isLeaf: true,
    })),
  };
}

function buildAreaTree(data: { areas: OrgAreaNode[]; unassignedStores: OrgStoreNode[] }, arealessLabel: string): OrgNode[] {
  const childrenByParent = new Map<string | null, OrgAreaNode[]>();
  for (const a of data.areas) {
    const key = a.parentAreaId ?? null;
    if (!childrenByParent.has(key)) childrenByParent.set(key, []);
    childrenByParent.get(key)!.push(a);
  }

  const buildArea = (a: OrgAreaNode): OrgNode => {
    const subAreas = (childrenByParent.get(a.id) ?? []).map(buildArea);
    const stores = a.stores.map(storeNode);
    return {
      key: `area:${a.id}`,
      meta: { kind: "area", code: a.code, name: a.name },
      searchText: `${a.code} ${a.name}`,
      children: [...subAreas, ...stores],
    };
  };

  const roots = (childrenByParent.get(null) ?? []).map(buildArea);

  if (data.unassignedStores.length > 0) {
    roots.push({
      key: "group:arealess",
      meta: { kind: "group", label: arealessLabel, icon: "arealess", count: data.unassignedStores.length },
      searchText: arealessLabel,
      children: data.unassignedStores.map(storeNode),
    });
  }

  return roots;
}

// ---------------- Page ----------------

export default function OrganizationPage() {
  const t = useTranslations("organization");
  const [detail, setDetail] = useState<OrgDetailTarget | null>(null);

  const hierarchy = useOrgHierarchy();
  const areaTree = useOrgAreaTree();

  const hierarchyNodes = useMemo(
    () => (hierarchy.data ? buildHierarchy(hierarchy.data, { mgmt: t("tierManagement"), unassigned: t("unassignedPgs") }) : []),
    [hierarchy.data, t],
  );
  const areaNodes = useMemo(
    () => (areaTree.data ? buildAreaTree(areaTree.data, t("storesNoArea")) : []),
    [areaTree.data, t],
  );

  const hierarchyTab = (
    <>
      <Alert type="info" showIcon className="mb-4" message={t("hierarchyHint")} />
      {hierarchy.isLoading ? (
        <Skeleton active />
      ) : hierarchyNodes.length === 0 ? (
        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} />
      ) : (
        <OrgTree nodes={hierarchyNodes} onPick={setDetail} />
      )}
    </>
  );

  const areaTab = (
    <>
      <Alert type="info" showIcon className="mb-4" message={t("areaHint")} />
      {areaTree.isLoading ? (
        <Skeleton active />
      ) : areaNodes.length === 0 ? (
        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} />
      ) : (
        <OrgTree nodes={areaNodes} onPick={setDetail} />
      )}
    </>
  );

  return (
    <Card>
      <div className="mb-2">
        <h1 className="m-0 text-lg font-semibold">{t("title")}</h1>
        <p className="m-0 text-sm text-neutral-500">{t("subtitle")}</p>
      </div>
      <Tabs
        items={[
          { key: "hierarchy", label: <Space size={6}><TeamOutlined />{t("tabHierarchy")}</Space>, children: hierarchyTab },
          { key: "area", label: <Space size={6}><ApartmentOutlined />{t("tabArea")}</Space>, children: areaTab },
        ]}
      />
      <OrgDetailDrawer target={detail} onClose={() => setDetail(null)} />
    </Card>
  );
}
