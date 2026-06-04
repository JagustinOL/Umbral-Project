# Keycloak (desarrollo local)

## Import del realm

Con `docker compose up`, Keycloak arranca en modo desarrollo y importa `umbral-realm.json` desde este directorio (`--import-realm`).

El usuario de aplicación `admin@umbral.com` **no** va en el JSON: lo crea `mission-management-service` al arrancar (`KeycloakBootstrapHostedService`).

## Arranque lento en dev

- `start-dev` en la imagen oficial: primer arranque ~2–3 min (Quarkus augment).
- El volumen `keycloak-data` acelera reinicios posteriores.
- Keycloak no depende de PostgreSQL en el compose actual (solo usa almacenamiento dev del contenedor).

## Producción / arranque optimizado (opcional)

Para entornos no locales, Keycloak recomienda:

1. Base de datos externa (`KC_DB=postgres`, etc.).
2. `kc.sh build` con las opciones necesarias.
3. `start --optimized` en lugar de `start-dev`.

Eso requiere un `Dockerfile` propio y no está en el `docker-compose.yml` raíz para mantener el setup de desarrollo simple. Ver [documentación de Keycloak](https://www.keycloak.org/server/configuration) y la sección Docker del [README](../../README.md) del monorepo.
