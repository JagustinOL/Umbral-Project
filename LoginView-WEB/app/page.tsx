'use client'

import { LoginForm } from '@/components/auth/LoginForm'

export default function LoginPage() {
  return (
    <div className="min-h-screen flex">
      <aside className="hidden lg:flex lg:w-[420px] shrink-0 bg-sidebar text-sidebar-foreground flex-col justify-between p-10">
        <div>
          <div className="flex items-center gap-3">
            <div className="size-10 rounded-lg bg-sidebar-foreground/10 flex items-center justify-center font-bold tracking-widest text-sm">
              UM
            </div>
            <div>
              <p className="font-semibold text-lg leading-none">UMBRAL</p>
              <p className="text-xs text-sidebar-foreground/50 mt-1">Mission Control Platform</p>
            </div>
          </div>
          <p className="mt-10 text-sidebar-foreground/70 text-sm leading-relaxed max-w-xs">
            Unified access for administrators and operators. Sign in with your Keycloak credentials to
            continue to your workspace.
          </p>
        </div>
        <p className="text-xs text-sidebar-foreground/40">
          Secured by Keycloak · umbral-realm
        </p>
      </aside>

      <main className="flex-1 flex items-center justify-center p-6">
        <div className="w-full max-w-md">
          <div className="mb-8 lg:hidden">
            <p className="font-semibold text-xl">UMBRAL</p>
            <p className="text-sm text-muted-foreground mt-1">Sign in to continue</p>
          </div>
          <LoginForm />
        </div>
      </main>
    </div>
  )
}
