import { useCallback, useEffect, useMemo, useState } from "react";
import { toFee, type FeeInput } from "@/lib/feeInput";
import { calculatorService } from "@/services/calculatorService";
import { creditConditionService } from "@/services/creditConditionService";
import { reportCalculatorError } from "../calculatorError";
import {
  validateCreditCalculator,
  type CreditCalculatorErrorKey,
} from "@/lib/validation/calculators";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import {
  FeeType,
  PaymentType,
  type CreditCalculatorRequest,
  type CreditCalculatorResponse,
  type Fee,
  type PublicCreditCondition,
} from "@/types";

function feeToInput(fee: Fee | undefined): FeeInput | undefined {
  return fee ? { type: fee.type, value: String(fee.value) } : undefined;
}

function feeFromAmount(amount: number): FeeInput | undefined {
  return amount > 0 ? { type: FeeType.Currency, value: String(amount) } : undefined;
}

function numberToInput(value: number | undefined): string {
  return value === undefined || value === null ? "" : String(value);
}

export function useCreditCalculator(isAuthenticated: boolean) {
  const [loanAmount, setLoanAmount] = useState("");
  const [termInMonths, setTermInMonths] = useState("");
  const [interestRate, setInterestRate] = useState("");
  const [paymentType, setPaymentType] = useState(PaymentType.Annuity);

  // Записаните кредитни продукти; при избор предзареждаме всички стандартни стойности от условията.
  const [products, setProducts] = useState<PublicCreditCondition[]>([]);
  const [selectedProductId, setSelectedProductId] = useState("");

  const [promoPeriod, setPromoPeriod] = useState("");
  const [promoRate, setPromoRate] = useState("");
  const [gracePeriod, setGracePeriod] = useState("");

  const [applicationFee, setApplicationFee] = useState<FeeInput>();
  const [processingFee, setProcessingFee] = useState<FeeInput>();
  const [otherInitialFees, setOtherInitialFees] = useState<FeeInput>();
  const [annualManagementFee, setAnnualManagementFee] = useState<FeeInput>();
  const [otherAnnualFees, setOtherAnnualFees] = useState<FeeInput>();
  const [monthlyManagementFee, setMonthlyManagementFee] = useState<FeeInput>();
  const [otherMonthlyFees, setOtherMonthlyFees] = useState<FeeInput>();

  const [errors, setErrors] = useState<FieldErrors<CreditCalculatorErrorKey>>({});
  const [isCalculating, setIsCalculating] = useState(false);
  const [result, setResult] = useState<CreditCalculatorResponse | null>(null);
  const [request, setRequest] = useState<CreditCalculatorRequest | null>(null);

  // Сглобява request от текущите стойности на формата (валидира; връща null при грешка). Споделя се между
  // Изчисли и Запази/Обнови, за да взима актуалните полета без да е нужно първо да се натиска Изчисли.
  const buildRequest = useCallback((): CreditCalculatorRequest | null => {
    const nextErrors = validateCreditCalculator({
      loanAmount,
      termInMonths,
      interestRate,
      promoPeriod,
      promoRate,
      gracePeriod,
    });
    setErrors(nextErrors);
    if (hasErrors(nextErrors)) {
      return null;
    }

    return {
      loanAmount: Number(loanAmount),
      termInMonths: Number(termInMonths),
      interestRate: Number(interestRate),
      paymentType,
      promoPeriod: promoPeriod ? Number(promoPeriod) : undefined,
      promoRate: promoRate ? Number(promoRate) : undefined,
      gracePeriod: gracePeriod ? Number(gracePeriod) : undefined,
      applicationFee: toFee(applicationFee),
      processingFee: toFee(processingFee),
      otherInitialFees: toFee(otherInitialFees),
      annualManagementFee: toFee(annualManagementFee),
      otherAnnualFees: toFee(otherAnnualFees),
      monthlyManagementFee: toFee(monthlyManagementFee),
      otherMonthlyFees: toFee(otherMonthlyFees),
    };
  }, [
    annualManagementFee,
    applicationFee,
    gracePeriod,
    interestRate,
    loanAmount,
    monthlyManagementFee,
    otherAnnualFees,
    otherInitialFees,
    otherMonthlyFees,
    paymentType,
    processingFee,
    promoPeriod,
    promoRate,
    termInMonths,
  ]);

  const calculate = useCallback(async () => {
    const payload = buildRequest();
    if (!payload) {
      return;
    }

    setIsCalculating(true);
    try {
      const response = await calculatorService.calculateCredit(payload);
      setResult(response);
      setRequest(payload);
    } catch (error) {
      reportCalculatorError(error, "Изчислението на кредита не бе успешно");
    } finally {
      setIsCalculating(false);
    }
  }, [buildRequest]);

  // Селекторът на продукти е само за вписани потребители. На публичните страници не зареждаме
  // условия и не предзареждаме нищо — формата остава празна за ръчно въвеждане.
  useEffect(() => {
    if (!isAuthenticated) {
      setProducts([]);
      setSelectedProductId("");
      return;
    }

    let active = true;

    async function loadProducts() {
      try {
        const conditions = await creditConditionService.getPublicCreditConditions();
        if (active) {
          setProducts(conditions);
        }
      } catch {
        // Тихо: при неуспех просто оставяме селектора празен.
      }
    }

    void loadProducts();

    return () => {
      active = false;
    };
  }, [isAuthenticated]);

  // Изборът на продукт предзарежда всички стандартни стойности от условията — сума, срок, лихва,
  // погасителен план, промоционален/гратисен период и стандартните такси (VIP условията остават за служители).
  const selectProduct = useCallback(
    (productId: string) => {
      setSelectedProductId(productId);
      if (productId === "") {
        return;
      }

      const product = products.find((candidate) => String(candidate.id) === productId);
      if (!product) {
        return;
      }

      setLoanAmount(String(product.maximumAmount));
      setTermInMonths(String(product.maximumTermMonths));
      setInterestRate(String(product.standardAnnualInterestRate));
      setPaymentType(product.defaultPaymentType);
      setPromoPeriod(product.promoPeriodMonths ? String(product.promoPeriodMonths) : "");
      setPromoRate(product.standardPromoRate ? String(product.standardPromoRate) : "");
      setGracePeriod(product.gracePeriodMonths ? String(product.gracePeriodMonths) : "");
      setProcessingFee(feeFromAmount(product.standardGrantingFee));
      setMonthlyManagementFee(feeFromAmount(product.standardMonthlyManagementFee));
      setAnnualManagementFee(feeFromAmount(product.standardAnnualManagementFee));
      setApplicationFee(undefined);
      setOtherInitialFees(undefined);
      setOtherAnnualFees(undefined);
      setOtherMonthlyFees(undefined);
      setErrors({});
    },
    [products],
  );

  const hydrate = useCallback((inputs: CreditCalculatorRequest, response: CreditCalculatorResponse) => {
    setSelectedProductId("");
    setLoanAmount(numberToInput(inputs.loanAmount));
    setTermInMonths(numberToInput(inputs.termInMonths));
    setInterestRate(numberToInput(inputs.interestRate));
    setPaymentType(inputs.paymentType ?? PaymentType.Annuity);
    setPromoPeriod(numberToInput(inputs.promoPeriod));
    setPromoRate(numberToInput(inputs.promoRate));
    setGracePeriod(numberToInput(inputs.gracePeriod));
    setApplicationFee(feeToInput(inputs.applicationFee));
    setProcessingFee(feeToInput(inputs.processingFee));
    setOtherInitialFees(feeToInput(inputs.otherInitialFees));
    setAnnualManagementFee(feeToInput(inputs.annualManagementFee));
    setOtherAnnualFees(feeToInput(inputs.otherAnnualFees));
    setMonthlyManagementFee(feeToInput(inputs.monthlyManagementFee));
    setOtherMonthlyFees(feeToInput(inputs.otherMonthlyFees));
    setErrors({});
    setRequest(inputs);
    setResult(response);
  }, []);

  const state = useMemo(
    () => ({
      products,
      selectedProductId,
      loanAmount,
      termInMonths,
      interestRate,
      paymentType,
      promoPeriod,
      promoRate,
      gracePeriod,
      applicationFee,
      processingFee,
      otherInitialFees,
      annualManagementFee,
      otherAnnualFees,
      monthlyManagementFee,
      otherMonthlyFees,
      errors,
      isCalculating,
      result,
      request,
    }),
    [
      products,
      selectedProductId,
      loanAmount,
      termInMonths,
      interestRate,
      paymentType,
      promoPeriod,
      promoRate,
      gracePeriod,
      applicationFee,
      processingFee,
      otherInitialFees,
      annualManagementFee,
      otherAnnualFees,
      monthlyManagementFee,
      otherMonthlyFees,
      errors,
      isCalculating,
      result,
      request,
    ],
  );

  const actions = useMemo(
    () => ({
      selectProduct,
      setLoanAmount,
      setTermInMonths,
      setInterestRate,
      setPaymentType,
      setPromoPeriod,
      setPromoRate,
      setGracePeriod,
      setApplicationFee,
      setProcessingFee,
      setOtherInitialFees,
      setAnnualManagementFee,
      setOtherAnnualFees,
      setMonthlyManagementFee,
      setOtherMonthlyFees,
      calculate,
      buildRequest,
      hydrate,
    }),
    [selectProduct, calculate, buildRequest, hydrate],
  );

  return { state, actions };
}
