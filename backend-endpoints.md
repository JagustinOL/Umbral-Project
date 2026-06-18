# Catálogo de Endpoints UMBRAL

Documentación alineada con los controladores WebApi tras el refactor **Route + Body + Mapping**. Las URLs HTTP, verbos y cuerpos JSON públicos se mantienen salvo **Crear Pista (HU-17)**, que usa `application/json` (sin adjuntos).

## URLs base

| Servicio | Docker (`dotnet`/compose) | Variable Postman |
|----------|---------------------------|------------------|
| MissionManagement | `http://localhost:5260` | `{{missionManagementUrl}}` |
| SessionManagement | `http://localhost:5278` | `{{sessionManagementUrl}}` |
| ScoringAudit | `http://localhost:5290` | `{{scoringAuditUrl}}` |

Prefijo común: `/api/v1`.

## Autenticación

La mayoría de mutaciones y flujos de operador/jugador requieren **JWT Bearer** (`Authorization: Bearer {accessToken}`) emitido por Keycloak vía `POST /api/v1/auth/token`.

| Rol | Uso típico |
|-----|------------|
| `admin` | Crear operadores/jugadores/admins, misiones, nodos, pistas, asignaciones |
| `operator` | Sesiones live del operador (`/operators/{operatorId}/…`); el `operatorId` de ruta debe coincidir con el JWT salvo rol `admin` |
| `player` | Unirse a sesión, enviar respuestas, consultar etapa actual |

Endpoints `[AllowAnonymous]` documentados en cada sección (p. ej. login, listado de misiones, validaciones de integración, GET pistas por nodo).

## Convención WebApi → CQRS

Los controladores son delgados: enlazan parámetros de ruta/query en un **`record struct`** (`WebApi/Contracts/Routes/`), el body en `WebApi/Contracts/`, y delegan el ensamblaje a extensiones en `WebApi/Mapping/`.

```text
HTTP → Route struct + Body contract → ToCommand() / ToQuery() → MediatR → Application Handler
```

### Tabla de mapeo por microservicio

| Microservicio | Controlador | Route struct(s) | Mapper |
|---------------|-------------|-----------------|--------|
| MissionManagement | `AuthController` | — | `AuthMappings` |
| MissionManagement | `AdminsController` | — | `AdminMappings` |
| MissionManagement | `MissionsController` | `MissionRoute` (`Id`) | `MissionMappings` |
| MissionManagement | `NodesController` | `MissionNodesRoute`, `MissionStageRoute`, `MissionNodeRoute` | `NodeMappings` |
| MissionManagement | `TriviaController` | `MissionParentNodeRoute`, `MissionNodeRoute` | `TriviaMappings` |
| MissionManagement | `TreasureHuntsController` | `MissionParentNodeRoute`, `MissionNodeRoute` | `TreasureHuntMappings` |
| MissionManagement | `HintsController` | `HintParentRoute`, `HintRoute` | `HintMappings` |
| MissionManagement | `MissionOperatorsController` | `MissionNodesRoute`, `MissionOperatorRoute` | `MissionOperatorMappings` |
| MissionManagement | `OperatorsController` | `OperatorRoute` | `OperatorMappings` |
| MissionManagement | `PlayersController` | `PlayerRoute` | `PlayerMappings` |
| SessionManagement | `OperatorSessionsController` | `OperatorRoute`, `OperatorMissionRoute`, `OperatorSessionRoute` | `OperatorSessionMappings` |
| SessionManagement | `LiveSessionsController` | `LiveSessionTeamRoute` | `LiveSessionMappings` |
| SessionManagement | `PlayerHintsController` | `LiveSessionTeamNodeRoute` | — (servicio `IPlayerHintPanelService`) |
| SessionManagement | `TeamsController` | `TeamRoute`, `TeamActionRoute`, `TeamJoinRequestRoute`, `TeamMemberActionRoute` | `TeamMappings` |
| SessionManagement | `MissionSessionValidationController` | `MissionRoute` | `MissionSessionValidationMappings` |
| SessionManagement | `PlayerTeamMembershipController` | `PlayerRoute` | `PlayerTeamMembershipMappings` |
| ScoringAudit | `RankingController` | `SessionRoute` | `RankingMappings` |

