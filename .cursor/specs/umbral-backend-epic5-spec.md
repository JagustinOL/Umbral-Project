# UMBRAL · Backend Spec: Épica 5 (Operación de Sesiones en Vivo)

## Contexto
Este spec define el motor central del juego. Controla el ciclo de vida de una sesión, la recepción de evidencias de los equipos, la validación por parte del operador y la aplicación de penalizaciones.
- **Bounded Context:** Session Management (Core).
- **Actores Principales:** Operador (Control) y Equipo (Interacción).
- **Historias de Usuario cubiertas:** HU-36 a HU-42.

## Entidades y Value Objects Involucrados
- `Sesion` (Agregado Raíz)
- `EstadoSesion` (Enum/Value Object: Pendiente, Activa, Pausada, Finalizada)
- `Evidencia` (Entidad Hija)
- `Penalizacion` (Entidad Hija)

## Reglas de Negocio y Validaciones (Business Rules)
- **RN-15 (Condición de Inicio):** Una sesión no puede iniciar su cronómetro ni pasar a estado *Activa* si no cuenta con al menos un (1) equipo formalmente aprobado.
- **RN-16 (Autorización de Operador):** El Operador que intenta manipular la sesión debe estar asignado previamente a la misión base de la misma (validación cruzada con Admin Service).
- **RN-17 (Inmutabilidad Post-Cierre):** Una vez que la sesión pasa a estado *Finalizada*, se congela. Ningún puntaje, tiempo, evidencia o estado puede ser alterado.
- **RB-03 (Bloqueo de Interacción):** El sistema debe rechazar cualquier envío de evidencias por parte de los equipos si la sesión se encuentra *Pausada* o *Finalizada*.

## Estructura CQRS y Endpoints (API REST & SignalR)

### 1. Ciclo de Vida de la Sesión (HU-36 a HU-39)
**Ruta Base:** `api/v1/sessions`

- **HU-36: Crear Sesión**
  - **Endpoint:** `POST /api/v1/sessions`
  - **Command:** `CreateSessionCommand(Guid MissionId, Guid OperatorId, DateTime FechaProgramada)`
  - **Flujo Técnico:** La sesión nace en estado *Pendiente*.

- **HU-37: Iniciar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/start`
  - **Command:** `StartSessionCommand(Guid SessionId)`
  - **Validación:** Ejecutar control de RN-15 (debe tener equipos). Cambiar estado a *Activa* y registrar `StartedAt`. Emitir evento por SignalR.

- **HU-38: Pausar/Reanudar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/toggle-pause`
  - **Command:** `ToggleSessionPauseCommand(Guid SessionId)`
  - **Validación:** Solo aplicable si la sesión no está finalizada. Emitir evento por SignalR para congelar los cronómetros de los clientes.

- **HU-39: Finalizar Sesión**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/finish`
  - **Command:** `FinishSessionCommand(Guid SessionId)`
  - **Flujo Técnico:** Cambia el estado a *Finalizada*. Calcula los tiempos finales, aplica la RN-17 y emite el evento de cierre global.

### 2. Interacción del Juego: Evidencias y Penalizaciones (HU-40 a HU-42)
**Ruta Base:** `api/v1/sessions/{sessionId}`

- **HU-40: Enviar Evidencia (Acción del Equipo)**
  - **Endpoint:** `POST /api/v1/sessions/{sessionId}/teams/{teamId}/evidences`
  - **Command:** `SubmitEvidenceCommand(Guid SessionId, Guid TeamId, Guid StageId, string Contenido, IFormFile Archivo)`
  - **Validación:** Ejecutar control de RB-03 (La sesión DEBE estar *Activa*). 

- **HU-41: Evaluar Evidencia (Acción del Operador)**
  - **Endpoint:** `PUT /api/v1/sessions/{sessionId}/evidences/{evidenceId}/evaluate`
  - **Command:** `EvaluateEvidenceCommand(Guid SessionId, Guid EvidenceId, bool Aprobada, string Comentario)`
  - **Flujo Técnico:** Si se aprueba, se suman los puntos al equipo. Emitir evento por SignalR para actualizar el tablero de puntuación en vivo. Ejecutar control de RN-17.

- **HU-42: Aplicar Penalización Manual**
  - **Endpoint:** `POST /api/v1/sessions/{sessionId}/teams/{teamId}/penalties`
  - **Command:** `ApplyPenaltyCommand(Guid SessionId, Guid TeamId, string Motivo, int PuntosRestados)`
  - **Validación:** Solo aplicable si la sesión está *Activa*. Ejecutar control de RN-17. Afecta directamente el cálculo del puntaje total del equipo.