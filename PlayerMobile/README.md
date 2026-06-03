# UMBRAL Player Mobile

React Native (Expo) app for players (HU-27 to HU-35). **Authentication is via Keycloak.** Team and player data come from the backend APIs (no in-app mock store).

## Stack

- Expo Router
- Keycloak OIDC (password grant + refresh token)
- MissionManagement `POST/GET /api/v1/players`
- SessionManagement ` /api/v1/teams/*`

## Setup

**Guía completa en español:** [COMO_PROBAR.md](COMO_PROBAR.md)

1. Levantar Docker (raíz del repo): `docker compose up -d db mq keycloak mission-management-service session-management-service`
2. El archivo **`.env`** ya está listo en esta carpeta (no hace falta copiarlo para PC + Expo Web).
3. Install and run:

```bash
npm install
npm start
```

## Environment variables

| Variable | Description |
|----------|-------------|
| `EXPO_PUBLIC_KEYCLOAK_URL` | Keycloak base URL (default `http://localhost:8081`) |
| `EXPO_PUBLIC_KEYCLOAK_REALM` | Realm (default `umbral-realm`) |
| `EXPO_PUBLIC_KEYCLOAK_CLIENT_ID` | Public client (default `umbral-player-mobile`) |
| `EXPO_PUBLIC_KEYCLOAK_CLIENT_SECRET` | Optional, for confidential clients |
| `EXPO_PUBLIC_MISSION_API_URL` | MissionManagement (default `http://localhost:5260`) |
| `EXPO_PUBLIC_SESSION_API_URL` | SessionManagement (default `http://localhost:5278`) |

On **Android emulator**, replace `localhost` with `10.0.2.2`.

## Auth flow

1. **Register:** `POST /api/v1/players` → Keycloak user + role `player` → login via Keycloak token endpoint.
2. **Login:** Keycloak `grant_type=password` → JWT stored in SecureStore → profile from `GET /api/v1/players/{id}` (`sub` claim).
3. **API calls:** `Authorization: Bearer <access_token>` on SessionManagement endpoints.

## Team membership

- `teamId` is persisted after creating a team.
- After a join request, `pendingTeamId` is stored; the app polls membership via `GET /api/v1/teams/{id}` until the leader approves.

## Business rules

Enforced by **SessionManagement** domain (RN-13, RN-14, max 4 members). The app surfaces API error messages; no local mock validation.
