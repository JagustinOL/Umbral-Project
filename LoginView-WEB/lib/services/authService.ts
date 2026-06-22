export interface AuthTokenResponse {
  accessToken: string
  refreshToken?: string
  expiresIn: number
  userId: string
  roles: string[]
}

export class AuthApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'AuthApiError'
    this.status = status
  }
}

const userApiBaseUrl = (
  process.env.NEXT_PUBLIC_USER_API_URL ?? 'http://localhost:5284'
).replace(/\/+$/, '')

export async function loginWithCredentials(
  username: string,
  password: string,
  signal?: AbortSignal,
): Promise<AuthTokenResponse> {
  const response = await fetch(`${userApiBaseUrl}/api/v1/auth/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
    signal,
  })

  return parseAuthResponse(response)
}

export interface SetupOperatorPasswordPayload {
  email: string
  setupCode: string
  password: string
}

export async function setupOperatorPassword(
  payload: SetupOperatorPasswordPayload,
  signal?: AbortSignal,
): Promise<AuthTokenResponse> {
  const response = await fetch(`${userApiBaseUrl}/api/v1/auth/operator/setup-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal,
  })

  return parseAuthResponse(response)
}

async function parseAuthResponse(response: Response): Promise<AuthTokenResponse> {
  const raw = await response.text()
  let body: unknown
  if (raw) {
    try {
      body = JSON.parse(raw)
    } catch {
      body = raw
    }
  }

  if (!response.ok) {
    const message = extractApiErrorMessage(body, response.status)
    throw new AuthApiError(message, response.status)
  }

  return body as AuthTokenResponse
}

function extractApiErrorMessage(body: unknown, status: number): string {
  if (typeof body === 'object' && body !== null) {
    const record = body as Record<string, unknown>
    if (typeof record.detail === 'string' && record.detail.length > 0) {
      return record.detail
    }
    if (typeof record.error === 'string' && record.error.length > 0) {
      return record.error
    }
    if (typeof record.title === 'string' && record.title.length > 0 && status !== 500) {
      return record.title
    }
  }

  return `Authentication failed (${status}).`
}
