import { Building2, Pencil, User } from "lucide-react";
import { Link } from "react-router-dom";
import { CustomerTypeBadge, EntityGrid, VipBadge } from "@/shared/components";
import { type Customer, CustomerType } from "@/types";

type CustomersTableProps = {
  customers: Customer[];
  onEdit: (customerId: number) => void;
};

export default function CustomersTable({ customers, onEdit }: CustomersTableProps) {
  return (
    <EntityGrid>
      <thead>
        <tr className="border-b border-slate-200 text-xs uppercase tracking-wide text-tertiary">
          <th className="px-4 py-3">Име</th>
          <th className="px-4 py-3">Вид</th>
          <th className="px-4 py-3">VIP</th>
          <th className="px-4 py-3">Идентификатор</th>
          <th className="w-px whitespace-nowrap px-4 py-3 text-right">Действия</th>
        </tr>
      </thead>
      <tbody>
        {customers.map((customer) => (
          <tr key={customer.id} className="border-b border-slate-100 text-sm last:border-b-0">
            <td className="px-4 py-3 font-semibold">
              <Link
                to={`/customers/${customer.id}`}
                className="inline-flex items-center gap-2 hover:underline!"
              >
                {customer.customerType === CustomerType.Company ? (
                  <Building2 className="h-4 w-4 text-tertiary" />
                ) : (
                  <User className="h-4 w-4 text-tertiary" />
                )}
                {customer.displayName}
              </Link>
            </td>
            <td className="px-4 py-3">
              <CustomerTypeBadge customerType={customer.customerType} />
            </td>
            <td className="px-4 py-3">
              <VipBadge isVip={customer.isVip} />
            </td>
            <td className="px-4 py-3 text-secondary">{customer.identifier}</td>
            <td className="w-px whitespace-nowrap px-4 py-3">
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => onEdit(customer.id)}
                  className="bank-secondary-btn bank-btn-action"
                >
                  <Pencil className="h-3.5 w-3.5" />
                  Редактирай
                </button>
              </div>
            </td>
          </tr>
        ))}
      </tbody>
    </EntityGrid>
  );
}
