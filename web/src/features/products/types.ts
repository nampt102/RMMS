/** Mirrors `Rmms.Application.Organization.Products.ProductDto` (camelCase over the wire). */

export type Product = {
  id: string;
  sku: string;
  name: string;
  brand: string | null;
  categoryId: string | null;
  categoryName: string | null;
  attributes: string | null; // raw JSON string (jsonb)
  status: string; // "active" | "inactive"
  createdAt: string;
};

export type ListProductsParams = {
  page: number;
  pageSize: number;
  categoryId?: string;
  search?: string;
};

export type CreateProductPayload = {
  sku: string;
  name: string;
  brand?: string;
  categoryId?: string;
  attributes?: string;
};

export type UpdateProductPayload = {
  name: string;
  brand?: string;
  categoryId?: string;
  attributes?: string;
};
