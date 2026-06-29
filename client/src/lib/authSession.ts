const refreshSessionKey = "bank.auth.hasRefreshSession";

function canUseBrowserStorage(): boolean {
  return typeof window !== "undefined" && typeof window.localStorage !== "undefined";
}

export function hasRefreshSessionMarker(): boolean {
  if (!canUseBrowserStorage()) {
    return false;
  }

  try {
    return window.localStorage.getItem(refreshSessionKey) == "1";
  } catch {
    return false;
  }
}

export function markRefreshSession(): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  try {
    window.localStorage.setItem(refreshSessionKey, "1");
  } catch {}
}

export function clearRefreshSessionMarker(): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  try {
    window.localStorage.removeItem(refreshSessionKey);
  } catch {}
}
