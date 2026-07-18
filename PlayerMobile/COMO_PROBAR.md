# Cómo probar la app móvil (UMBRAL Player)

Guía paso a paso con Docker + Keycloak + APIs reales.

---

## 1. Qué es el archivo `.env`

Expo lee variables que empiezan por `EXPO_PUBLIC_` desde **`PlayerMobile/.env`** (ya está creado en el proyecto).

| Variable | Para qué sirve |
|----------|----------------|
| `EXPO_PUBLIC_KEYCLOAK_URL` | Login/registro (token JWT) |
| `EXPO_PUBLIC_KEYCLOAK_REALM` | Realm (`umbral-realm`) |
| `EXPO_PUBLIC_KEYCLOAK_CLIENT_ID` | Cliente móvil (`umbral-player-mobile`) |
| `EXPO_PUBLIC_USER_API_URL` | Auth IAM y jugadores (`/api/v1/players`, etc.) |
| `EXPO_PUBLIC_MISSION_API_URL` | Rutas de misión vía gateway (mismo host que abajo) |
| `EXPO_PUBLIC_SESSION_API_URL` | Equipos, solicitudes, sesiones live |

**No hace falta copiar nada a mano** si usas PC + navegador o simulador: los valores apuntan a `localhost` y coinciden con los puertos de Docker.

### Si pruebas en teléfono físico (Expo Go)

El móvil no ve `localhost` de tu PC. En `.env` sustituye `localhost` por la **IPv4 de tu PC** (en PowerShell: `ipconfig`, busca algo como `192.168.1.50`):

```env
EXPO_PUBLIC_KEYCLOAK_URL=http://192.168.1.50:8081
EXPO_PUBLIC_USER_API_URL=http://192.168.1.50:5200
EXPO_PUBLIC_MISSION_API_URL=http://192.168.1.50:5200
EXPO_PUBLIC_SESSION_API_URL=http://192.168.1.50:5200
```

Reinicia Expo después de cambiar `.env` (`r` en la terminal o cierra y `npm start`).

### Si usas emulador Android

Usa `10.0.2.2` en lugar de `localhost` (ver comentarios en `.env.example`).

---

## 2. Levantar el entorno

### Opción A — Todo en Docker (recomendado para navegador)

En la **raíz del repo** (`Umbral-Project`):

```powershell
docker compose --profile full up -d --build
```

Levanta infra, backends, Admin/Operador/Login y PlayerMobile en modo web. Abre http://localhost:19000 (no hace falta `npm install` en `PlayerMobile/`).

Solo backends + Player web:

```powershell
docker compose --profile player up -d --build
```

### Opción B — Expo Go / emulador nativo (Metro en el host)

Desde la raíz:

```powershell
.\scripts\dev-expo-go.ps1
```

El script levanta Docker (backends) y luego `npm start` en `PlayerMobile/`. Requiere Node.js LTS instalado.

### Opción C — Manual (solo backends en Docker)

```powershell
docker compose up -d db mq keycloak user-service mission-management-service session-management-service api-gateway
```

La primera vez compila las imágenes .NET (puede tardar varios minutos).

Comprobar que están arriba:

```powershell
docker compose ps
```

URLs útiles:

| Servicio | URL |
|----------|-----|
| Keycloak | http://localhost:8081 |
| API Gateway (clientes) | http://localhost:5200 |
| MissionManagement *(directo)* | http://localhost:5260 |
| SessionManagement *(directo)* | http://localhost:5278 |

Keycloak importa solo el realm **`umbral-realm`** y el cliente **`umbral-player-mobile`** desde `infra/keycloak/umbral-realm.json` (ya no hace falta crearlos a mano en la consola).

Consola Keycloak (opcional): http://localhost:8081 — usuario `admin` / `admin`.

**Usuarios jugador:** no aparecen en el realm `master`. En el desplegable superior izquierdo elige **`umbral-realm`** → Users.

---

## 3. Instalar y arrancar la app móvil (solo si usas Opción B o C)

Si usaste **Opción A** (`--profile full` o `--profile player`), la app web ya corre en http://localhost:19000; salta esta sección.

Con **Opción B** (`scripts/dev-expo-go.ps1`) o **Opción C**, la app React Native se ejecuta en tu PC con Node.js.

### Requisito: Node.js

Si `npm` no se reconoce, instala Node LTS: https://nodejs.org o `winget install OpenJS.NodeJS.LTS`, cierra y abre PowerShell.

