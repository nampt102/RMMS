import { setRequestLocale } from "next-intl/server";
import { useTranslations } from "next-intl";

export default function HomePage({ params: { locale } }: { params: { locale: string } }) {
  setRequestLocale(locale);

  const t = useTranslations("app");
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-3 p-8">
      <h1 className="text-3xl font-semibold">{t("name")}</h1>
      <p className="text-base text-neutral-600">{t("tagline")}</p>
    </main>
  );
}
