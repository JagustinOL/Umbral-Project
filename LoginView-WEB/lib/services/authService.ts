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

const missionApiBaseUrl = (
  process.env.NEXT_PUBLIC_MISSION_API_URL ?? 'http://localhost:5260'
).replace(/\/+$/, '')

export async function loginWithCredentials(
  username: string,
  password: string,
  signal?: AbortSignal,
): Promise<AuthTokenResponse> {
  const response = await fetch(`${missionApiBaseUrl}/api/v1/auth/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
    signal,
  })

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
    const message =
      typeof body === 'object' &&
      body !== null &&
      'error' in body &&
      typeof (body as { error: unknown }).error === 'string'
        ? (body as { error: string }).error
        : `Authentication failed (${response.status}).`
    throw new AuthApiError(message, response.status)
  }

  return body as AuthTokenResponse
}
