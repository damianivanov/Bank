import type { ReactNode } from "react";

type AsyncSectionProps = {
  isLoading: boolean;
  error?: string | null;
  onRetry?: () => void;
  isEmpty?: boolean;
  loadingLabel?: string;
  emptyLabel?: string;
  emptyState?: ReactNode;
  children: ReactNode;
};

export function AsyncSection({
  isLoading,
  error,
  onRetry,
  isEmpty = false,
  loadingLabel = "Зареждане...",
  emptyLabel,
  emptyState,
  children,
}: AsyncSectionProps) {
  if (isLoading) {
    return <p className="text-sm text-secondary">{loadingLabel}</p>;
  }

  if (error) {
    return (
      <div className="bank-panel rounded-2xl px-5 py-8 text-center">
        <p className="text-sm font-semibold text-rose-500">{error}</p>
        {onRetry ? (
          <button
            type="button"
            onClick={onRetry}
            className="bank-secondary-btn mt-4 inline-flex items-center gap-2 bank-btn"
          >
            Опитай отново
          </button>
        ) : null}
      </div>
    );
  }

  if (isEmpty) {
    if (emptyState != null) {
      return <>{emptyState}</>;
    }

    if (emptyLabel) {
      return <p className="text-sm text-secondary">{emptyLabel}</p>;
    }

    return null;
  }

  return <>{children}</>;
}
