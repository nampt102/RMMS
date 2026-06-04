import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/client";
import { fetchUsers } from "@/features/users/api";

vi.mock("@/lib/api/client", () => ({
  apiClient: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

const mockedGet = vi.mocked(apiClient.get);

describe("fetchUsers", () => {
  beforeEach(() => vi.clearAllMocks());

  it("unwraps the double `data` envelope into a PaginatedResponse", async () => {
    mockedGet.mockResolvedValueOnce({
      data: {
        data: {
          data: [
            { id: "u1", email: "a@b.c", fullName: "A", role: "leader", status: "active" },
          ],
          meta: { page: 1, pageSize: 20, total: 1, totalPages: 1 },
        },
      },
    });

    const res = await fetchUsers({ page: 1, pageSize: 20 });

    expect(res.meta.total).toBe(1);
    expect(res.data).toHaveLength(1);
    expect(res.data[0]?.email).toBe("a@b.c");
  });

  it("forwards filters and omits empty ones", async () => {
    mockedGet.mockResolvedValueOnce({
      data: { data: { data: [], meta: { page: 2, pageSize: 10, total: 0, totalPages: 0 } } },
    });

    await fetchUsers({ page: 2, pageSize: 10, role: "leader", status: "", search: "abc" });

    expect(mockedGet).toHaveBeenCalledWith("/admin/users", {
      params: { page: 2, pageSize: 10, role: "leader", status: undefined, search: "abc" },
    });
  });
});
