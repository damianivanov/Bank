type FormNoticeProps = {
  message?: string;
};

/** Form-level success/info banner, e.g. confirming a completed action like registration. */
export default function FormNotice({ message }: FormNoticeProps) {
  if (!message) {
    return null;
  }

  return (
    <p
      role="status"
      className="mt-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm font-medium text-emerald-600"
    >
      {message}
    </p>
  );
}
