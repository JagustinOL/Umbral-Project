'use client'

import { useState } from 'react'
import { AlertCircle, Loader2, LogIn } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { buildRedirectUrl, saveAuthSession } from '@/lib/auth/session'
import { getRedirectUrlForRole, resolvePrimaryRole } from '@/lib/auth/roles'
import { AuthApiError, loginWithCredentials } from '@/lib/services/authService'

export function LoginForm() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await loginWithCredentials(username.trim(), password)
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

  return (
    <Card className="shadow-sm">
      <CardHeader>
        <CardTitle className="text-2xl">Sign in</CardTitle>
        <CardDescription>
          Use your UMBRAL email and password. You will be redirected based on your Keycloak role.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="size-4" />
              <AlertTitle>Sign in failed</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

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
      </CardContent>
    </Card>
  )
}
