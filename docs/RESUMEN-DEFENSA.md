# UMBRAL — Resumen unificado (arquitectura + defensa)

**Proyecto de Desarrollo de Software — Grupo 7**  
Daniel Pérez · José Ojeda · UCAB 2026

Documento único de estudio: visión del producto, arquitectura, microservicios capa a capa, patrones, puertos, integraciones, flujos de negocio y guion de defensa.

---

## 1. ¿Qué es Umbral?

Plataforma para **operar en tiempo real experiencias de investigación inmersivas**:

| Actor             | Rol        | App                     | Qué hace                                                                                                |
| ----------------- | ---------- | ----------------------- | ------------------------------------------------------------------------------------------------------- |
| **Administrador** | `admin`    | AdminView (`:3000`)     | Diseña misiones (etapas, Trivia, Búsqueda del Tesoro, pistas), gestiona operadores, consulta auditoría  |
| **Operador**      | `operator` | OperadorView (`:3001`)  | Crea/supervisa sesiones de misiones asignadas, aprueba equipos, libera pistas, penaliza, pausa/finaliza |
| **Jugador**       | `player`   | PlayerMobile (`:19000`) | Crea/une equipos, juega, envía respuestas/QR, ve ranking y pistas liberadas en tiempo real              |

**Login unificado (admin/operador):** LoginView (`:3002`) → redirige a Admin u Operador con tokens.

---

## 2. Arquitectura general

### Estilo

- **Monorepo** con **arquitectura de microservicios**.
- Cada microservicio de negocio sigue **Clean Architecture / DDD**:
  - `Domain` → agregados, entidades, value objects, eventos, puertos
  - `Application` → casos de uso CQRS (MediatR), validadores FluentValidation
  - `Infrastructure` → EF Core, RabbitMQ, clientes HTTP, Keycloak
  - `WebApi` → controllers delgados, auth, SignalR (solo Session)
- Clientes hablan con un **único punto de entrada**: **API Gateway YARP** (`:5200`).
- Los microservicios se hablan entre sí por **HTTP interno** (ACL) y por **RabbitMQ** (eventos de integración).
- Identidad: **Keycloak** (OIDC/JWT). Auth de negocio: **UserService**.

### Diagrama de alto nivel

```
┌─────────────┐  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐
│ LoginView   │  │ AdminView    │  │ OperadorView│  │ PlayerMobile │
│ :3002       │  │ :3000        │  │ :3001       │  │ :19000       │
└──────┬──────┘  └──────┬───────┘  └──────┬──────┘  └──────┬───────┘
       │                │                 │                 │
       └────────────────┴────────┬────────┴─────────────────┘
                                 ▼
                    ┌────────────────────────┐
                    │   API Gateway (YARP)   │
                    │      :5200 → 8080      │
                    └────────────┬───────────┘
         ┌───────────┬───────────┼───────────┬───────────┐
         ▼           ▼           ▼           ▼           │
   UserService  MissionMgmt  SessionMgmt  ScoringAudit   │
     :5284        :5260        :5278        :5290         │
         │           │           │           │            │
         └───────────┴─────┬─────┴───────────┘            │
                           ▼                              │
        ┌──────────────────────────────────┐              │
        │ PostgreSQL :5432 (umbral_db)     │              │
        │ RabbitMQ :5672 / UI :15672       │◄─────────────┘
        │ Keycloak :8081                   │  (Player también
        │ MailHog SMTP :1025 / UI :8025    │   habla a Keycloak)
        │ pgAdmin :5050                    │
        └──────────────────────────────────┘
```

### Stack tecnológico

| Capa         | Tecnología                                           |
| ------------ | ---------------------------------------------------- |
| Backend      | C# / ASP.NET Core **10** (`net10.0`)                 |
| Gateway      | **YARP** (Yet Another Reverse Proxy)                 |
| Persistencia | **EF Core** + **PostgreSQL 17**                      |
| Mensajería   | **RabbitMQ** (exchange topic `umbral.domain.events`) |
| Tiempo real  | **SignalR** (`/hubs/live-session`)                   |
| Identidad    | **Keycloak 24** (realm `umbral-realm`)               |
| CQRS         | **MediatR** + FluentValidation                       |
| Front web    | **Next.js 16** + React 19 + Tailwind                 |
| Front móvil  | **Expo 54** / React Native                           |
| Contenedores | **Docker Compose**                                   |

---

## 3. Cumplimiento de exigencias mínimas de diseño

