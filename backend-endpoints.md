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

### Desactivar Misión (HU-04)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{id}`
- **Capa Application:** `DeactivateMissionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

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
    "destination": "GpsCoordinateRequest",
    "executionOrder": "int"
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
    "destination": "GpsCoordinateRequest"
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
- **Capa Application:** `AddHintCommand`
- **Body / Payload (Request - `multipart/form-data`):**
  ```json
  {
    "content": "string",
    "attachment": "IFormFile (jpg/png, opcional)"
  }
  ```

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
    "id": "Guid"
  }
  ```
- **Errores esperados:**
  - `409 Conflict` cuando el correo ya existe en Keycloak.
  - `400 BadRequest` para payload inválido.

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

## SessionManagement · Épica 4 (Gestión de Equipos e Integrantes)

### Crear Equipo (HU-27)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `POST /api/v1/teams`
- **Capa Application:** `CreateTeamCommand`
- **Body / Payload (Request):**
  ```json
  {
    "name": "string"
  }
  ```

### Consultar Equipo por Id (HU-29)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/teams/{teamId}`
- **Capa Application:** `GetTeamByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Modificar Equipo (HU-33)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/teams/{teamId}`
- **Capa Application:** `UpdateTeamCommand`
- **Body / Payload (Request):**
  ```json
  {
    "newName": "string"
  }
  ```

### Disolver Equipo (HU-34)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `DELETE /api/v1/teams/{teamId}`
- **Capa Application:** `DisbandTeamCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

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

### Consultar Solicitudes Pendientes de Equipo (HU-32)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/teams/{teamId}/requests`
- **Capa Application:** `GetPendingRequestsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

### Procesar Solicitud de Unión (HU-30)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `PUT /api/v1/teams/{teamId}/requests/{requestId}`
- **Capa Application:** `ProcessJoinRequestCommand`
- **Body / Payload (Request):**
  ```json
  {
    "approve": "bool"
  }
  ```

### Expulsar Integrante del Equipo (HU-31)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `DELETE /api/v1/teams/{teamId}/members/{playerId}`
- **Capa Application:** `RemoveMemberCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```

###IMPORTANTE: REVISAR CRUD DE USUARIO (HU 32 - HU 35)(IMPLEMENTACION CON KEYCLOAK)

## SessionManagement · Épica 5 (Participación Jugador/Equipo MVP)

### Listar Sesiones Activas (HU-36)
- **Microservicio:** SessionManagement
- **Método y Ruta:** `GET /api/v1/live-sessions/active`
- **Capa Application:** `GetActiveSessionsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

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
