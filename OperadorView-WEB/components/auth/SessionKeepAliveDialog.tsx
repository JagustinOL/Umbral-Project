'use client'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useSessionKeepAlive } from '@/lib/auth/useSessionKeepAlive'

export function SessionKeepAliveDialog() {
  const { isWarningOpen, secondsLeft, isRefreshing, staySignedIn, signOutNow } =
    useSessionKeepAlive()

  return (
    <AlertDialog open={isWarningOpen}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Sigues aquí?</AlertDialogTitle>
          <AlertDialogDescription>
            Tu sesión está por cerrarse por seguridad. Confirma que sigues
            activo para continuar trabajando. Si no respondes, se cerrará en{' '}
            {secondsLeft}s.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isRefreshing} onClick={signOutNow}>
            Cerrar sesión
          </AlertDialogCancel>
          <AlertDialogAction
            disabled={isRefreshing}
            onClick={(event) => {
              event.preventDefault()
              void staySignedIn()
            }}
          >
            {isRefreshing ? 'Renovando…' : 'Sí, sigo activo'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
