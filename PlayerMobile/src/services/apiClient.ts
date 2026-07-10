import { getAccessToken } from '../storage/secureTokenStorage';

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  baseUrl: string;
  skipAuth?: boolean;
};

type ApiErrorBody = {
  error?: string;
  title?: string;
  detail?: string;
};

export async function apiRequest<T>(
  path: string,
  options: RequestOptions,
): Promise<T> {
  const headers: Record<string, string> = {};

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (!options.skipAuth) {
    const token = await getAccessToken();
    if (!token) {
      throw new Error('Authentication required. Please sign in again.');
    }
    headers.Authorization = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(`${options.baseUrl}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    const usesLocalhost =
      options.baseUrl.includes('localhost') || options.baseUrl.includes('127.0.0.1');
    const hint = usesLocalhost
      ? ' On a physical phone, set your PC LAN IP in PlayerMobile/.env (not localhost).'
      : ' Check Docker is running and that the phone is on the same Wi-Fi network.';
    throw new Error(`Network error calling ${options.baseUrl}${path}.${hint}`);
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    const text = await response.text();
    if (text) {
      try {
        const body = JSON.parse(text) as ApiErrorBody;
        message = body.error ?? body.detail ?? body.title ?? message;
      } catch {
        message = text;
      }
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function apiFormRequest<T>(
  path: string,
  options: {
    baseUrl: string;
    formData: FormData;
    method?: 'POST' | 'PUT';
  },
): Promise<T> {
  const token = await getAccessToken();
  if (!token) {
    throw new Error('Authentication required. Please sign in again.');
  }

  let response: Response;
  try {
    response = await fetch(`${options.baseUrl}${path}`, {
      method: options.method ?? 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: options.formData,
    });
  } catch {
    throw new Error(`Network error calling ${options.baseUrl}${path}.`);
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    const text = await response.text();
    if (text) {
      try {
        const body = JSON.parse(text) as ApiErrorBody;
        message = body.error ?? body.detail ?? body.title ?? message;
      } catch {
        message = text;
      }
    }
    throw new Error(message);
  }

  return (await response.json()) as T;
}
