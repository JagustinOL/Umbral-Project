const missionApiBaseUrl = (
  process.env.NEXT_PUBLIC_MISSION_API_URL ?? "http://localhost:5260"
).replace(/\/+$/, "");

const API_BASE_URL = `${missionApiBaseUrl}/api/v1`;

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

interface FormRequestOptions {
  method?: Extract<HttpMethod, "POST" | "PUT">;
  signal?: AbortSignal;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const getErrorMessage = (status: number, payload: unknown): string => {
  if (isRecord(payload)) {
    const message = payload.message;
    if (typeof message === "string" && message.length > 0) {
      return message;
    }

    const title = payload.title;
    if (typeof title === "string" && title.length > 0) {
      return title;
    }
  }

  return `Request failed with status ${status}.`;
};

export async function apiRequest<T>(
  endpoint: string,
  { method = "GET", body, signal }: RequestOptions = {},
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method,
    headers: {
      "Content-Type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

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

/** Multipart requests (e.g. AddHint HU-17). Do not set Content-Type; the browser sets the boundary. */
export async function apiFormRequest<T>(
  endpoint: string,
  formData: FormData,
  { method = "POST", signal }: FormRequestOptions = {},
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method,
    body: formData,
    signal,
  });

  return parseResponse<T>(response);
}
