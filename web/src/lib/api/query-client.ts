import { QueryClient } from "@tanstack/react-query";

/**
 * Singleton TanStack Query client. Defaults tuned for an admin dashboard:
 *  - 1 min staleTime → reduces refetch storms
 *  - retry once for queries; no retry for mutations (idempotency handled per-call)
 */
export function makeQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        retry: 1,
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: 0,
      },
    },
  });
}
