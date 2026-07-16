import {
  captureAuthFromHash,
  getAuthSession,
  redirectToLogin,
  saveAuthSession,
  type AuthSession,
} from "@/lib/auth/session";

const missionApiBaseUrl = (
  process.env.NEXT_PUBLIC_MISSION_API_URL ?? "http://localhost:5200"
).replace(/\/+$/, "");

const userApiBaseUrl = (
  process.env.NEXT_PUBLIC_USER_API_URL ?? "http://localhost:5200"
).replace(/\/+$/, "");

const API_BASE_URL = `${missionApiBaseUrl}/api/v1`;
const USER_API_BASE_URL = `${userApiBaseUrl}/api/v1`;
const SCORING_API_BASE_URL = `${missionApiBaseUrl}/api/v1`;

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
  /** Internal: skip refresh retry to avoid loops. */
  skipAuthRefresh?: boolean;
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

let refreshInFlight: Promise<boolean> | null = null;

export async function refreshAuthSession(): Promise<boolean> {
  return tryRefreshSession()
}

async function tryRefreshSession(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    const session = getAuthSession();
    if (!session?.refreshToken) return false;

    try {
      const response = await fetch(`${USER_API_BASE_URL}/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: session.refreshToken }),
      });

      if (!response.ok) return false;

      const payload = (await response.json()) as {
        accessToken?: string;
        refreshToken?: string;
        expiresIn?: number;
        userId?: string;
        roles?: string[];
      };

      if (!payload.accessToken) return false;

      const nextSession: AuthSession = {
        accessToken: payload.accessToken,
        refreshToken: payload.refreshToken ?? session.refreshToken,
        expiresIn: payload.expiresIn ?? session.expiresIn,
        userId: payload.userId ?? session.userId,
        roles: payload.roles?.length ? payload.roles : session.roles,
      };
      saveAuthSession(nextSession);
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

export async function userApiRequest<T>(
  endpoint: string,
  { method = "GET", body, signal }: RequestOptions = {},
): Promise<T> {
  return requestWithBaseUrl<T>(USER_API_BASE_URL, endpoint, { method, body, signal });
}

export async function apiRequest<T>(
  endpoint: string,
  { method = "GET", body, signal }: RequestOptions = {},
): Promise<T> {
  return requestWithBaseUrl<T>(API_BASE_URL, endpoint, { method, body, signal });
}

export async function scoringApiRequest<T>(
  endpoint: string,
  { method = "GET", body, signal }: RequestOptions = {},
): Promise<T> {
  return requestWithBaseUrl<T>(SCORING_API_BASE_URL, endpoint, { method, body, signal });
}

async function requestWithBaseUrl<T>(
  baseUrl: string,
  endpoint: string,
  { method = "GET", body, signal, skipAuthRefresh = false }: RequestOptions = {},
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

  if (response.status === 401 && !skipAuthRefresh) {
    const refreshed = await tryRefreshSession();
    if (refreshed) {
      return requestWithBaseUrl<T>(baseUrl, endpoint, {
        method,
        body,
        signal,
        skipAuthRefresh: true,
      });
    }
    redirectToLogin();
  }

  return parseResponse<T>(response);
}

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
