/** Mirrors `Rmms.Application.Organization.*` DTOs (camelCase over the wire). */

export type Store = {
  id: string;
  code: string;
  name: string;
  address: string | null;
  latitude: number;
  longitude: number;
  areaId: string | null;
  areaName: string | null;
  status: string; // "active" | "inactive"
  createdAt: string;
  updatedAt: string | null;
};

export type Area = {
  id: string;
  code: string;
  name: string;
  parentAreaId: string | null;
  parentAreaName: string | null;
  createdAt: string;
};

export type Category = {
  id: string;
  code: string;
  name: string;
  createdAt: string;
};

export type ListStoresParams = {
  page: number;
  pageSize: number;
  areaId?: string;
  status?: string;
  search?: string;
};

export type CreateStorePayload = {
  code: string;
  name: string;
  address?: string;
  latitude: number;
  longitude: number;
  areaId?: string;
};

export type UpdateStorePayload = {
  name: string;
  address?: string;
  latitude: number;
  longitude: number;
  areaId?: string;
};
