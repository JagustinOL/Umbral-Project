# Umbral-Project

Proyecto de Desarrollo de Software Grupo 7

- Daniel Pérez
- José Ojeda

---

# Umbral — Monorepo

Plataforma para la operación en tiempo real de experiencias de investigación interactivas inmersivas (UCAB 2026).

| Componente | Tecnología | Carpeta |
|---|---|---|
| API Gateway (YARP) | C# / ASP.NET Core 10 | `ApiGateway/` — reverse proxy para clientes |
| Mission Management API | C# / ASP.NET Core 10 | `MissionManagement/` |
| Session Management API | C# / ASP.NET Core 10 | `SessionManagement/` |
| Scoring Audit API | C# / ASP.NET Core 10 | `ScoringAudit/` |
| UserService | C# | `UserService/` — IAM, auth y usuarios |
| Admin (Next.js) | Next.js + Tailwind | `AdminView-WEB/` |
| Operador (Next.js) | Next.js + Tailwind | `OperadorView-WEB/` |
| Login (Next.js) | Next.js + Tailwind | `LoginView-WEB/` |
| Jugador (Expo web) | React Native / Expo | `PlayerMobile/` |

---

## Prerrequisitos

| Herramienta | Versión mínima | Verificar con |
|---|---|---|
| Git | 2.x | `git --version` |
| Docker Desktop | 24+ | `docker --version` |
| Docker Compose | v2+ | `docker compose version` |
| .NET SDK | 10.x | `dotnet --version` |
| ReportGenerator *(cobertura)* | global tool | `dotnet tool install --global dotnet-reportgenerator-globaltool` |
| Node.js *(solo frontend)* | 20 LTS | `node -v` |
| npm *(solo frontend)* | 10+ | `npm -v` |

---

## 1. Clonar el repositorio

```bash
git clone git@github.com:JagustinOL/Umbral-Project.git
cd Umbral-Project
```

---

## 2. Ejecutar con Docker (recomendado)

Por defecto Compose levanta **infraestructura** (PostgreSQL, RabbitMQ, Keycloak, pgAdmin) y **tres microservicios backend** (.NET). Las apps Next.js (Admin, Operador, Login) y PlayerMobile web usan perfiles Compose.

### Arranque completo (recomendado)

Un solo comando levanta infra, backends, las tres UIs Next.js y PlayerMobile en modo web:

```bash
docker compose --profile full up -d --build
```

Puertos: Admin `3000`, Operador `3001`, Login `3002`, Player web `19000`.

### ¿Qué tarda en `build` vs `up`?

| Fase | ¿Incluye Keycloak? | Qué construye / arranca | Tiempo típico (referencia) |
|---|---|---|---|
| `docker compose build` | **No** (imagen prepublicada) | 4 imágenes .NET (`dotnet restore` + `publish`) | ~2–8 min según caché |
| `docker compose build --profile frontend` | No | Lo anterior + 3 Next.js (`npm install` + `build`) | +5–15 min |
| `docker compose build --profile full` | No | Lo anterior + PlayerMobile (`npm ci` + Expo web) | +2–5 min |
| `docker compose up -d` | **Sí** (pull + arranque) | Keycloak `start-dev` + healthcheck | **2–3 min** primer arranque; ~30–90 s con volumen `keycloak-data` ya caliente |
| `user-service` | Tras Keycloak `healthy` | Bootstrap OIDC + usuario `admin@umbral.com` en Keycloak y sincronización en tabla `users` | Segundos tras arrancar el servicio |

Keycloak **no tiene Dockerfile** en este repo: la lentitud que parece de “build” suele ser la suma de compilar frontends/backends y, acto seguido, el warmup de Keycloak en el `up`.

### Backend (recomendado para APIs / depuración)

Primera vez o tras cambios en código backend:

```bash
docker compose build
docker compose up -d
```

Sin reconstruir:

```bash
docker compose up -d
```

### Arranque en dos fases (Keycloak primero)

Útil si quieres levantar backends solo cuando Keycloak ya está listo:

```bash
docker compose up -d db mq keycloak
docker compose ps   # esperar keycloak (healthy)
docker compose up -d user-service
docker compose up -d user-service mission-management-service session-management-service scoring-audit-service api-gateway
```

### Con frontends (perfil `frontend`)

```bash
docker compose --profile frontend build
docker compose --profile frontend up -d
```

Puertos: Admin `3000`, Operador `3001`, Login `3002`.

### Player en navegador (perfil `player` o `full`)

Solo PlayerMobile web en Docker (sin Node local):

```bash
docker compose --profile player up -d --build
```

Incluido automáticamente en `--profile full` (ver arriba). URL: http://localhost:19000

### Expo Go en teléfono físico o emulador nativo

Metro debe correr en el host (no dentro de Docker). Desde la raíz del repo:

```powershell
.\scripts\dev-expo-go.ps1
```

En Linux/macOS: `chmod +x scripts/dev-expo-go.sh && ./scripts/dev-expo-go.sh`

