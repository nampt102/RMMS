/** Mirrors `Rmms.Application.Documents.DocumentDto` (camelCase over the wire). */

export type DocumentFolderType = "public" | "private";

export type DocumentItem = {
  id: string;
  name: string;
  description: string | null;
  folderType: DocumentFolderType;
  fileSizeBytes: number;
  mimeType: string;
  createdAt: string;
};

export type AssignDocumentPayload = {
  role?: string;
  userId?: string;
};
