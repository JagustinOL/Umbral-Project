import type { UmbralRole } from '@/lib/auth/session'

const ROLE_PRIORITY: UmbralRole[] = ['admin', 'operator', 'player']

export function resolvePrimaryRole(roles: string[]): UmbralRole | null {
  const normalized = new Set(roles.map((role) => role.toLowerCase()))
  for (const role of ROLE_PRIORITY) {
    if (normalized.has(role)) return role
  }
  return null
}

export function getRedirectUrlForRole(role: UmbralRole): string {
  switch (role) {
    case 'admin':
      return process.env.NEXT_PUBLIC_ADMIN_URL ?? 'http://localhost:3000'
    case 'operator':
      return process.env.NEXT_PUBLIC_OPERATOR_URL ?? 'http://localhost:3001'
    default:
      throw new Error(`No redirect configured for role: ${role}`)
  }
}
