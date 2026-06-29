type UserStatusBadgeProps = {
  isActive: boolean;
};

export function UserStatusBadge({ isActive }: UserStatusBadgeProps) {
  return (
    <span
      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
        isActive ? "bank-chip bank-chip-success" : "bank-chip bank-chip-warn"
      }`}
    >
      {isActive ? "Активен" : "Неактивен"}
    </span>
  );
}

type PersonDisplayProps = {
  personDisplayName?: string;
};

export function PersonDisplay({ personDisplayName }: PersonDisplayProps) {
  if (personDisplayName) {
    return <>{personDisplayName}</>;
  }

  return (
    <span className="bank-chip bank-chip-warn rounded-full px-2 py-0.5 text-xs font-semibold">Не е свързано</span>
  );
}
