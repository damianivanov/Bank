import axios, { type AxiosResponse } from "axios";
import type { JsonData } from "@/types";

type ErrorPayload = {
  error?: unknown;
};

function isErrorPayload(value: unknown): value is ErrorPayload {
  return typeof value === "object" && value !== null;
}

function extractCommonModelError(data: unknown): string | null {
  if (!isErrorPayload(data) || typeof data.error !== "string") {
    return null;
  }

  const errorMessage = data.error.trim();
  return errorMessage.length > 0 ? errorMessage : null;
}

export class CommonModelError extends Error {
  readonly statusCode?: number;

  constructor(message: string, statusCode?: number) {
    super(message);
    this.name = "CommonModelError";
    this.statusCode = statusCode;
  }
}

export function unwrapCommonModel<T>(
  response: AxiosResponse<JsonData<T>>,
  fallbackMessage: string,
): T {
  const result = response.data;

  if (!result.success) {
    throw new CommonModelError(result.error || fallbackMessage, response.status);
  }

  if (result.data === undefined || result.data === null) {
    throw new CommonModelError(fallbackMessage, response.status);
  }

  return result.data;
}

// 429 от rate limiter-а. При не-2xx статус axios отхвърля с AxiosError; CommonModelError се проверява
// за всеки случай, ако някога обвием отговора другаде.
export function isRateLimitError(error: unknown): boolean {
  if (error instanceof CommonModelError) {
    return error.statusCode === 429;
  }

  return axios.isAxiosError(error) && error.response?.status === 429;
}

export function getCommonModelErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof CommonModelError) {
    return error.message;
  }

  if (axios.isAxiosError(error)) {
    const commonModelError = extractCommonModelError(error.response?.data);
    if (commonModelError) {
      return commonModelError;
    }
  }

  if (error instanceof Error) {
    const message = error.message.trim();
    if (message.length > 0) {
      return message;
    }
  }

  return fallbackMessage;
}
