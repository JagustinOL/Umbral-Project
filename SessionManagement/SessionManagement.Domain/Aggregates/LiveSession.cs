using Common;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Events;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using System.Security.Cryptography;

namespace SessionManagement.Domain.Aggregates;

/// <summary>
/// AGGREGATE ROOT principal — LiveSession.
/// El corazón del sistema: controla el ciclo de vida del juego en vivo.
///
/// INVARIANTES protegidas:
/// — RB-02: Start() lanza excepción si no hay equipos registrados.
/// — RB-03: AcceptEvidence() lanza excepción si el estado no es Active.
/// — RB-04: ReleaseHint() lanza excepción si la pista ya fue liberada
///          al mismo equipo para el mismo nodo.
/// — RB-05: AcceptEvidence() verifica que el MissionNodeId pertenezca
///          a los AllowedNodes cargados al crear la sesión.
/// — RB-09: Todas las transiciones de estado pasan por TransitionTo(),
///          que valida la matriz de transiciones válidas.
/// — RB-10: El OperatorRef se verifica en el Application Service antes
///          de invocar cualquier método de este agregado.
/// </summary>
public sealed class LiveSession : AggregateRoot
{
    private readonly List<Guid> _registeredTeamIds = [];
    private readonly List<EvidenceSubmission> _evidenceSubmissions = [];
    private readonly List<ReleasedHint> _releasedHints = [];
    private readonly List<AllowedNode> _allowedNodes = [];

    // ── Propiedades ────────────────────────────────────────────────────────────

    /// <summary>Referencia a la Misión activa (MissionManagement context).</summary>
    public Guid MissionRef { get; private set; }

    /// <summary>Referencia al Operador asignado (Keycloak ID). Enforza RB-10.</summary>
    public Guid OperatorRef { get; private set; }
    public string JoinCode { get; private set; } = string.Empty;

