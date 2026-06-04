'use client'

import { useEffect } from 'react'
import { captureAuthFromHash } from '@/lib/auth/session'

export function AuthBootstrap() {
  useEffect(() => {
    captureAuthFromHash()
  }, [])

  return null
}
