import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import type { AuthSession } from '../types/auth';
import * as authService from '../services/authService';
import { AuthContext, type AuthContextValue } from './authContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshSession = useCallback(async () => {
    const stored = await authService.loadStoredSession();
    setSession(stored);
  }, []);

  const updatePlayerProfile = useCallback(
    async (player: AuthSession['player']) => {
      setSession((current) => {
        if (!current) {
          return current;
        }
        void authService.updateSessionPlayer(current, player);
        return { ...current, player };
      });
    },
    [],
  );

  useEffect(() => {
    let mounted = true;
    authService.loadStoredSession().then((stored) => {
      if (mounted) {
        setSession(stored);
        setIsLoading(false);
      }
    });
    return () => {
      mounted = false;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const nextSession = await authService.login(email, password);
    setSession(nextSession);
    return nextSession;
  }, []);

  const register = useCallback(
    async (input: {
      firstName: string;
      lastName: string;
      email: string;
      password: string;
    }) => {
      const nextSession = await authService.registerPlayer(input);
      setSession(nextSession);
    },
    [],
  );

  const logout = useCallback(async () => {
    await authService.logout();
    setSession(null);
  }, []);

  const setTeamId = useCallback(
    async (teamId: string | null, pendingTeamId: string | null = null) => {
      setSession((current) => {
        if (!current) {
          return current;
        }
        void authService.updateSessionTeam(current, teamId, pendingTeamId);
        return { ...current, teamId, pendingTeamId };
      });
    },
    [],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isLoading,
      login,
      register,
      logout,
      setTeamId,
      refreshSession,
      updatePlayerProfile,
    }),
    [
      session,
      isLoading,
      login,
      register,
      logout,
      setTeamId,
      refreshSession,
      updatePlayerProfile,
    ],
  );

  return (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  );
}
