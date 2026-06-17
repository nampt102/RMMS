import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type { ApiResponse } from "@/types/api";

/** Mirrors `Rmms.Application.Organization.Views.*` DTOs (camelCase over the wire). */

export type OrgPerson = {
  id: string;
  fullName: string;
  email: string;
  role: string; // "pg" | "leader" | "buh"
  status: string; // "active" | "inactive" | "pending_email_verify"
};

export type OrgLeaderNode = {
  id: string;
  fullName: string;
  email: string;
  status: string;
  pgs: OrgPerson[];
};

export type OrgHierarchy = {
  buhs: OrgPerson[];
  leaders: OrgLeaderNode[];
  unassignedPgs: OrgPerson[];
};

export type OrgStoreEmployee = {
  id: string;
  fullName: string;
  role: string;
  status: string;
};

export type OrgStoreNode = {
  id: string;
  code: string;
  name: string;
  status: string; // "active" | "inactive"
  employees: OrgStoreEmployee[];
};

export type OrgAreaNode = {
  id: string;
  code: string;
  name: string;
  parentAreaId: string | null;
  stores: OrgStoreNode[];
};

export type OrgAreaTree = {
  areas: OrgAreaNode[];
  unassignedStores: OrgStoreNode[];
};

const ORG_KEY = ["admin", "org"] as const;

/** Rank hierarchy (BUH → Leader → PG) — GET /api/v1/admin/org/hierarchy. */
export function useOrgHierarchy() {
  return useQuery({
    queryKey: [...ORG_KEY, "hierarchy"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<OrgHierarchy>>("/admin/org/hierarchy");
      return data.data;
    },
  });
}

/** Area tree (Area → Store → assigned employees) — GET /api/v1/admin/org/area-tree. */
export function useOrgAreaTree() {
  return useQuery({
    queryKey: [...ORG_KEY, "area-tree"],
    queryFn: async () => {
      const { data } = await apiClient.get<ApiResponse<OrgAreaTree>>("/admin/org/area-tree");
      return data.data;
    },
  });
}
