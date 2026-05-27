import type { ReactNode } from "react";

type EntityGridProps = {
  children: ReactNode;
};

export default function EntityGrid({ children }: EntityGridProps) {
  return (
    <div className="bank-panel overflow-hidden rounded-2xl">
      <div className="overflow-x-auto">
        <table className="w-full min-w-180 border-collapse text-left text-sm">{children}</table>
      </div>
    </div>
  );
}
