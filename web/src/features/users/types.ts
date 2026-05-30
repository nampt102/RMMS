import type { UserRole } from "@/types/api";

/** Mirrors `Rmms.Application.Admin.Users.AdminUserDto` (camelCase over the wire). */
export type AdminUser = {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  role: UserRole;
  status: string;
  preferredLanguage: string;
  emailVerifiedAt: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateUserPayload = {
  email: string;
  fullName: string;
  phone?: string;
  role: "leader" | "buh" | "admin";
  preferredLanguage: "vi" | "en";
};

export type UpdateUserPayload = {
  fullName?: string;
  phone?: string;
  status?: "active" | "inactive";
  preferredLanguage?: "vi" | "en";
};

export type ListUsersParams = {
  page: number;
  pageSize: number;
  role?: string;
  status?: string;
  search?: string;
};
