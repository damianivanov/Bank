import { formatDateTime } from "@/lib/formatters";
import { DetailField, Modal } from "@/shared/components";
import type { ApiError } from "@/types";

type ErrorDetailsModalProps = {
  error: ApiError | null;
  onClose: () => void;
};

export default function ErrorDetailsModal({ error, onClose }: ErrorDetailsModalProps) {
  return (
    <Modal isOpen={error != null} onClose={onClose} title="Детайли за грешката" widthClassName="max-w-3xl">
      {error ? (
        <div className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-3">
            <DetailField label="ID" valueClassName="font-semibold">
              {error.id}
            </DetailField>
            <DetailField label="Дата" valueClassName="font-semibold">
              {formatDateTime(error.dateCreated)}
            </DetailField>
            <DetailField label="Потребител" valueClassName="font-semibold">
              {error.userName || "-"}
            </DetailField>
          </div>

          <DetailField label="Път" valueClassName="font-mono break-all">
            {error.path || "-"}
          </DetailField>

          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Съобщение</p>
            <p className="mt-1.5 text-sm break-words">{error.message}</p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-tertiary">Стек на грешката</p>
            <pre className="mt-1.5 max-h-96 overflow-auto rounded-xl border border-black/10 bg-black/[0.03] p-4 text-xs leading-relaxed break-words whitespace-pre-wrap dark:border-white/10 dark:bg-white/[0.04]">
              {error.details || "Няма допълнителни детайли."}
            </pre>
          </div>
        </div>
      ) : null}
    </Modal>
  );
}
