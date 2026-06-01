using Common;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain.Aggregates;

/// <summary>
/// AGGREGATE ROOT — Team.
///
/// Ciclo de vida independiente de LiveSession: un Team puede
/// existir entre sesiones, bloquearse durante una sesión activa
/// y desbloquearse al terminar.
///
/// INVARIANTES:
/// — Máximo 4 jugadores por equipo.
/// — Un jugador no puede abandonar el equipo si IsLocked = true (sesión activa).
/// — Un jugador no puede unirse dos veces al mismo equipo.
/// — Un jugador no puede estar en dos equipos de la misma sesión.
///   (esta última se valida en el Application Service, no aquí)
/// </summary>
public sealed class Team : AggregateRoot
{
    private const int MaxMembers = 4;
    private readonly List<TeamMember> _members = [];
    private readonly List<JoinRequest> _joinRequests = [];

    public string Name { get; private set; } = string.Empty;
    public TeamCode Code { get; private set; } = TeamCode.Generate();

    /// <summary>
    /// Sesión a la que el equipo está asignado actualmente.
    /// Null = equipo sin sesión asignada.
    /// </summary>
    public Guid? CurrentSessionRef { get; private set; }

    /// <summary>
    /// Cuando IsLocked = true, ningún miembro puede abandonar el equipo.
    /// Se activa al iniciar la sesión (SessionStarted) y se desactiva
    /// al finalizar o cancelar (SessionFinalized / Cancelled).
    /// </summary>
    public bool IsLocked { get; private set; }

