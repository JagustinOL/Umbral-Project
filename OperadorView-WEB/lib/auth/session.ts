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

export function clearAuthSession(): void {
  if (typeof window === 'undefined') return
  sessionStorage.removeItem(AUTH_STORAGE_KEY)
}

export function getLoginUrl(): string {
  return process.env.NEXT_PUBLIC_LOGIN_URL ?? 'http://localhost:3002'
}

export function redirectToLogin(): void {
  if (typeof window === 'undefined') return
  clearAuthSession()
  window.location.href = getLoginUrl()
}

export function hasRole(session: AuthSession, role: string): boolean {
  const normalized = role.toLowerCase()
  return session.roles.some((r) => r.toLowerCase() === normalized)
}

export function getAccessTokenExpiresAt(accessToken: string): number | null {
  try {
    const payload = JSON.parse(atob(accessToken.split('.')[1] ?? '')) as { exp?: number }
    if (typeof payload.exp === 'number') {
      return payload.exp * 1000
    }
  } catch {
    // Ignore malformed tokens; API will reject them.
  }
  return null
}

function isAccessTokenExpired(accessToken: string): boolean {
  const expiresAt = getAccessTokenExpiresAt(accessToken)
  if (expiresAt == null) return false
  return expiresAt < Date.now()
}

export function isAuthSessionValid(session: AuthSession | null): session is AuthSession {
  if (!session?.accessToken?.trim()) return false
  return !isAccessTokenExpired(session.accessToken)
}

export function getSessionUsername(session: AuthSession): string {
  try {
    const payload = JSON.parse(atob(session.accessToken.split('.')[1] ?? '')) as {
      preferred_username?: string
      email?: string
    }
    return payload.preferred_username ?? payload.email ?? session.userId
  } catch {
    return session.userId
  }
}

export function getSessionRoleLabel(session: AuthSession): string {
  if (hasRole(session, 'admin')) return 'Administrator'
  if (hasRole(session, 'operator')) return 'Operator'
  return session.roles[0] ?? 'User'
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
