import { useTranslations } from "next-intl";

export default function HomePage() {
  const t = useTranslations("app");
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-3 p-8">
      <h1 className="text-3xl font-semibold">{t("name")}</h1>
      <p className="text-base text-neutral-600">{t("tagline")}</p>
      <p className="text-sm text-neutral-400">
        Scaffold ok. Replace this page in Sprint 01 (Admin dashboard shell).
      </p>
    </main>
  );
}
