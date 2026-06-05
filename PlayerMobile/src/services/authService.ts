import type { AuthSession, PlayerProfile } from '../types/auth';
import {
  clearAccessToken,
  clearSessionJson,
  getSessionJson,
  saveAccessToken,
  saveSessionJson,
} from '../storage/secureTokenStorage';
import { decodeJwtPayload } from '../utils/jwt';
import { isPasswordMinLength, isValidEmail } from '../utils/validation';
import * as keycloakAuth from './keycloakAuthService';
import * as playerApi from './playerApi';
import { resolveTeamMembership } from './playerTeamResolver';

function buildSessionFromToken(
  tokenResponse: Awaited<ReturnType<typeof keycloakAuth.loginWithPassword>>,
  player: PlayerProfile,
  teamState: { teamId: string | null; pendingTeamId: string | null },
): AuthSession {
  const expiresAt = tokenResponse.expires_in
    ? Date.now() + tokenResponse.expires_in * 1000
    : null;

  return {
    accessToken: tokenResponse.access_token,
    refreshToken: tokenResponse.refresh_token ?? null,
    expiresAt,
    player,
    teamId: teamState.teamId,
    pendingTeamId: teamState.pendingTeamId,
  };
}

async function persistSession(session: AuthSession): Promise<void> {
  await saveAccessToken(session.accessToken);
  await saveSessionJson(JSON.stringify(session));
}

async function fetchPlayerProfile(
  accessToken: string,
  email: string,
): Promise<PlayerProfile> {
  const payload = decodeJwtPayload(accessToken);
  const playerId = payload?.sub;

  if (!playerId) {
    throw new Error('Keycloak token does not contain a valid subject (sub).');
  }

  try {
    return await playerApi.getPlayerById(playerId);
  } catch {
    return {
      playerId,
      firstName: payload?.given_name ?? '',
      lastName: payload?.family_name ?? '',
      email: payload?.email ?? payload?.preferred_username ?? email,
    };
  }
}

async function ensureValidAccessToken(
  session: AuthSession,
): Promise<AuthSession> {
  const isExpired =
    session.expiresAt !== null && Date.now() >= session.expiresAt - 30_000;

  if (!isExpired || !session.refreshToken) {
    return session;
  }

  const tokenResponse = await keycloakAuth.refreshAccessToken(
    session.refreshToken,
  );

  const refreshed: AuthSession = {
    ...session,
    accessToken: tokenResponse.access_token,
    refreshToken: tokenResponse.refresh_token ?? session.refreshToken,
    expiresAt: tokenResponse.expires_in
      ? Date.now() + tokenResponse.expires_in * 1000
      : session.expiresAt,
  };

  await persistSession(refreshed);
  return refreshed;
}

export async function login(
  email: string,
  password: string,
): Promise<AuthSession> {
  if (!isValidEmail(email)) {
    throw new Error('Enter a valid email address.');
  }

  if (!password) {
    throw new Error('Password is required.');
  }

  const tokenResponse = await keycloakAuth.loginWithPassword(email, password);
  // Guardar token antes de llamadas API (team-membership, getPlayerById usan Bearer).
  await saveAccessToken(tokenResponse.access_token);

  const player = await fetchPlayerProfile(
    tokenResponse.access_token,
    email.trim(),
  );

  let teamState = { teamId: null as string | null, pendingTeamId: null as string | null };
  try {
    teamState = await resolveTeamMembership(player.playerId);
  } catch {
    // Si SessionManagement no responde, el jugador puede entrar y reintentar desde no-team.
  }

  const session = buildSessionFromToken(tokenResponse, player, teamState);
  await persistSession(session);
  return session;
}

export async function registerPlayer(input: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}): Promise<AuthSession> {
  if (!input.firstName.trim() || !input.lastName.trim()) {
    throw new Error('First name and last name are required.');
  }

  if (!isValidEmail(input.email)) {
    throw new Error('Enter a valid email address.');
  }

  if (!isPasswordMinLength(input.password)) {
    throw new Error('Password must be at least 8 characters.');
  }

  await playerApi.createPlayer({
    firstName: input.firstName.trim(),
    lastName: input.lastName.trim(),
    email: input.email.trim(),
    password: input.password,
  });

  return login(input.email, input.password);
}

export async function loadStoredSession(): Promise<AuthSession | null> {
  const raw = await getSessionJson();
  if (!raw) {
    return null;
  }

  try {
    let session = JSON.parse(raw) as AuthSession;
    session = await ensureValidAccessToken(session);

    try {
      const teamState = await resolveTeamMembership(session.player.playerId);

      if (
        teamState.teamId !== session.teamId ||
        teamState.pendingTeamId !== session.pendingTeamId
      ) {
        session = {
          ...session,
          teamId: teamState.teamId,
          pendingTeamId: teamState.pendingTeamId,
        };
        await persistSession(session);
      }
    } catch {
      // Mantener teamId/pendingTeamId guardados si la API no está disponible.
    }

    return session;
  } catch {
    await logout();
    return null;
  }
}

export async function updateSessionTeam(
  session: AuthSession,
  teamId: string | null,
  pendingTeamId: string | null = null,
): Promise<AuthSession> {
  const updated: AuthSession = {
    ...session,
    teamId,
    pendingTeamId,
  };
  await persistSession(updated);
  return updated;
}

export async function updateSessionPlayer(
  session: AuthSession,
  player: AuthSession['player'],
): Promise<AuthSession> {
  const updated: AuthSession = { ...session, player };
  await persistSession(updated);
  return updated;
}

export async function logout(): Promise<void> {
  const raw = await getSessionJson();
  if (raw) {
    try {
      const session = JSON.parse(raw) as AuthSession;
      if (session.refreshToken) {
        await keycloakAuth.revokeRefreshToken(session.refreshToken);
      }
    } catch {
      // Ignorar errores de revocación en Keycloak
    }
  }

  await clearAccessToken();
  await clearSessionJson();
}
