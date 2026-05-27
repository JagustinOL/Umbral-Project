# UMBRAL · Backend Spec: Épica 4 (Gestión de Equipos e Integrantes)

## Contexto
Este spec define la lógica de negocio y los contratos técnicos para la constitución de los equipos de juego, la generación de códigos de invitación y el flujo de aceptación de nuevos integrantes.
- **Bounded Context:** Team Service.
- **Actores Principales:** Jugador (Líder del Equipo) e Integrantes (Otros Jugadores).
- **Historias de Usuario cubiertas:** HU-27 a HU-35.

## Entidades y Value Objects Involucrados
- `Equipo` (Agregado Raíz)
- `Miembro` (Entidad dentro del Agregado Equipo, mapeado a un Jugador)
- `SolicitudUnion` (Entidad dentro del Agregado Equipo)
- `CodigoAcceso` (Value Object Inmutable)

## Reglas de Negocio y Validaciones (Business Rules)
- **RN-10 (Límites de Capacidad):** Un equipo debe tener como mínimo un (1) integrante (el creador) y un máximo estricto de cuatro (4) jugadores. Cualquier intento de aprobar un 5to miembro debe ser rechazado por el dominio.
- **RN-11 (Exclusividad de Participación):** Un jugador no puede ser miembro activo de más de un (1) equipo en una misma misión en curso de forma simultánea.
- **RN-12 (Bloqueo por Estado de Juego):** No se permite la alteración de la plantilla del equipo (añadir, aprobar o remover miembros) si el equipo ya ha sido vinculado a una sesión que se encuentra en estado *Activa* o *Finalizada*.

## Estructura CQRS y Endpoints (API REST)

### 1. Registro y Configuración del Equipo (HU-27, HU-29, HU-33, HU-34)
**Ruta Base:** `api/v1/teams`

- **HU-27: Crear Equipo**
  - **Endpoint:** `POST /api/v1/teams`
  - **Command:** `CreateTeamCommand(string NombreEquipo, Guid CreadorId, Guid MissionId)`
  - **Flujo Técnico:** El dominio debe generar automáticamente un `CodigoAcceso` alfanumérico único de 6 caracteres. El `CreadorId` se registra inmediatamente como el primer `Miembro` con el rol de *Líder*.

- **HU-29: Consultar Detalle del Equipo**
  - **Endpoint:** `GET /api/v1/teams/{teamId}`
  - **Query:** `GetTeamByIdQuery(Guid TeamId)`
  - **DTO de salida:** Datos del equipo, lista de miembros actuales con sus nombres/roles, y el estado actual del grupo.

- **HU-33: Modificar Datos del Equipo**
  - **Endpoint:** `PUT /api/v1/teams/{teamId}`
  - **Command:** `UpdateTeamCommand(Guid TeamId, string NuevoNombre)`
  - **Validación:** Ejecutar control de RN-12.

- **HU-34: Disolver Equipo**
  - **Endpoint:** `DELETE /api/v1/teams/{teamId}`
  - **Command:** `DisbandTeamCommand(Guid TeamId, Guid RequestorId)`
  - **Validación:** Solo el *Líder* puede disparar esta acción. Ejecutar control estricto de RN-12 (No se puede disolver en pleno juego).

### 2. Flujo de Unión e Invitaciones (HU-28, HU-30, HU-31, HU-32, HU-35)
**Ruta Base:** `api/v1/teams/{teamId}/requests`

- **HU-35 & HU-28: Validar Código y Enviar Solicitud**
  - **Endpoint:** `POST /api/v1/teams/join-requests`
  - **Command:** `SubmitJoinRequestCommand(string CodigoAcceso, Guid JugadorId)`
  - **Flujo Técnico:** El backend busca el equipo por el código. Si es válido, evalúa la RN-11 y la RN-10 (en estado pendiente). Si pasa, crea una `SolicitudUnion` con estado *Pendiente*.

- **HU-32: Consultar Solicitudes Pendientes**
  - **Endpoint:** `GET /api/v1/teams/{teamId}/requests`
  - **Query:** `GetPendingRequestsQuery(Guid TeamId)`
  - **Restricción:** Solo el *Líder* del equipo tiene autorización para consumir este endpoint.

- **HU-30: Procesar Solicitud (Aprobar/Rechazar)**
  - **Endpoint:** `PUT /api/v1/teams/{teamId}/requests/{requestId}`
  - **Command:** `ProcessJoinRequestCommand(Guid TeamId, Guid RequestId, bool Aprobado)`
  - **Validación:** Si `Aprobado == true`, validar inmediatamente la **RN-10** (Capacidad máxima). Si el equipo ya se llenó a 4 antes de procesar esta solicitud, lanzar una excepción de dominio.

- **HU-31: Salir o Remover del Equipo**
  - **Endpoint:** `DELETE /api/v1/teams/{teamId}/members/{jugadorId}`
  - **Command:** `RemoveMemberCommand(Guid TeamId, Guid JugadorId, Guid RequestorId)`
  - **Validación:** Ejecutar control RN-12. Si el jugador que sale es el Líder, la propiedad de liderazgo debe delegarse al siguiente miembro más antiguo o forzar la disolución.