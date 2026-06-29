import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { isStaffOrAdmin } from "@/lib/access";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { customerService } from "@/services/customerService";
import { useUserStore } from "@/stores/userStore";
import { type CustomerDetails } from "@/types";
import { getCustomerDisplayName } from "../utils/customerForm";

export function useCustomerDetailsPage() {
  const { customerId } = useParams();
  const navigate = useNavigate();
  const user = useUserStore((state) => state.user);
  const canManageVip = isStaffOrAdmin(user);
  const parsedCustomerId = Number(customerId);

  const [customer, setCustomer] = useState<CustomerDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUpdatingVip, setIsUpdatingVip] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const loadCustomer = useCallback(async () => {
    if (!Number.isFinite(parsedCustomerId) || parsedCustomerId <= 0) {
      toast.error("Невалиден идентификатор на клиент");
      navigate("/customers", { replace: true });
      return;
    }

    setIsLoading(true);

    try {
      const customerDetails = await customerService.getCustomer(parsedCustomerId);
      setCustomer(customerDetails);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Клиентът не може да бъде зареден"));
      navigate("/customers", { replace: true });
    } finally {
      setIsLoading(false);
    }
  }, [navigate, parsedCustomerId]);

  useEffect(() => {
    void loadCustomer();
  }, [loadCustomer]);

  const toggleVip = useCallback(async () => {
    if (!customer || !canManageVip) {
      return;
    }

    setIsUpdatingVip(true);
    try {
      const updatedCustomer = await customerService.updateCustomerVip(customer.id, { isVip: !customer.isVip });
      setCustomer(updatedCustomer);
      toast.success("VIP статусът е обновен");
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "VIP статусът не може да бъде обновен"));
    } finally {
      setIsUpdatingVip(false);
    }
  }, [canManageVip, customer]);

  const openEditModal = useCallback(() => setIsEditModalOpen(true), []);
  const closeEditModal = useCallback(() => setIsEditModalOpen(false), []);
  const handleCustomerSaved = useCallback(() => {
    void loadCustomer();
  }, [loadCustomer]);

  const customerName = useMemo(() => (customer ? getCustomerDisplayName(customer) : ""), [customer]);

  const state = useMemo(
    () => ({ customer, isLoading, isUpdatingVip, isEditModalOpen, canManageVip, customerName }),
    [customer, isLoading, isUpdatingVip, isEditModalOpen, canManageVip, customerName],
  );

  const actions = useMemo(
    () => ({ toggleVip, openEditModal, closeEditModal, handleCustomerSaved }),
    [toggleVip, openEditModal, closeEditModal, handleCustomerSaved],
  );

  return { state, actions };
}