Ajusta `PlayerMobile/.env` con la IP de tu PC o `10.0.2.2` (emulador Android). Ver [`PlayerMobile/COMO_PROBAR.md`](PlayerMobile/COMO_PROBAR.md).

### Ver logs

```bash
docker compose logs -f
docker compose logs user-service | Select-String -Pattern "default admin|Keycloak"
```

### Verificar login del admin de la app

Usuario en realm `umbral-realm` (creado por `user-service`, no el `admin` de la consola):

```powershell
# En PowerShell usar curl.exe (curl es alias de Invoke-WebRequest)
curl.exe -s -X POST "http://localhost:8081/realms/umbral-realm/protocol/openid-connect/token" `
  -d "grant_type=password" -d "client_id=umbral-web" `
  -d "username=admin@umbral.com" -d "password=Admin123!"
```

Debe devolver JSON con `access_token`.

### Detener todo

```bash
docker compose down
```

### Detener y borrar volúmenes (reinicia DB y Keycloak)

```bash
docker compose down -v
```

Tras `-v`, el primer `up` de Keycloak vuelve a tardar ~2–3 min; reinicia `user-service` o espera ~1 min al bootstrap periódico.

### Perfiles Compose

| Perfil | Comando | Descripción |
|---|---|---|
| *(ninguno)* | `docker compose up -d` | Infra + 4 backends |
| `frontend` | `docker compose --profile frontend up -d` | Añade AdminView, OperadorView y LoginView |
| `player` | `docker compose --profile player up -d` | Añade PlayerMobile web (`:19000`) |
| `full` | `docker compose --profile full up -d --build` | **Todo** el stack de desarrollo (recomendado) |

---

## 3. URLs del entorno local (Docker)

| Servicio | URL |
|---|---|
| **API Gateway** *(clientes web/jugador)* | http://localhost:5200 |
| User Service API *(directo, depuración)* | http://localhost:5284 |
| Mission Management API *(directo, depuración)* | http://localhost:5260 |
| Session Management API *(directo, depuración)* | http://localhost:5278 |
| Scoring Audit API *(directo, depuración)* | http://localhost:5290 |
| OpenAPI (dev) | `http://localhost:<puerto>/openapi/v1.json` |
| PostgreSQL | `localhost:5432` (user: `postgres`, pass: `postgres`, db: `umbral_db`) |
| pgAdmin | http://localhost:5050 (`admin@umbral.com` / `admin`) |
| RabbitMQ (AMQP) | `localhost:5672` |
| RabbitMQ (panel) | http://localhost:15672 (`guest` / `guest`) |
| Keycloak (consola `master`) | http://localhost:8081 (`admin` / `admin`) |
| AdminView *(perfil frontend/full)* | http://localhost:3000 |
| OperadorView *(perfil frontend/full)* | http://localhost:3001 |
| LoginView *(perfil frontend/full)* | http://localhost:3002 |
| MailHog (SMTP UI — códigos de activación) | http://localhost:8025 |
| PlayerMobile web *(perfil player/full)* | http://localhost:19000 |

> **API Gateway (YARP):** AdminView, OperadorView, LoginView y PlayerMobile deben apuntar todas sus variables `*_API_URL` a `http://localhost:5200`. El gateway enruta por path hacia UserService, MissionManagement, SessionManagement y ScoringAudit. Los microservicios se comunican entre sí por red interna de Docker (sin pasar por el gateway).
>
> **Keycloak:** el realm `umbral-realm` se importa desde `infra/keycloak/umbral-realm.json`. Modo `start-dev` en desarrollo: primer arranque ~2–3 min (Quarkus augment). Keycloak ya no espera a PostgreSQL (no usa `db` en dev). Los microservicios backend arrancan cuando Keycloak está `healthy`.
>
> **Admin de la app (realm `umbral-realm`):** al arrancar, `user-service` crea o actualiza `admin@umbral.com` / `Admin123!` con rol `admin` en Keycloak (`KeycloakBootstrapHostedService`) y lo registra en la tabla `users` de PostgreSQL (`DefaultAdminDirectoryBootstrapHostedService`). Variables `Keycloak__DefaultAdmin*` en `docker-compose.yml`. No confundir con el usuario `admin` de la consola Keycloak (`master`).
>
> **Keycloak más rápido (opcional, producción):** para arranques repetidos más cortos se puede usar imagen custom con `kc.sh build` + `start --optimized` y BD externa; no está en el compose actual para mantener el setup dev simple.

---

## 4. Ejecutar microservicios sin Docker

Útil para depurar un servicio concreto en el IDE. La infraestructura (DB, MQ, Keycloak) debe estar corriendo con Docker:

```bash
docker compose up -d db mq keycloak pgadmin
```

Luego, en cada carpeta de servicio:

