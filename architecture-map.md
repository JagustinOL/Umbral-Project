# Mapa de Arquitectura Backend

## Microservicio: MissionManagement

### Capa: Domain
- `Mission`: Agregado raíz que gobierna el ciclo de vida de la misión, protege invariantes de edición y aplica composición jerárquica de nodos (Patrón: Aggregate Root + Composite).  TODO LO RELACIONADO A MISION Y FUNCIONES PARA ADMINISTRADOR,
- `MissionNode`: Entidad de árbol que modela etapas y juegos, delegando reglas por tipo de nodo (Patrón: Entity + Composite).
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
- `UpdateMissionDetailsHandler`: Caso de uso que actualiza metadatos de misión sin romper reglas de estado (Patrón: CQRS Command Handler).
- `DeactivateMissionHandler`: Caso de uso que desactiva misiones y persiste transición de estado (Patrón: CQRS Command Handler).
- `AssignOperatorToMissionHandler`: Caso de uso que vincula operadores a misiones respetando reglas de asignación (Patrón: CQRS Command Handler).
- `RevokeOperatorFromMissionHandler`: Caso de uso que revoca operadores validando sesiones activas vía integración externa (Patrón: CQRS Command Handler + ACL Port).
- `AddRootNodeHandler`: Caso de uso que agrega etapas raíz de misión (Patrón: CQRS Command Handler).
- `AddTriviaNodeHandler`: Caso de uso que agrega nodos Trivia como hijos de etapa (Patrón: CQRS Command Handler).
- `AddTreasureHuntNodeHandler`: Caso de uso que agrega nodos Treasure Hunt con validación de coordenadas y código (Patrón: CQRS Command Handler).
- `AddHintHandler`: Caso de uso que crea pistas y valida adjuntos permitidos (Patrón: CQRS Command Handler).
- `GetMissionsHandler`: Consulta que proyecta misiones para listado de lectura (Patrón: CQRS Query Handler).
- `GetMissionByIdHandler`: Consulta que devuelve detalle de una misión por identificador (Patrón: CQRS Query Handler).
- `GetHintsByNodeHandler`: Consulta que expone pistas asociadas a un nodo (Patrón: CQRS Query Handler).
- `IIdentityService`: Puerto para gestionar operadores contra el servicio de identidad externo (Patrón: Integration Port / ACL).
- `ISessionValidationService`: Puerto para validar si un operador tiene sesiones activas antes de cambios críticos (Patrón: Integration Port / ACL).
- `NotFoundException`: Excepción de aplicación para recursos inexistentes en casos de uso (Patrón: Application Exception).
- `ConflictException`: Excepción de aplicación para violaciones de estado o conflictos de negocio (Patrón: Application Exception).

### Capa: Infrastructure
- `MissionManagementDbContext`: Contexto EF Core que modela tablas y relaciones del bounded context de misiones (Patrón: DbContext).
- `MissionRepository`: Adaptador EF Core que implementa el puerto de repositorio de misiones (Patrón: Repository Adapter).
- `MissionConfiguration`: Configuración de mapeo de la entidad Mission hacia PostgreSQL (Patrón: Fluent Configuration).
- `MissionNodeConfiguration`: Configuración de mapeo de nodos jerárquicos y datos específicos por tipo (Patrón: Fluent Configuration).
- `HintConfiguration`: Configuración de mapeo e índices de pistas (Patrón: Fluent Configuration).
- `FakeIdentityService`: Implementación simulada del puerto de identidad para ambientes locales (Patrón: Fake Adapter).
- `FakeSessionValidationService`: Implementación simulada del puerto de validación de sesiones (Patrón: Fake Adapter).

### Capa: WebApi
- `MissionsController`: Controlador REST para crear, listar, consultar, editar y desactivar misiones (Patrón: API Controller).
- `NodesController`: Controlador REST para gestionar nodos Stage raíz de misión (Patrón: API Controller).
- `TriviaController`: Controlador REST para crear y actualizar nodos Trivia (Patrón: API Controller).
- `TreasureHuntsController`: Controlador REST para crear y actualizar nodos Treasure Hunt (Patrón: API Controller).
- `HintsController`: Controlador REST para alta, consulta, edición y eliminación de pistas por nodo (Patrón: API Controller).
- `MissionOperatorsController`: Controlador REST para asignar y revocar operadores de misión (Patrón: API Controller).
- `OperatorsController`: Controlador REST para alta, listado y baja de operadores (Patrón: API Controller).
- `ExceptionHandlingMiddleware`: Middleware transversal que traduce excepciones de dominio/aplicación a respuestas HTTP (Patrón: Exception Middleware).
- `Program`: Punto de composición de dependencias, MediatR, EF Core y contratos HTTP (Patrón: Composition Root).
- `SignalR Hubs`: No hay hubs de SignalR implementados en este microservicio (Patrón: Tiempo real pendiente).

## Microservicio: SessionManagement

