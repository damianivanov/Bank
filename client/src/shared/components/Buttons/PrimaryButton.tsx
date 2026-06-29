import type { ButtonHTMLAttributes, ReactNode } from "react";

type PrimaryButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode;
};

export default function PrimaryButton({ children, className = "", ...props }: PrimaryButtonProps) {
  return (
    <button
      type="button"
      className={`bank-primary-btn inline-flex items-center justify-center gap-2 bank-btn transition ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}
