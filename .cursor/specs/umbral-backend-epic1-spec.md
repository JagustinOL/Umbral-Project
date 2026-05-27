# UMBRAL · Backend Spec: Épica 1 (Misiones y Etapas)

## Contexto
Este spec define la implementación técnica para la gestión estructural (CRUD) del catálogo de misiones y sus fases lógicas.
- **Bounded Context:** Mission Management.
- **Actor Principal:** Administrador (Autenticado vía Keycloak).
- **Historias de Usuario cubiertas:** HU-01 a HU-08.

## Entidades Involucradas
- `Mission` (Agregado Raíz)
- `MissionNode` (Entidad Hija - Representa las etapas utilizando el patrón Composite)

## Reglas de Negocio (Business Rules)
- **RN-01 (Inmutabilidad en Uso):** El sistema debe bloquear cualquier intento de modificación (Update) o eliminación (Delete) a nivel estructural si la misión no se encuentra en estado `Draft`. Si la misión está `Active`, significa que está publicada para el Live Engine y su estructura es inmutable.

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Misiones (HU-01 a HU-04)
**Ruta Base:** `api/v1/missions`

- **HU-01: Crear Misión**
  - **Endpoint:** `POST /api/v1/missions`
  - **Command:** `CreateMissionCommand(string Title, string Description, int Difficulty, int? MaxDurationMinutes)`
  - **Validación:** Rechazar si el título ya existe. Asegurar que los datos obligatorios no estén vacíos. El dominio debe inicializarla obligatoriamente en estado `Draft`.

- **HU-02 & HU-03: Consultar Misiones**
  - **Endpoints:** `GET /api/v1/missions` (Lista completa) y `GET /api/v1/missions/{id}` (Detalle).
  - **Query:** `GetMissionsQuery` / `GetMissionByIdQuery`
  - **DTO de salida:** Debe incluir el estado actual del enum `MissionStatus` (`Draft`, `Active`, `Inactive`). Si no hay datos, retornar un arreglo vacío (Empty State).

- **HU-03 (Continuación): Modificar Misión**
  - **Endpoint:** `PUT /api/v1/missions/{id}`
  - **Command:** `UpdateMissionDetailsCommand(Guid Id, string Title, string Description, int? MaxDurationMinutes)`
  - **Validación:** Ejecutar control de RN-01 (El Agregado debe lanzar excepción mediante su método interno `ThrowIfNotDraft` si no está en borrador).

- **HU-04: Eliminar / Desactivar Misión**
  - **Endpoint:** `DELETE /api/v1/missions/{id}`
  - **Command:** `DeactivateMissionCommand(Guid Id)`
  - **Validación:** Ejecutar control de RN-01.

### 2. Gestión de Etapas (HU-05 a HU-08)
**Ruta Base:** `api/v1/missions/{missionId}/nodes`

- **HU-05: Crear Etapa (Nodo Raíz)**
  - **Endpoint:** `POST /api/v1/missions/{missionId}/nodes`
  - **Command:** `AddRootNodeCommand(Guid MissionId, string Title, string Description, int ExecutionOrder)`
  - **Validación:** Ejecutar control de RN-01 delegando la adición al método `mission.AddRootNode()` del Agregado Raíz. Validar que no haya conflictos de orden de ejecución.

- **HU-06: Consultar Etapas**
  - **Endpoint:** `GET /api/v1/missions/{missionId}/nodes`
  - **Query:** `GetNodesByMissionQuery(Guid MissionId)`
  - **Regla:** Los resultados deben devolverse estrictamente en su orden lógico de aparición (`ExecutionOrder`). Se filtran solo los nodos que sean de tipo `Stage`.

- **HU-07: Modificar Etapa**
  - **Endpoint:** `PUT /api/v1/missions/{missionId}/nodes/{nodeId}`
  - **Command:** `UpdateNodeCommand(Guid MissionId, Guid NodeId, string Title, string Description)`
  - **Validación:** Ejecutar control de RN-01 a través de la Raíz del Agregado `Mission`.

- **HU-08: Eliminar Etapa**
  - **Endpoint:** `DELETE /api/v1/missions/{missionId}/nodes/{nodeId}`
  - **Command:** `DeleteNodeCommand(Guid MissionId, Guid NodeId)`
  - **Validación:** Ejecutar control de RN-01. No se puede eliminar si la misión está `Active`.