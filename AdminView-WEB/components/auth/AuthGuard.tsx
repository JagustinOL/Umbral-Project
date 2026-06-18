'use client'

import { useEffect, useState, type ReactNode } from 'react'
import {
  captureAuthFromHash,
  getAuthSession,
  hasRole,
  isAuthSessionValid,
  redirectToLogin,
} from '@/lib/auth/session'

interface AuthGuardProps {
  children: ReactNode
}

export function AuthGuard({ children }: AuthGuardProps) {
  const [isAuthorized, setIsAuthorized] = useState(false)

  useEffect(() => {
    captureAuthFromHash()
    const session = getAuthSession()

    if (!isAuthSessionValid(session) || !hasRole(session, 'admin')) {
      redirectToLogin()
      return
    }

    setIsAuthorized(true)
  }, [])

  if (!isAuthorized) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <p className="text-sm text-muted-foreground">Verificando sesión…</p>
      </div>
    )
  }

  return <>{children}</>
}
