import { Wallet } from "lucide-react";
import { formatIban } from "@/lib/formatters";
import { CopyableValue } from "./CopyableValue";

type AccountIbanCardProps = {
  iban: string;
  className?: string;
};

/**
 * IBAN като идентичност на сметка — икона-плочка + копируема, форматирана
 * моноширинна стойност. Споделя се между диалозите за движения/депозит/теглене.
 */
export function AccountIbanCard({ iban, className }: AccountIbanCardProps) {
  return (
    <div
      className={`flex items-center gap-3.5 rounded-xl border border-black/10 bg-black/[0.02] p-3.5 dark:border-white/10 dark:bg-white/[0.03] ${className ?? ""}`.trim()}
    >
      <span className="bank-icon-tile-soft flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">
        <Wallet className="h-5 w-5" />
      </span>
      <div className="min-w-0">
        <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">IBAN</p>
        <CopyableValue value={iban} display={formatIban(iban)} label="IBAN" className="text-base sm:text-lg" />
      </div>
    </div>
  );
}
