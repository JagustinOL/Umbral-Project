# UMBRAL — Resumen Técnico de la Capa de Dominio

> **Proyecto:** UMBRAL — Plataforma para la operación en tiempo real de experiencias de investigación inmersiva  
> **Universidad:** UCAB — Ingeniería Informática 2026  
> **Arquitectura:** Microservicios con Domain-Driven Design (DDD) + Arquitectura Limpia/Hexagonal  
> **Stack:** C# 13 · .NET Core · MediatR · CQRS · EF Core · PostgreSQL · RabbitMQ · SignalR  

---

## Tabla de Contenidos

1. [Visión General de la Arquitectura](#1-visión-general-de-la-arquitectura)
2. [Carpeta Common — Clases Base](#2-carpeta-common--clases-base)
3. [MissionManagement.Domain](#3-missionmanagementdomain)
4. [SessionManagement.Domain](#4-sessionmanagementdomain)
5. [ScoringAudit.Domain](#5-scoringauditdomain)
6. [Flujos de Dominio y Eventos](#6-flujos-de-dominio-y-eventos)
7. [Reglas de Negocio y Dónde se Enforzan](#7-reglas-de-negocio-y-dónde-se-enforzan)
8. [Patrones de Diseño Aplicados](#8-patrones-de-diseño-aplicados)
9. [Principios SOLID Evidenciados](#9-principios-solid-evidenciados)

---

## 1. Visión General de la Arquitectura

UMBRAL está dividido en **tres Bounded Contexts** (contextos delimitados), cada uno implementado como un microservicio independiente en .NET Core. Cada microservicio tiene su propia capa de dominio completamente aislada de infraestructura.

```
┌─────────────────────────────────────────────────────────────────┐
│                        UMBRAL — Sistema                         │
├──────────────────┬───────────────────┬──────────────────────────┤
│  MissionManage-  │  SessionManage-   │   ScoringAudit           │
│  ment.Domain     │  ment.Domain      │   .Domain                │
│  (CORE)          │  (CORE)           │   (SUPPORTING)           │
│                  │                   │                          │
│  Mission.API     │  LiveEngine.API   │   ScoringAudit.API       │
├──────────────────┴───────────────────┴──────────────────────────┤
│              Common/ — Clases base compartidas                  │
│         Entity · AggregateRoot · IDomainEvent                   │
└─────────────────────────────────────────────────────────────────┘
```

### Comunicación entre contextos

Los microservicios **no se llaman entre sí de forma síncrona** para operaciones de escritura. Se comunican a través de **Eventos de Dominio publicados en RabbitMQ**. Esto garantiza el desacoplamiento y la independencia de cada contexto.

```
MissionManagement ──[MissionActivatedEvent]──────► SessionManagement
SessionManagement ──[SessionStartedEvent]────────► ScoringAudit
SessionManagement ──[TeamRegisteredEvent]────────► ScoringAudit
SessionManagement ──[EvidenceValidatedEvent]─────► ScoringAudit
SessionManagement ──[HintReleasedEvent]──────────► ScoringAudit
SessionManagement ──[ManualPenaltyAppliedEvent]──► ScoringAudit
SessionManagement ──[SessionFinalizedEvent]──────► ScoringAudit
ScoringAudit      ──[TeamScoreUpdatedEvent]──────► SignalR Hub → Frontend
```

---

## 2. Carpeta Common — Clases Base

Librería compartida por los tres microservicios. Contiene los contratos y clases abstractas que definen los pilares del modelo DDD.

### `IDomainEvent`

**Archivo:** `Common/IDomainEvent.cs`

Interfaz que marca un objeto como **Evento de Dominio**. Todo evento que cruza las fronteras de un Bounded Context (viaja por RabbitMQ) implementa esta interfaz.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `EventId` | `Guid` | Identificador único del evento para trazabilidad |
| `OccurredOnUtc` | `DateTime` | Momento exacto en que ocurrió el evento (siempre UTC) |

**Por qué existe:** Permite que la capa de Aplicación trate todos los eventos de forma polimórfica al publicarlos en RabbitMQ, sin conocer los tipos concretos.

---

### `Entity`

**Archivo:** `Common/Entity.cs`

Clase base abstracta para todas las **Entidades** del dominio. Una entidad tiene identidad propia (`Id`) que la distingue de otras instancias aunque sus atributos sean idénticos.

| Miembro | Descripción |
|---|---|
| `Id` (Guid, `init`) | Identidad inmutable de la entidad |
| `Equals()` / `GetHashCode()` | Igualdad por identidad, no por valor |
| `==` / `!=` operators | Comparación estructural segura con null-check |

**Decisión de diseño:** El constructor protegido valida que el `Id` nunca sea `Guid.Empty`. El constructor sin parámetros existe solo para compatibilidad con EF Core (ORM) y está marcado como `protected` para que no pueda usarse desde código de dominio.

---

### `AggregateRoot`

**Archivo:** `Common/AggregateRoot.cs`

Clase base abstracta para todos los **Aggregate Roots**. Extiende `Entity` y añade la capacidad de **acumular Eventos de Dominio** de forma interna.

| Miembro | Descripción |
|---|---|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` — eventos pendientes de despachar |
| `RaiseDomainEvent()` | Registra un evento internamente (solo llamado desde el agregado) |
| `ClearDomainEvents()` | Limpia la lista tras el despacho (llamado por Application Service) |

**Patrón clave — Outbox Pattern simplificado:** Los eventos se acumulan en memoria durante la ejecución del método de dominio. El Application Service los despacha a RabbitMQ **después** de que `SaveAsync()` confirma la persistencia. Esto garantiza consistencia: si la base de datos falla, el evento nunca se publica; si el bus falla, la transacción puede reintentarse.

```
Handler:
  1. session.Start()         ← evento acumulado internamente
  2. _repo.SaveAsync(session) ← persiste en PostgreSQL
  3. Despacha DomainEvents   ← publica en RabbitMQ
  4. session.ClearDomainEvents()
```

---

## 3. MissionManagement.Domain

**Bounded Context:** Diseño de Misiones  
**Tipo:** CORE — sin el diseño del juego no hay nada que jugar  
**Microservicio:** `Mission.API`  
**Responsabilidad:** Gestionar el catálogo de misiones (plantillas del juego)

### Estructura de carpetas

```
MissionManagement.Domain/
├── Aggregates/
│   ├── Mission.cs               ← Aggregate Root
│   └── MissionStatus.cs         ← Enum de estados
├── Entities/
│   ├── MissionNode.cs           ← Nodo/Etapa (patrón Composite)
│   ├── MissionNodeType.cs       ← Enum de tipos
│   └── Hint.cs                  ← Pista de un nodo
├── ValueObjects/
│   └── DifficultyLevel.cs       ← Nivel de dificultad + multiplicador
├── Events/
│   └── MissionActivatedEvent.cs ← Evento publicado al activar
└── Repositories/
    └── IMissionRepository.cs    ← Contrato de persistencia
```

---

### `DifficultyLevel` — Value Object

**Archivo:** `ValueObjects/DifficultyLevel.cs`

Representa el nivel de dificultad de una Misión. Implementado como `sealed record` para igualdad por valor automática.

| Instancia estática | Multiplicador | Descripción |
|---|---|---|
| `DifficultyLevel.Easy` | 1.0× | Dificultad baja |
| `DifficultyLevel.Medium` | 1.5× | Dificultad media |
| `DifficultyLevel.Hard` | 2.0× | Dificultad alta |

**Por qué incluye `ScoreMultiplier`:** El multiplicador viaja en `MissionActivatedEvent` hacia SessionManagement, y de ahí en `EvidenceValidatedEvent` hacia ScoringAudit. Esto permite que ScoringAudit calcule el puntaje correcto **sin necesidad de consultar a MissionManagement** en tiempo de ejecución — desacoplamiento total.

**Método `FromName()`:** ACL (Anti-Corruption Layer) para reconstruir el VO desde texto persistido en base de datos o recibido desde una API.

---

### `Hint` — Entidad

**Archivo:** `Entities/Hint.cs`

Representa una pista asociada a un `MissionNode`. Identificable de forma única por la combinación `(MissionNodeId + Order)`.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `Order` | `int` | Posición de la pista en el nodo (1 = primera) |
| `Content` | `string` | Texto de la pista |
| `PenaltyPoints` | `int` | Puntos que se restan al equipo por usarla |
| `MissionNodeId` | `Guid` | FK lógica al nodo dueño |

**Invariante:** `PenaltyPoints` no puede ser negativo. `UpdateContent()` es `internal` — solo el agregado `Mission` puede modificar el contenido de una pista, y solo mientras está en estado `Draft`.

---

### `MissionNode` — Entidad (Patrón Composite)

**Archivo:** `Entities/MissionNode.cs`

Entidad que implementa el **patrón Composite** para modelar la jerarquía de una misión: Etapas → Subetapas → Nodos hoja. Un nodo puede contener otros nodos (`_children`) y pistas (`_hints`).

| Propiedad | Descripción |
|---|---|
| `NodeType` | `Stage`, `Trivia` o `TreasureHunt` |
| `ExecutionOrder` | Orden de ejecución entre hermanos |
| `BaseScore` | Puntaje base al completar el nodo |
| `ParentNodeId` | `null` si es nodo raíz |
| `Children` | Sub-nodos (solo lectura desde exterior) |
| `Hints` | Pistas del nodo (solo lectura desde exterior) |

**Métodos clave:**
- `ContainsNode(Guid)` — búsqueda recursiva. Usado por `LiveSession` para validar que un nodo enviado en una evidencia realmente pertenece a la misión.
- `GetLeafNodeIds()` — retorna todos los IDs de nodos hoja del subárbol. Usado al activar la misión para construir el snapshot de `AllowedNodes`.

**Acceso controlado:** `AddChild()` y `AddHint()` son `internal` — solo `Mission` puede invocarlos, garantizando que la estructura del árbol nunca se modifique desde fuera del agregado.

---

### `Mission` — Aggregate Root

**Archivo:** `Aggregates/Mission.cs`

Aggregate Root del contexto. Gestiona el ciclo de vida completo de una misión y protege todas las invariantes de la estructura del juego.

**Estados (`MissionStatus`):**

```
Draft ──► Active ──► Inactive
  │                      ▲
  └──────────────────────┘ (no se puede reactivar)
```

| Estado | Descripción |
|---|---|
| `Draft` | En construcción. Se pueden agregar/modificar nodos y pistas |
| `Active` | Publicada. Estructura inmutable. Disponible para crear sesiones |
| `Inactive` | Retirada. No admite nuevas sesiones |

**Métodos de comportamiento:**

| Método | Invariante protegida |
|---|---|
| `AddRootNode()` | Solo en `Draft`. Sin conflicto de `ExecutionOrder` |
| `AddChildNode()` | Solo en `Draft`. El padre debe ser de tipo `Stage` |
| `AddHintToNode()` | Solo en `Draft`. Sin conflicto de `Order` en el nodo |
| `Activate()` | **RB-01:** Requiere al menos un `MissionNode`. Dispara `MissionActivatedEvent` |
| `Deactivate()` | Solo desde `Active`. No hay vuelta atrás a `Draft` |
| `UpdateDetails()` | Solo en `Draft` |

**`Activate()` — la operación más importante de este contexto:**
Al activar, la misión recolecta todos los IDs de nodos hoja (`GetLeafNodeIds()`) y los empaqueta en `MissionActivatedEvent` como `ActivatedNodeSnapshot[]`. SessionManagement consume este evento y almacena localmente la lista de nodos válidos — resolviendo **RB-05** de forma asíncrona y desacoplada.

---

### `MissionActivatedEvent` — Evento de Dominio

**Archivo:** `Events/MissionActivatedEvent.cs`

| Campo | Descripción |
|---|---|
| `MissionId` | ID de la misión activada |
| `MissionTitle` | Nombre para logging/auditoría |
| `AllowedNodes` | Lista de `ActivatedNodeSnapshot` (nodos hoja con BaseScore y NodeType) |
| `DifficultyMultiplier` | Multiplicador de dificultad para ScoringAudit |

**`ActivatedNodeSnapshot`** es un `record` ligero que solo transporta los datos necesarios para SessionManagement. No es una entidad — no tiene identidad propia.

---

### `IMissionRepository` — Interfaz de Repositorio

**Archivo:** `Repositories/IMissionRepository.cs`

| Método | Descripción |
|---|---|
| `GetByIdAsync()` | Carga el árbol completo (Mission + Nodes + Hints) |
| `GetActiveMissionsAsync()` | Solo misiones en estado `Active` |
| `SaveAsync()` | INSERT o UPDATE según existencia del Id |

**Nota:** Esta interfaz vive en el Dominio. La implementación `MissionPostgresRepo` vive en la capa de Infraestructura. El Dominio nunca referencia EF Core directamente.

---

## 4. SessionManagement.Domain

**Bounded Context:** Operación de Sesiones (Live Engine)  
**Tipo:** CORE — el corazón diferenciador del sistema  
**Microservicio:** `LiveEngine.API`  
**Responsabilidad:** Controlar el ciclo de vida del juego en vivo

### Estructura de carpetas

```
SessionManagement.Domain/
├── Aggregates/
│   ├── LiveSession.cs            ← Aggregate Root principal
│   ├── LiveSessionStatus.cs      ← Enum de estados (patrón State)
│   └── Team.cs                   ← Aggregate Root independiente
├── Entities/
│   ├── EvidenceSubmission.cs     ← Evidencia enviada por equipo
│   ├── TeamMember.cs             ← Jugador dentro de un equipo
│   └── ReleasedHint.cs           ← Registro de pista liberada
├── ValueObjects/
│   ├── TeamCode.cs               ← Código de acceso del equipo
│   └── AllowedNode.cs            ← Snapshot de nodo válido
├── Events/
│   ├── SessionStartedEvent.cs
│   ├── SessionStateChangedEvent.cs
│   ├── SessionFinalizedEvent.cs
│   ├── TeamRegisteredEvent.cs
│   ├── EvidenceValidatedEvent.cs
│   ├── HintReleasedEvent.cs
│   └── ManualPenaltyAppliedEvent.cs
├── Repositories/
│   ├── ILiveSessionRepository.cs
│   └── ITeamRepository.cs
└── Exceptions/
    └── SessionDomainException.cs
```

---

### `SessionDomainException`

**Archivo:** `Exceptions/SessionDomainException.cs`

Excepción de dominio específica del contexto. El middleware global de la API la captura para devolver HTTP 400 (error de negocio) en lugar de HTTP 500 (error de sistema).

---

### `TeamCode` — Value Object

**Archivo:** `ValueObjects/TeamCode.cs`

Código único de 6 caracteres alfanuméricos en mayúsculas (ej: `AB12CD`). Los participantes lo usan para unirse a su equipo desde el frontend.

| Método | Descripción |
|---|---|
| `TeamCode.Generate()` | Genera un código aleatorio. Excluye caracteres ambiguos (O, 0, I, 1) |
| `TeamCode.From(string)` | Reconstruye desde persistencia. Valida formato con Regex |

**Inmutabilidad garantizada por `sealed record`:** una vez asignado al equipo, el código nunca cambia.

---

### `AllowedNode` — Value Object

**Archivo:** `ValueObjects/AllowedNode.cs`

Snapshot ligero de un nodo de misión almacenado localmente en `LiveSession`.

```csharp
public sealed record AllowedNode(Guid NodeId, string NodeType, int BaseScore);
```

**Propósito crítico — Resolución de RB-05:** Cuando `LiveSession.AcceptEvidence()` recibe una evidencia, verifica que el `MissionNodeId` esté en `_allowedNodes`. Si no está, lanza `SessionDomainException`. Esto ocurre **sin ninguna llamada HTTP al microservicio MissionManagement** — el snapshot fue cargado al crear la sesión desde el evento `MissionActivatedEvent`.

---

### `TeamMember` — Entidad

**Archivo:** `Entities/TeamMember.cs`

Representa a un jugador dentro de un `Team`. Solo almacena `PlayerRef` (ID de Keycloak) y `DisplayName` — este contexto no gestiona autenticación.

---

### `ReleasedHint` — Entidad

**Archivo:** `Entities/ReleasedHint.cs`

Registro inmutable de una pista liberada a un equipo específico. Su existencia es la evidencia que `LiveSession` usa para enforcar **RB-04**.

| Propiedad | Descripción |
|---|---|
| `TeamId` | Equipo al que se liberó |
| `HintId` | ID de la pista en MissionManagement |
| `MissionNodeId` | Nodo al que pertenece la pista |
| `PenaltyPoints` | Puntos a descontar (viajan en `HintReleasedEvent`) |
| `WasManualRelease` | `true` = Operador, `false` = regla automática |

---

### `EvidenceSubmission` — Entidad

**Archivo:** `Entities/EvidenceSubmission.cs`

Registro de la respuesta o evidencia enviada por un equipo. Implementa el principio de que **una evidencia solo puede validarse o rechazarse una vez**.

| Propiedad | Tipo | Descripción |
|---|---|---|
| `TeamId` | `Guid` | Equipo que envió la evidencia |
| `MissionNodeId` | `Guid` | Nodo al que responde |
| `Payload` | `string` | Contenido (texto, URL, código QR, etc.) |
| `IsValid` | `bool?` | `null`=pendiente, `true`=válida, `false`=rechazada |
| `SubmittedAtUtc` | `DateTime` | Registro de tiempo obligatorio (RF-09) |

**Control de acceso mediante `internal`:**
- `EvidenceSubmission.Create()` — solo `LiveSession` puede crear evidencias
- `MarkAsValid()` / `MarkAsInvalid()` — solo `LiveSession` puede validarlas
- Cualquier intento de modificar una evidencia ya procesada lanza `SessionDomainException`

---

### `Team` — Aggregate Root

**Archivo:** `Aggregates/Team.cs`

Aggregate Root con ciclo de vida **independiente** de `LiveSession`. Un equipo puede existir entre sesiones, participar en una, bloquearse durante ella y desbloquearse al terminar.

**Invariantes protegidas:**

| Invariante | Método | Excepción |
|---|---|---|
| Máximo 4 jugadores | `AddMember()` | `SessionDomainException` |
| No duplicados | `AddMember()` | `SessionDomainException` |
| No abandono en sesión activa | `RemoveMember()` | `SessionDomainException` con mensaje RB |
| No bloquear si ya bloqueado | `Lock()` | `SessionDomainException` |

**Ciclo `IsLocked`:**
1. `Team.Lock()` — llamado cuando `SessionStartedEvent` llega al handler de equipos
2. `Team.Unlock()` — llamado cuando `SessionFinalizedEvent` llega al handler de equipos

`AssignToSession()` es `internal` — solo `LiveSession` puede asignar un equipo a una sesión.

---

### `LiveSession` — Aggregate Root

**Archivo:** `Aggregates/LiveSession.cs`

El agregado más complejo del sistema. Controla el ciclo de vida del juego en vivo y protege las invariantes más críticas del dominio.

**Estados y transiciones válidas (RB-09 — Patrón State):**

```
Scheduled ──► Preparation ──► Active ──► Paused ──► Active
    │               │            │           │
    └──► Cancelled  └──► Cancel  └──► Final  └──► Final/Cancel
```

Implementado con un switch de tuplas `(estadoActual, estadoNuevo)`. Cualquier transición no listada lanza `SessionDomainException` inmediatamente.

**Método `TransitionTo()` — corazón del patrón State:**
```csharp
private void TransitionTo(LiveSessionStatus newStatus)
{
    bool isValid = (Status, newStatus) switch
    {
        (Scheduled,   Preparation) => true,
        (Preparation, Active)      => true,
        (Active,      Paused)      => true,
        (Paused,      Active)      => true,
        // ... resto de transiciones válidas
        _ => false
    };
    if (!isValid) throw new SessionDomainException(...);
    Status = newStatus;
}
```

**Métodos de comportamiento y sus invariantes:**

| Método | Invariante / Regla | Evento disparado |
|---|---|---|
| `RegisterTeam()` | Solo en `Scheduled`/`Preparation`. Sin duplicados | `TeamRegisteredEvent` |
| `BeginPreparation()` | Transición válida | `SessionStateChangedEvent` |
| `Start()` | **RB-02:** al menos un equipo registrado | `SessionStartedEvent` + `SessionStateChangedEvent` |
| `Pause()` | Transición válida desde `Active` | `SessionStateChangedEvent` |
| `Resume()` | Transición válida desde `Paused` | `SessionStateChangedEvent` |
| `Finalize()` | Transición válida | `SessionFinalizedEvent` + `SessionStateChangedEvent` |
| `Cancel()` | Cualquier estado no terminal | `SessionFinalizedEvent` + `SessionStateChangedEvent` |
| `AcceptEvidence()` | **RB-03:** solo `Active`. **RB-05:** nodo en `AllowedNodes`. Equipo registrado | — (retorna `EvidenceSubmission`) |
| `MarkEvidenceAsValid()` | Evidencia debe existir y estar pendiente | `EvidenceValidatedEvent` |
| `ReleaseHint()` | **RB-03:** solo `Active`. **RB-04:** no repetir pista+equipo | `HintReleasedEvent` |
| `ApplyManualPenalty()` | Solo `Active`. **RB-06:** motivo obligatorio | `ManualPenaltyAppliedEvent` |

**`EvidenceValidatedEvent` — el evento más importante del sistema:**

Incluye `NodeType`, `BaseScore`, `DifficultyMultiplier` y `ElapsedSeconds` calculado en el momento exacto. ScoringAudit puede calcular el puntaje final sin consultar ningún otro servicio.

---

### Eventos de SessionManagement

| Evento | Disparado en | Consumidores |
|---|---|---|
| `SessionStartedEvent` | `Start()` | ScoringAudit (AuditLog), Team handlers (Lock) |
| `SessionStateChangedEvent` | Toda transición | ScoringAudit (AuditLog), SignalR Hub |
| `SessionFinalizedEvent` | `Finalize()` / `Cancel()` | ScoringAudit (cierra Ledger/AuditLog), Team handlers (Unlock) |
| `TeamRegisteredEvent` | `RegisterTeam()` | ScoringAudit (crea TeamLedger) |
| `EvidenceValidatedEvent` | `MarkEvidenceAsValid()` | ScoringAudit (ScoreCalculator + Ledger) |
| `HintReleasedEvent` | `ReleaseHint()` | ScoringAudit (penalización + AuditLog), SignalR (entrega la pista) |
| `ManualPenaltyAppliedEvent` | `ApplyManualPenalty()` | ScoringAudit (penalización + AuditLog) |

---

## 5. ScoringAudit.Domain

**Bounded Context:** Puntuación y Monitoreo  
**Tipo:** SUPPORTING — reacciona al Live Engine para llevar la contabilidad  
**Microservicio:** `ScoringAudit.API`  
**Responsabilidad:** Calcular puntajes, mantener el historial inmutable y construir el ranking

### Estructura de carpetas

```
ScoringAudit.Domain/
├── Aggregates/
│   ├── TeamLedger.cs              ← Libro mayor del equipo (Aggregate Root)
│   └── AuditLog.cs                ← Historial de sesión (Aggregate Root)
├── Entities/
│   ├── ScoreEntry.cs              ← Entrada contable inmutable
│   ├── ScoreEntryType.cs          ← Enum de tipos de entrada
│   ├── SessionEvent.cs            ← Evento registrado en auditoría
│   └── SessionEventType.cs        ← Enum de tipos de evento
├── ValueObjects/
│   ├── PenaltyReason.cs           ← Motivo de penalización (RB-06)
│   ├── PenaltyCategory.cs         ← Enum de categorías
│   ├── ScoreOrigin.cs             ← Origen de puntos ganados (RB-07)
│   └── RankingEntry.cs            ← Posición en el ranking (RB-08)
├── Services/
│   ├── IScoreCalculationStrategy.cs    ← Interfaz del patrón Strategy
│   ├── TriviaScoreStrategy.cs          ← Estrategia para nodos Trivia
│   ├── TreasureHuntScoreStrategy.cs    ← Estrategia para TreasureHunt
│   ├── ScoreCalculatorService.cs       ← Orquestador de estrategias
│   └── RankingManagerService.cs        ← Constructor del ranking
├── Events/
│   └── TeamScoreUpdatedEvent.cs   ← Notificación a SignalR
├── Repositories/
│   ├── ITeamLedgerRepository.cs
│   └── IAuditLogRepository.cs
└── Exceptions/
    └── ScoringDomainException.cs
```

---

### `PenaltyReason` — Value Object

**Archivo:** `ValueObjects/PenaltyReason.cs`

Encapsula el motivo de cualquier penalización. Su existencia **fuerza** RB-06 a nivel de tipo: no puede existir un `ScoreEntry` de penalización sin un `PenaltyReason` válido.

**Métodos de fábrica:**

| Método | Categoría | Validación |
|---|---|---|
| `ForManualPenalty(string)` | `ManualOperator` | Mínimo 5 caracteres (descripción significativa) |
| `ForHintUsage(Guid, int)` | `HintUsage` | Automático — no requiere descripción manual |
| `ForTimeExpired()` | `TimeExpired` | Automático |

---

### `ScoreOrigin` — Value Object

**Archivo:** `ValueObjects/ScoreOrigin.cs`

Documenta el origen de un `ScoreEntry` positivo. Junto con `PenaltyReason`, garantiza **RB-07** (trazabilidad total del puntaje).

```csharp
public sealed record ScoreOrigin(
    Guid MissionNodeId,
    string NodeType,
    int BaseScore,
    decimal DifficultyMultiplier,
    double ElapsedSeconds
)
```

Incluye `ComputedScore` como propiedad calculada (`BaseScore × DifficultyMultiplier`), almacenada en el record para que la trazabilidad sea directamente legible sin recalcular.

---

### `RankingEntry` — Value Object

**Archivo:** `ValueObjects/RankingEntry.cs`

Snapshot de la posición de un equipo en el ranking. Producido por `RankingManagerService` y transportado en `TeamScoreUpdatedEvent` hacia SignalR.

```csharp
public sealed record RankingEntry(
    int Position,
    Guid TeamId,
    string TeamName,
    int TotalScore,
    double TotalElapsedSeconds,  // criterio de desempate RB-08
    int CompletedNodes,
    int PenaltiesApplied
)
```

---

### `ScoreEntry` — Entidad

**Archivo:** `Entities/ScoreEntry.cs`

Entrada contable **inmutable** del `TeamLedger`. Puede ser positiva (evidencia) o negativa (penalización). Nunca se modifica después de crearse — si hay un error, se crea un entry compensatorio.

| Constructores internos | Cuándo se usa |
|---|---|
| `ForEvidence(ScoreOrigin, Guid)` | Al recibir `EvidenceValidatedEvent`. `Points` = `origin.ComputedScore` |
| `ForPenalty(int, PenaltyReason, ScoreEntryType, Guid)` | Al recibir penalización. `Points` = negativo |

`SourceEventId` referencia el `EventId` del evento de dominio original. Permite correlacionar el ledger con el historial de RabbitMQ para debugging y auditoría.

---

### `SessionEvent` — Entidad

**Archivo:** `Entities/SessionEvent.cs`

Registro inmutable en el `AuditLog`. Captura qué ocurrió, cuándo, en qué sesión, para qué equipo y en qué nodo.

`internal static Create()` — solo `AuditLog` puede crear `SessionEvent`. Nadie externo al agregado puede inyectar eventos en el historial.

---

### `IScoreCalculationStrategy` — Interfaz Strategy

**Archivo:** `Services/IScoreCalculationStrategy.cs`

```csharp
public interface IScoreCalculationStrategy
{
    string NodeType { get; }
    int Calculate(int baseScore, decimal difficultyMultiplier, double elapsedSeconds);
}
```

**OCP en acción:** para agregar soporte a un nuevo tipo de nodo (ej: `PhotoChallenge`), se crea una nueva clase que implemente esta interfaz y se registra en el contenedor IoC. Nada más cambia.

---

### `TriviaScoreStrategy` y `TreasureHuntScoreStrategy`

**Archivos:** `Services/TriviaScoreStrategy.cs` / `TreasureHuntScoreStrategy.cs`

**Trivia — con bonificación por velocidad:**

| Tiempo de respuesta | Multiplicador extra |
|---|---|
| < 30 segundos | ×1.20 (respuesta muy rápida) |
| < 60 segundos | ×1.10 |
| < 120 segundos | ×1.00 (sin bonificación) |
| ≥ 120 segundos | ×0.90 (penalización por lentitud) |

**TreasureHunt — sin bonificación de velocidad:**
`puntaje = baseScore × difficultyMultiplier`. El tiempo solo se usa como criterio de desempate en el ranking (RB-08), no afecta el puntaje directo.

---

### `ScoreCalculatorService` — Domain Service

**Archivo:** `Services/ScoreCalculatorService.cs`

Orquestador del patrón Strategy. Recibe todas las implementaciones de `IScoreCalculationStrategy` por inyección de dependencias y las indexa por `NodeType` para lookup en O(1).

```
EvidenceValidatedEvent.NodeType = "Trivia"
       │
       ▼
ScoreCalculatorService._strategies["Trivia"]
       │
       ▼
TriviaScoreStrategy.Calculate(baseScore, multiplier, elapsed)
       │
       ▼
ScoreOrigin(ComputedScore = resultado)
       │
       ▼
TeamLedger.AddEvidenceScore(origin, eventId)
```

---

### `RankingManagerService` — Domain Service

**Archivo:** `Services/RankingManagerService.cs`

Construye el ranking comparando todos los `TeamLedger` de una sesión.

**Algoritmo de ordenamiento (RB-08):**
1. Orden primario: `TotalScore` descendente (mayor puntaje primero)
2. Criterio de desempate: `LastPositiveEntryElapsedSeconds` ascendente (menor tiempo gana)

Es **stateless** — recibe los datos, calcula y retorna. No persiste nada; el Application Service es quien persiste y dispara `TeamScoreUpdatedEvent`.

---

### `AuditLog` — Aggregate Root

**Archivo:** `Aggregates/AuditLog.cs`

Registro histórico **append-only** de una sesión. Un `AuditLog` = una sesión.

**Ciclo de vida:**
- `Open` (IsClosed = false): acepta nuevos `SessionEvent`
- `Closed` (IsClosed = true): sellado. Ningún evento adicional es aceptado

`Close()` es llamado por el Application Service al recibir `SessionFinalizedEvent` o al procesar una cancelación.

**Métodos de consulta:**
- `GetEventsByTeam(Guid)` — para el panel de progreso del equipo (RF-06)
- `GetEventsByType(SessionEventType)` — para filtros de auditoría del Operador (RF-15)

---

### `TeamLedger` — Aggregate Root

**Archivo:** `Aggregates/TeamLedger.cs`

El libro mayor contable del equipo en una sesión. Núcleo de RB-07.

**Propiedades calculadas (nunca asignadas directamente):**

| Propiedad | Cálculo | Propósito |
|---|---|---|
| `TotalScore` | `_entries.Sum(e => e.Points)` | Puntaje actual. RB-07 |
| `CompletedNodesCount` | Nodos únicos con `EvidenceRewarded` | Estadística de progreso |
| `PenaltiesCount` | Entradas con `Points < 0` | Estadística para ranking |
| `LastPositiveEntryElapsedSeconds` | `ElapsedSeconds` del último entry positivo | Desempate RB-08 |

**Invariantes:**

| Invariante | Donde se enforza |
|---|---|
| RB-07: TotalScore sin setter directo | Propiedad calculada — imposible asignar |
| RB-06: PenaltyReason obligatorio | `ArgumentNullException` en `ApplyPenalty()` |
| No recompensar el mismo nodo dos veces | Verificación en `AddEvidenceScore()` |
| No entradas en ledger cerrado | `ThrowIfClosed()` en ambos métodos de escritura |

**`TeamScoreUpdatedEvent` se dispara en tres puntos:**
- `AddEvidenceScore()` — puntos ganados
- `ApplyPenalty()` — penalización aplicada
- `Close()` — cierre final de la sesión

En todos los casos, SignalR recibe la notificación y actualiza el ranking en pantalla en tiempo real (RF-12).

---

## 6. Flujos de Dominio y Eventos

### Flujo 1 — Activación de Misión y Carga de Sesión

```
Administrador activa misión
  → Mission.Activate()                        [MissionManagement]
  → MissionActivatedEvent (con AllowedNodes)  ──RabbitMQ──►
  → CreateLiveSessionCommandHandler consume el evento
    y almacena AllowedNodes en LiveSession    [SessionManagement]
```

### Flujo 2 — Inicio de Sesión y Bloqueo de Equipos

```
Operador inicia sesión
  → LiveSession.Start()                       [SessionManagement]
  → SessionStartedEvent ──RabbitMQ──►
      → AuditLog.Create() + RecordEvent()     [ScoringAudit]
      → Team.Lock() para cada equipo          [SessionManagement]
```

### Flujo 3 — Motor de Puntos (el más crítico)

```
Equipo envía evidencia (Trivia / TreasureHunt)
  → SessionOperationFacade                    Facade
  → EvidenceSubmissionProcessor.Process()     Template Method
  → EvidenceValidatorService                  Chain of Responsibility
  → LiveSession.AcceptEvidence()              RB-03, RB-05
  → LiveSession.MarkEvidenceAsValid()
  → EvidenceValidatedEvent ──RabbitMQ──►
      → ProcessEvidenceValidatedHandler       [ScoringAudit.Application]
      → ScoreCalculatorService.Calculate()   Strategy Pattern
      → TeamLedger.AddEvidenceScore()        RB-07 (ScoreOrigin.FinalScore)
      → TeamScoreUpdatedEvent
      → RankingManagerService.BuildRanking() RB-08
      → SignalR Hub → Frontend               RF-12 (pendiente)
```

### Flujo 4 — Penalización Manual

```
Operador aplica penalización
  → LiveSession.ApplyManualPenalty()         RB-06 (motivo requerido)
  → ManualPenaltyAppliedEvent ──RabbitMQ──►
      → PenaltyReason.ForManualPenalty()     RB-06 enforzado por VO
      → TeamLedger.ApplyPenalty()            RB-07
      → AuditLog.RecordEvent()               RF-15
      → TeamScoreUpdatedEvent → SignalR      RF-12
```

### Flujo 5 — Liberación de Pistas

```
Operador libera pista
  → LiveSession.ReleaseHint()                RB-03, RB-04
  → HintReleasedEvent ──RabbitMQ──►
      → TeamLedger.ApplyPenalty()            descuento automático
      → AuditLog.RecordEvent()               trazabilidad
      → SignalR Hub → Panel del equipo       RF-07
```

### Flujo 6 — Finalización de Sesión

```
Operador finaliza sesión
  → LiveSession.Finalize()
  → SessionFinalizedEvent ──RabbitMQ──►
      → TeamLedger.Close() para cada equipo  sella ledgers
      → AuditLog.Close()                     sella historial
      → RankingManagerService.BuildRanking() ranking final inmutable
      → TeamScoreUpdatedEvent → SignalR      notificación final
      → Team.Unlock() para cada equipo       libera equipos
```

---

## 7. Reglas de Negocio y Dónde se Enforzan

| Código | Regla | Agregado/Servicio | Método |
|---|---|---|---|
| **RB-01** | Misión activa para crear sesiones | `Mission` | `Activate()` valida nodos; `LiveSession.Create()` requiere `AllowedNodes` válidos |
| **RB-02** | Sesión no inicia sin equipos | `LiveSession` | `Start()` verifica `_registeredTeamIds.Count > 0` |
| **RB-03** | No evidencias si no está `Active` | `LiveSession` | `AcceptEvidence()` y `ReleaseHint()` verifican `Status == Active` |
| **RB-04** | Pista no se libera dos veces al mismo equipo | `LiveSession` | `ReleaseHint()` busca en `_releasedHints` por `TeamId + HintId` |
| **RB-05** | Evidencia asociada a equipo + sesión + etapa | `LiveSession` | `AcceptEvidence()` verifica `MissionNodeId` en `_allowedNodes` |
| **RB-06** | Penalización con motivo obligatorio | `PenaltyReason` VO | Constructor privado — imposible crear sin descripción válida |
| **RB-07** | Puntaje con trazabilidad de origen | `TeamLedger` | `TotalScore` es propiedad calculada sin setter. `ScoreEntry` es inmutable |
| **RB-08** | Ranking por puntaje, tiempo como desempate | `RankingManagerService` | `OrderByDescending(score).ThenBy(elapsed)` |
| **RB-09** | Transiciones de estado válidas | `LiveSession` | `TransitionTo()` con switch de tuplas exhaustivo |
| **RB-10** | Operador solo administra sus sesiones | `EnsureOperatorMatchesRoute` + `LiveSession.OperatorRef` | JWT valida identidad; filtro de ruta en `OperatorSessionsController`; handler verifica asignación RN-16 |

---

## 8. Patrones de Diseño Aplicados

> Mapa completo de componentes y capas: ver [`RESUMEN-DEFENSA.md`](./RESUMEN-DEFENSA.md) (resumen unificado arquitectura + defensa).

### Composite — `MissionNode`
```
Mission
└── MissionNode (Stage)
    ├── MissionNode (Trivia)    ← nodo hoja
    ├── MissionNode (TreasureHunt) ← nodo hoja
    └── MissionNode (Stage)
        └── MissionNode (Trivia) ← nodo hoja
```
`GetLeafNodeIds()` recorre el árbol recursivamente. `ContainsNode()` búsqueda en profundidad.

### State — `LiveSession.TransitionTo()`
Matriz de transiciones válidas implementada con switch de tuplas. Rechaza cualquier transición no explícitamente permitida (RB-09).

### Strategy — `IScoreCalculationStrategy`
Tres componentes: interfaz (`IScoreCalculationStrategy`), implementaciones concretas (`TriviaScoreStrategy`, `TreasureHuntScoreStrategy`) y contexto (`ScoreCalculatorService`). El flujo end-to-end se completa con `ProcessEvidenceValidatedHandler` en ScoringAudit.Application, que consume el evento vía RabbitMQ. `ScoreOrigin.FinalScore` persiste el resultado de la estrategia (incluye bonificación Trivia).

### Facade — `SessionOperationFacade`
Coordinador en SessionManagement.Application que unifica operaciones de sesión (crear, iniciar, finalizar, cancelar, enviar evidencias), persistencia en repositorios y publicación de eventos de dominio tras `SaveAsync`. Los handlers CQRS delegan en `ISessionOperationFacade`.

### Proxy — acceso a pistas
- `MissionHintService` + `DraftOnlyHintProxy` (MissionManagement): sujeto real + proxy que restringe consulta según rol y estado de la misión.
- `PlayerReleasedHintsProxy` (SessionManagement): expone al jugador solo pistas ya liberadas (`ReleasedHint`) para su equipo.

### Template Method — `EvidenceSubmissionProcessor`
Clase abstracta con flujo fijo `Process()`: validar (cadena) → `AcceptEvidence` → marcar válida/inválida → `SubmissionResult`. Subclases: `TriviaEvidenceSubmissionProcessor`, `TreasureHuntEvidenceSubmissionProcessor`.

### Chain of Responsibility — `EvidenceValidatorService`
Cadena de handlers: `SessionActiveValidationHandler` → `TeamRegisteredValidationHandler` → `NodeAllowedValidationHandler` → `SequentialProgressValidationHandler` → `AnswerCorrectnessValidationHandler`. Se ejecuta antes de mutar el agregado en el Template Method.

### Cross-cutting — `Umbral.Shared`
Librería compartida: JWT Keycloak (`AddUmbralAuthentication`), `ICurrentUser`, `ExceptionHandlingMiddleware` (ProblemDetails), Serilog, `ValidationBehavior` (FluentValidation + MediatR), `RabbitMqPublisher` y contratos de integración.

### Outbox Pattern (simplificado) — `AggregateRoot`
Los eventos se acumulan en `_domainEvents` durante la ejecución del método de dominio. El Application Service los publica en RabbitMQ **después** de confirmar la persistencia. Garantiza consistencia eventual.

### Repository Pattern
Contratos definidos en el Dominio (`IMissionRepository`, `ILiveSessionRepository`, etc.) e implementaciones en Infraestructura. El Dominio nunca referencia EF Core.

### Factory Method — `Entity.Create()` estáticos
Toda creación de entidades pasa por métodos de fábrica estáticos que validan invariantes. Los constructores privados/protegidos previenen la creación directa desde fuera del dominio.

### Value Object (record)
`DifficultyLevel`, `TeamCode`, `AllowedNode`, `PenaltyReason`, `ScoreOrigin`, `RankingEntry`. Igualdad por valor, inmutabilidad garantizada por `sealed record`.

---

## 9. Principios SOLID Evidenciados

### SRP — Responsabilidad Única
- `Mission` solo gestiona el ciclo de vida y la estructura de la misión
- `LiveSession` solo controla el juego en vivo
- `ScoreCalculatorService` solo calcula puntajes — no persiste ni publica eventos
- `RankingManagerService` solo ordena ledgers — no calcula puntajes ni persiste

### OCP — Abierto/Cerrado
- `IScoreCalculationStrategy`: agregar `PhotoChallengeScoreStrategy` no modifica `ScoreCalculatorService`
- Nuevos `SessionEventType` no modifican `AuditLog.RecordEvent()`

### LSP — Sustitución de Liskov
- Todas las entidades concretas extienden `Entity` o `AggregateRoot` sin alterar sus contratos
- `sealed` en todas las clases concretas previene herencia incorrecta

### ISP — Segregación de Interfaces
- `IMissionRepository`, `ILiveSessionRepository`, `ITeamRepository`, `ITeamLedgerRepository`, `IAuditLogRepository` — cada repositorio expone solo lo que su agregado necesita
- `IScoreCalculationStrategy` expone un único método `Calculate()` + una propiedad

### DIP — Inversión de Dependencias
- `ScoreCalculatorService` recibe `IEnumerable<IScoreCalculationStrategy>` por constructor
- Todos los repositorios son inyectados como interfaces — el Dominio nunca instancia EF Core
- Los Application Services reciben repositorios e interfaces de mensajería por DI

---

## Resumen de Archivos por Contexto

| Contexto | Archivos | Clases/Records/Interfaces |
|---|---|---|
| `Common/` | 3 | `Entity`, `AggregateRoot`, `IDomainEvent` |
| `MissionManagement.Domain` | 8 | `Mission`, `MissionNode`, `Hint`, `MissionStatus`, `MissionNodeType`, `DifficultyLevel`, `MissionActivatedEvent`, `IMissionRepository` |
| `SessionManagement.Domain` | 16 | `LiveSession`, `LiveSessionStatus`, `Team`, `EvidenceSubmission`, `TeamMember`, `ReleasedHint`, `TeamCode`, `AllowedNode`, `SessionDomainException` + 7 eventos + 2 repositorios |
| `ScoringAudit.Domain` | 17 | `TeamLedger`, `AuditLog`, `ScoreEntry`, `ScoreEntryType`, `SessionEvent`, `SessionEventType`, `PenaltyReason`, `PenaltyCategory`, `ScoreOrigin`, `RankingEntry`, `IScoreCalculationStrategy`, `TriviaScoreStrategy`, `TreasureHuntScoreStrategy`, `ScoreCalculatorService`, `RankingManagerService`, `TeamScoreUpdatedEvent`, `ScoringDomainException` + 2 repositorios |
| **Total** | **44** | **44 clases/records/interfaces** |

---

*Documento generado como referencia técnica del Proyecto Integrador UMBRAL — UCAB 2026*  
*Arquitectura: DDD + Arquitectura Limpia | Lenguaje: C# 13 | Framework: .NET Core*