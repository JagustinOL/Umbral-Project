import {
  getKeycloakLogoutUrl,
  getKeycloakTokenUrl,
  KEYCLOAK_CONFIG,
} from '../constants/keycloak';
import type { KeycloakTokenResponse } from '../types/keycloak';

async function requestToken(
  body: Record<string, string>,
): Promise<KeycloakTokenResponse> {
  const params = new URLSearchParams(body);

  if (KEYCLOAK_CONFIG.clientSecret) {
    params.set('client_secret', KEYCLOAK_CONFIG.clientSecret);
  }

  let response: Response;
  try {
    response = await fetch(getKeycloakTokenUrl(), {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params.toString(),
    });
  } catch {
    throw new Error(
      'Network error contacting Keycloak. Verify Docker (port 8081) and realm umbral-realm.',
    );
  }

  if (!response.ok) {
    const errorText = await response.text();
    if (response.status === 401) {
      throw new Error('Invalid email or password.');
    }
    throw new Error(
      errorText || `Keycloak authentication failed (${response.status}).`,
    );
  }

  return (await response.json()) as KeycloakTokenResponse;
}

export async function loginWithPassword(
  email: string,
  password: string,
): Promise<KeycloakTokenResponse> {
  return requestToken({
    grant_type: 'password',
    client_id: KEYCLOAK_CONFIG.clientId,
    username: email.trim(),
    password,
  });
}

export async function refreshAccessToken(
  refreshToken: string,
): Promise<KeycloakTokenResponse> {
  return requestToken({
    grant_type: 'refresh_token',
    client_id: KEYCLOAK_CONFIG.clientId,
    refresh_token: refreshToken,
  });
}

export async function revokeRefreshToken(
  refreshToken: string,
): Promise<void> {
  const params = new URLSearchParams({
    client_id: KEYCLOAK_CONFIG.clientId,
    refresh_token: refreshToken,
  });

  if (KEYCLOAK_CONFIG.clientSecret) {
    params.set('client_secret', KEYCLOAK_CONFIG.clientSecret);
  }

  await fetch(getKeycloakLogoutUrl(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params.toString(),
  });
}
