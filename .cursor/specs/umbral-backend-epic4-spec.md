# UMBRAL · Backend Spec: Épica 4 (Gestión de Equipos e Integrantes)

## Contexto
Este spec define la lógica de negocio y los contratos técnicos para la constitución de los equipos de juego, la generación de códigos de invitación y el flujo de aceptación de nuevos integrantes.
- **Bounded Context:** Session Management (Core).
- **Actores Principales:** Jugador (Líder del Equipo) e Integrantes (Otros Jugadores).
- **Historias de Usuario cubiertas:** HU-27 a HU-35.

## Entidades y Value Objects Involucrados
- `Team` (Agregado Raíz - Independiente pero perteneciente al contexto de Sesiones)
- `TeamMember` (Entidad dentro del Agregado Team, mapeado mediante un PlayerRef)
- `JoinRequest` (Entidad dentro del Agregado Team)
- `TeamCode` (Value Object Inmutable)

## Reglas de Negocio y Validaciones (Business Rules)
- **Límites de Capacidad:** Un equipo debe tener como mínimo un (1) integrante (el creador) y un máximo estricto de cuatro (4) jugadores. Cualquier intento de aprobar un 5to miembro debe ser rechazado por el dominio del `Team`.
- **RN-14 (Identidad Única de Equipo):** Para garantizar la integridad del ranking, no pueden existir dos equipos con el mismo nombre.
- **RN-13 (Bloqueo de Modificación de Equipo en Juego):** El Agregado `Team` posee una propiedad `IsLocked`. No se permite la alteración de la plantilla del equipo (abandonar, disolver, añadir o aprobar miembros) si `IsLocked = true` (lo cual ocurre cuando están participando en una sesión Activa o Pausada).

## Estructura CQRS y Endpoints (API REST)

### 1. Registro y Configuración del Equipo (HU-27, HU-29, HU-33, HU-34)
**Ruta Base:** `api/v1/teams`

- **HU-27: Crear Equipo**
  - **Endpoint:** `POST /api/v1/teams`
  - **Command:** `CreateTeamCommand(string Name, Guid CreatorId)`
  - **Flujo Técnico:** El dominio debe generar automáticamente un `TeamCode` alfanumérico único de 6 caracteres. El `CreatorId` se registra inmediatamente como el primer `TeamMember` con el rol de *Líder*.
  - **Validación:** Ejecutar control de RN-14 (Unicidad de nombre).

- **HU-29: Consultar Detalle del Equipo**
  - **Endpoint:** `GET /api/v1/teams/{teamId}`
  - **Query:** `GetTeamByIdQuery(Guid TeamId)`
  - **DTO de salida:** Datos del equipo, lista de miembros actuales con sus nombres/roles, y el estado actual de bloqueo (`IsLocked`).

- **HU-33: Modificar Datos del Equipo**
  - **Endpoint:** `PUT /api/v1/teams/{teamId}`
  - **Command:** `UpdateTeamCommand(Guid TeamId, string NewName, Guid RequestorId)`
  - **Validación:** Solo el *Líder* (`RequestorId`). Ejecutar control de RN-13 (Rechazar si `IsLocked == true`).

- **HU-34: Disolver Equipo**
  - **Endpoint:** `DELETE /api/v1/teams/{teamId}`
  - **Command:** `DisbandTeamCommand(Guid TeamId, Guid RequestorId)`
  - **Validación:** Solo el *Líder* puede disparar esta acción. Ejecutar control estricto de RN-13 (El dominio lanza excepción si `IsLocked == true`).

### 2. Flujo de Unión e Invitaciones (HU-28, HU-30, HU-31, HU-32, HU-35)
**Ruta Base:** `api/v1/teams/{teamId}/requests`

- **HU-35 & HU-28: Validar Código y Enviar Solicitud**
  - **Endpoint:** `POST /api/v1/teams/join-requests`
  - **Command:** `SubmitJoinRequestCommand(string AccessCode, Guid PlayerId)`
  - **Flujo Técnico:** El backend busca el equipo por el `TeamCode`. Si es válido y no está lleno, crea una `JoinRequest` con estado *Pendiente*.

- **HU-32: Consultar Solicitudes Pendientes**
  - **Endpoint:** `GET /api/v1/teams/{teamId}/requests?requestorId={guid}`
  - **Query:** `GetPendingRequestsQuery(Guid TeamId, Guid RequestorId)`
  - **Restricción:** Solo el *Líder* del equipo (`RequestorId` debe coincidir con el miembro con rol Leader).

- **HU-30: Procesar Solicitud (Aprobar/Rechazar)**
  - **Endpoint:** `PUT /api/v1/teams/{teamId}/requests/{requestId}`
  - **Command:** `ProcessJoinRequestCommand(Guid TeamId, Guid RequestId, bool IsApproved, Guid RequestorId)`
  - **Restricción:** Solo el *Líder* (`RequestorId`).
  - **Validación:** Si `IsApproved == true`, validar inmediatamente los límites de capacidad y la RN-13 (No se pueden aprobar solicitudes si el equipo ya empezó a jugar). Si el equipo ya tiene 4 miembros, lanzar una excepción de dominio.

- **HU-31: Salir o Remover del Equipo**
  - **Endpoint:** `DELETE /api/v1/teams/{teamId}/members/{playerId}`
  - **Command:** `RemoveMemberCommand(Guid TeamId, Guid PlayerId, Guid RequestorId)`
  - **Validación:** Ejecutar control RN-13 (No puede abandonar si `IsLocked == true`). Si el jugador que sale es el Líder, la propiedad de liderazgo debe delegarse al siguiente miembro más antiguo o forzar la disolución.