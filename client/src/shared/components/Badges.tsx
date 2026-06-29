import {
  BankAccountStatus,
  CreditPaymentStatus,
  CreditStatus,
  CustomerType,
  DepositRequestStatus,
  MoneyTransactionType,
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
  return isVip ? <Chip className="bank-chip-success" label="VIP" /> : <Chip className="bank-chip" label="Стандартен" />;
}

export function CustomerTypeBadge({ customerType }: { customerType: CustomerType }) {
  const label = customerType === CustomerType.Individual ? "Физическо лице" : "Юридическо лице";
  const className = customerType === CustomerType.Individual ? "bank-chip-info" : "bank-chip-warn";
  return <Chip className={className} label={label} />;
}

export function AccountStatusBadge({ status }: { status: BankAccountStatus }) {
  return status === BankAccountStatus.Active
    ? <Chip className="bank-chip-success" label="Активна" />
    : <Chip className="bank-chip" label="Закрита" />;
}

export function CreditStatusBadge({ status }: { status: CreditStatus }) {
  return status === CreditStatus.Active
    ? <Chip className="bank-chip-info" label="Активен" />
    : <Chip className="bank-chip-success" label="Погасен" />;
}

export function PaymentStatusBadge({ status }: { status: CreditPaymentStatus }) {
  return status === CreditPaymentStatus.Pending
    ? <Chip className="bank-chip-warn" label="Предстои" />
    : <Chip className="bank-chip-success" label="Платена" />;
}

export function DepositStatusBadge({ status }: { status: DepositRequestStatus }) {
  if (status === DepositRequestStatus.Pending) {
    return <Chip className="bank-chip-warn" label="Чака одобрение" />;
  }
  if (status === DepositRequestStatus.Approved) {
    return <Chip className="bank-chip-success" label="Одобрена" />;
  }
  return <Chip className="bank-chip-danger" label="Отхвърлена" />;
}

export function MoneyTransactionTypeBadge({ type }: { type: MoneyTransactionType }) {
  if (type === MoneyTransactionType.Deposit) {
    return <Chip className="bank-chip-success" label="Депозит" />;
  }
  if (type === MoneyTransactionType.Withdrawal) {
    return <Chip className="bank-chip-warn" label="Теглене" />;
  }
  return <Chip className="bank-chip-info" label="Вноска по кредит" />;
}
