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

Docker levanta la infraestructura (PostgreSQL, RabbitMQ, Keycloak) y los cuatro microservicios backend.

### Primera vez o tras cambios en el código

```bash
docker compose build
docker compose up -d
```

### Solo infraestructura + backend (sin reconstruir)

```bash
docker compose up -d
```

### Ver logs

```bash
docker compose logs -f
```

### Detener todo

```bash
docker compose down
```

### Detener y borrar volúmenes (reinicia la base de datos)

```bash
docker compose down -v
```

### Perfiles opcionales

| Perfil | Comando | Descripción |
|---|---|---|
| `frontend` | `docker compose --profile frontend up -d` | Levanta `umbral-web` en el puerto 3000 (requiere que exista la carpeta `umbral-web/`) |
| `tools` | `docker compose --profile tools up -d` | Levanta SonarQube en el puerto 9000 |

Ejemplo con backend y frontend:

```bash
docker compose --profile frontend up -d --build
```

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
| Keycloak | http://localhost:8081 (admin: `admin` / `admin`) |
| Frontend *(perfil frontend)* | http://localhost:3000 |
| SonarQube *(perfil tools)* | http://localhost:9000 |

> **Keycloak:** el realm `umbral-realm` se importa solo desde `infra/keycloak/umbral-realm.json`. El **primer** `docker compose up` puede tardar **2–3 minutos** en Keycloak (compilación Quarkus); espera `(healthy)` antes de levantar servicios que dependen de él, o usa: `docker compose up -d db mq keycloak` y luego el resto.
>
> **Admin de la app (realm `umbral-realm`):** `mission-management-service` crea al arrancar el usuario `admin@umbral.com` / `Admin123!` con rol `admin` (no es el usuario `admin` de la consola de Keycloak). Si recreas el contenedor de Keycloak sin volumen, reinicia también `mission-management-service` o espera ~1 min (bootstrap periódico).

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
docker compose build admin-service
docker compose up -d admin-service

# Ver estado de los contenedores
docker compose ps

# Entrar al contenedor de PostgreSQL
docker exec -it umbral-db psql -U postgres -d umbral_db
```

---

## Documentación adicional

- Reglas de negocio: [`docs/reglas_negocio.md`](docs/reglas_negocio.md)
