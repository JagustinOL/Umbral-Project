import { createContext } from 'react';
import type { AuthSession } from '../types/auth';

export type AuthContextValue = {
  session: AuthSession | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<AuthSession>;
  register: (input: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
  }) => Promise<void>;
  logout: () => Promise<void>;
  setTeamId: (
    teamId: string | null,
    pendingTeamId?: string | null,
  ) => Promise<void>;
  refreshSession: () => Promise<void>;
  updatePlayerProfile: (player: AuthSession['player']) => Promise<void>;
};

export const AuthContext = createContext<AuthContextValue | undefined>(
  undefined,
);
