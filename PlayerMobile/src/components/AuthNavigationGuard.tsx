import { useRouter, useSegments } from 'expo-router';
import { useEffect, type ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';

/**
 * En web la URL puede restaurarse (p. ej. /team-dashboard); sin sesión debe ir a login.
 */
export function AuthNavigationGuard({ children }: { children: ReactNode }) {
  const { session, isLoading } = useAuth();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) {
      return;
    }

    const inAuthGroup = segments[0] === '(auth)';

    if (!session && !inAuthGroup) {
      router.replace('/(auth)/login');
    }
  }, [isLoading, session, segments, router]);

  return children;
}
