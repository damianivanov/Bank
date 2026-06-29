type FormErrorProps = {
  message?: string;
};

/** Form-level error banner for submit failures that are not tied to a single field. */
export default function FormError({ message }: FormErrorProps) {
  if (!message) {
    return null;
  }

  return (
    <p
      role="alert"
      className="mt-4 rounded-xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm font-medium text-rose-500"
    >
      {message}
    </p>
  );
}
