import { CreditType } from "@/types";

export function formatCreditType(creditType: CreditType | number): string {
  return creditType === CreditType.Consumer ? "Потребителски" : "Ипотечен";
}
