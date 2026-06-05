export const KEYCLOAK_CONFIG = {
  baseUrl: process.env.EXPO_PUBLIC_KEYCLOAK_URL ?? 'http://localhost:8081',
  realm: process.env.EXPO_PUBLIC_KEYCLOAK_REALM ?? 'umbral-realm',
  clientId: process.env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID ?? 'umbral-player-mobile',
  clientSecret: process.env.EXPO_PUBLIC_KEYCLOAK_CLIENT_SECRET ?? '',
} as const;

export function getKeycloakTokenUrl(): string {
  return `${KEYCLOAK_CONFIG.baseUrl}/realms/${KEYCLOAK_CONFIG.realm}/protocol/openid-connect/token`;
}

export function getKeycloakLogoutUrl(): string {
  return `${KEYCLOAK_CONFIG.baseUrl}/realms/${KEYCLOAK_CONFIG.realm}/protocol/openid-connect/logout`;
}
