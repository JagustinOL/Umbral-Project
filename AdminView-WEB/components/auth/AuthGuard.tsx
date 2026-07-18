'use client'

import { useEffect, useState, type ReactNode } from 'react'
import { SessionKeepAliveDialog } from '@/components/auth/SessionKeepAliveDialog'
import { refreshAuthSession } from '@/lib/api/client'
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
    let cancelled = false

    const authorize = async () => {
      captureAuthFromHash()
      let session = getAuthSession()

      if (!session?.accessToken?.trim() || !hasRole(session, 'admin')) {
        redirectToLogin()
        return
      }

      if (!isAuthSessionValid(session)) {
        const refreshed = await refreshAuthSession()
        if (!refreshed) {
          redirectToLogin()
          return
        }
        session = getAuthSession()
        if (!isAuthSessionValid(session) || !hasRole(session, 'admin')) {
          redirectToLogin()
          return
        }
      }

      if (!cancelled) {
        setIsAuthorized(true)
      }
    }

    void authorize()
    return () => {
      cancelled = true
    }
  }, [])

  if (!isAuthorized) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <p className="text-sm text-muted-foreground">Verificando sesión…</p>
      </div>
    )
  }

  return (
    <>
      {children}
      <SessionKeepAliveDialog />
    </>
  )
}
