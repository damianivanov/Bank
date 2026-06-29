import { useCallback, useMemo, useState } from "react";
import { toFee, type FeeInput } from "@/lib/feeInput";
import { calculatorService } from "@/services/calculatorService";
import { reportCalculatorError } from "../calculatorError";
import {
  validateLeasingCalculator,
  type LeasingCalculatorErrorKey,
} from "@/lib/validation/calculators";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import type { Fee, LeasingCalculatorRequest, LeasingCalculatorResponse } from "@/types";

function feeToInput(fee: Fee | undefined): FeeInput | undefined {
  return fee ? { type: fee.type, value: String(fee.value) } : undefined;
}

function numberToInput(value: number | undefined): string {
  return value === undefined || value === null ? "" : String(value);
}

export function useLeasingCalculator() {
  const [priceWithVAT, setPriceWithVAT] = useState("");
  const [downPayment, setDownPayment] = useState("");
  const [leasingTerm, setLeasingTerm] = useState("");
  const [monthlyPayment, setMonthlyPayment] = useState("");
  const [processingFee, setProcessingFee] = useState<FeeInput>();

  const [errors, setErrors] = useState<FieldErrors<LeasingCalculatorErrorKey>>({});
  const [isCalculating, setIsCalculating] = useState(false);
  const [result, setResult] = useState<LeasingCalculatorResponse | null>(null);
  const [request, setRequest] = useState<LeasingCalculatorRequest | null>(null);

  // Сглобява request от текущите стойности (валидира; връща null при грешка). Споделя се между Изчисли и
  // Запази/Обнови, за да взима актуалните полета без да е нужно първо да се натиска Изчисли.
  const buildRequest = useCallback((): LeasingCalculatorRequest | null => {
    const nextErrors = validateLeasingCalculator({
      priceWithVAT,
      downPayment,
      leasingTerm,
      monthlyPayment,
    });
    setErrors(nextErrors);
    if (hasErrors(nextErrors)) {
      return null;
    }

    return {
      priceWithVAT: Number(priceWithVAT),
      downPayment: Number(downPayment),
      leasingTerm: Number(leasingTerm),
      monthlyPayment: Number(monthlyPayment),
      processingFee: toFee(processingFee),
    };
  }, [downPayment, leasingTerm, monthlyPayment, priceWithVAT, processingFee]);

  const calculate = useCallback(async () => {
    const payload = buildRequest();
    if (!payload) {
      return;
    }

    setIsCalculating(true);
    try {
      const response = await calculatorService.calculateLeasing(payload);
      setResult(response);
      setRequest(payload);
    } catch (error) {
      reportCalculatorError(error, "Изчислението на лизинга не бе успешно");
    } finally {
      setIsCalculating(false);
    }
  }, [buildRequest]);

  const hydrate = useCallback((inputs: LeasingCalculatorRequest, response: LeasingCalculatorResponse) => {
    setPriceWithVAT(numberToInput(inputs.priceWithVAT));
    setDownPayment(numberToInput(inputs.downPayment));
    setLeasingTerm(numberToInput(inputs.leasingTerm));
    setMonthlyPayment(numberToInput(inputs.monthlyPayment));
    setProcessingFee(feeToInput(inputs.processingFee));
    setErrors({});
    setRequest(inputs);
    setResult(response);
  }, []);

  const state = useMemo(
    () => ({ priceWithVAT, downPayment, leasingTerm, monthlyPayment, processingFee, errors, isCalculating, result, request }),
    [priceWithVAT, downPayment, leasingTerm, monthlyPayment, processingFee, errors, isCalculating, result, request],
  );

  const actions = useMemo(
    () => ({
      setPriceWithVAT,
      setDownPayment,
      setLeasingTerm,
      setMonthlyPayment,
      setProcessingFee,
      calculate,
      buildRequest,
      hydrate,
    }),
    [calculate, buildRequest, hydrate],
  );

  return { state, actions };
}
