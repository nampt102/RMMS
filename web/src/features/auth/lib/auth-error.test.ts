import { AxiosError } from "axios";
import { describe, expect, it } from "vitest";
import { errorCodeFromUnknown } from "./auth-error";

describe("errorCodeFromUnknown", () => {
  it("extracts the backend error code from the response envelope", () => {
    const error = new AxiosError("bad", "ERR_BAD_REQUEST", undefined, undefined, {
      data: { error: { code: "INVALID_CREDENTIALS" } },
      status: 401,
      statusText: "Unauthorized",
      headers: {},
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      config: {} as any,
    });

    expect(errorCodeFromUnknown(error)).toBe("INVALID_CREDENTIALS");
  });

  it("returns NETWORK_ERROR when there is no response", () => {
    const error = new AxiosError("offline", "ERR_NETWORK");
    expect(errorCodeFromUnknown(error)).toBe("NETWORK_ERROR");
  });

  it("returns INTERNAL_ERROR when the response has no error code", () => {
    const error = new AxiosError("weird", "ERR_BAD_RESPONSE", undefined, undefined, {
      data: {},
      status: 500,
      statusText: "Server Error",
      headers: {},
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      config: {} as any,
    });
    expect(errorCodeFromUnknown(error)).toBe("INTERNAL_ERROR");
  });

  it("returns INTERNAL_ERROR for a non-axios error", () => {
    expect(errorCodeFromUnknown(new Error("boom"))).toBe("INTERNAL_ERROR");
  });
});
