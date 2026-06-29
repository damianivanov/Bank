type ResultStatProps = {
  label: string;
  value: string;
  emphasize?: boolean;
};

export default function ResultStat({ label, value, emphasize = false }: ResultStatProps) {
  return (
    <div className="bank-panel rounded-2xl p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">{label}</p>
      <p className="mt-1 text-2xl font-bold" style={emphasize ? { color: "var(--accent)" } : undefined}>
        {value}
      </p>
    </div>
  );
}
