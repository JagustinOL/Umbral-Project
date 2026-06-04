# Keycloak setup for UMBRAL Player Mobile

The mobile app authenticates **only** against Keycloak (OIDC token endpoint). Player registration calls MissionManagement, which creates the user in Keycloak.

## Prerequisites

- Keycloak running at `http://localhost:8081` (Docker Compose).
- With `docker compose up`, the realm is **imported automatically** from `infra/keycloak/umbral-realm.json` (realm `umbral-realm`, client `umbral-player-mobile`, roles `player` / `operator`).

## Manual setup (only if you run Keycloak without Docker import)

1. Create realm **umbral-realm**.
2. Client **umbral-player-mobile**: public, **Direct access grants** ON.
3. Realm roles **player** and **operator**.

## Test flow

1. Start infrastructure: `docker compose up -d db keycloak mission-management-service session-management-service`
2. Register in the app → creates player via API + Keycloak password.
3. Login → `POST .../protocol/openid-connect/token` with `grant_type=password`.
4. Team actions → SessionManagement with `Authorization: Bearer <access_token>`.

## Production note

Prefer Authorization Code + PKCE (`expo-auth-session`) instead of direct access grants. The current implementation uses the resource-owner password grant for parity with email/password screens and local Keycloak dev.
