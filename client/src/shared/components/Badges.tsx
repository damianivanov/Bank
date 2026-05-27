import {
  BankAccountStatus,
  CreditPaymentStatus,
  CreditStatus,
  CustomerType,
} from "@/types";

function Chip({
  className,
  label,
}: {
  className: string;
  label: string;
}) {
  return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${className}`}>{label}</span>;
}

export function VipBadge({ isVip }: { isVip: boolean }) {
  return isVip ? <Chip className="bank-chip-success" label="VIP" /> : <Chip className="bank-chip" label="Standard" />;
}

export function CustomerTypeBadge({ customerType }: { customerType: CustomerType }) {
  const label = customerType === CustomerType.Individual ? "Individual" : "Company";
  const className = customerType === CustomerType.Individual ? "bank-chip-info" : "bank-chip-warn";
  return <Chip className={className} label={label} />;
}

export function AccountStatusBadge({ status }: { status: BankAccountStatus }) {
  return status === BankAccountStatus.Active
    ? <Chip className="bank-chip-success" label="Active" />
    : <Chip className="bank-chip" label="Closed" />;
}

export function CreditStatusBadge({ status }: { status: CreditStatus }) {
  return status === CreditStatus.Active
    ? <Chip className="bank-chip-info" label="Active" />
    : <Chip className="bank-chip-success" label="Repaid" />;
}

export function PaymentStatusBadge({ status }: { status: CreditPaymentStatus }) {
  return status === CreditPaymentStatus.Pending
    ? <Chip className="bank-chip-warn" label="Pending" />
    : <Chip className="bank-chip-success" label="Paid" />;
}
