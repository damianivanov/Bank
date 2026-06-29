import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { formatCurrency } from "@/lib/formatters";
import { toFee, type FeeInput } from "@/lib/feeInput";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateCreditCalculator, type CreditCalculatorErrorKey } from "@/lib/validation/calculators";
import { calculatorService } from "@/services/calculatorService";
import { creditConditionService } from "@/services/creditConditionService";
import { creditService } from "@/services/creditService";
import { customerService } from "@/services/customerService";
import { reportCalculatorError } from "@/pages/Calculators/calculatorError";
import {
  CreditType,
  FeeType,
  PaymentType,
  type CreateCreditRequest,
  type CreditCalculatorRequest,
  type CreditCalculatorResponse,
  type CreditTypeCondition,
  type CustomerLookup,
} from "@/types";

type UseCreditIssuanceModalArgs = {
  isOpen: boolean;
  presetCustomerId?: number;
  presetCustomerIsVip?: boolean;
  onClose: () => void;
  onCreated?: (creditId: number) => void;
};

function feeFromAmount(amount: number): FeeInput | undefined {
  return amount > 0 ? { type: FeeType.Currency, value: String(amount) } : undefined;
}

export function useCreditIssuanceModal({
  isOpen,
  presetCustomerId,
  presetCustomerIsVip,
  onClose,
  onCreated,
}: UseCreditIssuanceModalArgs) {
  const hasPreset = presetCustomerId !== undefined;

  const [conditions, setConditions] = useState<CreditTypeCondition[]>([]);
  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [isCustomerLoading, setIsCustomerLoading] = useState(false);
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 250);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerLookup | null>(null);
  const [creditType, setCreditType] = useState(CreditType.Consumer);

  const [loanAmount, setLoanAmount] = useState("");
  const [termInMonths, setTermInMonths] = useState("");
  const [interestRate, setInterestRate] = useState("");
  const [paymentType, setPaymentType] = useState(PaymentType.Annuity);
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
  const [result, setResult] = useState<CreditCalculatorResponse | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const condition = useMemo(
    () => conditions.find((candidate) => candidate.creditType === creditType) ?? null,
    [conditions, creditType],
  );
  const isVip = selectedCustomer?.isVip ?? presetCustomerIsVip ?? false;

  // Запазваме избрания клиент като обект (а не само Id), за да оцелее VIP статусът дори когато
  // резултатите от typeahead търсенето се сменят и клиентът вече не е в текущия списък.
  const selectCustomer = useCallback(
    (nextCustomerId: string) => {
      setCustomerId(nextCustomerId);
      setSelectedCustomer(customers.find((candidate) => String(candidate.id) === nextCustomerId) ?? null);
    },
    [customers],
  );

  // Предзарежда ценовите полета от продукта (std/VIP вариант); сумата и срокът остават за служителя.
  const applyDefaults = useCallback((source: CreditTypeCondition, vip: boolean) => {
    setInterestRate(String(vip ? source.vipAnnualInterestRate : source.standardAnnualInterestRate));
    setPaymentType(source.defaultPaymentType);
    setPromoPeriod(source.promoPeriodMonths ? String(source.promoPeriodMonths) : "");
    const promo = vip ? source.vipPromoRate : source.standardPromoRate;
    setPromoRate(promo ? String(promo) : "");
    setGracePeriod(source.gracePeriodMonths ? String(source.gracePeriodMonths) : "");
    setProcessingFee(feeFromAmount(vip ? source.vipGrantingFee : source.standardGrantingFee));
    setMonthlyManagementFee(feeFromAmount(vip ? source.vipMonthlyManagementFee : source.standardMonthlyManagementFee));
    setAnnualManagementFee(feeFromAmount(vip ? source.vipAnnualManagementFee : source.standardAnnualManagementFee));
    setApplicationFee(undefined);
    setOtherInitialFees(undefined);
    setOtherAnnualFees(undefined);
    setOtherMonthlyFees(undefined);
    setErrors({});
  }, []);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setCustomerId(hasPreset ? String(presetCustomerId) : "");
    setSelectedCustomer(null);
    setCustomerSearch("");
    setCreditType(CreditType.Consumer);
    setLoanAmount("");
    setTermInMonths("");
    setResult(null);
    setErrors({});

    async function loadConditions() {
      try {
        const loaded = await creditConditionService.getCreditConditions();
        setConditions(loaded.filter((existing) => existing.isActive));
      } catch {
        // Тихо: ако условията не се заредят, селекторът остава празен.
      }
    }

    void loadConditions();
  }, [isOpen, hasPreset, presetCustomerId]);

  // Сървърно подаван typeahead за клиента (с дебоунс) — зарежда само съвпаденията, не целия списък.
  useEffect(() => {
    if (!isOpen || hasPreset) {
      return;
    }

    let isCancelled = false;
    setIsCustomerLoading(true);

    async function loadCustomers() {
      try {
        const data = await customerService.getCustomerLookup(debouncedCustomerSearch);
        if (!isCancelled) {
          setCustomers(data);
        }
      } catch {
        if (!isCancelled) {
          setCustomers([]);
        }
      } finally {
        if (!isCancelled) {
          setIsCustomerLoading(false);
        }
      }
    }

    void loadCustomers();

    return () => {
      isCancelled = true;
    };
  }, [isOpen, hasPreset, debouncedCustomerSearch]);

  // Презарежда defaults при смяна на продукт или на VIP статуса на избрания клиент.
  useEffect(() => {
    if (!isOpen || !condition) {
      return;
    }

    applyDefaults(condition, isVip);
  }, [isOpen, condition, isVip, applyDefaults]);

  const buildCalcRequest = useCallback((): CreditCalculatorRequest | null => {
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
    const payload = buildCalcRequest();
    if (!payload) {
      return;
    }

    setIsCalculating(true);
    try {
      setResult(await calculatorService.calculateCredit(payload));
    } catch (error) {
      reportCalculatorError(error, "Изчислението на кредита не бе успешно");
    } finally {
      setIsCalculating(false);
    }
  }, [buildCalcRequest]);

  const submit = useCallback(async () => {
    const calc = buildCalcRequest();
    if (!calc) {
      return;
    }

    const parsedCustomerId = Number(customerId);
    if (!parsedCustomerId) {
      toast.error("Изберете клиент");
      return;
    }

    if (condition) {
      if (calc.loanAmount > condition.maximumAmount) {
        toast.error(`Сумата надвишава максимума (${formatCurrency(condition.maximumAmount)})`);
        return;
      }
      if (calc.termInMonths > condition.maximumTermMonths) {
        toast.error(`Срокът надвишава максимума (${condition.maximumTermMonths} месеца)`);
        return;
      }
    }

    const payload: CreateCreditRequest = {
      customerId: parsedCustomerId,
      creditType,
      grantedAmount: calc.loanAmount,
      termMonths: calc.termInMonths,
      interestRate: calc.interestRate,
      paymentType: calc.paymentType,
      promoPeriod: calc.promoPeriod,
      promoRate: calc.promoRate,
      gracePeriod: calc.gracePeriod,
      applicationFee: calc.applicationFee,
      processingFee: calc.processingFee,
      otherInitialFees: calc.otherInitialFees,
      annualManagementFee: calc.annualManagementFee,
      otherAnnualFees: calc.otherAnnualFees,
      monthlyManagementFee: calc.monthlyManagementFee,
      otherMonthlyFees: calc.otherMonthlyFees,
    };

    setIsSubmitting(true);
    try {
      const created = await creditService.createCredit(payload);
      toast.success("Кредитът е отпуснат");
      onCreated?.(created.id);
      onClose();
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Кредитът не можа да бъде отпуснат"));
    } finally {
      setIsSubmitting(false);
    }
  }, [buildCalcRequest, condition, creditType, customerId, onClose, onCreated]);

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }

    onClose();
  }, [isSubmitting, onClose]);

  const state = {
    hasPreset,
    conditions,
    customers,
    customerId,
    customerSearch,
    isCustomerLoading,
    creditType,
    condition,
    fields: {
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
    },
    errors,
    result,
    isCalculating,
    isSubmitting,
  };

  const actions = {
    setCustomerId,
    setCustomerSearch,
    selectCustomer,
    setCreditType,
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
    submit,
    close,
  };

  return { state, actions };
}
