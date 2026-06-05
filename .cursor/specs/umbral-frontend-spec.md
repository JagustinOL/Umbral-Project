# Frontend Spec

## Player Mobile (`PlayerMobile/`)

Expo Router app for player auth and team workspace (HU-27–HU-35).

- **Auth:** Keycloak OIDC (token endpoint + refresh). Register via `POST /api/v1/players` (Keycloak provisioning on server).
- **Teams:** SessionManagement REST API only (no client-side mock).
- **JWT:** Stored in SecureStore; sent as Bearer on protected calls.

See `PlayerMobile/README.md` and `PlayerMobile/docs/KEYCLOAK_SETUP.md`.
