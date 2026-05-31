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

### Consultar Misión por Id (HU-02)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/missions/{id}`
- **Capa Application:** `GetMissionByIdQuery`
- **Body / Payload (Request):**
  ```json
  { }
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
- **Body / Payload (Request):**
  ```json
  {
    "content": "string",
    "attachment": "IFormFile (jpg/png)"
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

### Consultar Operadores (HU-23)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `GET /api/v1/operators`
- **Capa Application:** `GetOperatorsQuery`
- **Body / Payload (Request):**
  ```json
  { }
  ```

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

### Revocar Operador de Misión (HU-25)
- **Microservicio:** MissionManagement
- **Método y Ruta:** `DELETE /api/v1/missions/{missionId}/operators/{operatorId}`
- **Capa Application:** `RevokeOperatorFromMissionCommand`
- **Body / Payload (Request):**
  ```json
  { }
  ```
