import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { customerService } from "@/services/customerService";
import { userManagementService } from "@/services/userManagementService";
import { Modal } from "@/shared/components";
import { CustomerType, type CreateCustomerRequest, type CustomerDetails } from "@/types";
import CustomerForm from "./CustomerForm";

export type LinkUserContext = {
  linkUserId: number;
  linkUserEmail?: string;
  linkUserFirstName?: string;
  linkUserLastName?: string;
};

type CustomerUpsertModalProps = {
  isOpen: boolean;
  mode: "create" | "edit";
  customerId?: number;
  linkUserContext?: LinkUserContext | null;
  onClose: () => void;
  onSaved?: (customer: CustomerDetails) => void;
};

function mapCustomerToFormValue(customer: CustomerDetails): CreateCustomerRequest {
  return {
    customerType: customer.customerType,
    firstName: customer.firstName,
    lastName: customer.lastName,
    personalIdentifier: customer.personalIdentifier,
    companyName: customer.companyName,
    companyIdentifier: customer.companyIdentifier,
    representativeName: customer.representativeName,
  };
}

function buildLinkedUserDisplayName(linkUserContext: LinkUserContext): string {
  const fullName = [linkUserContext.linkUserFirstName, linkUserContext.linkUserLastName]
    .filter(Boolean)
    .join(" ")
    .trim();

  if (fullName) {
    return linkUserContext.linkUserEmail ? `${fullName} (${linkUserContext.linkUserEmail})` : fullName;
  }

  return linkUserContext.linkUserEmail ?? `User #${linkUserContext.linkUserId}`;
}

export default function CustomerUpsertModal({
  isOpen,
  mode,
  customerId,
  linkUserContext,
  onClose,
  onSaved,
}: CustomerUpsertModalProps) {
  const isEditMode = mode === "edit";
  const [customer, setCustomer] = useState<CustomerDetails | null>(null);
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

  const modalTitle = isEditMode ? "Edit customer" : "Create customer";

  useEffect(() => {
    if (!isOpen) {
      setCustomer(null);
      return;
    }

    if (!isEditMode) {
      setCustomer(null);
      return;
    }

    const parsedCustomerId = Number(customerId);
    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Invalid customer id");
      onClose();
      return;
    }

    let isCancelled = false;

    const loadCustomer = async () => {
      setIsLoadingCustomer(true);

      try {
        const customerDetails = await customerService.getCustomer(parsedCustomerId);
        if (!isCancelled) {
          setCustomer(customerDetails);
        }
      } catch (error) {
        if (!isCancelled) {
          toast.error(getCommonModelErrorMessage(error, "Could not load customer"));
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

  const handleSubmit = async (payload: CreateCustomerRequest) => {
    setIsSubmitting(true);

    try {
      if (isEditMode) {
        if (!customer) {
          return;
        }

        const updatedCustomer = await customerService.updateCustomer(customer.id, payload);
        toast.success("Customer updated");
        onSaved?.(updatedCustomer);
        onClose();
        return;
      }

      const createdCustomer = linkUserContext
        ? await userManagementService.createCustomerForUser(linkUserContext.linkUserId, payload)
        : await customerService.createCustomer(payload);

      toast.success(linkUserContext ? "Customer created and connected to user" : "Customer created");

      onSaved?.(createdCustomer);
      onClose();
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, isEditMode ? "Could not update customer" : "Could not create customer"));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (isSubmitting) {
      return;
    }

    onClose();
  };

  return (
    <Modal title={modalTitle} isOpen={isOpen} onClose={handleClose}>
      {linkedUserDisplayName ? (
        <p className="mb-2 text-sm text-secondary">
          This customer will be connected to <span className="font-semibold text-foreground">{linkedUserDisplayName}</span>.
        </p>
      ) : null}

      {isEditMode && isLoadingCustomer ? (
        <p className="text-sm text-secondary">Loading customer...</p>
      ) : (
        <CustomerForm
          key={`${mode}-${customerId ?? "new"}-${linkUserContext?.linkUserId ?? "none"}`}
          initialValue={initialValue}
          submitLabel={isEditMode ? "Save changes" : "Create customer"}
          isSubmitting={isSubmitting}
          showPanel={false}
          onSubmit={handleSubmit}
        />
      )}
    </Modal>
  );
}
