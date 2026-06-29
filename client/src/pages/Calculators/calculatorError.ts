import { toast } from "sonner";
import { getCommonModelErrorMessage, isRateLimitError } from "@/lib/commonModel";
import { useRateLimitStore } from "@/stores/rateLimitStore";

// Единна обработка на грешки от калкулаторите: изчерпан часов лимит (429, само за гости) отваря модал
// с покана за вход; всичко останало остава като toast.
export function reportCalculatorError(error: unknown, fallbackMessage: string): void {
  if (isRateLimitError(error)) {
    useRateLimitStore.getState().open();
    return;
  }

  toast.error(getCommonModelErrorMessage(error, fallbackMessage));
}
