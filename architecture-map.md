# Mapa de Arquitectura Backend

Documento guía del backend UMBRAL. Describe microservicios, capas, patrones de diseño aplicados y componentes transversales.

---

## Cumplimiento de exigencias mínimas de diseño

| Exigencia | Patrón / concern | Implementación principal |
|-----------|------------------|------------------------|
| Cross-cutting | Logging, excepciones, validación, seguridad | [`Umbral.Shared`](#librería-compartida-umbralshared) |
| Strategy | Cálculo de puntaje por tipo de nodo | `ScoringAudit.Domain/Services/*ScoreStrategy` + `ProcessEvidenceValidatedHandler` |
| Composite | Jerarquía misión → etapas → juegos | `Mission` + `MissionNode` |
| Facade | Coordinación de sesión y publicación de eventos | `SessionOperationFacade` |
| Proxy | Acceso a pistas y paneles restringidos | `DraftOnlyHintProxy`, `PlayerReleasedHintsProxy` |
| Template Method | Flujo de procesamiento de evidencias | `EvidenceSubmissionProcessor` + subclases |
| State | Ciclo de vida de sesión | `LiveSession` + `LiveSessionStatus` |
| Chain of Responsibility | Validaciones de evidencias | `EvidenceValidatorService` + handlers encadenados |

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
```

---

## Librería compartida: Umbral.Shared

Componentes transversales reutilizados por los tres microservicios WebApi.

### Auth y seguridad
- `KeycloakAuthOptions`: Configuración de realm, cliente y roles (`admin`, `operator`, `player`).
- `UmbralAuthenticationExtensions.AddUmbralAuthentication`: Registro JWT Bearer contra Keycloak.
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
- `IntegrationEvents`: Contratos (`EvidenceValidatedIntegrationEvent`, `TeamRegisteredIntegrationEvent`, `SessionFinalizedIntegrationEvent`).

### Capa WebApi — Mapeo HTTP → CQRS

Convención compartida por los tres microservicios WebApi:

1. **`Contracts/Routes/`** — `record struct` inmutables cuyas propiedades coinciden con tokens de ruta (y query cuando aplica), p. ej. `OperatorSessionRoute`, `MissionNodeRoute`, `HintParentRoute`.
2. **`Contracts/`** — DTOs de body JSON existentes (`CreateLiveSessionRequest`, `AddHintRequest`, etc.).
3. **`Mapping/`** — extensiones `ToCommand()` / `ToQuery()` que ensamblan Commands/Queries MediatR sin lógica en el controlador.
4. **Controladores delgados** — reciben `Route` + `[FromBody] body` y delegan en `_mediator.Send(body.ToCommand(route))`.

Pistas (HU-17): `POST …/hints` usa **JSON** `{ "content": "..." }` (sin adjuntos multipart).

---

## Microservicio: MissionManagement

### Capa: Domain
- `Mission`: Agregado raíz que gobierna el ciclo de vida de la misión, protege invariantes de edición y aplica composición jerárquica de nodos (Patrón: **Aggregate Root + Composite**).
- `MissionNode`: Entidad de árbol que modela etapas y juegos, delegando reglas por tipo de nodo (Patrón: **Entity + Composite**).
- `Hint`: Entidad que representa pistas ordenadas con penalización asociada a un nodo (Patrón: Entity).
- `MissionActivatedEvent`: Evento de dominio que publica la activación y snapshot de nodos puntuables (Patrón: Domain Event).
- `ActivatedNodeSnapshot`: Estructura de datos del evento para transportar nodos y puntaje base (Patrón: Event Payload).
- `IMissionRepository`: Puerto de persistencia del agregado de misión y sus consultas de negocio (Patrón: Repository Port).
- `DifficultyLevel`: Objeto de valor que encapsula dificultad y multiplicador de score (Patrón: Value Object).
- `GpsCoordinate`: Objeto de valor que valida coordenadas geográficas para nodos Treasure Hunt (Patrón: Value Object).
- `OperatorRef`: Objeto de valor inmutable para referencia de operador asignado (Patrón: Value Object).
- `TriviaQuestion`: Objeto de valor que contiene pregunta, opciones y respuesta correcta (Patrón: Value Object).

### Capa: Application
- `CreateMissionHandler`: Caso de uso que crea misiones en borrador validando unicidad del título (Patrón: CQRS Command Handler).
- `CreateMissionCommandValidator`: Validación FluentValidation de entrada (Patrón: Validation).
- `UpdateMissionDetailsHandler`: Caso de uso que actualiza metadatos de misión sin romper reglas de estado (Patrón: CQRS Command Handler).
- `DeactivateMissionHandler`: Caso de uso que desactiva misiones y persiste transición de estado (Patrón: CQRS Command Handler).
- `AssignOperatorToMissionHandler`: Caso de uso que vincula operadores a misiones respetando reglas de asignación (Patrón: CQRS Command Handler).
- `RevokeOperatorFromMissionHandler`: Caso de uso que revoca operadores validando sesiones activas vía integración externa (Patrón: CQRS Command Handler + ACL Port).
- `AddRootNodeHandler`: Caso de uso que agrega etapas raíz de misión (Patrón: CQRS Command Handler).
- `AddTriviaNodeHandler`: Caso de uso que agrega nodos Trivia como hijos de etapa (Patrón: CQRS Command Handler).
- `AddTreasureHuntNodeHandler`: Caso de uso que agrega nodos Treasure Hunt con validación de coordenadas y código (Patrón: CQRS Command Handler).
- `AddHintHandler`: Caso de uso que crea pistas de texto en nodos de juego (Patrón: CQRS Command Handler).
- `GetMissionsHandler`: Consulta que proyecta misiones para listado de lectura (Patrón: CQRS Query Handler).
- `GetMissionByIdHandler`: Consulta que devuelve detalle de una misión por identificador (Patrón: CQRS Query Handler).
- `GetHintsByNodeHandler`: Consulta que expone pistas asociadas a un nodo vía `IHintAccessService` (Patrón: CQRS Query Handler).
- `IHintAccessService`: Puerto de acceso a pistas (sujeto del patrón **Proxy**).
- `MissionHintService`: Implementación real del acceso a pistas del repositorio de misiones.
- `DraftOnlyHintProxy`: **Proxy** que restringe consulta de pistas según rol y estado de la misión (RN borrador / admin-operador).
- `IIdentityService`: Puerto para gestionar operadores contra Keycloak (Patrón: Integration Port / ACL). Alta sin contraseña (`CreateOperatorAsync` → código de activación) y onboarding (`SetupOperatorPasswordAsync`).
- `ISessionValidationService`: Puerto para validar si un operador tiene sesiones activas antes de cambios críticos (Patrón: Integration Port / ACL).
- `IDomainEventPublisher`: Puerto de publicación de eventos de dominio.
- `NotFoundException`, `ConflictException`, `UnauthorizedException`: Excepciones de aplicación (Patrón: Application Exception).

### Capa: Infrastructure
- `MissionManagementDbContext`: Contexto EF Core que modela tablas y relaciones del bounded context de misiones (Patrón: DbContext).
- `MissionRepository`: Adaptador EF Core que implementa el puerto de repositorio de misiones (Patrón: Repository Adapter).
- `MissionConfiguration`, `MissionNodeConfiguration`, `HintConfiguration`: Mapeos EF Core (Patrón: Fluent Configuration).
- `RabbitMqDomainEventPublisher`: Publica `MissionActivatedEvent` y otros eventos al bus RabbitMQ.
- `KeycloakAuthService`, `KeycloakIdentityService`, `KeycloakPlayerIdentityService`: Integración IAM (Patrón: ACL).
- `KeycloakBootstrapHostedService`, `KeycloakWebClientInitializer`, `KeycloakOperatorProfileInitializer`: Bootstrap al arrancar — cliente OIDC `umbral-web`, usuario admin por defecto y atributos de perfil para códigos de activación de operadores.
- `HttpSessionValidationService`: Cliente HTTP hacia SessionManagement para validación de sesiones abiertas.
- `FakeIdentityService`, `FakeSessionValidationService`: Adaptadores simulados para pruebas locales (Patrón: Fake Adapter).

### Capa: WebApi
- `MissionsController`, `NodesController`, `TriviaController`, `TreasureHuntsController`, `HintsController`, `MissionOperatorsController`, `OperatorsController`, `AdminsController`, `PlayersController`, `AuthController`: Controladores REST delgados con mapeo en `WebApi/Mapping/` (Patrón: API Controller + Role Security).
- `Program`: Composition root — Umbral.Shared (auth, Serilog, middleware, FluentValidation), MediatR, EF Core, RabbitMQ, Keycloak.
- Endpoints `[AllowAnonymous]`: `POST /auth/token`, `POST /auth/operator/setup-password`, `POST /players` (registro), lecturas de integración (`node-validations`, `session-validation/has-open`, GET misión/hints para SessionManagement).
- `SignalR Hubs`: Pendiente.

---

## Microservicio: SessionManagement

### Capa: Domain
- `LiveSession`: Agregado raíz que administra estados de sesión, equipos, evidencias, pistas liberadas y penalizaciones (Patrón: **Aggregate Root + State**).
- `LiveSessionStatus`: Enum con matriz de transiciones válidas RB-09 (Patrón: **State** tabular).
- `Team`: Agregado raíz que modela miembros, código de acceso y reglas de bloqueo operativo (Patrón: Aggregate Root).
- `EvidenceSubmission`: Entidad que registra cada envío de respuesta/evidencia y su validación única (Patrón: Entity).
- `ReleasedHint`: Entidad que registra pistas liberadas por equipo con su penalización aplicada (Patrón: Entity).
- `TeamMember`: Entidad que representa integrantes de un equipo con referencia a identidad externa (Patrón: Entity).
- `ILiveSessionRepository`, `ITeamRepository`: Puertos de persistencia (Patrón: Repository Port).
- `AllowedNode`, `TeamCode`, `NodeValidationRule`, `SubmissionResult`: Value Objects.
- Eventos de dominio: `TeamRegisteredEvent`, `SessionStartedEvent`, `SessionStateChangedEvent`, `SessionFinalizedEvent`, `EvidenceValidatedEvent`, `HintReleasedEvent`, `ManualPenaltyAppliedEvent`.
- `SessionDomainException`: Excepción de dominio (Patrón: Domain Exception).

### Capa: Application

#### Facade (coordinación de sesión)
- `ISessionOperationFacade` / `SessionOperationFacade`: **Facade** que unifica crear/iniciar/finalizar/cancelar sesión, envío de evidencias, persistencia y publicación de eventos de dominio.

#### Chain of Responsibility (validación de evidencias)
- `EvidenceValidatorService`: Compone y ejecuta la cadena de validaciones previas al procesamiento.
- `IEvidenceValidationHandler` / `EvidenceValidationHandlerBase`: Eslabón base con `SetNext()`.
- `SessionActiveValidationHandler`: RB-03 — sesión en estado Active.
- `TeamRegisteredValidationHandler`: Equipo registrado en la sesión.
- `NodeAllowedValidationHandler`: RB-05 — nodo permitido en snapshot.
- `SequentialProgressValidationHandler`: RN-04, RN-11 — progresión secuencial.
- `AnswerCorrectnessValidationHandler`: RN-12 — corrección de respuesta.

#### Template Method (procesamiento de evidencias)
- `EvidenceSubmissionProcessor`: Clase abstracta con flujo template `Process()` (validar → aceptar → marcar válida/inválida → resultado).
- `TriviaEvidenceSubmissionProcessor`: Variante Trivia.
- `TreasureHuntEvidenceSubmissionProcessor`: Variante Treasure Hunt (normaliza código en mayúsculas).

#### CQRS (handlers delegan al Facade)
- `CreateLiveSessionHandler`, `StartLiveSessionHandler`, `FinalizeLiveSessionHandler`, `CancelLiveSessionHandler`: Delegan en `ISessionOperationFacade`.
- `SubmitTriviaAnswerHandler`, `SubmitTreasureHuntCodeHandler`: Delegan en `ISessionOperationFacade`.
- `JoinSessionHandler`: Registro de equipo + publicación de `TeamRegisteredEvent` vía `IDomainEventPublisher`.
- `CreateLiveSessionCommandValidator`: FluentValidation de entrada.
- Consultas: `GetActiveSessionsHandler`, `GetTeamCurrentStageHandler`, `GetOperatorAssignedMissionsHandler`, `GetSessionTeamsHandler`, etc.

#### Proxy (panel de pistas jugador)
- `IPlayerHintPanelService`: Puerto de consulta de pistas para equipos en sesión.
- `PlayerReleasedHintsProxy`: **Proxy** que filtra y expone solo pistas liberadas al equipo (`ReleasedHint`).

#### Integración
- `IMissionIntegrationService`: ACL hacia MissionManagement (misiones, validaciones, hints).
- `IDomainEventPublisher`: Puerto de publicación post-persistencia.
- `NotFoundException`, `ConflictException`: Excepciones de aplicación.

### Capa: Infrastructure
- `SessionManagementDbContext`, `LiveSessionRepository`, configuraciones EF Core.
- `HttpMissionIntegrationService`: Cliente HTTP hacia MissionManagement.
- `RabbitMqDomainEventPublisher`: Publica eventos de sesión a RabbitMQ (`session.evidence.validated`, `session.team.registered`, `session.finalized`).
- `FakeMissionIntegrationService`: Adaptador simulado para pruebas.

### Capa: WebApi
- `LiveSessionsController`, `OperatorSessionsController`, `OperatorSessionValidationController`, `TeamsController`, `PlayerHintsController`, `MissionSessionValidationController`, `PlayerTeamMembershipController`: Controladores delgados con `Contracts/Routes/` + `Mapping/`.
- `OperatorSessionsController`: flujos de operador autenticados (`admin`/`operator` + `EnsureOperatorMatchesRoute`).
- `OperatorSessionValidationController`: validaciones de integración para MissionManagement (`has-active`, `is-supervising`) — `[AllowAnonymous]`.
- `Program`: Composition root — Umbral.Shared, Facade, cadena de validación, processors, RabbitMQ.
- `SignalR Hubs`: Pendiente.

---

## Microservicio: ScoringAudit

### Capa: Domain
- `TeamLedger`: Agregado raíz del libro mayor de puntos por equipo/sesión (Patrón: Aggregate Root).
- `ScoreEntry`: Entrada inmutable del historial de puntaje (Patrón: Entity).
- `ScoreOrigin`: Origen trazable del puntaje; `ComputedScore` respeta `FinalScore` de la Strategy.
- `PenaltyReason`, `RankingEntry`: Value Objects.
- `IScoreCalculationStrategy`: Interfaz del patrón **Strategy** para cálculo de puntaje.
- `TriviaScoreStrategy`: Bonificación por velocidad de respuesta.
- `TreasureHuntScoreStrategy`: Puntaje base × dificultad sin bonificación temporal.
- `ScoreCalculatorService`: Contexto Strategy — selecciona estrategia por `NodeType`.
- `RankingManagerService`: Ordena equipos por puntaje y tiempo (RB-08).
- `ITeamLedgerRepository`: Puerto de persistencia.
- `ScoringDomainException`: Excepción de dominio.

### Capa: Application
- `ProcessEvidenceValidatedHandler`: Consume evento de evidencia validada, aplica Strategy y registra en `TeamLedger`.
- `ProcessTeamRegisteredHandler`: Crea `TeamLedger` al registrar equipo.
- `ProcessSessionFinalizedHandler`: Cierra ledgers al finalizar sesión.
- `GetSessionRankingHandler`: Consulta ranking de una sesión (CQRS Query).

### Capa: Infrastructure
- `ScoringAuditDbContext`, `TeamLedgerRepository`, configuraciones EF Core (`team_ledgers`, `score_entries`).
- `ScoringAuditRabbitMqConsumer`: Consume eventos de SessionManagement y despacha handlers MediatR.

### Capa: WebApi
- `RankingController`: `GET /api/v1/sessions/{sessionId}/ranking` con `SessionRoute` + `RankingMappings` (`[Authorize: admin,operator]`).
- `Program`: Composition root — Umbral.Shared, MediatR, EF Core, Strategy DI, consumidor RabbitMQ.
- `GET /health`: Health check (`[AllowAnonymous]`).

---

## Infraestructura Docker (docker-compose)

| Servicio | Puerto | Dependencias (Compose) |
|----------|--------|------------------------|
| `mission-management-service` | 5260 | PostgreSQL, RabbitMQ, Keycloak (`healthy`) |
| `session-management-service` | 5278 | PostgreSQL, RabbitMQ, MissionManagement *(JWT vía Keycloak en runtime; no en `depends_on`)* |
| `scoring-audit-service` | 5290 | PostgreSQL, RabbitMQ *(JWT vía Keycloak en runtime; no en `depends_on`)* |
| `keycloak` | 8081 | — |
| `mq` (RabbitMQ) | 5672 / 15672 | — |
| `db` (PostgreSQL) | 5432 | — |

---

## Pendientes conocidos

- **SignalR**: Hubs de tiempo real no implementados en ningún microservicio.
- **Modo de sesión como Strategy**: La variación de puntaje es por `NodeType` + multiplicador de dificultad, no por modo de sesión.
- **State GoF estricto**: `LiveSession` usa matriz de transiciones (enum + switch), no clases de estado por objeto.
- **Documentación de dominio**: Actualizar `docs/Resumen-Dominio-md` si se usa en informes académicos (referencia cruzada con este mapa).
