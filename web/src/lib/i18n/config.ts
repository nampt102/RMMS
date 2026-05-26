/**
 * i18n configuration for next-intl.
 * Locales per project rule: Vietnamese (default) + English. Never hardcode user-visible strings.
 */
export const locales = ["vi", "en"] as const;
export type Locale = (typeof locales)[number];

export const defaultLocale: Locale = "vi";

export function isLocale(value: string): value is Locale {
  return (locales as readonly string[]).includes(value);
}
