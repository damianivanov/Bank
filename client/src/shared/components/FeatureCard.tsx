import type { ReactNode } from "react";

type FeatureCardProps = {
  title: string;
  value: ReactNode;
  tone?: "default" | "success" | "info" | "warn" | "danger";
};

const toneClassName = {
  default: "bank-chip",
  success: "bank-chip-success",
  info: "bank-chip-info",
  warn: "bank-chip-warn",
  danger: "bank-chip-danger",
};

export default function FeatureCard({ title, value, tone = "default" }: FeatureCardProps) {
  return (
    <section className="bank-panel rounded-2xl p-5">
      <div className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${toneClassName[tone]}`}>
        {title}
      </div>
      <div className="mt-4 text-3xl font-bold tracking-tight">{value}</div>
    </section>
  );
}
