import axios from "axios";
import type { ApiError } from "@/types/api";

/**
 * Map a thrown request error to a backend error code that doubles as an `errors.*`
 * i18n key. Network failures (no response) collapse to `NETWORK_ERROR`; anything
 * unexpected falls back to `INTERNAL_ERROR`.
 */
export function errorCodeFromUnknown(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) {
      return "NETWORK_ERROR";
    }
    const data = error.response.data as ApiError | undefined;
    return data?.error?.code ?? "INTERNAL_ERROR";
  }
  return "INTERNAL_ERROR";
}
