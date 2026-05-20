import type { ButtonHTMLAttributes, ReactNode } from "react";

type OutlinedButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode;
};

export default function OutlinedButton({ children, className = "", ...props }: OutlinedButtonProps) {
  return (
    <button
      type="button"
      className={`bank-secondary-btn inline-flex items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}
