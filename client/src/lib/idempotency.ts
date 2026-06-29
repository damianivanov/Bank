// Идемпотентен ключ за операции с пари: генерира се веднъж за всеки опит и се праща пак при retry,
// така че повторно изпращане (двоен клик / повторна заявка след timeout) да не създаде второ движение.

export function newIdempotencyKey(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  // Резервен вариант за среди без crypto.randomUUID — достатъчно уникален за идемпотентен ключ.
  return `idem-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`;
}