    public bool IsDisbanded { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<TeamMember> Members => _members.AsReadOnly();
    public IReadOnlyList<JoinRequest> JoinRequests => _joinRequests.AsReadOnly();

    private Team() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo equipo sin sesión asignada.
    /// El TeamCode se genera automáticamente.
    /// </summary>
    public static Team Create(string name, Guid creatorId, string creatorDisplayName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del equipo no puede estar vacío.", nameof(name));
        if (creatorId == Guid.Empty)
            throw new ArgumentException("CreatorId no puede ser vacío.", nameof(creatorId));
        if (string.IsNullOrWhiteSpace(creatorDisplayName))
            throw new ArgumentException("El nombre del creador no puede estar vacío.", nameof(creatorDisplayName));

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Code = TeamCode.Generate(),
            IsLocked = false,
            IsDisbanded = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        team.AddMember(TeamMember.Create(
            creatorId,
            creatorDisplayName,
            TeamMemberRole.Leader));

        return team;
    }

    // ── Comportamiento ─────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega un jugador al equipo.
    /// INVARIANTE: máximo 4 miembros.
    /// INVARIANTE: un jugador no puede unirse dos veces.
    /// </summary>
    public void AddMember(TeamMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        EnsureNotDisbanded();

        if (_members.Count >= MaxMembers)
            throw new SessionDomainException(
                $"El equipo '{Name}' ya alcanzó el límite máximo de {MaxMembers} jugadores.");

        bool alreadyMember = _members.Any(m => m.PlayerRef == member.PlayerRef);
        if (alreadyMember)
            throw new SessionDomainException(
                $"El jugador {member.PlayerRef} ya es miembro del equipo '{Name}'.");

        if (member.Role == TeamMemberRole.Leader && _members.Any(x => x.Role == TeamMemberRole.Leader))
            throw new SessionDomainException("El equipo ya tiene un líder asignado.");

        if (!_members.Any() && member.Role != TeamMemberRole.Leader)
            throw new SessionDomainException("El primer integrante del equipo debe ser el líder.");

        _members.Add(member);
    }

    public void RegenerateCode()
    {
        EnsureNotDisbanded();
        Code = TeamCode.Generate();
    }

    public bool IsLeader(Guid playerRef)
    {
        return _members.Any(x =>
            x.PlayerRef == playerRef &&
            x.Role == TeamMemberRole.Leader);
    }

    public void UpdateName(string newName, Guid requestorId)
    {
        EnsureNotDisbanded();
        EnsureLeader(requestorId);

        if (IsLocked)
            throw new SessionDomainException("No se puede cambiar el nombre del equipo durante una sesión activa.");
        if (string.IsNullOrWhiteSpace(newName))
            throw new SessionDomainException("El nuevo nombre del equipo no puede estar vacío.");

        Name = newName.Trim();
    }

    public void SubmitJoinRequest(Guid playerRef, string displayName)
    {
        EnsureNotDisbanded();

        if (IsLocked)
            throw new SessionDomainException("No se aceptan solicitudes de unión mientras el equipo está en juego.");

        if (_members.Count >= MaxMembers)
            throw new SessionDomainException($"El equipo '{Name}' ya está completo.");

        if (_members.Any(x => x.PlayerRef == playerRef))
            throw new SessionDomainException("El jugador ya es miembro del equipo.");

        if (_joinRequests.Any(x => x.PlayerRef == playerRef && x.Status == JoinRequestStatus.Pending))
            throw new SessionDomainException("El jugador ya tiene una solicitud pendiente para este equipo.");

        _joinRequests.Add(JoinRequest.Create(playerRef, displayName));
    }

    public void ProcessJoinRequest(Guid requestId, bool isApproved)
    {
        EnsureNotDisbanded();
        if (requestId == Guid.Empty)
            throw new SessionDomainException("RequestId no puede ser vacío.");

        if (IsLocked)
            throw new SessionDomainException("No se pueden procesar solicitudes durante una sesión activa.");

        var joinRequest = _joinRequests.FirstOrDefault(x => x.Id == requestId)
            ?? throw new SessionDomainException("No se encontró la solicitud de unión.");

        if (joinRequest.Status != JoinRequestStatus.Pending)
            throw new SessionDomainException("La solicitud ya fue procesada.");

        if (isApproved)
        {
            AddMember(TeamMember.Create(joinRequest.PlayerRef, joinRequest.DisplayName));
            joinRequest.Approve();
            return;
        }

        joinRequest.Reject();
    }

    public void RemoveMember(Guid playerRef, Guid requestorId)
    {
        EnsureNotDisbanded();

        if (requestorId == Guid.Empty)
            throw new SessionDomainException("RequestorId no puede ser vacío.");

        if (!IsLeader(requestorId) && requestorId != playerRef)
            throw new SessionDomainException("Solo el líder puede remover a otros integrantes.");

        if (IsLocked)
            throw new SessionDomainException(
                $"No se puede eliminar al jugador {playerRef} del equipo '{Name}' " +
                $"porque el equipo está bloqueado durante una sesión activa.");

        var member = _members.FirstOrDefault(m => m.PlayerRef == playerRef)
            ?? throw new SessionDomainException(
                $"El jugador {playerRef} no es miembro del equipo '{Name}'.");

        _members.Remove(member);

        if (member.Role != TeamMemberRole.Leader)
            return;

        if (_members.Count == 0)
        {
            IsDisbanded = true;
            return;
        }

        var nextLeader = _members
            .OrderBy(x => x.JoinedAtUtc)
            .First();

        nextLeader.PromoteToLeader();
    }

    public void Disband(Guid requestorId)
    {
        EnsureNotDisbanded();
        EnsureLeader(requestorId);

        if (IsLocked)
            throw new SessionDomainException("No se puede disolver el equipo durante una sesión activa.");

        IsDisbanded = true;
    }

    /// <summary>
    /// Asigna el equipo a una sesión.
    /// Llamado desde LiveSession.RegisterTeam().
    /// </summary>
    internal void AssignToSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));

        EnsureNotDisbanded();

        CurrentSessionRef = sessionId;
    }

    /// <summary>
    /// Bloquea el equipo al iniciar la sesión.
    /// Reacción al evento SessionStarted (consumido por el handler de equipo).
    /// </summary>
    public void Lock()
    {
        EnsureNotDisbanded();

        if (IsLocked)
            throw new SessionDomainException(
                $"El equipo '{Name}' ya está bloqueado.");

        IsLocked = true;
    }

    /// <summary>
    /// Desbloquea el equipo al finalizar o cancelar la sesión.
    /// Reacción a SessionFinalizedEvent o SessionStateChangedEvent (Cancelled).
    /// </summary>
    public void Unlock()
    {
        EnsureNotDisbanded();

        if (!IsLocked)
            throw new SessionDomainException(
                $"El equipo '{Name}' no está bloqueado.");

        IsLocked = false;
        CurrentSessionRef = null;
    }

    private void EnsureLeader(Guid requestorId)
    {
        if (!IsLeader(requestorId))
            throw new SessionDomainException("Solo el líder del equipo puede ejecutar esta acción.");
    }

    private void EnsureNotDisbanded()
    {
        if (IsDisbanded)
            throw new SessionDomainException($"El equipo '{Name}' ya fue disuelto.");
    }
}