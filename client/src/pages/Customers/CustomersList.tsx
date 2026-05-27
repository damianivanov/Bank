import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { getCommonModelErrorMessage } from "@/lib/commonModel";
import { customerService } from "@/services/customerService";
import { CustomerTypeBadge, EntityGrid, VipBadge } from "@/shared/components";
import type { Customer } from "@/types";
import CustomerUpsertModal from "./CustomerUpsertModal";

export default function CustomersList() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCustomerModalOpen, setIsCustomerModalOpen] = useState(false);
  const [editingCustomerId, setEditingCustomerId] = useState<number | null>(null);

  const isEditMode = editingCustomerId !== null;

  const loadCustomers = async () => {
    setIsLoading(true);

    try {
      const customersData = await customerService.getCustomers();
      setCustomers(customersData);
    } catch (error) {
      toast.error(getCommonModelErrorMessage(error, "Could not load customers"));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadCustomers();
  }, []);

  const handleCustomerModalClose = () => {
    setIsCustomerModalOpen(false);
    setEditingCustomerId(null);
  };

  const handleCustomerSaved = () => {
    void loadCustomers();
  };

  const handleOpenNewCustomerModal = () => {
    setEditingCustomerId(null);
    setIsCustomerModalOpen(true);
  };

  const handleOpenEditCustomerModal = (customerId: number) => {
    setEditingCustomerId(customerId);
    setIsCustomerModalOpen(true);
  };

  const createOpenEditCustomerHandler = (customerId: number) => () => {
    handleOpenEditCustomerModal(customerId);
  };

  const renderContent = () => {
    if (isLoading) {
      return <p className="text-sm text-secondary">Loading customers...</p>;
    }

    if (customers.length === 0) {
      return <p className="text-sm text-secondary">No customers yet.</p>;
    }

    return (
      <EntityGrid>
        <thead>
          <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
            <th className="px-4 py-3">Name</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">VIP</th>
            <th className="px-4 py-3">Identifier</th>
            <th className="px-4 py-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          {customers.map((customer) => (
            <tr key={customer.id} className="border-b border-slate-100 text-sm last:border-b-0">
              <td className="px-4 py-3 font-semibold">{customer.displayName}</td>
              <td className="px-4 py-3">
                <CustomerTypeBadge customerType={customer.customerType} />
              </td>
              <td className="px-4 py-3">
                <VipBadge isVip={customer.isVip} />
              </td>
              <td className="px-4 py-3 text-secondary">{customer.identifier}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  <Link to={`/customers/${customer.id}`} className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold">
                    View
                  </Link>
                  <button
                    type="button"
                    onClick={createOpenEditCustomerHandler(customer.id)}
                    className="bank-secondary-btn rounded-lg px-3 py-1.5 text-xs font-semibold"
                  >
                    Edit
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </EntityGrid>
    );
  };

  return (
    <section className="w-full px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Customers</h1>
          <p className="mt-1 text-sm text-secondary">Manage individual and company clients, including VIP category.</p>
        </div>
        <button
          type="button"
          onClick={handleOpenNewCustomerModal}
          className="bank-primary-btn rounded-xl px-4 py-2 text-sm font-semibold"
        >
          New customer
        </button>
      </div>

      <div className="mt-6">{renderContent()}</div>

      <CustomerUpsertModal
        isOpen={isCustomerModalOpen}
        mode={isEditMode ? "edit" : "create"}
        customerId={editingCustomerId ?? undefined}
        onClose={handleCustomerModalClose}
        onSaved={handleCustomerSaved}
      />
    </section>
  );
}

