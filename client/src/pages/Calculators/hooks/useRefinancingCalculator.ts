import { useCallback, useMemo, useState } from "react";
import { calculatorService } from "@/services/calculatorService";
import { reportCalculatorError } from "../calculatorError";
import {
  validateRefinancingCalculator,
  type RefinancingCalculatorErrorKey,
} from "@/lib/validation/calculators";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import type { RefinancingCalculatorRequest, RefinancingCalculatorResponse } from "@/types";

function numberToInput(value: number | undefined): string {
  return value === undefined || value === null ? "" : String(value);
}

export function useRefinancingCalculator() {
  const [principal, setPrincipal] = useState("");
  const [currentRate, setCurrentRate] = useState("");
  const [currentTerm, setCurrentTerm] = useState("");
  const [paymentsMade, setPaymentsMade] = useState("");
  const [prepaymentFee, setPrepaymentFee] = useState("");

  const [newRate, setNewRate] = useState("");
  const [originationFeePercent, setOriginationFeePercent] = useState("");
  const [originationFeeFixed, setOriginationFeeFixed] = useState("");

  const [errors, setErrors] = useState<FieldErrors<RefinancingCalculatorErrorKey>>({});
  const [isCalculating, setIsCalculating] = useState(false);
  const [result, setResult] = useState<RefinancingCalculatorResponse | null>(null);
  const [request, setRequest] = useState<RefinancingCalculatorRequest | null>(null);

  // Сглобява request от текущите стойности (валидира; връща null при грешка). Споделя се между Изчисли и
  // Запази/Обнови, за да взима актуалните полета без да е нужно първо да се натиска Изчисли.
  const buildRequest = useCallback((): RefinancingCalculatorRequest | null => {
    const nextErrors = validateRefinancingCalculator({
      principal,
      currentRate,
      currentTerm,
      paymentsMade,
      prepaymentFee,
      newRate,
      originationFeePercent,
      originationFeeFixed,
    });
    setErrors(nextErrors);
    if (hasErrors(nextErrors)) {
      return null;
    }

    return {
      currentLoan: {
        principal: Number(principal),
        annualRatePercent: Number(currentRate),
        termMonths: Number(currentTerm),
        paymentsMade: Number(paymentsMade),
        prepaymentFeePercent: Number(prepaymentFee),
      },
      newLoan: {
        annualRatePercent: Number(newRate),
        originationFeePercent: Number(originationFeePercent),
        originationFeeFixed: Number(originationFeeFixed),
      },
    };
  }, [
    currentRate,
    currentTerm,
    newRate,
    originationFeeFixed,
    originationFeePercent,
    paymentsMade,
    prepaymentFee,
    principal,
  ]);

  const calculate = useCallback(async () => {
    const payload = buildRequest();
    if (!payload) {
      return;
    }

    setIsCalculating(true);
    try {
      const response = await calculatorService.calculateRefinancing(payload);
      setResult(response);
      setRequest(payload);
    } catch (error) {
      reportCalculatorError(error, "Изчислението на рефинансирането не бе успешно");
    } finally {
      setIsCalculating(false);
    }
  }, [buildRequest]);

  const hydrate = useCallback((inputs: RefinancingCalculatorRequest, response: RefinancingCalculatorResponse) => {
    setPrincipal(numberToInput(inputs.currentLoan.principal));
    setCurrentRate(numberToInput(inputs.currentLoan.annualRatePercent));
    setCurrentTerm(numberToInput(inputs.currentLoan.termMonths));
    setPaymentsMade(numberToInput(inputs.currentLoan.paymentsMade));
    setPrepaymentFee(numberToInput(inputs.currentLoan.prepaymentFeePercent));
    setNewRate(numberToInput(inputs.newLoan.annualRatePercent));
    setOriginationFeePercent(numberToInput(inputs.newLoan.originationFeePercent));
    setOriginationFeeFixed(numberToInput(inputs.newLoan.originationFeeFixed));
    setErrors({});
    setRequest(inputs);
    setResult(response);
  }, []);

  const monthlyDelta = result ? result.current.monthlyPayment - result.new.monthlyPayment : 0;

  const state = useMemo(
    () => ({
      principal,
      currentRate,
      currentTerm,
      paymentsMade,
      prepaymentFee,
      newRate,
      originationFeePercent,
      originationFeeFixed,
      errors,
      isCalculating,
      result,
      request,
      monthlyDelta,
    }),
    [
      principal,
      currentRate,
      currentTerm,
      paymentsMade,
      prepaymentFee,
      newRate,
      originationFeePercent,
      originationFeeFixed,
      errors,
      isCalculating,
      result,
      request,
      monthlyDelta,
    ],
  );

  const actions = useMemo(
    () => ({
      setPrincipal,
      setCurrentRate,
      setCurrentTerm,
      setPaymentsMade,
      setPrepaymentFee,
      setNewRate,
      setOriginationFeePercent,
      setOriginationFeeFixed,
      calculate,
      buildRequest,
      hydrate,
    }),
    [calculate, buildRequest, hydrate],
  );

  return { state, actions };
}
