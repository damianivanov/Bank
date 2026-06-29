import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { hasErrors, type FieldErrors } from "@/lib/validation/rules";
import { validateAccountCreate, type AccountCreateFields } from "@/lib/validation/forms";
import { accountService } from "@/services/accountService";
import { customerService } from "@/services/customerService";
import type { CreateBankAccountRequest, CustomerLookup } from "@/types";

type UseAccountCreateModalArgs = {
  isOpen: boolean;
  presetCustomerId?: number;
  onClose: () => void;
  onCreated?: (accountId: number) => void;
};

export function useAccountCreateModal({ isOpen, presetCustomerId, onClose, onCreated }: UseAccountCreateModalArgs) {
  const hasPreset = presetCustomerId !== undefined;

  const [customers, setCustomers] = useState<CustomerLookup[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [isCustomerLoading, setIsCustomerLoading] = useState(false);
  const debouncedCustomerSearch = useDebouncedValue(customerSearch, 250);
  const [openingBalance, setOpeningBalance] = useState("0");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<FieldErrors<AccountCreateFields>>({});

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setOpeningBalance("0");
    setErrors({});
    setCustomerId(hasPreset ? String(presetCustomerId) : "");
    setCustomerSearch("");
  }, [isOpen, hasPreset, presetCustomerId]);

  // Сървърно подаван typeahead: при отворен модал зареждаме клиентите според търсенето (с дебоунс),
  // вместо да дърпаме целия списък наведнъж.
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

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }

    onClose();
  }, [isSubmitting, onClose]);

  const submit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      const parsedCustomerId = Number(customerId);
      const validationErrors = validateAccountCreate({ customerId: parsedCustomerId, openingBalance });
      setErrors(validationErrors);
      if (hasErrors(validationErrors)) {
        return;
      }

      const payload: CreateBankAccountRequest = {
        customerId: parsedCustomerId,
        openingBalance: Number(openingBalance),
      };

      setIsSubmitting(true);
      try {
        const createdAccount = await accountService.createAccount(payload);
        toast.success("Сметката е открита");
        onCreated?.(createdAccount.id);
        onClose();
      } catch (error) {
        toast.error(getCommonModelErrorMessage(error, "Сметката не може да бъде създадена"));
      } finally {
        setIsSubmitting(false);
      }
    },
    [customerId, onClose, onCreated, openingBalance],
  );

  const state = useMemo(
    () => ({ customers, customerId, customerSearch, isCustomerLoading, hasPreset, openingBalance, isSubmitting, errors }),
    [customers, customerId, customerSearch, isCustomerLoading, hasPreset, openingBalance, isSubmitting, errors],
  );
  const actions = useMemo(
    () => ({ setCustomerId, setCustomerSearch, setOpeningBalance, submit, close }),
    [submit, close],
  );

  return { state, actions };
}
