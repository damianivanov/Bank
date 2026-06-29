import { useState } from "react";
import { Check, Pencil, RefreshCw, Save, X } from "lucide-react";
import { Modal, TextInputField } from "@/shared/components";
import { requiredText } from "@/lib/validation/rules";
import type { SaveCalculationRequest } from "@/types";

type SaveCalculationButtonProps = {
  // Сглобява payload-а от ТЕКУЩИТЕ полета при отваряне (валидира; null при грешка), а не от последно
  // изчисления request — така Запази/Обнови взима актуалните стойности без да се натиска първо Изчисли.
  buildPayload: () => Omit<SaveCalculationRequest, "name"> | null;
  isSaving: boolean;
  onSave: (payload: SaveCalculationRequest) => Promise<boolean>;
  // Когато се редактира заредено изчисление: id-то му и текущото име. Иначе null/празно -> режим "ново".
  editingId?: number | null;
  editingName?: string;
  onUpdate?: (id: number, payload: SaveCalculationRequest) => Promise<boolean>;
  // Известяват родителя, за да поддържа edit контекста синхронен след действие.
  onUpdated?: (name: string) => void;
  onSavedAsNew?: () => void;
};

export default function SaveCalculationButton({
  buildPayload,
  isSaving,
  onSave,
  editingId = null,
  editingName = "",
  onUpdate,
  onUpdated,
  onSavedAsNew,
}: SaveCalculationButtonProps) {
  const isEditing = editingId != null && onUpdate != null;

  const [isOpen, setIsOpen] = useState(false);
  const [name, setName] = useState(editingName);
  const [error, setError] = useState<string>();
  // Payload-ът, заснет в момента на отваряне на диалога (валидни текущи полета).
  const [pendingPayload, setPendingPayload] = useState<Omit<SaveCalculationRequest, "name"> | null>(null);

  const open = () => {
    // Валидираме текущите полета още тук: при невалидни вход не отваряме диалога — грешките се показват върху формата.
    const payload = buildPayload();
    if (!payload) {
      return;
    }
    setPendingPayload(payload);
    setName(editingName);
    setError(undefined);
    setIsOpen(true);
  };

  const close = () => {
    setIsOpen(false);
    setName(editingName);
    setError(undefined);
  };

  const isNameValid = () => {
    const nameError = requiredText(name, "Име");
    if (nameError) {
      setError(nameError);
      return false;
    }
    return true;
  };

  // Създава нов запис (POST). В режим на редактиране това е "Запази като ново" — отделя копие.
  const saveAsNew = async () => {
    if (!isNameValid() || !pendingPayload) {
      return;
    }
    const ok = await onSave({ ...pendingPayload, name: name.trim() });
    if (ok) {
      onSavedAsNew?.();
      setIsOpen(false);
      setError(undefined);
    }
  };

  // Презаписва текущия запис (PUT).
  const update = async () => {
    if (!isNameValid() || editingId == null || !onUpdate || !pendingPayload) {
      return;
    }
    const ok = await onUpdate(editingId, { ...pendingPayload, name: name.trim() });
    if (ok) {
      onUpdated?.(name.trim());
      setIsOpen(false);
      setError(undefined);
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={open}
        className="bank-secondary-btn inline-flex items-center gap-2 bank-btn"
      >
        {isEditing ? <Pencil className="h-4 w-4" /> : <Save className="h-4 w-4" />}
        {isEditing ? "Обнови изчислението" : "Запази изчислението"}
      </button>

      <Modal
        title={isEditing ? "Обновяване на изчислението" : "Запазване на изчислението"}
        isOpen={isOpen}
        onClose={close}
      >
        <div className="space-y-4">
          <TextInputField
            label="Име"
            placeholder="напр. Ипотека 25 г. при 3.5%"
            value={name}
            error={error}
            onChange={(event) => {
              setName(event.target.value);
              if (error) {
                setError(undefined);
              }
            }}
          />
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={close}
              className="bank-secondary-btn bank-btn"
            >
              <X className="h-4 w-4" />
              Отказ
            </button>
            {isEditing ? (
              <>
                <button
                  type="button"
                  onClick={saveAsNew}
                  disabled={isSaving}
                  className="bank-secondary-btn bank-btn disabled:opacity-60"
                >
                  <Check className="h-4 w-4" />
                  Запази като ново
                </button>
                <button
                  type="button"
                  onClick={update}
                  disabled={isSaving}
                  className="bank-primary-btn inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2 text-sm font-semibold disabled:opacity-60"
                >
                  <RefreshCw className="h-4 w-4" />
                  {isSaving ? "Запазване..." : "Обнови"}
                </button>
              </>
            ) : (
              <button
                type="button"
                onClick={saveAsNew}
                disabled={isSaving}
                className="bank-primary-btn inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2 text-sm font-semibold disabled:opacity-60"
              >
                <Check className="h-4 w-4" />
                {isSaving ? "Запазване..." : "Запази"}
              </button>
            )}
          </div>
        </div>
      </Modal>
    </>
  );
}
