/** Mirrors `Rmms.Application.News.AdminNewsDto` (camelCase over the wire). */

export type AdminNewsItem = {
  id: string;
  titleVi: string;
  titleEn: string;
  contentVi: string;
  contentEn: string;
  category: string | null;
  isImportant: boolean;
  isPublished: boolean;
  publishedAt: string | null;
  createdAt: string;
};

export type NewsPayload = {
  titleVi: string;
  titleEn: string;
  contentVi: string;
  contentEn: string;
  category?: string;
  isImportant: boolean;
};

export type AssignNewsPayload = {
  role?: string;
  userId?: string;
};
