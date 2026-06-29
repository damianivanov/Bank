import { formatCurrency } from "@/lib/formatters";
import { CreditFeeKind, CreditTermsOrigin, CreditType, FeeType, PaymentType, PricingChangeReason } from "@/types";

export function formatCreditType(creditType: CreditType | number): string {
  return creditType === CreditType.Consumer ? "Потребителски" : "Ипотечен";
}

export const paymentTypeLabels: Record<PaymentType, string> = {
  [PaymentType.Annuity]: "Анюитетен",
  [PaymentType.Declining]: "Намаляващи вноски",
};

export const feeKindLabels: Record<CreditFeeKind, string> = {
  [CreditFeeKind.Application]: "Такса за кандидатстване",
  [CreditFeeKind.Processing]: "Такса за обработка",
  [CreditFeeKind.OtherInitial]: "Други първоначални такси",
  [CreditFeeKind.MonthlyManagement]: "Месечна такса за управление",
  [CreditFeeKind.OtherMonthly]: "Други месечни такси",
  [CreditFeeKind.AnnualManagement]: "Годишна такса за управление",
  [CreditFeeKind.OtherAnnual]: "Други годишни такси",
};

export function formatFeeValue(fee: { type: FeeType; value: number }): string {
  return fee.type === FeeType.Percent ? `${fee.value}%` : formatCurrency(fee.value);
}

// Произход на версия от условията — как е възникнала тази редакция в хронологията на кредита.
export const creditTermsOriginLabels: Record<CreditTermsOrigin, string> = {
  [CreditTermsOrigin.Origination]: "Първоначални условия",
  [CreditTermsOrigin.VipRepricing]: "Преоразмеряване (VIP)",
};

// Причина за ценова промяна, така както е записана в CreditPricingChange.
export const pricingChangeReasonLabels: Record<PricingChangeReason, string> = {
  [PricingChangeReason.VipStatusChanged]: "Промяна на VIP статус",
};
