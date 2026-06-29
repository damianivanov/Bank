import { create } from "zustand";

type Theme = "light" | "dark";

type ThemeState = {
  theme: Theme;
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
};

const themeStorageKey = "bank-theme";

function getStoredTheme(): Theme {
  try {
    const storedTheme = localStorage.getItem(themeStorageKey);
    if (storedTheme === "light" || storedTheme === "dark") {
      return storedTheme;
    }
  } catch {}

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function applyTheme(theme: Theme) {
  document.documentElement.classList.toggle("dark", theme === "dark");

  try {
    localStorage.setItem(themeStorageKey, theme);
  } catch {}
}

const initialTheme = getStoredTheme();
applyTheme(initialTheme);

export const useThemeStore = create<ThemeState>((set) => ({
  theme: initialTheme,
  toggleTheme: () =>
    set((state) => {
      const nextTheme = state.theme === "dark" ? "light" : "dark";
      applyTheme(nextTheme);

      return { theme: nextTheme };
    }),
  setTheme: (theme) => {
    applyTheme(theme);
    set({ theme });
  },
}));
