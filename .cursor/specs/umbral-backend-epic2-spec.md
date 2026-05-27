# UMBRAL · Backend Spec: Épica 2 (Juegos y Pistas)

## Contexto
Este spec define la implementación técnica para la configuración de los retos específicos (Juegos) que conforman las etapas de una misión, así como los recursos de ayuda (Pistas).
- **Bounded Context:** Mission Management.
- **Actor Principal:** Administrador (Autenticado vía Keycloak).
- **Historias de Usuario cubiertas:** HU-09 a HU-21.

## Entidades y Value Objects Involucrados
- `Mission` (Agregado Raíz - Para protección de invariantes y estado)
- `MissionNode` (Entidad Hija - Usando patrón Composite para representar los juegos)
- `Hint` (Entidad - Representa las pistas asociadas a un nodo)
- `TriviaQuestion` (Value Object / Entidad interna del nodo)
- `GpsCoordinate` (Value Object)
- `MissionNodeType` (Enum: Stage, Trivia, TreasureHunt)

## Reglas de Negocio (Business Rules)
- **RN-01 (Inmutabilidad en Uso):** No se puede editar ni eliminar un sub-nodo (Juego) o `Hint` si la `Mission` asociada ya se encuentra en estado `Active`. Toda mutación debe ser bloqueada por la raíz del agregado.
- **RN-02 (Tipificación Obligatoria):** El sistema controla los tipos de juego mediante el enum `MissionNodeType`, garantizando que solo existan Trivias y Búsquedas del Tesoro colgando de una Etapa.

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Juegos de Trivia (HU-09 a HU-12)
**Ruta Base:** `api/v1/missions/{missionId}/nodes/{parentNodeId}/trivia`

- **HU-09: Añadir Trivia**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/nodes/{parentNodeId}/trivia`
  - **Command:** `AddTriviaNodeCommand(Guid MissionId, Guid ParentNodeId, List<TriviaQuestion> Questions, int ExecutionOrder)`
  - **Validación:** El sistema rechaza la transacción si no se marca ninguna respuesta como "Correcta" dentro del Payload. El comando debe invocar `mission.AddChildNode()` para que la raíz ejecute el control RN-01.

- **HU-10: Consultar Trivia**
  - **Endpoint:** `GET /api/v1/missions/{missionId}/nodes/{nodeId}/trivia`
  - **Query:** `GetTriviaNodeByIdQuery(Guid MissionId, Guid NodeId)`
  - **DTO de salida:** Muestra los metadatos del nodo y la colección de preguntas y respuestas configuradas.

- **HU-11: Modificar Trivia**
  - **Endpoint:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}/trivia`
  - **Command:** `UpdateTriviaNodeCommand(Guid MissionId, Guid NodeId, List<TriviaQuestion> Questions)`
  - **Validación:** Bloquear si se intenta eliminar la única respuesta correcta de una pregunta. Ejecutar control RN-01 validando que la misión esté en estado `Draft`.

- **HU-12: Eliminar Trivia**
  - **Endpoint:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}`
  - **Command:** `DeleteNodeCommand(Guid MissionId, Guid NodeId)`
  - **Validación:** Ejecutar control RN-01.

### 2. Gestión de Juegos de Búsqueda (HU-13 a HU-16)
**Ruta Base:** `api/v1/missions/{missionId}/nodes/{parentNodeId}/treasure-hunts`

- **HU-13: Añadir Búsqueda**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/nodes/{parentNodeId}/treasure-hunts`
  - **Command:** `AddTreasureHuntNodeCommand(Guid MissionId, Guid ParentNodeId, string Instructions, string SecretCode, GpsCoordinate Destination, int ExecutionOrder)`
  - **Validación:** El sistema exige datos clave (como mapa/coordenadas y código secreto) antes de instanciar el nodo. Ejecutar control RN-01 a través de la raíz.

- **HU-14 a HU-16: Consultar, Modificar y Eliminar Búsqueda**
  - **Endpoints:** `GET`, `PUT`, `DELETE` referenciando el `{nodeId}` específico de la búsqueda.
  - **Validación:** Las modificaciones e intentos de eliminación deben bloquearse si la misión no está en estado `Draft` (RN-01).

### 3. Gestión de Pistas (HU-17 a HU-21)
**Ruta Base:** `api/v1/missions/{missionId}/nodes/{nodeId}/hints`

- **HU-17: Crear Pista**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/nodes/{nodeId}/hints`
  - **Command:** `AddHintCommand(Guid MissionId, Guid NodeId, string Content, IFormFile Attachment)`
  - **Validación:** Rechazar en la capa de Aplicación si el contenido multimedia supera el tamaño máximo o si el formato no es válido (solo JPG/PNG permitidos). Delegar la inserción a `mission.AddHintToNode()`.

- **HU-18 a HU-20: Consultar, Modificar y Eliminar Pista**
  - **Endpoints:** `GET`, `PUT`, `DELETE` referenciando el `{hintId}`.
  - **Validación:** No se pueden alterar pistas si la misión padre ha pasado a estado `Active` (RN-01).

- **HU-21: Configuración de Liberación**
  - **Regla del Dominio:** Asegurar a nivel de dominio que la entidad `MissionNode` posea la colección de `Hints` y que la lógica permita exponerlas ordenadamente para que el *Live Engine* las libere durante la ejecución de la sesión.