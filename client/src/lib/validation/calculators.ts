import {
  calculatorLimits,
  optionalIntegerInRange,
  optionalNumberInRange,
  requiredIntegerInRange,
  requiredNumberInRange,
  type FieldErrors,
} from "./rules";

const {
  minTermMonths,
  maxTermMonths,
  minAmount,
  maxAmount,
  minRate,
  maxRate,
  minPercent,
  maxPercent,
} = calculatorLimits;

export type CreditCalculatorErrorKey =
  | "loanAmount"
  | "termInMonths"
  | "interestRate"
  | "promoPeriod"
  | "promoRate"
  | "gracePeriod";

export type LeasingCalculatorErrorKey = "priceWithVAT" | "downPayment" | "leasingTerm" | "monthlyPayment";

export type RefinancingCalculatorErrorKey =
  | "principal"
  | "currentRate"
  | "currentTerm"
  | "paymentsMade"
  | "prepaymentFee"
  | "newRate"
  | "originationFeePercent"
  | "originationFeeFixed";

export type CreditCalculatorValues = {
  loanAmount: string;
  termInMonths: string;
  interestRate: string;
  promoPeriod: string;
  promoRate: string;
  gracePeriod: string;
};

export type LeasingCalculatorValues = {
  priceWithVAT: string;
  downPayment: string;
  leasingTerm: string;
  monthlyPayment: string;
};

export type RefinancingCalculatorValues = {
  principal: string;
  currentRate: string;
  currentTerm: string;
  paymentsMade: string;
  prepaymentFee: string;
  newRate: string;
  originationFeePercent: string;
  originationFeeFixed: string;
};

export function validateCreditCalculator(
  values: CreditCalculatorValues,
): FieldErrors<CreditCalculatorErrorKey> {
  return {
    loanAmount: requiredNumberInRange(values.loanAmount, minAmount, maxAmount, "Размер на кредита"),
    termInMonths: requiredIntegerInRange(values.termInMonths, minTermMonths, maxTermMonths, "Срок (месеци)"),
    interestRate: requiredNumberInRange(values.interestRate, minRate, maxRate, "Годишен лихвен процент"),
    promoPeriod: optionalIntegerInRange(values.promoPeriod, 0, maxTermMonths, "Промоционален период"),
    promoRate: optionalNumberInRange(values.promoRate, minRate, maxRate, "Промоционална лихва"),
    gracePeriod: optionalIntegerInRange(values.gracePeriod, 0, maxTermMonths, "Гратисен период"),
  };
}

export function validateLeasingCalculator(
  values: LeasingCalculatorValues,
): FieldErrors<LeasingCalculatorErrorKey> {
  const errors: FieldErrors<LeasingCalculatorErrorKey> = {
    priceWithVAT: requiredNumberInRange(values.priceWithVAT, minAmount, maxAmount, "Цена на актива"),
    downPayment: requiredNumberInRange(values.downPayment, 0, maxAmount, "Първоначална вноска"),
    leasingTerm: requiredIntegerInRange(values.leasingTerm, minTermMonths, maxTermMonths, "Срок на лизинг"),
    monthlyPayment: requiredNumberInRange(values.monthlyPayment, minAmount, maxAmount, "Месечна вноска"),
  };

  // Между полета: първоначалната вноска трябва да е строго под цената на актива.
  if (!errors.downPayment && !errors.priceWithVAT) {
    if (Number(values.downPayment) >= Number(values.priceWithVAT)) {
      errors.downPayment = "Първоначалната вноска трябва да е по-малка от цената на актива.";
    }
  }

  return errors;
}

export function validateRefinancingCalculator(
  values: RefinancingCalculatorValues,
): FieldErrors<RefinancingCalculatorErrorKey> {
  const errors: FieldErrors<RefinancingCalculatorErrorKey> = {
    principal: requiredNumberInRange(values.principal, minAmount, maxAmount, "Първоначална главница"),
    currentRate: requiredNumberInRange(values.currentRate, minRate, maxRate, "Текуща годишна лихва"),
    currentTerm: requiredIntegerInRange(values.currentTerm, minTermMonths, maxTermMonths, "Срок (месеци)"),
    paymentsMade: requiredIntegerInRange(values.paymentsMade, 0, maxTermMonths, "Платени вноски"),
    prepaymentFee: requiredNumberInRange(values.prepaymentFee, minPercent, maxPercent, "Такса за предсрочно погасяване"),
    newRate: requiredNumberInRange(values.newRate, minRate, maxRate, "Нова годишна лихва"),
    originationFeePercent: requiredNumberInRange(
      values.originationFeePercent,
      minPercent,
      maxPercent,
      "Такса за отпускане (%)",
    ),
    originationFeeFixed: requiredNumberInRange(values.originationFeeFixed, 0, maxAmount, "Такса за отпускане (фиксирана)"),
  };

  // Между полета: платените вноски не могат да надвишават срока на кредита.
  if (!errors.paymentsMade && !errors.currentTerm) {
    if (Number(values.paymentsMade) > Number(values.currentTerm)) {
      errors.paymentsMade = "Платените вноски не могат да надвишават срока.";
    }
  }

  return errors;
}
