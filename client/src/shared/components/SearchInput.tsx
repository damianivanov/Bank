import { useRef, type ChangeEvent, type InputHTMLAttributes } from "react";
import { LuSearch, LuX } from "react-icons/lu";

type SearchInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  containerClassName?: string;
};

export default function SearchInput({
  containerClassName = "max-w-sm",
  className = "",
  placeholder,
  "aria-label": ariaLabel,
  value,
  onChange,
  ...props
}: SearchInputProps) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const hasValue = value !== undefined && value !== null && String(value).length > 0;

  const handleClear = () => {
    const input = inputRef.current;
    if (input) {
      // Изпразваме стойността преди да известим родителя, за да носи синтетичното събитие „".
      input.value = "";
      onChange?.({ target: input, currentTarget: input } as ChangeEvent<HTMLInputElement>);
      input.focus();
    }
  };

  return (
    <div className={`relative ${containerClassName}`.trim()}>
      <LuSearch className="pointer-events-none absolute left-3 top-1/2 z-10 h-4 w-4 -translate-y-1/2 text-[color:var(--text-primary)]" />
      <input
        ref={inputRef}
        type="text"
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        aria-label={ariaLabel ?? (typeof placeholder === "string" ? placeholder : undefined)}
        className={`bank-input w-full py-2.5 pl-9 ${hasValue ? "pr-9" : "pr-3"} text-sm ${className}`.trim()}
        {...props}
      />
      {hasValue ? (
        <button
          type="button"
          onClick={handleClear}
          aria-label="Изчисти търсенето"
          className="absolute right-2 top-1/2 z-10 flex -translate-y-1/2 cursor-pointer items-center justify-center rounded-full p-1 text-rose-500 transition-colors hover:bg-rose-500/10 hover:text-rose-600"
        >
          <LuX className="h-4 w-4" />
        </button>
      ) : null}
    </div>
  );
}
