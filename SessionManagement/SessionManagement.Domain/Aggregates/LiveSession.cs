using SessionManagement.Domain.Common;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Events;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Services;
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
    private readonly List<SessionJoinRequest> _joinRequests = [];
    private readonly List<TeamParticipation> _teamParticipations = [];

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
    public IReadOnlyList<SessionJoinRequest> JoinRequests => _joinRequests.AsReadOnly();
    public IReadOnlyList<TeamParticipation> TeamParticipations => _teamParticipations.AsReadOnly();

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
    public void RegisterTeam(Guid teamId, string? teamName = null)
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

        if (_teamParticipations.All(x => x.TeamId != teamId))
            _teamParticipations.Add(TeamParticipation.Create(teamId));

        RaiseDomainEvent(new TeamRegisteredEvent
        {
            SessionId = Id,
            TeamId = teamId,
            TeamName = string.IsNullOrWhiteSpace(teamName) ? string.Empty : teamName.Trim()
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

    /// <summary>
    /// Crea una solicitud formal Pending para unirse a la sesión (HU-49).
    /// No registra el equipo hasta ApproveJoinRequest (RN-15).
    /// </summary>
    public SessionJoinRequest SubmitJoinRequest(Guid teamId, string providedJoinCode)
    {
        if (string.IsNullOrWhiteSpace(providedJoinCode))
            throw new ArgumentException("El código de unión no puede estar vacío.", nameof(providedJoinCode));

        if (!string.Equals(JoinCode, providedJoinCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new SessionDomainException("Código de sesión inválido.");

        if (Status is not (LiveSessionStatus.Pending or LiveSessionStatus.Preparation))
            throw new SessionDomainException(
                $"No se aceptan solicitudes de unión en estado '{Status}'.");

        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));

        if (_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} ya está registrado en esta sesión.");

        var existingPending = _joinRequests.FirstOrDefault(
            x => x.TeamId == teamId && x.Status == JoinRequestStatus.Pending);
        if (existingPending is not null)
            return existingPending;

        var rejected = _joinRequests.FirstOrDefault(
            x => x.TeamId == teamId && x.Status == JoinRequestStatus.Rejected);
        if (rejected is not null)
        {
            // Permite reintentar creando una nueva solicitud Pending.
        }

        var request = SessionJoinRequest.Create(teamId);
        _joinRequests.Add(request);

        RaiseDomainEvent(new SessionJoinRequestCreatedEvent
        {
            SessionId = Id,
            TeamId = teamId,
            RequestId = request.Id
        });

        return request;
    }

    /// <summary>Compatibilidad: crea solicitud Pending (ya no registra directo).</summary>
    public void JoinTeam(Guid teamId, string providedJoinCode)
    {
        SubmitJoinRequest(teamId, providedJoinCode);
    }

    public void ApproveJoinRequest(Guid teamId, Guid operatorId, string? teamName = null)
    {
        EnsureOperatorOwnsSession(operatorId);

        var request = FindPendingJoinRequest(teamId);
        request.Approve(operatorId);

        RegisterTeam(teamId, teamName);

        RaiseDomainEvent(new SessionJoinRequestResolvedEvent
        {
            SessionId = Id,
            TeamId = teamId,
            RequestId = request.Id,
            Decision = JoinRequestStatus.Approved,
            OperatorId = operatorId
        });
    }

    public void RejectJoinRequest(Guid teamId, Guid operatorId)
    {
        EnsureOperatorOwnsSession(operatorId);

        var request = FindPendingJoinRequest(teamId);
        request.Reject(operatorId);

        RaiseDomainEvent(new SessionJoinRequestResolvedEvent
        {
            SessionId = Id,
            TeamId = teamId,
            RequestId = request.Id,
            Decision = JoinRequestStatus.Rejected,
            OperatorId = operatorId
        });
    }

    public void TogglePause(string? reason = null)
    {
        if (Status is LiveSessionStatus.Finalized or LiveSessionStatus.Cancelled)
            throw new SessionDomainException(
                "Una sesión finalizada o cancelada no puede pausarse ni reanudarse (RN-17).");

        if (Status == LiveSessionStatus.Active)
            Pause(reason);
        else if (Status == LiveSessionStatus.Paused)
            Resume(reason);
        else
            throw new SessionDomainException(
                $"No se puede alternar pausa desde el estado '{Status}'.");
    }

    public void SendSupportMessage(Guid teamId, Guid operatorId, string message)
    {
        EnsureOperatorOwnsSession(operatorId);

        if (Status is LiveSessionStatus.Finalized or LiveSessionStatus.Cancelled)
            throw new SessionDomainException(
                "No se pueden enviar mensajes en una sesión finalizada (RN-17).");

        if (string.IsNullOrWhiteSpace(message))
            throw new SessionDomainException("El mensaje de soporte no puede estar vacío.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        var participation = _teamParticipations.FirstOrDefault(x => x.TeamId == teamId)
            ?? throw new SessionDomainException($"No hay participación registrada para el equipo {teamId}.");

        if (!participation.CanReceiveSupportMessage)
            throw new SessionDomainException(
                "No se puede enviar un mensaje a un equipo expulsado o que ya finalizó la misión (RN-18).");

        RaiseDomainEvent(new SupportMessageSentEvent
        {
            SessionId = Id,
            TeamId = teamId,
            OperatorId = operatorId,
            Message = message.Trim()
        });
    }

    /// <summary>
    /// Marca al equipo como Completado al superar el último nodo (HU-61).
    /// La sesión permanece Active/Paused hasta que el operador la finalice (RN-17).
    /// </summary>
    public void MarkTeamCompleted(Guid teamId)
    {
        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        var participation = _teamParticipations.FirstOrDefault(x => x.TeamId == teamId)
            ?? throw new SessionDomainException($"No hay participación registrada para el equipo {teamId}.");

        if (participation.Status == TeamParticipationStatus.Completed)
            return;

        participation.MarkCompleted();

        var elapsed = StartedAtUtc.HasValue
            ? (DateTime.UtcNow - StartedAtUtc.Value).TotalSeconds
            : 0;

        RaiseDomainEvent(new TeamCompletedMissionEvent
        {
            SessionId = Id,
            TeamId = teamId,
            CompletedAtUtc = participation.CompletedAtUtc ?? DateTime.UtcNow,
            ElapsedSeconds = elapsed
        });
    }

    public TeamParticipationStatus GetTeamParticipationStatus(Guid teamId)
    {
        var participation = _teamParticipations.FirstOrDefault(x => x.TeamId == teamId);
        return participation?.Status ?? TeamParticipationStatus.Active;
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
            MissionRef = MissionRef,
            OperatorRef = OperatorRef,
            FinalizedAtUtc = FinalizedAtUtc.Value,
            ParticipatingTeamIds = [.. _registeredTeamIds],
            Status = "Finalized"
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
            MissionRef = MissionRef,
            OperatorRef = OperatorRef,
            FinalizedAtUtc = FinalizedAtUtc.Value,
            ParticipatingTeamIds = [.. _registeredTeamIds],
            Status = "Cancelled"
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
        string payload,
        int? questionIndex = null)
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

        var evidence = EvidenceSubmission.Create(teamId, missionNodeId, payload, questionIndex);
        _evidenceSubmissions.Add(evidence);
        return evidence;
    }

    public void MarkEvidenceAsValid(Guid evidenceId, bool publishScoreEvent = true)
    {
        var evidence = FindEvidence(evidenceId);
        evidence.MarkAsValid();

        if (!publishScoreEvent)
            return;

        PublishEvidenceValidatedScore(evidence);
    }

    private void PublishEvidenceValidatedScore(EvidenceSubmission evidence)
    {
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
            NodeTitle = node.Title,
            BaseScore = node.BaseScore,
            DifficultyMultiplier = DifficultyMultiplier,
            ElapsedSeconds = elapsedSeconds
        });
    }

    public Guid? GetCurrentNodeForTeam(
        Guid teamId,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException($"El equipo {teamId} no está registrado en la sesión {Id}.");
        if (validationRules is null || validationRules.Count == 0)
            throw new ArgumentException("Las reglas de validación no pueden estar vacías.", nameof(validationRules));

        var orderedRules = validationRules.OrderBy(x => x.ExecutionOrder).ToList();
        foreach (var rule in orderedRules)
        {
            if (!IsNodeCompletedForTeam(teamId, rule))
                return rule.NodeId;
        }

        return null;
    }

    public SubmissionResult SubmitTriviaAnswer(
        Guid teamId,
        Guid nodeId,
        string answer,
        int questionIndex,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        return SubmitByValidationType(
            teamId: teamId,
            nodeId: nodeId,
            payload: answer,
            questionIndex: questionIndex,
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
            questionIndex: null,
            expectedType: NodeValidationType.TreasureHunt,
            validationRules: validationRules);
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
    /// INVARIANTE RB-04 / RN-06: no puede liberarse la misma pista dos veces al mismo equipo.
    /// INVARIANTE RN-04 / RN-07: solo pistas del juego (nodo) actual del equipo;
    /// no se liberan pistas de juegos ya superados ni futuros.
    ///
    /// Dispara: HintReleasedEvent — ScoringAudit registra en AuditLog
    /// y aplica la penalización de puntaje correspondiente.
    /// </summary>
    public void ReleaseHint(
        Guid teamId,
        Guid hintId,
        Guid missionNodeId,
        int penaltyPoints,
        IReadOnlyList<NodeValidationRule> validationRules,
        bool wasManualRelease = true,
        int hintOrder = 0)
    {
        // RB-03
        if (Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden liberar pistas en una sesión con estado '{Status}'.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        EnsureTeamCanPlay(teamId);

        if (validationRules is null || validationRules.Count == 0)
            throw new ArgumentException(
                "Las reglas de validación no pueden estar vacías.", nameof(validationRules));

        var node = _allowedNodes.FirstOrDefault(n => n.NodeId == missionNodeId)
            ?? throw new SessionDomainException(
                $"El nodo {missionNodeId} no pertenece a esta sesión.");

        var currentNodeId = GetCurrentNodeForTeam(teamId, validationRules);
        if (currentNodeId is null)
            throw new SessionDomainException(
                "El equipo ya completó todos los juegos; no se pueden liberar más pistas (RN-04).");

        if (missionNodeId != currentNodeId.Value)
            throw new SessionDomainException(
                $"Solo se pueden liberar pistas del juego actual del equipo (nodo {currentNodeId.Value}). " +
                $"La pista pertenece al nodo {missionNodeId} (RN-04/RN-07).");

        // RB-04 / RN-06: misma pista al mismo equipo
        bool alreadyReleased = _releasedHints
            .Any(r => r.TeamId == teamId && r.HintId == hintId);
        if (alreadyReleased)
            throw new SessionDomainException(
                $"La pista {hintId} ya fue liberada al equipo {teamId} (RB-04/RN-06). " +
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
            NodeType = node.NodeType,
            NodeTitle = node.Title,
            HintOrder = hintOrder,
            PenaltyPoints = penaltyPoints,
            WasManualRelease = wasManualRelease
        });
    }

    /// <summary>
    /// Al completar una Búsqueda del Tesoro, libera automáticamente todas las pistas
    /// del nodo que el equipo aún no haya recibido.
    ///
    /// No aplica penalización (0 pts): solo revela el catálogo pendiente tras el hallazgo.
    /// Puede ejecutarse aunque el nodo ya no sea el actual o el equipo esté Completed.
    /// </summary>
    public void ReleaseRemainingHintsAutomatically(
        Guid teamId,
        Guid missionNodeId,
        IReadOnlyList<(Guid HintId, int Order)> catalogHints)
    {
        if (Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden liberar pistas en una sesión con estado '{Status}'.");

        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException(
                $"El equipo {teamId} no está registrado en la sesión {Id}.");

        var participationStatus = GetTeamParticipationStatus(teamId);
        if (participationStatus == TeamParticipationStatus.Expelled)
            throw new SessionDomainException(
                $"El equipo {teamId} no puede recibir pistas porque su estado es '{participationStatus}'.");

        var node = _allowedNodes.FirstOrDefault(n => n.NodeId == missionNodeId)
            ?? throw new SessionDomainException(
                $"El nodo {missionNodeId} no pertenece a esta sesión.");

        if (!string.Equals(node.NodeType, "TreasureHunt", StringComparison.OrdinalIgnoreCase))
            throw new SessionDomainException(
                "La liberación automática de pistas pendientes solo aplica a Búsqueda del Tesoro.");

        var treasureCompleted = _evidenceSubmissions.Any(e =>
            e.TeamId == teamId &&
            e.MissionNodeId == missionNodeId &&
            e.IsValid == true);
        if (!treasureCompleted)
            throw new SessionDomainException(
                $"El equipo {teamId} aún no completó la búsqueda del nodo {missionNodeId}.");

        if (catalogHints is null || catalogHints.Count == 0)
            return;

        foreach (var (hintId, order) in catalogHints.OrderBy(h => h.Order))
        {
            if (hintId == Guid.Empty)
                continue;

            if (_releasedHints.Any(r => r.TeamId == teamId && r.HintId == hintId))
                continue;

            var released = ReleasedHint.Create(
                teamId, hintId, missionNodeId, penaltyPoints: 0, wasManualRelease: false);
            _releasedHints.Add(released);

            RaiseDomainEvent(new HintReleasedEvent
            {
                SessionId = Id,
                TeamId = teamId,
                HintId = hintId,
                MissionNodeId = missionNodeId,
                NodeType = node.NodeType,
                NodeTitle = node.Title,
                HintOrder = order,
                PenaltyPoints = 0,
                WasManualRelease = false
            });
        }
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

        EnsureTeamCanPlay(teamId);

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

    private void EnsureOperatorOwnsSession(Guid operatorId)
    {
        if (operatorId != OperatorRef)
            throw new SessionDomainException(
                "El operador no está autorizado para operar esta sesión (RN-16).");
    }

    private SessionJoinRequest FindPendingJoinRequest(Guid teamId)
    {
        return _joinRequests.FirstOrDefault(
                   x => x.TeamId == teamId && x.Status == JoinRequestStatus.Pending)
               ?? throw new SessionDomainException(
                   $"No hay una solicitud pendiente del equipo {teamId} en la sesión {Id}.");
    }

    private void EnsureTeamCanPlay(Guid teamId)
    {
        var status = GetTeamParticipationStatus(teamId);
        if (status is TeamParticipationStatus.Completed or TeamParticipationStatus.Expelled)
            throw new SessionDomainException(
                $"El equipo {teamId} no puede interactuar porque su estado es '{status}'.");
    }

    private EvidenceSubmission FindEvidence(Guid evidenceId) =>
        _evidenceSubmissions.FirstOrDefault(e => e.Id == evidenceId)
        ?? throw new SessionDomainException(
            $"No se encontró la evidencia {evidenceId} en la sesión {Id}.");

    private SubmissionResult SubmitByValidationType(
        Guid teamId,
        Guid nodeId,
        string payload,
        int? questionIndex,
        NodeValidationType expectedType,
        IReadOnlyList<NodeValidationRule> validationRules)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("El payload no puede estar vacío.", nameof(payload));
        if (validationRules is null || validationRules.Count == 0)
            throw new ArgumentException("Las reglas de validación no pueden estar vacías.", nameof(validationRules));
        if (!_registeredTeamIds.Contains(teamId))
            throw new SessionDomainException($"El equipo {teamId} no está registrado en la sesión {Id}.");

        EnsureTeamCanPlay(teamId);

        var orderedRules = validationRules.OrderBy(x => x.ExecutionOrder).ToList();
        var currentRule = ResolveCurrentRule(teamId, orderedRules);
        if (currentRule is null)
            throw new SessionDomainException($"El equipo {teamId} ya completó todos los nodos.");

        if (IsNodeCompletedForTeam(teamId, currentRule))
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        var submittedRule = orderedRules.FirstOrDefault(r => r.NodeId == nodeId);
        if (submittedRule is not null && IsNodeCompletedForTeam(teamId, submittedRule))
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        if (currentRule.NodeId != nodeId)
            throw new SessionDomainException(
                $"Progresión secuencial inválida. Se esperaba el nodo {currentRule.NodeId} (RN-11).");

        if (currentRule.ValidationType != expectedType)
            throw new SessionDomainException("Tipo de validación no coincide con el nodo actual.");

        var resolvedQuestionIndex = ResolveQuestionIndex(teamId, currentRule, questionIndex);
        var expectedAnswer = currentRule.ExpectedAnswers[resolvedQuestionIndex];

        var evidence = AcceptEvidence(teamId, nodeId, payload, resolvedQuestionIndex);

        bool isCorrect = string.Equals(
            payload.Trim(),
            expectedAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase);

        var nodeCompleted = false;
        if (isCorrect)
        {
            nodeCompleted = resolvedQuestionIndex >= currentRule.ExpectedAnswers.Count - 1;
            MarkEvidenceAsValid(evidence.Id, publishScoreEvent: nodeCompleted);
        }
        else
        {
            MarkEvidenceAsInvalid(evidence.Id, "Respuesta/código incorrecto.");
            // Trivia: un solo intento; al fallar se cierra el nodo y se avanza (0 pts).
            // TreasureHunt: puede reintentar hasta enviar el código correcto.
            if (expectedType == NodeValidationType.Trivia)
                nodeCompleted = true;
        }

        var nextRule = ResolveCurrentRule(teamId, orderedRules);
        var baseScore = _allowedNodes.FirstOrDefault(x => x.NodeId == nodeId)?.BaseScore ?? 0;
        var awardedPoints = isCorrect && nodeCompleted
            ? ScoreAwardCalculator.Compute(baseScore, DifficultyMultiplier)
            : 0;

        if (nodeCompleted && nextRule is null)
            MarkTeamCompleted(teamId);

        return new SubmissionResult(
            IsCorrect: isCorrect,
            CurrentNodeId: nodeId,
            NextNodeId: nodeCompleted ? nextRule?.NodeId : nodeId,
            AwardedPoints: awardedPoints,
            AnsweredQuestionIndex: resolvedQuestionIndex,
            TotalQuestions: currentRule.ExpectedAnswers.Count,
            NodeCompleted: nodeCompleted);
    }

    private int ResolveQuestionIndex(
        Guid teamId,
        NodeValidationRule rule,
        int? requestedQuestionIndex)
    {
        if (rule.ValidationType == NodeValidationType.TreasureHunt)
            return 0;

        var nextQuestionIndex = GetNextQuestionIndex(teamId, rule.NodeId, rule.ExpectedAnswers.Count);
        if (nextQuestionIndex >= rule.ExpectedAnswers.Count)
            throw new SessionDomainException("La etapa ya está cerrada para este equipo (RN-04).");

        if (requestedQuestionIndex.HasValue && requestedQuestionIndex.Value != nextQuestionIndex)
            throw new SessionDomainException(
                $"Progresión secuencial inválida. Se esperaba la pregunta {nextQuestionIndex} (RN-11).");

        return nextQuestionIndex;
    }

    private int GetNextQuestionIndex(Guid teamId, Guid nodeId, int totalQuestions)
    {
        var answeredIndices = _evidenceSubmissions
            .Where(e =>
                e.TeamId == teamId &&
                e.MissionNodeId == nodeId &&
                e.IsValid == true &&
                e.QuestionIndex.HasValue)
            .Select(e => e.QuestionIndex!.Value)
            .ToHashSet();

        for (var index = 0; index < totalQuestions; index++)
        {
            if (!answeredIndices.Contains(index))
                return index;
        }

        return totalQuestions;
    }

    private bool IsNodeCompletedForTeam(Guid teamId, NodeValidationRule rule)
    {
        if (rule.ValidationType == NodeValidationType.TreasureHunt)
        {
            return _evidenceSubmissions.Any(e =>
                e.TeamId == teamId &&
                e.MissionNodeId == rule.NodeId &&
                e.IsValid == true);
        }

        // Trivia: cierra con fallo (cualquier intento inválido) o al completar todas las preguntas.
        var hasFailedAttempt = _evidenceSubmissions.Any(e =>
            e.TeamId == teamId &&
            e.MissionNodeId == rule.NodeId &&
            e.IsValid == false);

        if (hasFailedAttempt)
            return true;

        return GetNextQuestionIndex(teamId, rule.NodeId, rule.ExpectedAnswers.Count)
            >= rule.ExpectedAnswers.Count;
    }

    private NodeValidationRule? ResolveCurrentRule(
        Guid teamId,
        IReadOnlyList<NodeValidationRule> orderedRules)
    {
        return orderedRules.FirstOrDefault(rule => !IsNodeCompletedForTeam(teamId, rule));
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(6);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}