export const AUTH_STORAGE_KEY = 'umbral_auth'

export interface AuthSession {
  accessToken: string
  refreshToken?: string
  expiresIn: number
  userId: string
  roles: string[]
}

export function saveAuthSession(session: AuthSession): void {
  if (typeof window === 'undefined') return
  sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
}

export function getAuthSession(): AuthSession | null {
  if (typeof window === 'undefined') return null
  const raw = sessionStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthSession
  } catch {
    return null
  }
}

export function captureAuthFromHash(): AuthSession | null {
  if (typeof window === 'undefined') return null
  const hash = window.location.hash
  if (!hash.startsWith('#auth=')) return null

  const encoded = hash.slice('#auth='.length)
  const params = new URLSearchParams(decodeURIComponent(encoded))
  const accessToken = params.get('accessToken')
  const userId = params.get('userId')
  const rolesRaw = params.get('roles')

  if (!accessToken || !userId || !rolesRaw) return null

  const session: AuthSession = {
    accessToken,
    refreshToken: params.get('refreshToken') ?? undefined,
    expiresIn: 0,
    userId,
    roles: rolesRaw.split(',').filter(Boolean),
  }

  saveAuthSession(session)
  window.history.replaceState(null, '', window.location.pathname + window.location.search)
  return session
}
