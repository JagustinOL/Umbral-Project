# Umbral-Project

Proyecto de Desarrollo de Software Grupo 7

- Daniel Pérez
- José Ojeda

---

# Umbral — Monorepo

Plataforma para la operación en tiempo real de experiencias de investigación interactivas inmersivas (UCAB 2026).

| Componente | Tecnología | Carpeta |
|---|---|---|
| Admin API | C# / ASP.NET Core 10 | `AdminService/` |
| Identity API | C# / ASP.NET Core 10 | `IdentityService/` |
| Teams API | C# / ASP.NET Core 10 | `TeamService/` |
| Sessions API | C# / ASP.NET Core 10 | `SessionsManagement/` |
| Web | Next.js + Tailwind | `umbral-web/` |

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

Por defecto Compose levanta **infraestructura** (PostgreSQL, RabbitMQ, Keycloak, pgAdmin) y **tres microservicios backend** (.NET). Las apps Next.js (Admin, Operador, Login) usan el perfil `frontend`.

### ¿Qué tarda en `build` vs `up`?

| Fase | ¿Incluye Keycloak? | Qué construye / arranca | Tiempo típico (referencia) |
|---|---|---|---|
| `docker compose build` | **No** (imagen prepublicada) | 3 imágenes .NET (`dotnet restore` + `publish`) | ~2–8 min según caché |
| `docker compose build --profile frontend` | No | Lo anterior + 3 Next.js (`npm install` + `build`) | +5–15 min |
| `docker compose up -d` | **Sí** (pull + arranque) | Keycloak `start-dev` + healthcheck | **2–3 min** primer arranque; ~30–90 s con volumen `keycloak-data` ya caliente |
| `mission-management-service` | Tras Keycloak `healthy` | Bootstrap OIDC + usuario `admin@umbral.com` | Segundos tras arrancar el servicio |

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
docker compose up -d mission-management-service session-management-service scoring-audit-service
```

### Con frontends (perfil `frontend`)

```bash
docker compose --profile frontend build
docker compose --profile frontend up -d
```

Puertos: Admin `3000`, Operador `3001`, Login `3002`.

### Ver logs

```bash
docker compose logs -f
docker compose logs mission-management-service | Select-String -Pattern "default admin|Keycloak OIDC"
```

### Verificar login del admin de la app

Usuario en realm `umbral-realm` (creado por `mission-management-service`, no el `admin` de la consola):

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

Tras `-v`, el primer `up` de Keycloak vuelve a tardar ~2–3 min; reinicia `mission-management-service` o espera ~1 min al bootstrap periódico.

### Perfiles Compose

| Perfil | Comando | Descripción |
|---|---|---|
| *(ninguno)* | `docker compose up -d` | Infra + 3 backends |
| `frontend` | `docker compose --profile frontend up -d` | Añade AdminView, OperadorView y LoginView |

---

## 3. URLs del entorno local (Docker)

| Servicio | URL |
|---|---|
| Admin API | http://localhost:5149 |
| Identity API | http://localhost:5049 |
| Team API | http://localhost:5180 |
| Sessions API | http://localhost:5278 |
| OpenAPI (dev) | `http://localhost:<puerto>/openapi/v1.json` |
| PostgreSQL | `localhost:5432` (user: `postgres`, pass: `postgres`, db: `umbral_db`) |
| pgAdmin | http://localhost:5050 (`admin@umbral.com` / `admin`) |
| RabbitMQ (AMQP) | `localhost:5672` |
| RabbitMQ (panel) | http://localhost:15672 (`guest` / `guest`) |
| Keycloak (consola `master`) | http://localhost:8081 (`admin` / `admin`) |
| Mission Management API | http://localhost:5260 |
| Scoring Audit API | http://localhost:5290 |
| AdminView *(perfil frontend)* | http://localhost:3000 |
| OperadorView *(perfil frontend)* | http://localhost:3001 |
| LoginView *(perfil frontend)* | http://localhost:3002 |

> **Keycloak:** el realm `umbral-realm` se importa desde `infra/keycloak/umbral-realm.json`. Modo `start-dev` en desarrollo: primer arranque ~2–3 min (Quarkus augment). Keycloak ya no espera a PostgreSQL (no usa `db` en dev). `mission-management-service` arranca cuando Keycloak está `healthy`.
>
> **Admin de la app (realm `umbral-realm`):** al arrancar, `mission-management-service` crea o actualiza `admin@umbral.com` / `Admin123!` con rol `admin` vía `KeycloakBootstrapHostedService` (variables `Keycloak__DefaultAdmin*` en `docker-compose.yml`). No confundir con el usuario `admin` de la consola Keycloak (`master`).
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
# AdminService
cd AdminService/AdminService.WebApi
dotnet run

# IdentityService
cd IdentityService/IdentityService.WebApi
dotnet run

# TeamService
cd TeamService/TeamService.WebApi
dotnet run

# SessionsManagement
cd SessionsManagement/SessionManagement.WebApi
dotnet run
```

Los puertos locales coinciden con los definidos en cada `launchSettings.json` (5149, 5049, 5180 y 5278).

---

## 5. Ejecutar el frontend (`umbral-web`)

Con la carpeta `umbral-web/` ya creada en la raíz del monorepo:

```bash
cd umbral-web
npm install
npm run dev
```

La app quedará disponible en http://localhost:3000.

Variables de entorno esperadas (puedes definirlas en `.env.local`):

```env
NEXT_PUBLIC_ADMIN_API_URL=http://localhost:5149
NEXT_PUBLIC_IDENTITY_API_URL=http://localhost:5049
NEXT_PUBLIC_TEAM_API_URL=http://localhost:5180
NEXT_PUBLIC_SESSIONS_API_URL=http://localhost:5278
NEXT_PUBLIC_KEYCLOAK_URL=http://localhost:8081
```

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

### Generar informe

Desde la raíz del repositorio (PowerShell):

```powershell
.\scripts\run-coverage.ps1
```

Abre `coverage-report/index.html` para ver el detalle por ensamblado, clase y línea.

### Verificar umbral ≥ 90%

```powershell
.\scripts\check-coverage-threshold.ps1
```

Con la batería actual, el script debe terminar con `OK` y **90,0%** de líneas cubribles en Domain + Application (p. ej. 2699/2996 según `coverage-report/Summary.txt`).

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
