import { getRequestConfig } from "next-intl/server";
import { notFound } from "next/navigation";
import { defaultLocale, isLocale } from "./config";

/**
 * Loads messages per request. Required by next-intl plugin in next.config.mjs.
 */
export default getRequestConfig(async ({ locale }) => {
  const resolved = locale ?? defaultLocale;
  if (!isLocale(resolved)) notFound();

  return {
    locale: resolved,
    messages: (await import(`../../../messages/${resolved}.json`)).default,
    timeZone: "Asia/Ho_Chi_Minh",
    now: new Date(),
  };
});
