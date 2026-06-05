# UMBRAL · Backend Spec: Épica 5 (Operación de Sesiones en Vivo)

## Contexto
Este spec define el motor central del juego (Live Engine). Controla el ciclo de vida de una sesión, la recepción de evidencias de los equipos y la validación por parte del operador.
- **Bounded Context:** Session Management (Core).
- **Actores Principales:** Operador (Control) y Equipo (Interacción).
- **Historias de Usuario cubiertas:** HU-36 a HU-42.

## Entidades y Value Objects Involucrados
- `LiveSession` (Agregado Raíz)
- `SessionState` (Value Object / Patrón State: Pending, Active, Paused, Finished, Canceled)
- `EvidenceSubmission` (Entidad Hija de la sesión)
- `MissionRef` y `OperatorRef` (Referencias externas)

## Reglas de Negocio y Validaciones (Business Rules)
- **RN-15 (Condición de Inicio):** El método `Start()` del Agregado `LiveSession` lanzará una excepción de dominio si intenta activarse sin contar con al menos un (1) equipo formalmente aprobado (Validación cruzada con el Agregado `Team`).
- **RN-16 (Autorización de Operador):** El Operador que intenta crear o manipular la sesión debe estar asignado previamente a la misión base de la misma.
- **RN-17 (Inmutabilidad Post-Cierre):** El Patrón State protege la sesión. Una vez que pasa a estado `Finished`, se congela. El Agregado rechazará cualquier mutación adicional.
- **RB-03 (Bloqueo de Interacción):** El método interno `AcceptEvidence()` debe rechazar y lanzar error ante cualquier envío de evidencias por parte de los equipos si el `SessionState` se encuentra en `Paused` o `Finished`.

## Estructura CQRS y Endpoints (API REST & SignalR)

### 1. Ciclo de Vida de la Sesión (HU-36 a HU-39)
**Ruta Base:** `api/v1/sessions`

- **HU-36: Crear Sesión**
  - **Endpoint:** `POST /api/v1/sessions`
  - **Command:** `CreateLiveSessionCommand(Guid MissionId, Guid OperatorId, DateTime ScheduledAt)`
  - **Flujo Técnico:** La sesión nace instanciada con el estado `Pending`.

- **HU-37: Iniciar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/start`
  - **Command:** `StartLiveSessionCommand(Guid SessionId)`
  - **Validación:** Ejecutar control de RN-15 (debe tener equipos). Cambiar estado a `Active` y publicar evento de dominio `SessionStarted`. El Handler emite el evento por SignalR.

- **HU-38: Pausar/Reanudar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/toggle-pause`
  - **Command:** `ToggleSessionPauseCommand(Guid SessionId)`
  - **Validación:** El Agregado valida la transición de estado. Publica `SessionStateChanged` para emitir evento por SignalR y congelar los cronómetros de los clientes.

- **HU-39: Finalizar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/finish`
  - **Command:** `FinishLiveSessionCommand(Guid SessionId)`
  - **Flujo Técnico:** Cambia el estado a `Finished`. Aplica la RN-17 y publica el evento de cierre global para que el *ScoringAudit* consolide el ranking final.

### 2. Interacción del Juego: Evidencias y Penalizaciones (HU-40 a HU-42)
**Ruta Base:** `api/v1/sessions/{sessionId}`

- **HU-40: Enviar Evidencia (Acción del Equipo)**
  - **Endpoint:** `POST /api/v1/sessions/{sessionId}/teams/{teamId}/evidences`
  - **Command:** `SubmitEvidenceCommand(Guid SessionId, Guid TeamId, Guid NodeId, string Content, IFormFile Attachment)`
  - **Validación:** El Handler invoca al Domain Service `EvidenceValidatorService` que ejecuta el control de RB-03 (La sesión DEBE estar activa). 

- **HU-41: Evaluar Evidencia (Acción del Operador)**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/evidences/{evidenceId}/evaluate`
  - **Command:** `EvaluateEvidenceCommand(Guid SessionId, Guid EvidenceId, bool IsApproved, string Comment)`
  - **Flujo Técnico:** Si el operador la aprueba, se publica el Domain Event `EvidenceValidated` en RabbitMQ. El microservicio *ScoringAudit* consume este evento, suma los puntos y notifica a SignalR para actualizar el tablero de puntuación. Ejecutar control de RN-17 (la sesión no puede estar finalizada).

- **HU-42: Aplicar Penalización Manual**
  - **Endpoint:** `POST /api/v1/sessions/{sessionId}/teams/{teamId}/penalties`
  - **Command:** `ApplyManualPenaltyCommand(Guid SessionId, Guid TeamId, string Reason, int PenaltyPoints)`
  - **Validación:** Ejecutar control de RN-17. El Handler publica el evento `ManualPenaltyApplied`. El microservicio *ScoringAudit* lo captura y resta los puntos correspondientes en el `TeamLedger`.