En cada endpoint siguiente, **Capa Application** indica el Command/Query MediatR resultante del mapper.

---

## MissionManagement · Autenticación (Keycloak OIDC)

### Autenticar Usuario (Login)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/auth/token`
- **Capa Application:** `AuthenticateUserCommand`
- **Body / Payload (Request):**
  ```json
  {
    "username": "string (email)",
    "password": "string"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "accessToken": "string (JWT)",
    "refreshToken": "string",
    "expiresIn": 300,
    "userId": "Guid",
    "roles": ["admin", "operator", "player"]
  }
  ```
- **Errores esperados:**
  - `401 Unauthorized` si las credenciales son inválidas o la cuenta está deshabilitada.
  - `400 BadRequest` si faltan username o password.
  - `503 ServiceUnavailable` si Keycloak no está disponible.
- **Notas:**
  - El backend delega la validación de credenciales a Keycloak (`grant_type=password`, client `umbral-web`).
  - Los roles provienen de `realm_access.roles` del JWT emitido por Keycloak.
  - Usuarios admin deben tener el rol realm `admin`; operadores el rol `operator` (creados vía `POST /api/v1/operators`).
  - En desarrollo, el bootstrap crea `admin@umbral.com` / `Admin123!` si `Keycloak:DefaultAdminEmail` está configurado.

### Activar cuenta de Operador (onboarding)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/auth/operator/setup-password`
- **Capa Application:** `SetupOperatorPasswordCommand`
- **Autenticación:** `[AllowAnonymous]`
- **Body / Payload (Request):**
  ```json
  {
    "email": "string",
    "setupCode": "string (formato XXXX-XXXX)",
    "password": "string"
  }
  ```
- **Response (200 OK):** mismo shape que `POST /api/v1/auth/token` (JWT + roles).
- **Errores esperados:**
  - `404 NotFound` si el email no existe o no corresponde a un operador (mensaje genérico).
  - `409 Conflict` si la cuenta ya está activa o fue desactivada por un administrador (tiene contraseña).
  - `400 BadRequest` si el código es inválido o expiró, o la contraseña no cumple validación.
  - `503 ServiceUnavailable` si Keycloak no está disponible.
- **Notas:**
  - Solo aplica a operadores creados sin contraseña (`Enabled: false` en Keycloak).
  - Tras éxito: se fija la contraseña, se habilita la cuenta y se eliminan los atributos del código de activación.
  - El operador puede iniciar sesión con `POST /api/v1/auth/token` a partir de entonces.