```powershell
cd PlayerMobile
npm install
npm start
```

> **Puerto 8081:** Keycloak ya lo usa. El script `npm start` usa el puerto **19000** para Metro/Expo.

Abrir en el navegador directamente:

```powershell
npm run start:web
```

Luego abre http://localhost:19000 en el navegador (o pulsa **`w`** en la terminal de Expo).

En la terminal de Expo:

- **`w`** — abrir en navegador (forma más rápida de probar en PC).
- **`a`** — emulador Android (ajusta `.env` con `10.0.2.2` si hace falta).
- Escanea el QR con **Expo Go** en el móvil (ajusta `.env` con la IP de tu PC).

---

## 4. Flujo de prueba recomendado

### A. Registrar jugador 1 (futuro líder)

1. Pantalla **Register**
2. Datos de ejemplo:
   - First name: `Ana`
   - Last name: `Líder`
   - Email: `ana.lider@umbral.com`
   - Password: `SecurePass1` (mínimo 8 caracteres)
3. Tras registro → pantalla **sin equipo**
4. **Create Team** → nombre: `Equipo Alfa` → entras al **dashboard**
5. Anota el **código de 6 caracteres** (tap para copiar)

### B. Registrar jugador 2 (solicitud de unión)

1. Cierra sesión o usa otro navegador / incógnito / otro dispositivo
2. Registra:
   - Email: `bob.miembro@umbral.com`
   - Password: `SecurePass1`
3. **Join Team** → pega el código de 6 caracteres → enviar solicitud

### Restaurar equipo tras login

La app llama a `GET /api/v1/players/{playerId}/team-membership` al iniciar sesión para saber si el jugador sigue en un equipo.

### Consultar el equipo en Postman (HU-29)

No existe `GET /api/v1/teams` (listado). Solo **consulta por id**:

1. En la app, en el dashboard, copia el **TEAM ID** (tap en el bloque gris).
2. En Postman, variable de colección `teamId` = ese GUID.
3. Ejecuta **Consultar Equipo (HU-29)**: `GET {{sessionManagementUrl}}/api/v1/teams/{{teamId}}`

Si creas el equipo desde Postman (`POST /api/v1/teams`), el script de Tests guarda `teamId` automáticamente en la respuesta `201` (`body.id`).

### C. Aprobar como líder

1. Vuelve a sesión de `ana.lider@umbral.com`
2. En el dashboard, sección **Pending Requests (Leader)** → **Approve**
3. En la sesión de Bob, vuelve a la app o refresca: debería pasar al dashboard del equipo

### D. Probar RN-13 (equipo bloqueado)

Con el operador y una sesión live activa, `IsLocked` vendrá del backend. Mientras tanto puedes ver el badge **LOCKED / UNLOCKED** en el dashboard según el estado del equipo en API.

---

## 5. Si algo falla

| Síntoma | Qué revisar |
|---------|-------------|
| `Invalid email or password` tras registro | Keycloak arriba; realm `umbral-realm`; cliente `umbral-player-mobile` con **Direct access grants** ON |
| Error de red al registrar | `mission-management-service` en `docker compose ps`; URL en `.env` |
| CORS / registro solo falla en **navegador (web)** | El bundler web suele usar `:8082` (Keycloak ocupa `:8081`). En Development las APIs permiten cualquier `http://localhost:*`. Tras cambiar CORS en el backend: `docker compose build mission-management-service session-management-service` y `docker compose up -d` esos servicios. En Expo Go o emulador nativo no aplica CORS. |
| Error al crear equipo | `session-management-service`; logs: `docker compose logs session-management-service` |
| Expo no ve el `.env` | Reinicia `npm start` desde `PlayerMobile/` |
| Teléfono no conecta | IP de la PC en `.env`, mismo WiFi, firewall permite 5200, 8081 |

Ver logs:

```powershell
docker compose logs -f mission-management-service
docker compose logs -f session-management-service
docker compose logs -f keycloak
```

---

## 6. Resumen en una línea

```powershell
# Todo el stack (navegador, sin Node local en PlayerMobile)
docker compose --profile full up -d --build
# Abre http://localhost:19000 y registra ana.lider@umbral.com / SecurePass1
```

```powershell
# Expo Go / emulador (Metro en el host)
.\scripts\dev-expo-go.ps1
```

```powershell
# Solo backends + app manual
docker compose up -d db mq keycloak user-service mission-management-service session-management-service api-gateway
cd PlayerMobile
npm install
npm start
```
