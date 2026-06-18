'use client'

import { useState } from 'react'
import { AlertCircle, KeyRound, Loader2, LogIn } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { buildRedirectUrl, saveAuthSession } from '@/lib/auth/session'
import { getRedirectUrlForRole, resolvePrimaryRole } from '@/lib/auth/roles'
import {
  AuthApiError,
  loginWithCredentials,
  setupOperatorPassword,
} from '@/lib/services/authService'

type AuthMode = 'sign-in' | 'activate'

export function LoginForm() {
  const [mode, setMode] = useState<AuthMode>('sign-in')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [setupCode, setSetupCode] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const completeAuth = (response: {
    accessToken: string
    refreshToken?: string
    expiresIn: number
    userId: string
    roles: string[]
  }) => {
    const session = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresIn: response.expiresIn,
      userId: response.userId,
      roles: response.roles,
    }

    saveAuthSession(session)

    const primaryRole = resolvePrimaryRole(response.roles)
    if (!primaryRole || primaryRole === 'player') {
      setError(
        primaryRole === 'player'
          ? 'Player accounts cannot access the admin or operator consoles.'
          : 'Your account has no admin or operator role assigned in Keycloak.',
      )
      return
    }

    const targetBaseUrl = getRedirectUrlForRole(primaryRole)
    window.location.href = buildRedirectUrl(targetBaseUrl, session)
  }

  const handleSignIn = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await loginWithCredentials(username.trim(), password)
      completeAuth(response)
    } catch (err) {
      if (err instanceof AuthApiError) {
        setError(err.message)
      } else if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Unexpected error during sign in.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleActivate = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    if (password.length < 8) {
      setError('Password must be at least 8 characters.')
      return
    }

    setIsSubmitting(true)

    try {
      const response = await setupOperatorPassword({
        email: username.trim(),
        setupCode: setupCode.trim(),
        password,
      })
      completeAuth(response)
    } catch (err) {
      if (err instanceof AuthApiError) {
        setError(err.message)
      } else if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Unexpected error during account activation.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Card className="shadow-sm">
      <CardHeader>
        <CardTitle className="text-2xl">
          {mode === 'sign-in' ? 'Sign in' : 'Activate account'}
        </CardTitle>
        <CardDescription>
          {mode === 'sign-in'
            ? 'Use your UMBRAL email and password. You will be redirected based on your Keycloak role.'
            : 'First-time operators: enter the activation code provided by your administrator and choose a password.'}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-2 rounded-lg bg-muted p-1">
          <Button
            type="button"
            variant={mode === 'sign-in' ? 'default' : 'ghost'}
            className="h-9"
            onClick={() => {
              setMode('sign-in')
              setError(null)
            }}
          >
            <LogIn className="size-4" />
            Sign in
          </Button>
          <Button
            type="button"
            variant={mode === 'activate' ? 'default' : 'ghost'}
            className="h-9"
            onClick={() => {
              setMode('activate')
              setError(null)
            }}
          >
            <KeyRound className="size-4" />
            Activate
          </Button>
        </div>

        {error && (
          <Alert variant="destructive">
            <AlertCircle className="size-4" />
            <AlertTitle>{mode === 'sign-in' ? 'Sign in failed' : 'Activation failed'}</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {mode === 'sign-in' ? (
          <form onSubmit={handleSignIn} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">Email</Label>
              <Input
                id="username"
                type="email"
                autoComplete="username"
                placeholder="you@umbral.com"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
                disabled={isSubmitting}
              />
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="animate-spin" />
                  Signing in…
                </>
              ) : (
                <>
                  <LogIn />
                  Continue
                </>
              )}
            </Button>
          </form>
        ) : (
          <form onSubmit={handleActivate} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="activate-email">Email</Label>
              <Input
                id="activate-email"
                type="email"
                autoComplete="username"
                placeholder="operator@umbral.ops"
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="setup-code">Activation code</Label>
              <Input
                id="setup-code"
                autoComplete="one-time-code"
                placeholder="XXXX-XXXX"
                value={setupCode}
                onChange={(event) => setSetupCode(event.target.value.toUpperCase())}
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="new-password">New password</Label>
              <Input
                id="new-password"
                type="password"
                autoComplete="new-password"
                placeholder="Minimum 8 characters"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
                minLength={8}
                disabled={isSubmitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirm-password">Confirm password</Label>
              <Input
                id="confirm-password"
                type="password"
                autoComplete="new-password"
                placeholder="Repeat password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                required
                minLength={8}
                disabled={isSubmitting}
              />
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="animate-spin" />
                  Activating…
                </>
              ) : (
                <>
                  <KeyRound />
                  Activate and continue
                </>
              )}
            </Button>
          </form>
        )}
      </CardContent>
    </Card>
  )
}