    public LiveSessionStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }

    /// <summary>
    /// Duración máxima en minutos. Null = sin límite.
    /// Copiado desde la Misión al crear la sesión.
    /// </summary>
    public int? MaxDurationMinutes { get; private set; }

    /// <summary>
    /// Multiplicador de dificultad copiado desde la Misión.
    /// Viaja en EvidenceValidatedEvent para que ScoringAudit
    /// aplique la estrategia correcta sin llamadas síncronas.
    /// </summary>
    public decimal DifficultyMultiplier { get; private set; }

    // Colecciones de solo lectura
    public IReadOnlyList<Guid> RegisteredTeamIds   => _registeredTeamIds.AsReadOnly();
    public IReadOnlyList<EvidenceSubmission> EvidenceSubmissions => _evidenceSubmissions.AsReadOnly();
    public IReadOnlyList<ReleasedHint> ReleasedHints => _releasedHints.AsReadOnly();
    public IReadOnlyList<AllowedNode> AllowedNodes  => _allowedNodes.AsReadOnly();

    private LiveSession() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva LiveSession en estado Scheduled.
    ///
    /// allowedNodes: snapshot de los nodos hoja de la misión,
    /// cargado en el Application Service desde MissionManagement
    /// (o desde el evento MissionActivatedEvent). Resuelve RB-05
    /// de forma local y desacoplada.
    /// </summary>
    public static LiveSession Create(
        Guid missionRef,
        Guid operatorRef,
        IEnumerable<AllowedNode> allowedNodes,
        decimal difficultyMultiplier,
        int? maxDurationMinutes = null)
    {
        if (missionRef == Guid.Empty)
            throw new ArgumentException("MissionRef no puede ser vacío.", nameof(missionRef));
        if (operatorRef == Guid.Empty)
            throw new ArgumentException("OperatorRef no puede ser vacío.", nameof(operatorRef));
        if (difficultyMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(difficultyMultiplier),
                "El multiplicador de dificultad debe ser mayor que cero.");

        var nodeList = allowedNodes?.ToList()
            ?? throw new ArgumentNullException(nameof(allowedNodes));

        if (nodeList.Count == 0)
            throw new SessionDomainException(
                "No se puede crear una sesión sin nodos permitidos. " +
                "La misión debe tener al menos un nodo hoja activo.");

        var session = new LiveSession
        {
            Id = Guid.NewGuid(),
            MissionRef = missionRef,
            OperatorRef = operatorRef,
            DifficultyMultiplier = difficultyMultiplier,
            MaxDurationMinutes = maxDurationMinutes,
            Status = LiveSessionStatus.Pending,
            JoinCode = GenerateJoinCode(),
            CreatedAtUtc = DateTime.UtcNow
        };

        session._allowedNodes.AddRange(nodeList);
        return session;
    }

    public static LiveSession CreateForMission(
        Guid missionRef,
        Guid operatorRef,
        IEnumerable<AllowedNode> allowedNodes,
        decimal difficultyMultiplier,
        int? maxDurationMinutes = null)
    {
        return Create(
            missionRef: missionRef,
            operatorRef: operatorRef,
            allowedNodes: allowedNodes,
            difficultyMultiplier: difficultyMultiplier,
            maxDurationMinutes: maxDurationMinutes);
    }

    // ── Gestión de equipos ─────────────────────────────────────────────────────

    /// <summary>
    /// Registra un equipo en la sesión.
    /// Solo permitido en estados Scheduled y Preparation.
    ///
    /// Dispara: TeamRegisteredEvent — ScoringAudit crea el TeamLedger.
    /// </summary>
    public void RegisterTeam(Guid teamId)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));

        if (Status is not (LiveSessionStatus.Pending or LiveSessionStatus.Preparation))
            throw new SessionDomainException(
                $"No se pueden registrar equipos en una sesión con estado '{Status}'. " +
                $"Solo se permite en Pending o Preparation.");

        if (_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} ya está registrado en esta sesión.");

        _registeredTeamIds.Add(teamId);

        RaiseDomainEvent(new TeamRegisteredEvent
        {
            SessionId = Id,
            TeamId = teamId
        });
    }

    // ── Ciclo de vida de la sesión ─────────────────────────────────────────────

    /// <summary>
    /// Mueve la sesión a estado Preparation (configuración de equipos).
    /// Transición: Pending → Preparation.
    /// </summary>
    public void BeginPreparation()
    {
        TransitionTo(LiveSessionStatus.Preparation);
        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = LiveSessionStatus.Pending,
            NewStatus = LiveSessionStatus.Preparation
        });
    }

    public void JoinTeam(Guid teamId, string providedJoinCode)
    {
        if (string.IsNullOrWhiteSpace(providedJoinCode))
            throw new ArgumentException("El código de unión no puede estar vacío.", nameof(providedJoinCode));

        if (!string.Equals(JoinCode, providedJoinCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new SessionDomainException("Código de sesión inválido.");

        RegisterTeam(teamId);
    }

    /// <summary>
    /// Inicia el juego en vivo.
    /// Transición: Preparation → Active.
    ///
    /// INVARIANTE RB-02: Lanza excepción si no hay equipos registrados.
    ///
    /// Dispara: SessionStartedEvent — bloquea equipos, crea registro en AuditLog.
    /// </summary>
    public void Start()
    {
        StartSession();
    }

    public void StartSession()
    {
        // RN-15 / RB-02
        if (_registeredTeamIds.Count == 0)
            throw new InvalidOperationException(
                $"La sesión {Id} no puede iniciarse porque no tiene equipos registrados. " +
                "Debe existir al menos un equipo participante (RN-15).");

        var previous = Status;
        TransitionTo(LiveSessionStatus.Active);
        StartedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SessionStartedEvent
        {
            SessionId = Id,
            MissionRef = MissionRef,
            OperatorRef = OperatorRef,
            ParticipatingTeamIds = [.. _registeredTeamIds],
            StartedAtUtc = StartedAtUtc.Value
        });

        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = previous,
            NewStatus = LiveSessionStatus.Active
        });
    }

    /// <summary>
    /// Pausa la sesión. No se aceptarán evidencias mientras esté pausada.
    /// Transición: Active → Paused.
    /// </summary>
    public void Pause(string? reason = null)
    {
        var previous = Status;
        TransitionTo(LiveSessionStatus.Paused);

        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = previous,
            NewStatus = LiveSessionStatus.Paused,
            Reason = reason
        });
    }

    /// <summary>
    /// Reanuda la sesión pausada.
    /// Transición: Paused → Active.
    /// </summary>
    public void Resume(string? reason = null)
    {
        var previous = Status;
        TransitionTo(LiveSessionStatus.Active);

        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = previous,
            NewStatus = LiveSessionStatus.Active,
            Reason = reason
        });
    }

    /// <summary>
    /// Finaliza la sesión correctamente.
    /// Transición: Active | Paused → Finalized.
    ///
    /// Dispara: SessionFinalizedEvent — ScoringAudit genera ranking final,
    /// Team aggregate desbloquea equipos.
    /// </summary>
    public void Finalize(string? reason = null)
    {
        var previous = Status;
        TransitionTo(LiveSessionStatus.Finalized);
        FinalizedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SessionFinalizedEvent
        {
            SessionId = Id,
            FinalizedAtUtc = FinalizedAtUtc.Value,
            ParticipatingTeamIds = [.. _registeredTeamIds]
        });

        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = previous,
            NewStatus = LiveSessionStatus.Finalized,
            Reason = reason
        });
    }

    /// <summary>
    /// Cancela la sesión. Puede cancelarse desde cualquier estado no terminal.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        var previous = Status;
        TransitionTo(LiveSessionStatus.Cancelled);
        FinalizedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new SessionFinalizedEvent
        {
            SessionId = Id,
            FinalizedAtUtc = FinalizedAtUtc.Value,
            ParticipatingTeamIds = [.. _registeredTeamIds]
        });

        RaiseDomainEvent(new SessionStateChangedEvent
        {
            SessionId = Id,
            PreviousStatus = previous,
            NewStatus = LiveSessionStatus.Cancelled,
            Reason = reason
        });
    }

    // ── Motor de juego ─────────────────────────────────────────────────────────

    /// <summary>
    /// Acepta y registra una evidencia enviada por un equipo.
    ///
    /// INVARIANTE RB-03: solo acepta evidencias en estado Active.
    /// INVARIANTE RB-05: verifica que el MissionNodeId esté en AllowedNodes.
    ///
    /// No valida la CORRECCIÓN de la respuesta — eso lo hace
    /// EvidenceValidatorService (Chain of Responsibility) en la capa
    /// de Application antes de llamar a este método.
    /// Si el validador la aprueba, se llama a MarkEvidenceAsValid().
    ///
    /// Retorna la EvidenceSubmission creada para que el Application
    /// Service pueda referenciarla al disparar el evento.
    /// </summary>
    public EvidenceSubmission AcceptEvidence(
        Guid teamId,
        Guid missionNodeId,
        string payload)
    {
        // RB-03
        if (Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden aceptar evidencias en una sesión con estado '{Status}'. " +
                $"La sesión debe estar Active (RB-03).");

        // RB-05: el nodo debe pertenecer a esta sesión
        bool nodeAllowed = _allowedNodes.Any(n => n.NodeId == missionNodeId);
        if (!nodeAllowed)
            throw new SessionDomainException(
                $"El nodo {missionNodeId} no pertenece a los nodos permitidos " +
                $"de esta sesión (misión {MissionRef}). Violación de RB-05.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        var evidence = EvidenceSubmission.Create(teamId, missionNodeId, payload);
        _evidenceSubmissions.Add(evidence);
        return evidence;
    }

    public Guid? GetCurrentNodeForTeam(
        Guid teamId,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException($"El equipo {teamId} no está registrado en la sesión {Id}.");
        if (validationRules is null || validationRules.Count == 0)
            throw new ArgumentException("Las reglas de validación no pueden estar vacías.", nameof(validationRules));

        var completedNodeIds = _evidenceSubmissions
            .Where(e => e.TeamId == teamId && e.IsValid == true)
            .Select(e => e.MissionNodeId)
            .Distinct()
            .ToHashSet();

        var currentRule = validationRules
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefault(x => !completedNodeIds.Contains(x.NodeId));

        return currentRule?.NodeId;
    }

    public SubmissionResult SubmitTriviaAnswer(
        Guid teamId,
        Guid nodeId,
        string answer,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        return SubmitByValidationType(
            teamId: teamId,
            nodeId: nodeId,
            payload: answer,
            expectedType: NodeValidationType.Trivia,
            validationRules: validationRules);
    }

    public SubmissionResult SubmitTreasureHuntCode(
        Guid teamId,
        Guid nodeId,
        string foundCode,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        return SubmitByValidationType(
            teamId: teamId,
            nodeId: nodeId,
            payload: foundCode,
            expectedType: NodeValidationType.TreasureHunt,
            validationRules: validationRules);
    }

    /// <summary>
    /// Marca una evidencia previamente aceptada como válida y dispara
    /// el evento EvidenceValidated hacia ScoringAudit.
    ///
    /// Llamado por el Application Service tras la validación exitosa
    /// de EvidenceValidatorService.
    /// </summary>
    public void MarkEvidenceAsValid(Guid evidenceId)
    {
        var evidence = FindEvidence(evidenceId);
        evidence.MarkAsValid();

        var node = _allowedNodes.First(n => n.NodeId == evidence.MissionNodeId);
        double elapsedSeconds = StartedAtUtc.HasValue
            ? (DateTime.UtcNow - StartedAtUtc.Value).TotalSeconds
            : 0;

        RaiseDomainEvent(new EvidenceValidatedEvent
        {
            SessionId = Id,
            EvidenceSubmissionId = evidence.Id,
            TeamId = evidence.TeamId,
            MissionNodeId = evidence.MissionNodeId,
            NodeType = node.NodeType,
            BaseScore = node.BaseScore,
            DifficultyMultiplier = DifficultyMultiplier,
            ElapsedSeconds = elapsedSeconds
        });
    }

    /// <summary>
    /// Marca una evidencia como inválida (respuesta incorrecta).
    /// No dispara evento hacia ScoringAudit — no hay cambio de puntaje.
    /// </summary>
    public void MarkEvidenceAsInvalid(Guid evidenceId, string reason)
    {
        var evidence = FindEvidence(evidenceId);
        evidence.MarkAsInvalid(reason);
    }

    /// <summary>
    /// Libera una pista a un equipo específico.
    ///
    /// INVARIANTE RB-03: solo en estado Active.
    /// INVARIANTE RB-04: no puede liberarse la misma pista dos veces
    ///                   al mismo equipo para el mismo nodo.
    ///
    /// Dispara: HintReleasedEvent — ScoringAudit registra en AuditLog
    /// y aplica la penalización de puntaje correspondiente.
    /// </summary>
    public void ReleaseHint(
        Guid teamId,
        Guid hintId,
        Guid missionNodeId,
        int penaltyPoints,
        bool wasManualRelease = true)
    {
        // RB-03
        if (Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden liberar pistas en una sesión con estado '{Status}'.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        // RB-04: misma pista al mismo equipo
        bool alreadyReleased = _releasedHints
            .Any(r => r.TeamId == teamId && r.HintId == hintId);
        if (alreadyReleased)
            throw new SessionDomainException(
                $"La pista {hintId} ya fue liberada al equipo {teamId} (RB-04). " +
                $"No puede liberarse dos veces.");

        var released = ReleasedHint.Create(
            teamId, hintId, missionNodeId, penaltyPoints, wasManualRelease);
        _releasedHints.Add(released);

        RaiseDomainEvent(new HintReleasedEvent
        {
            SessionId = Id,
            TeamId = teamId,
            HintId = hintId,
            MissionNodeId = missionNodeId,
            PenaltyPoints = penaltyPoints,
            WasManualRelease = wasManualRelease
        });
    }

    /// <summary>
    /// Registra una penalización manual del Operador sobre un equipo.
    ///
    /// INVARIANTE RB-06: el motivo es obligatorio.
    /// Este método solo valida y dispara el evento.
    /// ScoringAudit es quien aplica el descuento real de puntos (Flujo 4).
    /// </summary>
    public void ApplyManualPenalty(
        Guid teamId,
        Guid operatorRef,
        int penaltyPoints,
        string reason)
    {
        if (Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden aplicar penalizaciones en una sesión con estado '{Status}'.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        // RB-06
        if (string.IsNullOrWhiteSpace(reason))
            throw new SessionDomainException(
                "El motivo de la penalización es obligatorio (RB-06). " +
                "Debes especificar la razón antes de aplicar la penalización.");

        if (penaltyPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(penaltyPoints),
                "Los puntos de penalización deben ser un valor positivo (se restarán del puntaje).");

        RaiseDomainEvent(new ManualPenaltyAppliedEvent
        {
            SessionId = Id,
            TeamId = teamId,
            OperatorRef = operatorRef,
            PenaltyPoints = penaltyPoints,
            Reason = reason
        });
    }

    // ── Patrón State — Matriz de transiciones (RB-09) ─────────────────────────

    /// <summary>
    /// Valida y ejecuta una transición de estado.
    ///
    /// Patrón State implementado como tabla de transiciones válidas.
    /// Cualquier transición no listada lanza SessionDomainException (RB-09).
    ///
    /// Transiciones válidas:
    ///   Pending   → Preparation, Cancelled
    ///   Preparation → Active, Cancelled
    ///   Active      → Paused, Finalized, Cancelled
    ///   Paused      → Active, Finalized, Cancelled
    /// </summary>
    private void TransitionTo(LiveSessionStatus newStatus)
    {
        bool isValid = (Status, newStatus) switch
        {
            (LiveSessionStatus.Pending,   LiveSessionStatus.Preparation) => true,
            (LiveSessionStatus.Pending,   LiveSessionStatus.Cancelled)   => true,
            (LiveSessionStatus.Preparation, LiveSessionStatus.Active)      => true,
            (LiveSessionStatus.Preparation, LiveSessionStatus.Cancelled)   => true,
            (LiveSessionStatus.Active,      LiveSessionStatus.Paused)      => true,
            (LiveSessionStatus.Active,      LiveSessionStatus.Finalized)   => true,
            (LiveSessionStatus.Active,      LiveSessionStatus.Cancelled)   => true,
            (LiveSessionStatus.Paused,      LiveSessionStatus.Active)      => true,
            (LiveSessionStatus.Paused,      LiveSessionStatus.Finalized)   => true,
            (LiveSessionStatus.Paused,      LiveSessionStatus.Cancelled)   => true,
            _ => false
        };

        if (!isValid)
            throw new SessionDomainException(
                $"Transición de estado inválida: '{Status}' → '{newStatus}'. " +
                $"Esta transición no está permitida por las reglas del dominio (RB-09).");

        Status = newStatus;
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    private EvidenceSubmission FindEvidence(Guid evidenceId) =>
        _evidenceSubmissions.FirstOrDefault(e => e.Id == evidenceId)
        ?? throw new SessionDomainException(
            $"No se encontró la evidencia {evidenceId} en la sesión {Id}.");

    private SubmissionResult SubmitByValidationType(
        Guid teamId,
        Guid nodeId,
        string payload,
        NodeValidationType expectedType,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("El payload no puede estar vacío.", nameof(payload));
        if (validationRules is null || validationRules.Count == 0)
            throw new ArgumentException("Las reglas de validación no pueden estar vacías.", nameof(validationRules));
        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException($"El equipo {teamId} no está registrado en la sesión {Id}.");

        var orderedRules = validationRules.OrderBy(x => x.ExecutionOrder).ToList();
        var currentRule = ResolveCurrentRule(teamId, orderedRules);
        if (currentRule is null)
            throw new SessionDomainException($"El equipo {teamId} ya completó todos los nodos.");

        // RN-04
        bool alreadyClosed = _evidenceSubmissions.Any(e =>
            e.TeamId == teamId &&
            e.MissionNodeId == nodeId &&
            e.IsValid == true);

        if (alreadyClosed)
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        // RN-11
        if (currentRule.NodeId != nodeId)
            throw new SessionDomainException(
                $"Progresión secuencial inválida. Se esperaba el nodo {currentRule.NodeId} (RN-11).");

        if (currentRule.ValidationType != expectedType)
            throw new SessionDomainException("Tipo de validación no coincide con el nodo actual.");

        var evidence = AcceptEvidence(teamId, nodeId, payload);

        // RN-12
        bool isCorrect = string.Equals(
            payload.Trim(),
            currentRule.ExpectedValue.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
            MarkEvidenceAsValid(evidence.Id);
        else
            MarkEvidenceAsInvalid(evidence.Id, "Respuesta/código incorrecto.");

        var nextRule = ResolveCurrentRule(teamId, orderedRules);
        var baseScore = _allowedNodes.FirstOrDefault(x => x.NodeId == nodeId)?.BaseScore ?? 0;

        return new SubmissionResult(
            IsCorrect: isCorrect,
            CurrentNodeId: nodeId,
            NextNodeId: nextRule?.NodeId,
            AwardedPoints: isCorrect ? baseScore : 0);
    }

    private NodeValidationRule? ResolveCurrentRule(
        Guid teamId,
        IReadOnlyList<NodeValidationRule> orderedRules)
    {
        var completedNodeIds = _evidenceSubmissions
            .Where(e => e.TeamId == teamId && e.IsValid == true)
            .Select(e => e.MissionNodeId)
            .Distinct()
            .ToHashSet();

        return orderedRules.FirstOrDefault(x => !completedNodeIds.Contains(x.NodeId));
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(6);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}