### Capa: Domain
- `LiveSession`: Agregado raíz que administra estados de sesión, equipos registrados, evidencias, pistas liberadas y penalizaciones (Patrón: Aggregate Root + State).
- `Team`: Agregado raíz que modela miembros, código de acceso y reglas de bloqueo operativo (Patrón: Aggregate Root).
- `EvidenceSubmission`: Entidad que registra cada envío de respuesta/evidencia y su validación única (Patrón: Entity).
- `ReleasedHint`: Entidad que registra pistas liberadas por equipo con su penalización aplicada (Patrón: Entity).
- `TeamMember`: Entidad que representa integrantes de un equipo con referencia a identidad externa (Patrón: Entity).
- `ILiveSessionRepository`: Puerto de persistencia para sesiones en vivo y su estado completo (Patrón: Repository Port).
- `ITeamRepository`: Puerto de persistencia para agregado Team y búsquedas por código/sesión (Patrón: Repository Port).
- `AllowedNode`: Objeto de valor con snapshot de nodos habilitados para validación local de progresión (Patrón: Value Object).
- `TeamCode`: Objeto de valor que encapsula generación y validación del código de unión de equipo (Patrón: Value Object).
- `NodeValidationRule`: Objeto de valor que define orden, tipo y valor esperado de validación por nodo (Patrón: Value Object).
- `SubmissionResult`: Objeto de valor que expresa resultado de validación y puntos otorgados (Patrón: Value Object).
- `TeamRegisteredEvent`: Evento de dominio que notifica registro de equipo en sesión (Patrón: Domain Event).
- `SessionStartedEvent`: Evento de dominio que notifica inicio operativo de la sesión (Patrón: Domain Event).
- `SessionStateChangedEvent`: Evento de dominio que comunica transiciones de estado de la sesión (Patrón: Domain Event).
- `SessionFinalizedEvent`: Evento de dominio que notifica cierre definitivo de sesión (Patrón: Domain Event).
- `EvidenceValidatedEvent`: Evento de dominio que publica validaciones correctas para cálculo externo de score (Patrón: Domain Event).
- `HintReleasedEvent`: Evento de dominio que notifica liberación de pista y penalización (Patrón: Domain Event).
- `ManualPenaltyAppliedEvent`: Evento de dominio que notifica penalización manual aplicada por operador (Patrón: Domain Event).
- `SessionDomainException`: Excepción de dominio para incumplimiento de invariantes de sesión (Patrón: Domain Exception).

### Capa: Application
- `CreateLiveSessionHandler`: Caso de uso que crea sesión en vivo validando misión asignada del operador (Patrón: CQRS Command Handler).
- `StartLiveSessionHandler`: Caso de uso que inicia la sesión y aplica transición controlada de estados (Patrón: CQRS Command Handler).
- `JoinSessionHandler`: Caso de uso que registra un equipo en una sesión usando código de ingreso (Patrón: CQRS Command Handler).
- `SubmitTriviaAnswerHandler`: Caso de uso que valida y procesa respuestas Trivia contra reglas de misión (Patrón: CQRS Command Handler).
- `SubmitTreasureHuntCodeHandler`: Caso de uso que valida y procesa códigos de Treasure Hunt (Patrón: CQRS Command Handler).
- `GetActiveSessionsHandler`: Consulta que lista sesiones activas para consumo operativo (Patrón: CQRS Query Handler).
- `GetTeamCurrentStageHandler`: Consulta que obtiene progreso y nodo actual de un equipo (Patrón: CQRS Query Handler).
- `GetOperatorAssignedMissionsHandler`: Consulta que obtiene misiones asignadas vía integración externa (Patrón: CQRS Query Handler + ACL Port).
- `GetSessionTeamsHandler`: Consulta que devuelve equipos registrados en una sesión del operador (Patrón: CQRS Query Handler).
- `IMissionIntegrationService`: Puerto anti-corrupción para leer misiones y nodos válidos desde MissionManagement (Patrón: Integration Port / ACL).
- `AssignedMissionData`: DTO de integración para transporte de misiones asignadas (Patrón: Integration DTO).
- `MissionNodeValidationData`: DTO de integración para reglas de validación por nodo (Patrón: Integration DTO).
- `NotFoundException`: Excepción de aplicación para entidades no encontradas o no autorizadas (Patrón: Application Exception).
- `ConflictException`: Excepción de aplicación para conflictos de transición o consistencia (Patrón: Application Exception).

### Capa: Infrastructure
- `SessionManagementDbContext`: Contexto EF Core de sesiones, evidencias y pistas liberadas (Patrón: DbContext).
- `LiveSessionRepository`: Adaptador EF Core que implementa persistencia del agregado LiveSession (Patrón: Repository Adapter).
- `LiveSessionConfiguration`: Configuración de mapeo de LiveSession y snapshots serializados (Patrón: Fluent Configuration).
- `EvidenceSubmissionConfiguration`: Configuración de mapeo de envíos de evidencia (Patrón: Fluent Configuration).
- `ReleasedHintConfiguration`: Configuración de mapeo de pistas liberadas (Patrón: Fluent Configuration).
- `FakeMissionIntegrationService`: Adaptador simulado del puerto de integración con MissionManagement (Patrón: Fake Adapter).

### Capa: WebApi
- `LiveSessionsController`: Controlador REST para flujo del jugador (unirse, enviar respuestas y consultar etapa actual) (Patrón: API Controller).
- `OperatorSessionsController`: Controlador REST para flujo del operador (crear/iniciar sesión y consultar equipos/misiones) (Patrón: API Controller).
- `ExceptionHandlingMiddleware`: Middleware transversal que homologa errores de dominio/aplicación a respuestas HTTP (Patrón: Exception Middleware).
- `Program`: Punto de composición de dependencias de API, EF Core, MediatR e integraciones (Patrón: Composition Root).
- `SignalR Hubs`: No hay hubs de SignalR implementados en este microservicio (Patrón: Tiempo real pendiente).
