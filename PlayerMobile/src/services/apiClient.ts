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
    throw new Error(
      `Network error calling ${options.baseUrl}${path}. Check Docker is running and CORS is enabled on the API.`,
    );
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