```bash
# MissionManagement (misiones, operadores, jugadores, auth)
cd MissionManagement/MissionManagement.WebApi
dotnet run

# SessionManagement (equipos, sesiones live, evidencias)
cd SessionManagement/SessionManagement.WebApi
dotnet run

# ScoringAudit (ranking por sesión)
cd ScoringAudit/ScoringAudit.WebApi
dotnet run

# UserService (IAM, auth)
cd UserService/UserService.WebApi
dotnet run

# API Gateway (YARP — requiere los cuatro servicios anteriores accesibles)
cd ApiGateway/Umbral.ApiGateway
dotnet run
```

Los clientes web/jugador usan el **gateway en `:5200`**. Los puertos **5260**, **5278**, **5290** y **5284** quedan para depuración directa de cada microservicio. Con `dotnet run`, los puertos vienen de cada `launchSettings.json`; alinea `ReverseProxy` en `ApiGateway/Umbral.ApiGateway/appsettings.json` si usas puertos distintos.

---

## 5. Ejecutar frontends sin Docker

Con los backends accesibles (Docker o `dotnet run`), levanta cada app Next.js en su carpeta:

```bash
# Admin (puerto 3000)
cd AdminView-WEB && npm install && npm run dev

# Operador (puerto 3001 — usar npm run dev -- -p 3001)
cd OperadorView-WEB && npm install && npm run dev -- -p 3001

# Login (puerto 3002 — usar npm run dev -- -p 3002)
cd LoginView-WEB && npm install && npm run dev -- -p 3002
```

Variables de entorno esperadas (`.env.local` en cada app; mismos nombres que en `docker-compose.yml`):

```env
NEXT_PUBLIC_USER_API_URL=http://localhost:5200
NEXT_PUBLIC_MISSION_API_URL=http://localhost:5200
NEXT_PUBLIC_SESSION_API_URL=http://localhost:5200
NEXT_PUBLIC_SCORING_API_URL=http://localhost:5200
NEXT_PUBLIC_KEYCLOAK_URL=http://localhost:8081
NEXT_PUBLIC_LOGIN_URL=http://localhost:3002
```

LoginView además usa `NEXT_PUBLIC_ADMIN_URL` y `NEXT_PUBLIC_OPERATOR_URL`. OperadorView puede usar `NEXT_PUBLIC_OPERATOR_ID` como fallback si no entras por LoginView.

PlayerMobile en web local: ver [`PlayerMobile/COMO_PROBAR.md`](PlayerMobile/COMO_PROBAR.md) (`EXPO_PUBLIC_MISSION_API_URL`, `EXPO_PUBLIC_SESSION_API_URL`, etc.).

---

## 6. Comandos útiles

```bash
# Reconstruir un solo microservicio
docker compose build mission-management-service
docker compose up -d mission-management-service

# Ver estado de los contenedores
docker compose ps

# Entrar al contenedor de PostgreSQL
docker exec -it umbral-db psql -U postgres -d umbral_db
```

---

## 7. Cobertura de pruebas unitarias

Mide la cobertura de **líneas** sobre los ensamblados `*.Domain` y `*.Application` de **MissionManagement** y **SessionManagement**, ejecutando solo la batería de **pruebas unitarias** (`*Domain.Tests` y `*Application.Tests` con xUnit y dependencias simuladas).

No incluye integración, E2E, WebApi ni Infrastructure. El proyecto `SessionManagement.Infrastructure.Tests` se ejecuta aparte y **no** cuenta para esta métrica.

### Herramientas globales requeridas

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global coverlet.console
```

### Generar informe

Desde la raíz del repositorio (PowerShell):

```powershell
.\scripts\run-coverage.ps1
```

El script compila, copia los ensamblados a `%TEMP%` (mitiga bloqueos de Windows Application Control sobre DLLs recién compiladas) y genera Cobertura con `coverlet.console`.

Abre `coverage-report/index.html` para ver el detalle por ensamblado, clase y línea.

### Verificar umbral ≥ 90%

```powershell
.\scripts\check-coverage-threshold.ps1
```

Con la batería actual, el script debe terminar con `OK` y **≥ 90%** de líneas cubribles **en total y en cada ensamblado** Domain/Application de MissionManagement y SessionManagement (p. ej. total **92,1%** / 3931/4265 según `coverage-report/Summary.txt`).

Para regenerar tests e informe en un solo paso:

```powershell
.\scripts\check-coverage-threshold.ps1 -RunTests
```

### Pruebas de infraestructura (fuera del informe)

```powershell
dotnet test SessionManagement/SessionManagement.Infrastructure.Tests/SessionManagement.Infrastructure.Tests.csproj
```

### Evidencia para memoria del proyecto

Captura o PDF de `coverage-report/index.html` y la salida de `check-coverage-threshold.ps1` cuando muestre `OK`.

---

## Documentación adicional

- Reglas de negocio: [`docs/reglas_negocio.md`](docs/reglas_negocio.md)
- Catálogo de endpoints REST: [`backend-endpoints.md`](backend-endpoints.md)
- Mapa de arquitectura backend: [`architecture-map.md`](architecture-map.md)
