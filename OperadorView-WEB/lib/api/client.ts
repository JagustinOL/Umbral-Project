import {
  captureAuthFromHash,
  getAuthSession,
  redirectToLogin,
} from "@/lib/auth/session";

const sessionApiBaseUrl = (
  process.env.NEXT_PUBLIC_SESSION_API_URL ?? "http://localhost:5200"
).replace(/\/+$/, "");

const missionApiBaseUrl = (
  process.env.NEXT_PUBLIC_MISSION_API_URL ?? "http://localhost:5200"
).replace(/\/+$/, "");

const SESSION_API_BASE_URL = `${sessionApiBaseUrl}/api/v1`;
const MISSION_API_BASE_URL = `${missionApiBaseUrl}/api/v1`;
const SCORING_API_BASE_URL = `${sessionApiBaseUrl}/api/v1`;

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

type HttpMethod = "GET" | "POST" | "PUT" | "DELETE";

interface RequestOptions {
  method?: HttpMethod;
  body?: unknown;
  signal?: AbortSignal;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const formatValidationErrors = (payload: Record<string, unknown>): string | null => {
  const errors = payload.errors;
  if (!isRecord(errors)) return null;

  const messages = Object.entries(errors).flatMap(([field, value]) => {
    if (Array.isArray(value)) {
      return value
        .filter((entry): entry is string => typeof entry === "string" && entry.length > 0)
        .map((entry) => (field === "$" ? entry : `${field}: ${entry}`));
    }
    return [];
  });

  return messages.length > 0 ? messages.join(" ") : null;
};

const getErrorMessage = (status: number, payload: unknown): string => {
  if (isRecord(payload)) {
    const validationMessage = formatValidationErrors(payload);
    if (validationMessage) {
      return validationMessage;
    }

    const detail = payload.detail;
    if (typeof detail === "string" && detail.length > 0) {
      return detail;
    }

    const message = payload.message;
    if (typeof message === "string" && message.length > 0) {
      return message;
    }

    const title = payload.title;
    if (
      typeof title === "string" &&
      title.length > 0 &&
      title !== "One or more validation errors occurred."
    ) {
      return title;
    }
  }

  if (status === 401) {
    return "Sesión expirada o no autorizada. Inicia sesión de nuevo.";
  }

  return `Request failed with status ${status}.`;
};

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  const raw = await response.text();
  let parsedBody: unknown;
  if (raw) {
    try {
      parsedBody = JSON.parse(raw);
    } catch {
      parsedBody = raw;
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      redirectToLogin();
    }

    if (response.status === 400 || response.status === 409 || response.status >= 500) {
      const errorDetails = isRecord(parsedBody)
        ? (parsedBody.details ?? parsedBody)
        : parsedBody;

      console.error("API Error Details:", errorDetails);
    }

    throw new ApiError(getErrorMessage(response.status, parsedBody), response.status, parsedBody);
  }

  return parsedBody as T;
}

async function request<T>(
  baseUrl: string,
  endpoint: string,
  { method = "GET", body, signal }: RequestOptions = {},
): Promise<T> {
  captureAuthFromHash();
  const session = getAuthSession();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (session?.accessToken) {
    headers.Authorization = `Bearer ${session.accessToken}`;
  }

  const response = await fetch(`${baseUrl}${endpoint}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  return parseResponse<T>(response);
}

export function sessionApiRequest<T>(
  endpoint: string,
  options?: RequestOptions,
): Promise<T> {
  return request<T>(SESSION_API_BASE_URL, endpoint, options);
}

export function missionApiRequest<T>(
  endpoint: string,
  options?: RequestOptions,
): Promise<T> {
  return request<T>(MISSION_API_BASE_URL, endpoint, options);
}

export function scoringApiRequest<T>(
  endpoint: string,
  options?: RequestOptions,
): Promise<T> {
  return request<T>(SCORING_API_BASE_URL, endpoint, options);
}