| Exigencia | Patrón / concern | Implementación principal |
|-----------|------------------|--------------------------|
| Cross-cutting | Logging, excepciones, validación, seguridad | [`Umbral.Shared`](#4-librería-compartida-umbralshared) |
| Strategy | Cálculo de puntaje por tipo de nodo | `ScoringAudit.Domain/Services/*ScoreStrategy` + `ProcessEvidenceValidatedHandler` |
| Composite | Jerarquía misión → etapas → juegos | `Mission` + `MissionNode` |
| Facade | Coordinación de sesión y publicación de eventos | `SessionOperationFacade` |
| Proxy | Acceso a pistas y paneles restringidos | `DraftOnlyHintProxy`, `PlayerReleasedHintsProxy` |
| Template Method | Flujo de procesamiento de evidencias | `EvidenceSubmissionProcessor` + subclases |
| State | Ciclo de vida de sesión | `LiveSession` + `LiveSessionStatus` |
| Chain of Responsibility | Validaciones de evidencias | `EvidenceValidatorService` + handlers encadenados |
| CQRS | Separar escrituras/lecturas | MediatR Commands/Queries en Application |
| DDD | Bounded contexts independientes | Aggregates, VOs, Domain Events, Repositories |
| ACL | Anti-Corruption Layer entre contextos | Clientes HTTP Keycloak / Mission / Session / User |

### Flujo de eventos (evidencia → puntaje)

```
Jugador → LiveSessionsController
       → SessionOperationFacade
       → EvidenceSubmissionProcessor (Template Method)
       → EvidenceValidatorService (Chain of Responsibility)
       → LiveSession (State + dominio)
       → RabbitMqDomainEventPublisher
       → ScoringAuditRabbitMqConsumer
       → ProcessEvidenceValidatedHandler
       → ScoreCalculatorService (Strategy)
       → TeamLedger
       → scoring.team.score.updated
       → SessionManagement → SignalR (ranking en vivo)
```

---

## 4. Librería compartida: Umbral.Shared

Componentes transversales reutilizados por los microservicios WebApi.

### Auth y seguridad

- `KeycloakAuthOptions`: Configuración de realm, cliente y roles (`admin`, `operator`, `player`).
- Autenticación de negocio: Mission / Session / Scoring **no** validan JWT contra Keycloak directamente; llaman a `POST /api/v1/auth/validate` en UserService (`UserServiceAuthenticationHandler`).
- `ICurrentUser` / `CurrentUser`: Contexto del usuario autenticado (sub, roles).
- `EnsureOperatorMatchesRouteAttribute`: Valida que el `operatorId` de la ruta coincida con el JWT (salvo rol `admin`).

### Cross-cutting

- `ExceptionHandlingMiddleware`: Mapeo unificado de excepciones a `ProblemDetails` HTTP con logging.
- `CorrelationIdMiddleware`: Propaga `X-Correlation-Id` en requests y logs (Serilog).
- `ValidationBehavior`: Pipeline MediatR con FluentValidation.
- `UmbralServiceCollectionExtensions`: `AddUmbralCrossCutting`, `UseUmbralCrossCutting`, `AddUmbralSerilog`.

### Mensajería

- `RabbitMqOptions`: Configuración de host, exchange (`umbral.domain.events`).
- `RabbitMqPublisher` / `IRabbitMqPublisher`: Publicación de eventos de integración.
- `RabbitMqConsumerHostedService`: Base para consumidores por cola y routing key.
- `IntegrationEvents`: Contratos (`EvidenceValidatedIntegrationEvent`, `TeamRegisteredIntegrationEvent`, `SessionFinalizedIntegrationEvent`, …).

### Capa WebApi — Mapeo HTTP → CQRS

Convención compartida por los microservicios WebApi:

1. **`Contracts/Routes/`** — `record struct` inmutables cuyas propiedades coinciden con tokens de ruta (y query cuando aplica), p. ej. `OperatorSessionRoute`, `MissionNodeRoute`, `HintParentRoute`.
2. **`Contracts/`** — DTOs de body JSON (`CreateLiveSessionRequest`, `AddHintRequest`, etc.).
3. **`Mapping/`** — extensiones `ToCommand()` / `ToQuery()` que ensamblan Commands/Queries MediatR sin lógica en el controlador.
4. **Controladores delgados** — reciben `Route` + `[FromBody] body` y delegan en `_mediator.Send(body.ToCommand(route))`.

Pistas (HU-17): `POST …/hints` usa **JSON** `{ "content": "..." }` (sin adjuntos multipart).

---

## 5. Microservicios backend

Hay **4 microservicios de dominio/IAM** + **1 API Gateway**.

### 5.1 API Gateway (`ApiGateway/`)

|                   |                                                                                          |
| ----------------- | ---------------------------------------------------------------------------------------- |
| **Proyecto**      | `Umbral.ApiGateway`                                                                      |
| **Puerto Docker** | **5200** (host) → 8080 (contenedor)                                                      |
| **Rol**           | Reverse proxy: enruta por path hacia los 4 backends. CORS para frontends. `GET /health`. |
| **Clase clave**   | `Program.cs` — `MapReverseProxy()`                                                       |

**Rutas principales (YARP):**

| Path                                                                              | Destino                                                                                               |
| --------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `/api/v1/auth/**`, `/admins/**`, `/operators/**` (genérico), `/players/**`        | **UserService**                                                                                       |
| `/api/v1/missions/**`                                                             | **MissionManagement**                                                                                 |
| `/api/v1/sessions/**`, `/teams/**`, `/live-sessions/**`, rutas operador de sesión | **SessionManagement**                                                                                 |
| `/api/v1/sessions/{id}/ranking/**`, `/audit/**`                                   | **ScoringAudit**                                                                                      |
| `/hubs/**` (SignalR)                                                              | **SessionManagement**                                                                                 |
| Excepciones de path                                                               | p. ej. `players/{id}/team-membership` → Session; validaciones de sesión bajo `missions/...` → Session |

> Los clientes **nunca** deben apuntar a 5260/5278/5290/5284 en producción local de demo: todo va por `:5200`.

---

### 5.2 UserService (`UserService/`) — IAM

|                     |                                                                                                                                                                  |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Puerto Docker**   | **5284**                                                                                                                                                         |
| **Responsabilidad** | Login/refresh, validación de tokens, CRUD usuarios (admin/operador/jugador), sync Keycloak ↔ PostgreSQL, códigos de activación de operadores (email vía MailHog) |
| **Capas**           | Domain / Application / Infrastructure / WebApi                                                                                                                   |

**Clases corazón:**

| Clase                                                                            | Por qué importa                                             |
| -------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `User` (Domain Aggregate)                                                        | Modelo de usuario de negocio                                |
| `AuthenticateUserHandler` / `ValidateTokenHandler`                               | Login y validación usada por el resto de microservicios     |
| `AuthController`, `AdminsController`, `OperatorsController`, `PlayersController` | Superficie REST                                             |
| `KeycloakIdentityService`, `KeycloakAuthService`                                 | ACL hacia Keycloak                                          |
| `KeycloakBootstrapHostedService`                                                 | Crea admin por defecto `admin@umbral.com` / `Admin123!`     |
| `HttpSessionValidationService`                                                   | Valida con SessionManagement antes de desactivar operadores |

---

### 5.3 MissionManagement (`MissionManagement/`) — Catálogo de misiones

|                        |                                                                                                                                 |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Puerto Docker**      | **5260**                                                                                                                        |
| **Responsabilidad**    | Misiones (Draft → Active → Inactive), árbol de nodos (etapas + juegos), Trivia, Treasure Hunt, pistas, asignación de operadores |
| **Publica a RabbitMQ** | `mission.activated` (snapshot de nodos puntuables)                                                                              |

#### Domain

| Clase | Patrón / rol |
| ----- | ------------ |
| `Mission` | Aggregate Root + **Composite** (misión → etapas → juegos); protege invariantes de edición |
| `MissionNode` | Entidad de árbol (etapas y juegos); reglas por tipo de nodo |
| `Hint` | Pistas ordenadas con penalización asociada a un nodo |
| `MissionActivatedEvent` | Domain event al activar + snapshot de nodos puntuables |
| `ActivatedNodeSnapshot` | Payload del evento (nodos y puntaje base) |
| `IMissionRepository` | Puerto de persistencia del agregado |
| `DifficultyLevel`, `GpsCoordinate`, `OperatorRef`, `TriviaQuestion` | Value Objects |

#### Application

| Clase | Rol |
| ----- | --- |
| `CreateMissionHandler`, `UpdateMissionDetailsHandler`, `DeactivateMissionHandler` | Ciclo de vida de misión |
| `AssignOperatorToMissionHandler`, `RevokeOperatorFromMissionHandler` | Asignación de operadores (revocación valida sesiones vía ACL) |
| `AddRootNodeHandler`, `AddTriviaNodeHandler`, `AddTreasureHuntNodeHandler`, `AddHintHandler` | Composición del árbol |
| `GetMissionsHandler`, `GetMissionByIdHandler`, `GetHintsByNodeHandler` | Consultas |
| `IHintAccessService` / `MissionHintService` | Puerto e implementación real de acceso a pistas |
| `DraftOnlyHintProxy` | **Proxy** — restringe lectura de pistas según rol/estado (borrador / admin-operador) |
| `ISessionValidationService` | Puerto ACL: ¿hay sesiones abiertas? (RN-01) |
| `IDomainEventPublisher` | Puerto de publicación de eventos de dominio |
| `NotFoundException`, `ConflictException`, `UnauthorizedException` | Excepciones de aplicación |

#### Infrastructure

| Clase | Rol |
| ----- | --- |
| `MissionManagementDbContext`, `MissionRepository` | EF Core + adaptador Repository |
| `MissionConfiguration`, `MissionNodeConfiguration`, `HintConfiguration` | Fluent mapping |
| `RabbitMqDomainEventPublisher` | Publica `MissionActivatedEvent` al bus |
| `HttpSessionValidationService` | Cliente HTTP hacia SessionManagement |
| `HttpOperatorValidationService` | Valida operador activo en UserService |
| `FakeIdentityService`, `FakeSessionValidationService` | Adaptadores simulados para pruebas locales |

#### WebApi

- Controladores: `MissionsController`, `NodesController`, `TriviaController`, `TreasureHuntsController`, `HintsController`, `MissionOperatorsController`.
- Endpoints de integración: lecturas `node-validations`, `session-validation/has-open`, GET misión/hints para SessionManagement.
- `Program`: Composition root — Umbral.Shared, MediatR, EF Core, RabbitMQ.

---

### 5.4 SessionManagement (`SessionManagement/`) — Motor en vivo

|                        |                                                                                                                                                                               |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Puerto Docker**      | **5278**                                                                                                                                                                      |
| **Responsabilidad**    | Equipos, join requests, sesiones live, evidencias, pistas liberadas, penalizaciones, pause/finalize, **SignalR**                                                              |
| **Publica a RabbitMQ** | `session.evidence.validated`, `session.team.registered`, `session.started`, `session.finalized`, `session.hint.released`, `session.penalty.applied`, `session.team.completed` |
| **Consume**            | `scoring.team.score.updated` → reenvía a SignalR                                                                                                                              |

#### Domain

| Clase | Patrón / rol |
| ----- | ------------ |
| `LiveSession` | Aggregate Root + **State** (Scheduled → Preparation → Active ↔ Paused → Finalized/Cancelled) |
| `LiveSessionStatus` | Enum con matriz de transiciones válidas (RB-09) |
| `Team` | Aggregate Root (miembros, código, bloqueos) |
| `EvidenceSubmission`, `ReleasedHint`, `SessionJoinRequest`, `TeamParticipation`, `TeamMember` | Entidades de sesión |
| `ILiveSessionRepository`, `ITeamRepository` | Puertos de persistencia |
| `AllowedNode`, `TeamCode`, `NodeValidationRule`, `SubmissionResult` | Value Objects |
| Eventos | `TeamRegisteredEvent`, `SessionStartedEvent`, `SessionStateChangedEvent`, `SessionFinalizedEvent`, `EvidenceValidatedEvent`, `HintReleasedEvent`, `ManualPenaltyAppliedEvent`, `SessionJoinRequestCreatedEvent`, `SessionJoinRequestResolvedEvent`, `SupportMessageSentEvent`, `TeamCompletedMissionEvent` |
| `SessionDomainException` | Excepción de dominio |

#### Application — patrones clave

**Facade (coordinación de sesión)**

- `ISessionOperationFacade` / `SessionOperationFacade`: unifica crear/iniciar/finalizar/cancelar sesión, envío de evidencias, persistencia y publicación de eventos.

**Chain of Responsibility (validación de evidencias)**

- `EvidenceValidatorService`: Compone y ejecuta la cadena.
- `IEvidenceValidationHandler` / `EvidenceValidationHandlerBase`: Eslabón base con `SetNext()`.
- `SessionActiveValidationHandler`: RB-03 — sesión Active.
- `TeamRegisteredValidationHandler`: Equipo registrado.
- `NodeAllowedValidationHandler`: RB-05 — nodo en snapshot.
- `SequentialProgressValidationHandler`: RN-04, RN-11 — progresión secuencial.
- `AnswerCorrectnessValidationHandler`: RN-12 — corrección de respuesta.

**Template Method (procesamiento de evidencias)**

- `EvidenceSubmissionProcessor`: Flujo template `Process()` (validar → aceptar → marcar válida/inválida → resultado).
- `TriviaEvidenceSubmissionProcessor` / `TreasureHuntEvidenceSubmissionProcessor` (normaliza código en mayúsculas).

**Proxy (panel de pistas jugador)**

- `IPlayerHintPanelService` / `PlayerReleasedHintsProxy`: solo pistas liberadas al equipo (`ReleasedHint`).

**CQRS (handlers delegan al Facade)**

- Ciclo de vida: `CreateLiveSessionHandler`, `StartLiveSessionHandler`, `FinalizeLiveSessionHandler`, `CancelLiveSessionHandler`.
- Evidencias: `SubmitTriviaAnswerHandler`, `SubmitTreasureHuntCodeHandler`.
- Join: `JoinSessionHandler` (Pending) + `ProcessSessionJoinRequestHandler` (aprueba/rechaza).
- Controles operador: `ReleaseManualHintHandler`, `ApplyManualPenaltyHandler`, `SendSupportMessageHandler`, `ToggleSessionPauseHandler`.
- Consultas: `GetActiveSessionsHandler`, `GetTeamCurrentStageHandler`, `GetOperatorAssignedMissionsHandler`, `GetSessionTeamsHandler`, `GetSessionJoinRequestsHandler`, etc.
- `ILiveSessionRealtimeNotifier`: puerto de broadcast en tiempo real.
- `IMissionIntegrationService`: ACL hacia MissionManagement.
- `IDomainEventPublisher`: publicación post-persistencia.

#### Infrastructure

- `SessionManagementDbContext`, `LiveSessionRepository`, configuraciones EF Core.
- `HttpMissionIntegrationService`: Cliente HTTP hacia MissionManagement.
- `RabbitMqDomainEventPublisher`: Publica eventos tipados y notifica SignalR.
- `SessionScoreUpdateRabbitMqConsumer`: Escucha scores de ScoringAudit.
- `FakeMissionIntegrationService`: Adaptador simulado.

#### WebApi

- Controladores: `LiveSessionsController`, `OperatorSessionsController`, `OperatorSessionControlController`, `SessionJoinRequestsController`, `OperatorSessionValidationController`, `TeamsController`, `PlayerHintsController`, `MissionSessionValidationController`, `PlayerTeamMembershipController`.
- `LiveSessionHub` (`/hubs/live-session`): grupos `Session_{id}`, `Operator_Session_{id}`, `Session_{id}_Team_{teamId}`.
- `SignalRLiveSessionRealtimeNotifier`: Bridge domain events → clientes.
- `OperatorSessionValidationController`: `has-active`, `is-supervising` para MissionManagement.
- `Program`: Composition root — Umbral.Shared, Facade, cadena de validación, processors, RabbitMQ, SignalR.

---

### 5.5 ScoringAudit (`ScoringAudit/`) — Puntaje y auditoría

|                     |                                                                                                                                    |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| **Puerto Docker**   | **5290**                                                                                                                           |
| **Responsabilidad** | Ledgers por equipo, ranking, timeline de auditoría (append-only). **Event-driven**: casi no recibe comandos de escritura por REST. |
| **Consume**         | Eventos `session.*` de SessionManagement                                                                                           |
| **Publica**         | `scoring.team.score.updated` (para SignalR vía Session)                                                                            |

#### Domain

| Clase | Patrón / rol |
| ----- | ------------ |
| `TeamLedger` | Aggregate Root — libro mayor de puntos por equipo/sesión |
| `AuditLog` | Aggregate append-only; se sella al finalizar (RN-17) |
| `ScoreEntry`, `SessionEvent` | Historial inmutable |
| `ScoreOrigin` | Origen trazable; `ComputedScore` respeta `FinalScore` de la Strategy |
| `PenaltyReason` (incluye `AppliedByOperatorId`), `RankingEntry` | Value Objects |
| `IScoreCalculationStrategy` | Interfaz **Strategy** |
| `TriviaScoreStrategy` | Bonificación por velocidad de respuesta |
| `TreasureHuntScoreStrategy` | Puntaje base × dificultad sin bonificación temporal |
| `ScoreCalculatorService` | Contexto Strategy — selecciona por `NodeType` |
| `RankingManagerService` | Orden por puntos + desempate por tiempo (RB-08 / RN-09) |
| `ITeamLedgerRepository`, `IAuditLogRepository` | Puertos de persistencia |
| `ScoringDomainException` | Excepción de dominio |

#### Application

- Escritura vía eventos: `ProcessEvidenceValidatedHandler`, `ProcessTeamRegisteredHandler`, `ProcessSessionStartedHandler`, `ProcessHintReleasedHandler`, `ProcessManualPenaltyHandler`, `ProcessTeamCompletedHandler`, `ProcessSessionFinalizedHandler`.
- Lecturas: `GetSessionRankingHandler`, `GetHistoricalSessionsHandler`, `GetSessionAuditDetailHandler`.

#### Infrastructure / WebApi

- `ScoringAuditDbContext`, `TeamLedgerRepository`, `AuditLogRepository`.
- `ScoringAuditRabbitMqConsumer`: Consume eventos y despacha handlers MediatR.
- `RankingController`: `GET /api/v1/sessions/{sessionId}/ranking`.
- `AuditController`: `GET /api/v1/audit/sessions` y `GET /api/v1/audit/sessions/{sessionId}` (HU-64/65).
- `TeamPenaltiesController`: lecturas de penalizaciones.
- `GET /health`: Health check (`[AllowAnonymous]`).

---

## 6. Infraestructura externa y puertos

### Tabla maestra (Docker Compose)

| Servicio              | Contenedor                          | Puerto(s) host                       | Uso                                                          |
| --------------------- | ----------------------------------- | ------------------------------------ | ------------------------------------------------------------ |
| **API Gateway**       | `umbral-api-gateway`                | **5200**                             | Único entrypoint de APIs + SignalR proxy                     |
| **UserService**       | `umbral-user-service`               | **5284**                             | IAM                                                          |
| **MissionManagement** | `umbral-mission-management-service` | **5260**                             | Misiones                                                     |
| **SessionManagement** | `umbral-session-management-service` | **5278**                             | Sesiones + SignalR                                           |
| **ScoringAudit**      | `umbral-scoring-audit-service`      | **5290**                             | Ranking / audit                                              |
| **PostgreSQL**        | `umbral-db`                         | **5432**                             | BD compartida `umbral_db` (user/pass: `postgres`/`postgres`) |
| **pgAdmin**           | `umbral-pgadmin`                    | **5050**                             | UI DB (`admin@umbral.com` / `admin`)                         |
| **RabbitMQ**          | `umbral-mq`                         | **5672** AMQP · **15672** management | Eventos (`guest`/`guest`)                                    |
| **Keycloak**          | `umbral-keycloak`                   | **8081**                             | OIDC realm `umbral-realm` (consola master: `admin`/`admin`)  |
| **MailHog**           | `umbral-mailhog`                    | **1025** SMTP · **8025** UI          | Emails de activación de operadores                           |
| **AdminView**         | *(perfil frontend/full)*            | **3000**                             | Consola admin                                                |
| **OperadorView**      | *(perfil frontend/full)*            | **3001**                             | Panel operador                                               |
| **LoginView**         | *(perfil frontend/full)*            | **3002**                             | Login web                                                    |
| **PlayerMobile web**  | *(perfil player/full)*              | **19000**                            | Jugador en navegador                                         |

> **No hay Redis** en el proyecto.

### Dependencias Compose (backends)

| Servicio | Dependencias (Compose) |
|----------|------------------------|
| `mission-management-service` | PostgreSQL, RabbitMQ, Keycloak (`healthy`) |
| `session-management-service` | PostgreSQL, RabbitMQ, MissionManagement *(JWT vía UserService/Keycloak en runtime)* |
| `scoring-audit-service` | PostgreSQL, RabbitMQ *(JWT vía UserService/Keycloak en runtime)* |

### Cómo se conectan

| Desde                       | Hacia                 | Cómo                                                                |
| --------------------------- | --------------------- | ------------------------------------------------------------------- |
| Frontends                   | Gateway `:5200`       | HTTP REST + WebSocket SignalR                                       |
| PlayerMobile                | Keycloak `:8081`      | Token password grant (cliente `umbral-player-mobile`)               |
| LoginView                   | Gateway → UserService | `POST /api/v1/auth/token` (UserService habla con Keycloak)          |
| UserService                 | Keycloak              | Admin API + token endpoint                                          |
| UserService                 | MailHog               | SMTP `:1025`                                                        |
| Mission / Session / Scoring | UserService           | `POST /auth/validate` (Bearer)                                      |
| Mission ↔ Session           | HTTP                  | Validación de sesiones abiertas / detalle misión / node-validations |
| Mission → UserService       | HTTP                  | ¿Operador activo?                                                   |
| Session → Mission           | HTTP                  | Snapshot misión, hints, validaciones                                |
| Session → RabbitMQ          | Publish               | Eventos `session.*`                                                 |
| Mission → RabbitMQ          | Publish               | `mission.activated`                                                 |
| Scoring ← RabbitMQ          | Consume               | Cola `scoring-audit.events`                                         |
| Scoring → RabbitMQ          | Publish               | `scoring.team.score.updated`                                        |
| Session ← RabbitMQ          | Consume               | Actualiza scores y empuja SignalR                                   |
| Todos los backends          | PostgreSQL            | Misma BD `umbral_db`, schemas/tablas por bounded context (EF)       |

### Levantar el stack

```bash
# Todo (recomendado para demo)
docker compose --profile full up -d --build

# Solo infra + backends
docker compose up -d
```

Keycloak tarda ~2–3 min la primera vez. Admin de la **app** (no de la consola Keycloak):

- Email: `admin@umbral.com`
- Password: `Admin123!`

---

## 7. Frontends

| App                  | Stack                  | Puerto | Auth                                                          | APIs                                                |
| -------------------- | ---------------------- | ------ | ------------------------------------------------------------- | --------------------------------------------------- |
| **LoginView-WEB**    | Next.js                | 3002   | `POST /auth/token`, setup-password operador                   | Solo UserService vía gateway                        |
| **AdminView-WEB**    | Next.js + Leaflet + QR | 3000   | JWT en `sessionStorage` (viene de LoginView), rol `admin`     | Missions, Operators, Audit                          |
| **OperadorView-WEB** | Next.js                | 3001   | JWT, rol `operator` (o admin); `operatorId` = `sub` del token | Sessions, join-requests, live board, ranking, audit |
| **PlayerMobile**     | Expo / RN              | 19000  | Keycloak directo (`umbral-player-mobile`) + SecureStore       | Teams, live-sessions, gameplay, ranking, SignalR    |

**Componentes UI clave:**

- Admin: `MissionBuilder`, `MissionCatalog`, `OperatorManagement`, `AuditHistory`, `AuthGuard`
- Operador: `OperatorDashboard`, `WaitingRoomView`, `LiveSessionView`, `ApplyPenaltyDialog`
- Login: `LoginForm`, `authService`
- Player: `AuthProvider`, `useLiveSessionGameplay`, `PlayPanel`, `TriviaQuestionPanel`, `QrCodeScannerModal`, `RankingPanel`, `signalRService`

---

## 8. Flujo de negocio extremo a extremo

```
1. Admin diseña misión (Draft) → agrega etapas/Trivia/Treasure/pistas
2. Admin activa misión → MissionActivatedEvent (RabbitMQ) + snapshot de nodos
3. Admin asigna operador(es) a la misión
4. Operador crea LiveSession sobre esa misión
5. Equipos solicitan unirse (Pending) → Operador aprueba (RN-15)
6. Operador Start → estado Active → cronómetro
7. Jugador responde Trivia o escanea QR
      → Facade → Chain validación → Template Method → LiveSession
      → publica session.evidence.validated
8. ScoringAudit consume → Strategy calcula score → TeamLedger
      → publica scoring.team.score.updated
9. SessionManagement consume → SignalR actualiza ranking en vivo
10. Operador libera pistas / aplica penalizaciones (con motivo RN-10)
11. Operador Finalize → sesión sellada (RN-17) → audit de solo lectura
```

### Estados de sesión

`Scheduled` → `Preparation` → `Active` ⇄ `Paused` → `Finalized` / `Cancelled`

### Tipos de juego (RN-02)

- **Trivia:** un intento; validación automática; si falla → 0 pts y avanza.
- **Treasure Hunt:** avance solo con código QR correcto; GPS en diseño de nodo.

### Progresión (RN-11)

Estrictamente secuencial: no se salta el juego N.

---

## 9. Reglas de negocio que debes poder explicar

| ID       | Idea en una frase                                              |
| -------- | -------------------------------------------------------------- |
| RN-01    | No editar misión/nodos/pistas si hay sesión activa/pausada     |
| RN-03    | Solo se juega con sesión **Active**                            |
| RN-07    | Pistas del catálogo solo visibles tras liberación del operador |
| RN-08/09 | Puntaje = juegos − penalizaciones; desempate por menor tiempo  |
| RN-10    | Penalización exige motivo                                      |
| RN-11    | Progresión secuencial estricta                                 |
| RN-15    | No Start sin ≥1 equipo aprobado                                |
| RN-16    | Operador solo ve misiones asignadas                            |
| RN-17    | Tras Finalize, todo es inmutable (auditoría)                   |

Lista completa: [`reglas_negocio.md`](./reglas_negocio.md).

---

## 10. Autenticación (cómo explicarla en defensa)

```
Admin / Operador:
  LoginView → UserService /auth/token → Keycloak (cliente umbral-web)
           → redirect con tokens → Admin/Operador
           → cada request: Bearer JWT → Gateway → microservicio
           → microservicio llama UserService /auth/validate

Jugador:
  PlayerMobile → Keycloak directo (cliente umbral-player-mobile)
              → Bearer en APIs vía Gateway
              → rol player obligatorio en la app
```

Roles realm: `admin`, `operator`, `player`.

Operadores nuevos: Admin los crea → código por email (MailHog `:8025`) → LoginView “Activate account” → `setup-password`.

---

## 11. Eventos RabbitMQ (cheat sheet)

**Exchange:** `umbral.domain.events` (topic)

| Routing key                  | Publica           | Consume                     |
| ---------------------------- | ----------------- | --------------------------- |
| `mission.activated`          | MissionManagement | (snapshot / integración)    |
| `session.evidence.validated` | SessionManagement | ScoringAudit                |
| `session.team.registered`    | SessionManagement | ScoringAudit                |
| `session.started`            | SessionManagement | ScoringAudit                |
| `session.finalized`          | SessionManagement | ScoringAudit                |
| `session.hint.released`      | SessionManagement | ScoringAudit                |
| `session.penalty.applied`    | SessionManagement | ScoringAudit                |
| `session.team.completed`     | SessionManagement | ScoringAudit                |
| `scoring.team.score.updated` | ScoringAudit      | SessionManagement → SignalR |

Si ScoringAudit estuvo caído: endpoint de **reconcile** en SessionManagement reemite eventos.

---

## 12. Pendientes conocidos

- **Modo de sesión como Strategy**: La variación de puntaje es por `NodeType` + multiplicador de dificultad, no por modo de sesión.
- **State GoF estricto**: `LiveSession` usa matriz de transiciones (enum + switch), no clases de estado por objeto.
- **Documentación de dominio**: Actualizar `docs/Resumen-Dominio.md` si se usa en informes académicos.

---

## 13. Guion oral sugerido (2–3 minutos)

1. **Problema:** operar experiencias inmersivas en vivo (admin diseña, operador controla, jugadores compiten).
2. **Arquitectura:** microservicios .NET 10 + Gateway YARP + PostgreSQL + RabbitMQ + Keycloak + SignalR; frontends Next.js y Expo.
3. **Bounded contexts:** User (IAM), Mission (catálogo), Session (motor live), Scoring (event-driven).
4. **Patrones:** Composite, Facade, Proxy, Template, Chain, Strategy, State + CQRS/DDD.
5. **Flujo estrella:** evidencia → RabbitMQ → score → SignalR ranking en vivo.
6. **Demo mental de puertos:** clientes en 3000/3001/3002/19000 → Gateway **5200** → backends 5284/5260/5278/5290; infra 5432, 5672, 8081.

---

## 14. Credenciales y URLs rápidas

| Qué                      | Valor                                                        |
| ------------------------ | ------------------------------------------------------------ |
| Gateway                  | [http://localhost:5200](http://localhost:5200)               |
| Admin / Operador / Login | [http://localhost:3000](http://localhost:3000) · 3001 · 3002 |
| Player web               | [http://localhost:19000](http://localhost:19000)             |
| Keycloak                 | [http://localhost:8081](http://localhost:8081)               |
| RabbitMQ UI              | [http://localhost:15672](http://localhost:15672)             |
| MailHog UI               | [http://localhost:8025](http://localhost:8025)               |
| Admin app                | `admin@umbral.com` / `Admin123!`                             |
| Keycloak console         | `admin` / `admin` (realm `master`)                           |

---

## 15. Documentación de apoyo

| Documento                                            | Contenido                        |
| ---------------------------------------------------- | -------------------------------- |
| [`../README.md`](../README.md)                       | Cómo levantar el monorepo        |
| [`../backend-endpoints.md`](../backend-endpoints.md) | Catálogo REST                    |
| [`reglas_negocio.md`](./reglas_negocio.md)           | RN-01 … RN-18                    |
| [`Resumen-Dominio.md`](./Resumen-Dominio.md)         | Dominio DDD académico            |

> Nota: la **fuente de verdad** operativa es el código + `docker-compose.yml` + este resumen unificado. Docs antiguos que no listen UserService o el Gateway están desactualizados.

---

*Última actualización — Julio 2026 (fusión architecture-map + resumen de defensa)*
