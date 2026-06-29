import { create } from "zustand";

// Държи видимостта на модала за достигнат лимит. Лимитът важи само за гости (логнатите нямат лимит),
// затова модалът е глобален "сигнал" — попълва се от мястото, където прихващаме 429, и се рендира веднъж.
interface RateLimitState {
  isOpen: boolean;
  open: () => void;
  close: () => void;
}

export const useRateLimitStore = create<RateLimitState>()((set) => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
}));
