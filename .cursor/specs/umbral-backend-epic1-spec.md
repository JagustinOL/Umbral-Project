# UMBRAL · Backend Spec: Épica 1 (Misiones y Etapas)

## Contexto
Este spec define la implementación técnica para la gestión estructural (CRUD) del catálogo de misiones y sus fases lógicas.
- **Bounded Context:** Admin Service.
- [cite_start]**Actor Principal:** Administrador[cite: 769].
- [cite_start]**Historias de Usuario cubiertas:** HU-01 a HU-08[cite: 772, 773].

## Entidades Involucradas
- `Mision` (Agregado Raíz)
- `Etapa` (Entidad Hija)

## Reglas de Negocio (Business Rules)
- [cite_start]**RN-01 (Inmutabilidad en Uso):** El sistema debe bloquear cualquier intento de modificación (Update) o eliminación (Delete) de una Misión o Etapa si existe al menos una Sesión activa (en curso o pausada) vinculada a ella[cite: 798]. 

## Estructura CQRS y Endpoints (API REST)

### 1. Gestión de Misiones (HU-01 a HU-04)
**Ruta Base:** `api/v1/missions`

- [cite_start]**HU-01: Crear Misión** [cite: 772]
  - **Endpoint:** `POST /api/v1/missions`
  - **Command:** `CreateMissionCommand(string Nombre, string Descripcion, string ImagenUrl)`
  - **Validación:** Rechazar si el nombre ya existe. Asegurar que los datos obligatorios no estén vacíos.

- [cite_start]**HU-02 & HU-03: Consultar Misiones** [cite: 772]
  - **Endpoints:** `GET /api/v1/missions` (Lista completa) y `GET /api/v1/missions/{id}` (Detalle).
  - **Query:** `GetMissionsQuery` / `GetMissionByIdQuery`
  - **DTO de salida:** Debe incluir el estado (Activa/Inactiva). Si no hay datos, retornar un arreglo vacío (Empty State).

- [cite_start]**HU-03 (Continuación): Modificar Misión** [cite: 772]
  - **Endpoint:** `PUT /api/v1/missions/{id}`
  - **Command:** `UpdateMissionCommand(Guid Id, string Nombre, string Descripcion, string ImagenUrl)`
  - **Validación:** Ejecutar control de RN-01.

- [cite_start]**HU-04: Eliminar Misión** [cite: 772]
  - **Endpoint:** `DELETE /api/v1/missions/{id}`
  - **Command:** `DeleteMissionCommand(Guid Id)`
  - **Validación:** Ejecutar control de RN-01.

### 2. Gestión de Etapas (HU-05 a HU-08)
**Ruta Base:** `api/v1/missions/{missionId}/stages`

- [cite_start]**HU-05: Crear Etapa** [cite: 772]
  - **Endpoint:** `POST /api/v1/missions/{missionId}/stages`
  - **Command:** `AddStageCommand(Guid MissionId, string Nombre, string Narrativa)`
  - **Validación:** Ejecutar control de RN-01 (No se pueden añadir etapas a misiones en vivo).

- [cite_start]**HU-06: Consultar Etapas** [cite: 772]
  - **Endpoint:** `GET /api/v1/missions/{missionId}/stages`
  - **Query:** `GetStagesByMissionQuery(Guid MissionId)`
  - **Regla:** Los resultados deben devolverse estrictamente en su orden lógico de aparición.

- [cite_start]**HU-07: Modificar Etapa** [cite: 773]
  - **Endpoint:** `PUT /api/v1/missions/{missionId}/stages/{stageId}`
  - **Command:** `UpdateStageCommand(Guid MissionId, Guid StageId, string Nombre, string Narrativa)`
  - **Validación:** Ejecutar control de RN-01.

- [cite_start]**HU-08: Eliminar Etapa** [cite: 773]
  - **Endpoint:** `DELETE /api/v1/missions/{missionId}/stages/{stageId}`
  - **Command:** `DeleteStageCommand(Guid MissionId, Guid StageId)`
  - **Validación:** Ejecutar control de RN-01.