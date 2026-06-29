import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { customerService } from "@/services/customerService";
import { userManagementService } from "@/services/userManagementService";
import { CustomerType, type CreateCustomerRequest, type Customer, type CustomerEdit } from "@/types";
import {
  buildLinkedUserDisplayName,
  mapCustomerToFormValue,
  type LinkUserContext,
} from "../utils/customerForm";

type UseCustomerUpsertModalArgs = {
  isOpen: boolean;
  customerId?: number;
  linkUserContext?: LinkUserContext | null;
  onClose: () => void;
  onSaved?: (customer: Customer) => void;
};

export function useCustomerUpsertModal({
  isOpen,
  customerId,
  linkUserContext,
  onClose,
  onSaved,
}: UseCustomerUpsertModalArgs) {
  const isEditMode = customerId != null;
  const [customer, setCustomer] = useState<CustomerEdit | null>(null);
  const [isLoadingCustomer, setIsLoadingCustomer] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const linkedUserDisplayName = useMemo(
    () => (linkUserContext ? buildLinkedUserDisplayName(linkUserContext) : null),
    [linkUserContext],
  );

  const initialValue = useMemo<CreateCustomerRequest>(() => {
    if (isEditMode && customer) {
      return mapCustomerToFormValue(customer);
    }

    return {
      customerType: CustomerType.Individual,
      firstName: linkUserContext?.linkUserFirstName?.trim() || undefined,
      lastName: linkUserContext?.linkUserLastName?.trim() || undefined,
    };
  }, [customer, isEditMode, linkUserContext]);

  const modalTitle = isEditMode ? "Редактирай клиент" : "Създай клиент";

  useEffect(() => {
    if (!isOpen || !isEditMode) {
      setCustomer(null);
      return;
    }

    const parsedCustomerId = Number(customerId);
    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Невалиден идентификатор на клиент");
      onClose();
      return;
    }

    let isCancelled = false;

    const loadCustomer = async () => {
      setIsLoadingCustomer(true);

      try {
        const customerDetails = await customerService.getCustomerForEdit(parsedCustomerId);
        if (!isCancelled) {
          setCustomer(customerDetails);
        }
      } catch (error) {
        if (!isCancelled) {
          toast.error(getCommonModelErrorMessage(error, "Клиентът не може да бъде зареден"));
          onClose();
        }
      } finally {
        if (!isCancelled) {
          setIsLoadingCustomer(false);
        }
      }
    };

    void loadCustomer();

    return () => {
      isCancelled = true;
    };
  }, [customerId, isEditMode, isOpen, onClose]);

  const submit = useCallback(
    async (payload: CreateCustomerRequest) => {
      setIsSubmitting(true);

      try {
        if (isEditMode) {
          if (!customer) {
            return;
          }

          const updatedCustomer = await customerService.updateCustomer(customer.id, payload);
          toast.success("Клиентът е обновен");
          onSaved?.(updatedCustomer);
          onClose();
          return;
        }

        const createdCustomer = linkUserContext
          ? await userManagementService.createCustomerForUser(linkUserContext.linkUserId, payload)
          : await customerService.createCustomer(payload);

        toast.success(linkUserContext ? "Клиентът е създаден и свързан с потребител" : "Клиентът е създаден");

        onSaved?.(createdCustomer);
        onClose();
      } catch (error) {
        toast.error(
          getCommonModelErrorMessage(error, isEditMode ? "Клиентът не може да бъде обновен" : "Клиентът не може да бъде създаден"),
        );
      } finally {
        setIsSubmitting(false);
      }
    },
    [customer, isEditMode, linkUserContext, onClose, onSaved],
  );

  const close = useCallback(() => {
    if (isSubmitting) {
      return;
    }

    onClose();
  }, [isSubmitting, onClose]);

  const state = useMemo(
    () => ({
      isEditMode,
      isLoadingCustomer,
      isSubmitting,
      linkedUserDisplayName,
      initialValue,
      modalTitle,
    }),
    [isEditMode, isLoadingCustomer, isSubmitting, linkedUserDisplayName, initialValue, modalTitle],
  );

  const actions = useMemo(() => ({ submit, close }), [submit, close]);

  return { state, actions };
}
