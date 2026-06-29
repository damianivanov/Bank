import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { calculatorLimits } from "@/lib/validation/rules";
import { creditConditionService } from "@/services/creditConditionService";
import type { CreditTypeCondition, UpdateCreditConditionRequest } from "@/types";

type UseCreditConditionEditModalArgs = {
  condition: CreditTypeCondition;
  onSaved: () => void;
  onClose: () => void;
};

type FieldKey =
  | "standardAnnualInterestRate"
  | "vipAnnualInterestRate"
  | "maximumAmount"
  | "maximumTermMonths"
  | "standardGrantingFee"
  | "vipGrantingFee";

type FormErrors = Partial<Record<FieldKey, string>>;

function parseNumber(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") {
    return null;
  }

  const value = Number(trimmed);
  return Number.isFinite(value) ? value : null;
}

// Огледало на сървърната валидация (CreditConditionService.Validate) за незабавна обратна връзка;
// сървърът остава меродавен и връща точното съобщение при разминаване.
function validate(fields: Record<FieldKey, string>): FormErrors {
  const errors: FormErrors = {};

  const standardRate = parseNumber(fields.standardAnnualInterestRate);
  if (standardRate === null || standardRate < 0 || standardRate > 100) {
    errors.standardAnnualInterestRate = "Лихвата трябва да е между 0 и 100.";
  }

  const vipRate = parseNumber(fields.vipAnnualInterestRate);
  if (vipRate === null || vipRate < 0 || vipRate > 100) {
    errors.vipAnnualInterestRate = "Лихвата трябва да е между 0 и 100.";
  }

  const maximumAmount = parseNumber(fields.maximumAmount);
  if (maximumAmount === null || maximumAmount <= 0) {
    errors.maximumAmount = "Сумата трябва да е положителна.";
  }

  const maximumTermMonths = parseNumber(fields.maximumTermMonths);
  if (
    maximumTermMonths === null ||
    !Number.isInteger(maximumTermMonths) ||
    maximumTermMonths < 1 ||
    maximumTermMonths > calculatorLimits.maxTermMonths
  ) {
    errors.maximumTermMonths = `Срокът трябва да е цяло число между 1 и ${calculatorLimits.maxTermMonths}.`;
  }

  const standardFee = parseNumber(fields.standardGrantingFee);
  if (standardFee === null || standardFee < 0) {
    errors.standardGrantingFee = "Таксата не може да е отрицателна.";
  }

  const vipFee = parseNumber(fields.vipGrantingFee);
  if (vipFee === null || vipFee < 0) {
    errors.vipGrantingFee = "Таксата не може да е отрицателна.";
  }

  // VIP е преференциална тарифа — не бива да е по-неизгодна от стандартната.
  if (!errors.standardAnnualInterestRate && !errors.vipAnnualInterestRate
    && standardRate !== null && vipRate !== null && vipRate > standardRate) {
    errors.vipAnnualInterestRate = "VIP лихвата не може да е по-висока от стандартната.";
  }

  if (!errors.standardGrantingFee && !errors.vipGrantingFee
    && standardFee !== null && vipFee !== null && vipFee > standardFee) {
    errors.vipGrantingFee = "VIP таксата не може да е по-висока от стандартната.";
  }

  return errors;
}

export function useCreditConditionEditModal({ condition, onSaved, onClose }: UseCreditConditionEditModalArgs) {
  const [fields, setFields] = useState<Record<FieldKey, string>>({
    standardAnnualInterestRate: String(condition.standardAnnualInterestRate),
    vipAnnualInterestRate: String(condition.vipAnnualInterestRate),
    maximumAmount: String(condition.maximumAmount),
    maximumTermMonths: String(condition.maximumTermMonths),
    standardGrantingFee: String(condition.standardGrantingFee),
    vipGrantingFee: String(condition.vipGrantingFee),
  });
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setField = useCallback((key: FieldKey, raw: string) => {
    setFields((current) => ({ ...current, [key]: raw }));
    setErrors((current) => (current[key] ? { ...current, [key]: undefined } : current));
  }, []);

  const submit = useCallback(async () => {
    const validationErrors = validate(fields);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    const payload: UpdateCreditConditionRequest = {
      standardAnnualInterestRate: Number(fields.standardAnnualInterestRate),
      vipAnnualInterestRate: Number(fields.vipAnnualInterestRate),
      maximumAmount: Number(fields.maximumAmount),
      maximumTermMonths: Number(fields.maximumTermMonths),
      standardGrantingFee: Number(fields.standardGrantingFee),
      vipGrantingFee: Number(fields.vipGrantingFee),
    };

    setIsSubmitting(true);
    try {
      await creditConditionService.updateCreditCondition(condition.id, payload);
      toast.success("Кредитните условия са обновени");
      onSaved();
      onClose();
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Кредитните условия не можаха да бъдат обновени"));
    } finally {
      setIsSubmitting(false);
    }
  }, [fields, condition.id, onSaved, onClose]);

  const state = useMemo(() => ({ fields, errors, isSubmitting }), [fields, errors, isSubmitting]);
  const actions = useMemo(() => ({ setField, submit, close: onClose }), [setField, submit, onClose]);

  return { state, actions };
}
