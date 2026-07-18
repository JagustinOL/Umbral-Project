'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { refreshAuthSession } from '@/lib/api/client'
import {
  getAccessTokenExpiresAt,
  getAuthSession,
  redirectToLogin,
} from '@/lib/auth/session'

/** Avisar al usuario este tiempo antes de que expire el access token. */
const WARN_BEFORE_MS = 2 * 60 * 1000
/** Si no responde el aviso, forzar cierre tras este margen. */
const FORCE_LOGOUT_AFTER_WARN_MS = 90 * 1000

export function useSessionKeepAlive() {
  const [isWarningOpen, setIsWarningOpen] = useState(false)
  const [secondsLeft, setSecondsLeft] = useState(0)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const warnTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const forceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const countdownRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const warnedForExpRef = useRef<number | null>(null)

  const clearTimers = useCallback(() => {
    if (warnTimerRef.current) clearTimeout(warnTimerRef.current)
    if (forceTimerRef.current) clearTimeout(forceTimerRef.current)
    if (countdownRef.current) clearInterval(countdownRef.current)
    warnTimerRef.current = null
    forceTimerRef.current = null
    countdownRef.current = null
  }, [])

  const scheduleNextCheck = useCallback(() => {
    clearTimers()
    setIsWarningOpen(false)

    const session = getAuthSession()
    if (!session?.accessToken) return

    const expiresAt = getAccessTokenExpiresAt(session.accessToken)
    if (!expiresAt) return

    const msUntilExpiry = expiresAt - Date.now()
    if (msUntilExpiry <= 0) {
      void (async () => {
        const ok = await refreshAuthSession()
        if (!ok) {
          redirectToLogin()
          return
        }
        scheduleNextCheck()
      })()
      return
    }

    const msUntilWarn = Math.max(0, msUntilExpiry - WARN_BEFORE_MS)

    warnTimerRef.current = setTimeout(() => {
      const currentExp = getAccessTokenExpiresAt(getAuthSession()?.accessToken ?? '')
      if (!currentExp || warnedForExpRef.current === currentExp) {
        return
      }
      warnedForExpRef.current = currentExp
      setIsWarningOpen(true)
      setSecondsLeft(Math.ceil(FORCE_LOGOUT_AFTER_WARN_MS / 1000))

      countdownRef.current = setInterval(() => {
        setSecondsLeft((prev) => Math.max(0, prev - 1))
      }, 1000)

      forceTimerRef.current = setTimeout(() => {
        redirectToLogin()
      }, FORCE_LOGOUT_AFTER_WARN_MS)
    }, msUntilWarn)
  }, [clearTimers])

  const staySignedIn = useCallback(async () => {
    setIsRefreshing(true)
    try {
      const ok = await refreshAuthSession()
      if (!ok) {
        redirectToLogin()
        return
      }
      warnedForExpRef.current = null
      scheduleNextCheck()
    } finally {
      setIsRefreshing(false)
    }
  }, [scheduleNextCheck])

  const signOutNow = useCallback(() => {
    clearTimers()
    redirectToLogin()
  }, [clearTimers])

  useEffect(() => {
    scheduleNextCheck()
    return () => clearTimers()
  }, [scheduleNextCheck, clearTimers])

  return {
    isWarningOpen,
    secondsLeft,
    isRefreshing,
    staySignedIn,
    signOutNow,
  }
}
