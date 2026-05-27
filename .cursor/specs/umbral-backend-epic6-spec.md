# UMBRAL · Backend Spec: Épica 6 (Operación y Control de Sesión)

## Contexto
Este spec define la interfaz y las acciones de control del Operador sobre el motor de juego en vivo, así como las rutinas automáticas de validación ejecutadas por el Sistema.
- **Bounded Context:** Session Management (Core).
- **Actores Principales:** Operador y Sistema (Background Jobs / Domain Events).
- **Historias de Usuario cubiertas:** HU-47 a HU-63.

## Entidades y Value Objects Involucrados
- `Operador` (Referencia)
- `Sesion` (Agregado Raíz)
- `ProgresoEquipo` (Read Model / Proyección)
- `Pista` y `Penalizacion` (Entidades hijas)

## Reglas de Negocio y Validaciones (Business Rules)
- [cite_start]**RN-16 (Restricción de Acceso Operativo):** Un Operador únicamente puede visualizar solicitudes, crear sesiones y monitorear misiones que le hayan sido explícitamente asignadas[cite: 44]. [cite_start]El sistema no debe permitir el acceso a cualquier otra misión[cite: 45].
- [cite_start]**RN-15 (Condición de Inicio):** Una sesión no puede pasar a estado *Activa* sin al menos un (1) equipo formalmente aprobado por el Operador[cite: 43].
- [cite_start]**RN-06 (No Duplicidad de Pistas):** El sistema debe impedir que un Operador envíe manualmente una pista que el equipo ya haya recibido en esa etapa[cite: 39].
- [cite_start]**RN-04 (Cierre de Etapa):** Una etapa se considera "Cerrada" al cumplir los objetivos[cite: 36]. [cite_start]No se aceptan acciones para etapas ya superadas[cite: 37].
- [cite_start]**RN-18 (Mensajería Condicionada):** El Operador no puede enviar mensajes a un equipo expulsado o que haya finalizado[cite: 48].
- [cite_start]**RN-17 (Inmutabilidad):** Una sesión Finalizada rechaza cambios; el registro pasa a ser de solo lectura[cite: 46, 47].

## Estructura CQRS y Endpoints (API REST & SignalR)

### 1. Tablero Inicial y Monitoreo Live (HU-47, HU-49, HU-51, HU-52, HU-53)
**Rutas de Lectura (Queries) y WebSockets:**

- **HU-47: Misiones Asignadas**
  - **Endpoint:** `GET /api/v1/operators/{operatorId}/missions`
  - **Query:** `GetOperatorAssignedMissionsQuery` (Filtra aplicando RN-16).
  
- **HU-49: Solicitudes de Unión**
  - **Endpoint/Hub:** Transmisión vía SignalR al grupo del Operador cuando un equipo solicita acceso, respaldado por `GET /api/v1/sessions/{sessionId}/join-requests`.

- **HU-51, HU-52, HU-53: Monitoreo Live**
  - **Flujo Técnico:** El Operador debe suscribirse a un grupo de SignalR (`Operator_Session_{Id}`).
  - **Eventos recibidos:** `TeamProgressUpdated`, `TriviaAnswerSubmitted`, `HuntLocationReached`. 
  - **Nota de resiliencia:** Si el equipo pierde conexión, el sistema emite `TeamDisconnected` al panel del operador.

### 2. Acciones de Control Manual del Operador (HU-54, HU-55, HU-56, HU-62)
**Ruta Base:** `api/v1/sessions/{sessionId}/teams/{teamId}`

- **HU-54: Liberar Pista Manual**
  - **Endpoint:** `POST /clues/release`
  - **Command:** `ReleaseManualClueCommand(Guid SessionId, Guid TeamId, Guid ClueId)`
  - **Validación:** Ejecutar control de RN-06 (Bloquear pistas duplicadas).

- **HU-55: Aplicar Penalización Manual**
  - **Endpoint:** `POST /penalties`
  - **Command:** `ApplyManualPenaltyCommand(Guid SessionId, Guid TeamId, int Puntos, string Motivo)`
  - **Validación:** Rechazar si el `Motivo` viene vacío (justificación obligatoria).

- **HU-56: Enviar Mensaje de Soporte**
  - **Endpoint:** `POST /messages`
  - **Command:** `SendSupportMessageCommand(Guid SessionId, Guid TeamId, string Mensaje)`
  - **Validación:** Ejecutar control RN-18 (El equipo no puede estar finalizado/expulsado).

- **HU-62: Congelamiento por Pausa**
  - **Command:** `ToggleSessionPauseCommand` (Definido en Epic 5).
  - **Flujo:** Dispara evento de dominio que congela timers y rechaza interacciones. [cite_start]Bloquear si la sesión ya finalizó[cite: 27].

### 3. Automatizaciones del Sistema (HU-58 a HU-61, HU-63)
Estas historias no tienen un endpoint directo del operador, sino que son **Domain Handlers** (Respuestas del sistema a acciones del equipo):

- **HU-58 & HU-59: Validación Automática**
  - **Handler:** `EvaluateTriviaAnswerCommandHandler` y `ValidateTreasureHuntCodeCommandHandler`.
  - **Flujo:** Evalúa el payload. Si es incorrecto, no suma puntos y notifica error. Si es correcto, aplica la puntuación y dispara el evento para la HU-60.

- **HU-60 & HU-61: Progresión de Etapa y Finalización**
  - **Flujo Técnico:** Al superar el último reto de una etapa, el dominio automáticamente transiciona al equipo a la siguiente. Si es la última etapa de la misión, se detiene el cronómetro del equipo, se marca como "Completado" y se emite el evento para que el ranking evalúe el desempate dinámico (HU-63).