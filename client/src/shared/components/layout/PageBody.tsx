import type { ReactNode } from "react";

type PageBodyProps = {
  children: ReactNode;
  className?: string;
};

export function PageBody({ children, className }: PageBodyProps) {
  return (
    <section className={`w-full px-4 py-6 md:px-8${className ? ` ${className}` : ""}`}>
      {children}
    </section>
  );
}