### Crear Administrador
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/admins`
- **Capa Application:** `CreateAdminCommand`
- **Body / Payload (Request):**
  ```json
  {
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "password": "string"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "Guid"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` cuando el correo ya existe en Keycloak.
  - `400 BadRequest` para contraseña menor a 8 caracteres.
- **Notas:**
  - Tras crear el usuario en Keycloak se asigna el rol realm `admin` y se establece la contraseña vía Admin API.

## MissionManagement · Épica 1 (Misiones y Etapas)

### Crear Misión (HU-01)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions`
- **Capa Application:** `CreateMissionCommand`
- **Body / Payload (Request):**
  ```json
  {
    "title": "string",
    "description": "string",
    "difficulty": "int",
    "maxDurationMinutes": "int?"
  }
  ```

### Consultar Misiones (HU-02)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions`
- **Capa Application:** `GetMissionsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  [
    {
      "id": "Guid",
      "title": "string",
      "description": "string",
      "status": "Draft|Active|Inactive",
      "difficulty": "Easy|Medium|Hard",
      "maxDurationMinutes": 0,
      "createdAtUtc": "2026-01-01T00:00:00Z",
      "lastModifiedAtUtc": "2026-01-01T00:00:00Z",
      "operatorIds": [
        "Guid"
      ]
    }
  ]
  ```

### Consultar Misión por Id (HU-02)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{id}`
- **Capa Application:** `GetMissionByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  {
    "id": "Guid",
    "title": "string",
    "description": "string",
    "status": "Draft|Active|Inactive",
    "difficulty": "Easy|Medium|Hard",
    "maxDurationMinutes": 0,
    "createdAtUtc": "2026-01-01T00:00:00Z",
    "lastModifiedAtUtc": "2026-01-01T00:00:00Z",
    "operatorIds": [
      "Guid"
    ]
  }
  ```

### Modificar Misión (HU-03)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{id}`
- **Capa Application:** `UpdateMissionDetailsCommand`
- **Body / Payload (Request):**
  ```json
  {
    "title": "string",
    "description": "string",
    "maxDurationMinutes": "int?"
  }
  ```

### Activar Misión
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{id}/activate`
- **Capa Application:** `ActivateMissionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (204 No Content)**
- **Errores esperados:**
  - `400 BadRequest` si la misión no tiene nodos, ya está `Active`, o está `Inactive` (no reactivable tras desactivar).
  - `404 NotFound` si la misión no existe.
- **Notas:**
  - Flujo recomendado: crear misión → etapas → juegos → **activar** → asignar operador → crear/iniciar sesión.

### Desactivar Misión (HU-04)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{id}`
- **Capa Application:** `DeactivateMissionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (204 No Content)**
- **Errores esperados:**
  - `409 Conflict` si la misión tiene sesiones abiertas (`Pending`, `Preparation`, `Active` o `Paused`) en SessionManagement (RN-01).
  - `404 NotFound` si la misión no existe.
- **Notas:**
  - Desactiva lógicamente la misión (`Inactive`); no es borrado físico.

### Crear Etapa (Nodo Raíz) (HU-05)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions/{missionId}/nodes`
- **Capa Application:** `AddRootNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "title": "string",
    "description": "string",
    "executionOrder": "int"
  }
  ```

### Consultar Etapas por Misión (HU-06)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/nodes`
- **Capa Application:** `GetNodesByMissionQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Listar Juegos de una Etapa
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/nodes/{stageId}/games`
- **Capa Application:** `GetGamesByStageQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  [
    {
      "id": "Guid",
      "nodeType": "Trivia | TreasureHunt",
      "executionOrder": 1,
      "baseScore": 50,
      "title": "string"
    }
  ]
  ```
- **Errores esperados:**
  - `404 NotFound` si la misión o la etapa no existen, o el nodo no es de tipo `Stage`.

### Modificar Etapa (HU-07)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}`
- **Capa Application:** `UpdateNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "title": "string",
    "description": "string"
  }
  ```

### Eliminar Etapa (HU-08)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}`
- **Capa Application:** `DeleteNodeCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

## MissionManagement · Épica 2 (Juegos y Pistas)

### Añadir Trivia (HU-09)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions/{missionId}/nodes/{parentNodeId}/trivia`
- **Capa Application:** `AddTriviaNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "questions": [
      {
        "prompt": "¿Capital de Venezuela?",
        "options": ["Caracas", "Valencia", "Maracaibo"],
        "correctOptionIndex": 0
      },
      {
        "prompt": "¿2 + 2?",
        "options": ["3", "4", "5"],
        "correctOptionIndex": 1
      }
    ],
    "executionOrder": 1
  }
  ```

### Consultar Trivia por Id (HU-10)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/nodes/{nodeId}/trivia`
- **Capa Application:** `GetTriviaNodeByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Modificar Trivia (HU-11)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}/trivia`
- **Capa Application:** `UpdateTriviaNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "questions": [
      {
        "prompt": "¿Capital de Venezuela?",
        "options": ["Caracas", "Valencia", "Maracaibo"],
        "correctOptionIndex": 0
      },
      {
        "prompt": "¿2 + 2?",
        "options": ["3", "4", "5"],
        "correctOptionIndex": 1
      }
    ]
  }
  ```

### Eliminar Trivia (HU-12)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}`
- **Capa Application:** `DeleteNodeCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Añadir Búsqueda del Tesoro (HU-13)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions/{missionId}/nodes/{parentNodeId}/treasure-hunts`
- **Capa Application:** `AddTreasureHuntNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "instructions": "string",
    "secretCode": "string",
    "destination": {
      "latitude": "double",
      "longitude": "double"
    },
    "executionOrder": 1
  }
  ```

### Consultar Búsqueda del Tesoro por Id (HU-14)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/nodes/{nodeId}/treasure-hunts`
- **Capa Application:** `GetTreasureHuntNodeByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Modificar Búsqueda del Tesoro (HU-15)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}/treasure-hunts`
- **Capa Application:** `UpdateTreasureHuntNodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "instructions": "string",
    "secretCode": "string",
    "destination": {
      "latitude": "double",
      "longitude": "double"
    }
  }
  ```

### Eliminar Búsqueda del Tesoro (HU-16)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}`
- **Capa Application:** `DeleteNodeCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Crear Pista (HU-17)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions/{missionId}/nodes/{nodeId}/hints`
- **WebApi:** `HintParentRoute` + `AddHintRequest` → `HintMappings.ToCommand()`
- **Capa Application:** `AddHintCommand`
- **Auth:** `admin`, `operator`
- **Content-Type:** `application/json`
- **Body / Payload (Request):**
  ```json
  {
    "content": "string"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "Guid"
  }
  ```
- **Notas:**
  - La pista es solo texto; no se admiten archivos adjuntos.
  - `nodeId` debe ser un nodo de juego (`Trivia` o `TreasureHunt`), no una `Stage`.

### Consultar Pistas por Nodo (HU-18)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/nodes/{nodeId}/hints`
- **Capa Application:** `GetHintsByNodeQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Modificar Pista (HU-19)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}/hints/{hintId}`
- **Capa Application:** `UpdateHintCommand`
- **Body / Payload (Request):**
  ```json
  {
    "content": "string"
  }
  ```

### Eliminar Pista (HU-20)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}/hints/{hintId}`
- **Capa Application:** `DeleteHintCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

## MissionManagement · Épica 3 (Gestión y Asignación de Operadores)

### Crear cuenta de Operador (HU-22)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/operators`
- **Capa Application:** `CreateOperatorCommand`
- **Body / Payload (Request):**
  ```json
  {
    "firstName": "string",
    "lastName": "string",
    "email": "string"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "Guid",
    "setupCode": "string (XXXX-XXXX, mostrado una sola vez)"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` cuando el correo ya existe en Keycloak.
  - `400 BadRequest` para payload inválido.
- **Notas:**
  - Crea el usuario en Keycloak con `Enabled: false`, rol `operator`, **sin contraseña**.
  - El código de activación se almacena hasheado en atributos de Keycloak; TTL por defecto 7 días (`Keycloak:OperatorSetupCodeTtlDays`).
  - El administrador debe entregar `setupCode` al operador de forma segura; el operador completa el onboarding vía `POST /api/v1/auth/operator/setup-password` o la pestaña **Activar cuenta** en LoginView-WEB.

### Consultar Operadores (HU-23)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/operators`
- **Capa Application:** `GetOperatorsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  [
    {
      "operatorId": "Guid",
      "firstName": "string",
      "lastName": "string",
      "email": "string",
      "isActive": true
    }
  ]
  ```
- **Notas:**
  - Si no existen operadores, retorna `[]`.
  - El listado se obtiene desde Keycloak (usuarios con rol `operator`).

### Desactivar Operador (HU-26)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/operators/{operatorId}/deactivate`
- **Capa Application:** `DeactivateOperatorCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Asignar Operador a Misión (HU-24)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/missions/{missionId}/operators`
- **Capa Application:** `AssignOperatorToMissionCommand`
- **Body / Payload (Request):**
  ```json
  {
    "operatorId": "Guid"
  }
  ```
- **Response (204 No Content)**
- **Errores esperados:**
  - `400 BadRequest` si el operador ya está asignado a la misión.
  - `404 NotFound` si la misión no existe.

### Revocar Operador de Misión (HU-25)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/operators/{operatorId}`
- **Capa Application:** `RevokeOperatorFromMissionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

## MissionManagement · Gestión de Jugadores (TeamMember / Keycloak)

### Crear Jugador
- **Microservicio:** MissionManagement
- **Método y Ruta:** `POST /api/v1/players`
- **Capa Application:** `CreatePlayerCommand`
- **Body / Payload (Request):**
  ```json
  {
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "password": "string"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "Guid"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` cuando el correo ya existe en Keycloak.
  - `400 BadRequest` para contraseña menor a 8 caracteres.
- **Notas:**
  - Tras crear el usuario en Keycloak se asigna el rol de jugador y se establece la contraseña vía Admin API (`reset-password`).

### Consultar Jugadores
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/players`
- **Capa Application:** `GetPlayersQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  [
    {
      "playerId": "Guid",
      "firstName": "string",
      "lastName": "string",
      "email": "string",
      "isActive": true
    }
  ]
  ```

### Consultar Jugador por Id
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/players/{playerId}`
- **Capa Application:** `GetPlayerByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  {
    "playerId": "Guid",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "isActive": true
  }
  ```
- **Errores esperados:**
  - `404 NotFound` si el jugador no existe.

### Modificar Jugador
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/players/{playerId}`
- **Capa Application:** `UpdatePlayerCommand`
- **Body / Payload (Request):**
  ```json
  {
    "firstName": "string",
    "lastName": "string",
    "email": "string"
  }
  ```
- **Response (204 No Content)**
- **Errores esperados:**
  - `404 NotFound` si el jugador no existe.
  - `409 Conflict` cuando el correo ya existe en Keycloak.

### Desactivar Jugador
- **Microservicio:** MissionManagement
- **Método y Ruta:** `PUT /api/v1/players/{playerId}/deactivate`
- **Capa Application:** `DeactivatePlayerCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (204 No Content)**
- **Errores esperados:**
  - `404 NotFound` si el jugador no existe.

## SessionManagement · Épica 4 (Gestión de Equipos e Integrantes)

### Consultar membresía de equipo del jugador
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/players/{playerId}/team-membership`
- **Capa Application:** `GetPlayerTeamMembershipQuery`
- **Body / Payload (Request):** ninguno.
- **Response (200 OK):**
  ```json
  {
    "isMember": true,
    "teamId": "Guid",
    "teamName": "string",
    "teamCode": "string",
    "role": "Leader|Member",
    "hasPendingJoinRequest": false,
    "pendingTeamId": null
  }
  ```
- **Notas:**
  - Si el jugador no pertenece a ningún equipo ni tiene solicitud pendiente, `isMember` y `hasPendingJoinRequest` son `false` y los demás campos son `null`.
  - Usar tras login para restaurar el workspace de equipo en la app móvil.

### Crear Equipo (HU-27)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/teams`
- **Capa Application:** `CreateTeamCommand`
- **Body / Payload (Request):**
  ```json
  {
    "name": "string",
    "creatorId": "Guid",
    "creatorDisplayName": "string (opcional)"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "Guid"
  }
  ```
- **Validación:** el `creatorId` no puede pertenecer ya a otro equipo activo ni tener otra solicitud de unión pendiente (409 Conflict).

### Consultar Equipo por Id (HU-29)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/teams/{teamId}`
- **Nota:** No hay `GET /api/v1/teams` (listado). Tras `POST` crear equipo, usar el `id` devuelto en `201` como `{teamId}` en Postman o en la app (dashboard → TEAM ID).
- **Capa Application:** `GetTeamByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):** incluye `isLocked` y `currentSessionRef` (Guid o null) para validar RN-13 en la app jugador.

### Modificar Equipo (HU-33)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/teams/{teamId}`
- **Capa Application:** `UpdateTeamCommand`
- **Body / Payload (Request):**
  ```json
  {
    "newName": "string",
    "requestorId": "Guid"
  }
  ```
- **Nota:** `requestorId` debe ser el `playerRef` del miembro con rol `Leader` (tras crear equipo, usar el mismo `creatorId`).

### Disolver Equipo (HU-34)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `DELETE /api/v1/teams/{teamId}?requestorId={guid}`
- **Capa Application:** `DisbandTeamCommand`
- **Query params:** `requestorId` (Guid, obligatorio) — jugador que solicita la disolución; debe ser el líder.
- **Body / Payload (Request):** ninguno.
- **Nota:** Tras crear equipo, usar el mismo `creatorId` como `requestorId` salvo que el liderazgo se haya delegado (consultar `GET /api/v1/teams/{teamId}` → miembro con `role: "Leader"`).

### Solicitar Unión a Equipo (HU-28/HU-35)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/teams/join-requests`
- **Capa Application:** `SubmitJoinRequestCommand`
- **Body / Payload (Request):**
  ```json
  {
    "teamCode": "string",
    "playerRef": "Guid",
    "displayName": "string"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "requestId": "Guid",
    "teamId": "Guid"
  }
  ```
- **Validación:** `playerRef` no puede estar en otro equipo activo ni tener solicitud pendiente en otro equipo (409 Conflict). Al aprobar (`PUT .../requests/{requestId}`), se vuelve a validar que el solicitante siga sin equipo.

### Consultar Solicitudes Pendientes de Equipo (HU-32)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/teams/{teamId}/requests?requestorId={guid}`
- **Capa Application:** `GetPendingRequestsQuery`
- **Query params:** `requestorId` (Guid, obligatorio) — debe ser el líder del equipo.
- **Body / Payload (Request):** ninguno.
- **Nota:** Tras crear equipo, usar el mismo `creatorId` como `requestorId`.

### Procesar Solicitud de Unión (HU-30)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/teams/{teamId}/requests/{requestId}`
- **Capa Application:** `ProcessJoinRequestCommand`
- **Body / Payload (Request):**
  ```json
  {
    "approve": true,
    "requestorId": "Guid"
  }
  ```
- **Nota:** `requestorId` debe ser el `playerRef` del miembro con rol `Leader`.

### Expulsar Integrante del Equipo (HU-31)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `DELETE /api/v1/teams/{teamId}/members/{playerId}?requestorId={guid}`
- **Capa Application:** `RemoveMemberCommand`
- **Query params:** `requestorId` (Guid, obligatorio) — quien ejecuta la acción (líder al expulsar a otro, o el propio jugador al abandonar).
- **Body / Payload (Request):** ninguno.

## SessionManagement · Épica 5 (Participación Jugador/Equipo MVP)

### Listar Sesiones Activas (HU-36)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/live-sessions/active`
- **Capa Application:** `GetActiveSessionsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Nota:** Devuelve sesiones en estado `Pending`, `Preparation`, `Active` o `Paused` (no finalizadas ni canceladas). El equipo solo puede registrarse con `POST .../join` cuando la sesión está en `Pending` o `Preparation`.

### Unirse a Sesión por Código (HU-37)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/live-sessions/join`
- **Capa Application:** `JoinSessionCommand`
- **Body / Payload (Request):**
  ```json
  {
    "joinCode": "string",
    "teamId": "Guid"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` si el equipo está bloqueado en una sesión en curso (RN-13) o ya vinculado a otra sesión abierta.
  - `404 NotFound` si el código o el equipo no existen.

### Consultar Etapa Actual del Equipo (HU-38/HU-39/HU-41)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/live-sessions/{sessionId}/teams/{teamId}/current-stage`
- **Capa Application:** `GetTeamCurrentStageQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Enviar Código de Búsqueda del Tesoro (HU-40)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/live-sessions/{sessionId}/teams/{teamId}/treasure-hunt-code`
- **Capa Application:** `SubmitTreasureHuntCodeCommand`
- **Body / Payload (Request):**
  ```json
  {
    "nodeId": "Guid",
    "foundCode": "string"
  }
  ```

### Enviar Respuesta Trivia (HU-42)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/live-sessions/{sessionId}/teams/{teamId}/trivia-answer`
- **Capa Application:** `SubmitTriviaAnswerCommand`
- **Body / Payload (Request):**
  ```json
  {
    "nodeId": "Guid",
    "answer": "string"
  }
  ```

## SessionManagement · Épica 6 (Operador, alcance reducido)

### Consultar Misiones Asignadas al Operador (HU-47)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/operators/{operatorId}/missions`
- **Capa Application:** `GetOperatorAssignedMissionsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Validar Sesiones Abiertas por Misión
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/missions/{missionId}/session-validation/has-open`
- **Capa Application:** `MissionHasOpenSessionsQuery`
- **Response (200 OK):**
  ```json
  {
    "hasOpenSessions": true
  }
  ```

### Crear Sesión Live para una Misión Asignada (HU-48)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/operators/{operatorId}/sessions`
- **Capa Application:** `CreateLiveSessionCommand`
- **Body / Payload (Request):**
  ```json
  {
    "missionId": "Guid"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` si la misión no está en estado `Active` (RB-01).
  - `404 NotFound` si la misión no está asignada al operador.

### Consultar Sesiones Abiertas del Operador
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/operators/{operatorId}/sessions/open`
- **Capa Application:** `GetOperatorOpenSessionsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Response (200 OK):**
  ```json
  [
    {
      "sessionId": "Guid",
      "missionId": "Guid",
      "joinCode": "string",
      "status": "Pending | Preparation | Active | Paused"
    }
  ]
  ```
- **Notas:**
  - Permite reanudar la sala de espera tras recargar la consola del operador.
  - Solo devuelve sesiones del operador en estados no terminales.

### Consultar Equipos Unidos a una Sesión Pending (HU-49)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/operators/{operatorId}/sessions/{sessionId}/teams`
- **Capa Application:** `GetSessionTeamsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Iniciar Sesión Live (HU-50)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/operators/{operatorId}/sessions/{sessionId}/start`
- **Capa Application:** `StartLiveSessionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Notas:**
  - Bloquea los equipos registrados (`isLocked: true`, RN-13) al pasar la sesión a `Active`.

### Finalizar Sesión Live
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/operators/{operatorId}/sessions/{sessionId}/finalize`
- **Capa Application:** `FinalizeLiveSessionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Notas:**
  - Desbloquea equipos y limpia su referencia a la sesión.

### Cancelar Sesión Live
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/operators/{operatorId}/sessions/{sessionId}/cancel`
- **Capa Application:** `CancelLiveSessionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
- **Notas:**
  - Desbloquea equipos y limpia su referencia a la sesión.

### Validar si el operador tiene sesiones activas
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/operators/{operatorId}/session-validation/has-active`
- **WebApi:** `OperatorRoute` → `OperatorSessionMappings.ToHasActiveSessionsQuery()`
- **Capa Application:** `OperatorHasActiveSessionsQuery`
- **Auth:** `admin`, `operator` (+ `EnsureOperatorMatchesRoute`)
- **Response (200 OK):**
  ```json
  {
    "hasActiveSessions": true
  }
  ```

### Validar si el operador supervisa una misión
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/operators/{operatorId}/missions/{missionId}/session-validation/is-supervising`
- **WebApi:** `OperatorMissionRoute` → `OperatorSessionMappings.ToIsSupervisingMissionQuery()`
- **Capa Application:** `OperatorIsSupervisingMissionQuery`
- **Auth:** `admin`, `operator` (+ `EnsureOperatorMatchesRoute`)
- **Response (200 OK):**
  ```json
  {
    "isSupervising": true
  }
  ```

### Consultar pistas liberadas al equipo (jugador)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/live-sessions/{sessionId}/teams/{teamId}/nodes/{nodeId}/hints`
- **WebApi:** `LiveSessionTeamNodeRoute` → `IPlayerHintPanelService.GetReleasedHintsForTeamAsync`
- **Auth:** `player`, `operator`, `admin`
- **Body / Payload (Request):** ninguno.
- **Response (200 OK):** lista de pistas ya liberadas al equipo (proxy `PlayerReleasedHintsProxy`).

## MissionManagement · Integración y validación

### Validaciones de nodos de misión (integración)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{id}/node-validations`
- **WebApi:** `MissionRoute` → `MissionMappings.ToNodeValidationsQuery()`
- **Capa Application:** `GetMissionNodeValidationsQuery`
- **Auth:** `[AllowAnonymous]`
- **Body / Payload (Request):** ninguno.

### Validar sesiones abiertas por misión (MissionManagement)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{id}/session-validation/has-open`
- **WebApi:** `MissionRoute` → `ISessionValidationService.HasOpenSessionsForMissionAsync`
- **Auth:** `[AllowAnonymous]`
- **Response (200 OK):**
  ```json
  {
    "hasOpenSessions": true
  }
  ```

## ScoringAudit · Ranking

### Consultar ranking de sesión
- **Microservicio:** ScoringAudit
- **Método y Ruta:** `GET /api/v1/sessions/{sessionId}/ranking`
- **WebApi:** `SessionRoute` → `RankingMappings.ToQuery()`
- **Capa Application:** `GetSessionRankingQuery`
- **Auth:** `admin`, `operator`
- **Body / Payload (Request):** ninguno.
- **Notas:**
  - Requiere que la sesión haya generado entradas en `TeamLedger` (equipos registrados y evidencias procesadas).
