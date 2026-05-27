using Common;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Events;
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

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<TeamMember> Members => _members.AsReadOnly();

    private Team() { }

    // ── Fábrica ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuevo equipo sin sesión asignada.
    /// El TeamCode se genera automáticamente.
    /// </summary>
    public static Team Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del equipo no puede estar vacío.", nameof(name));

        return new Team
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Code = TeamCode.Generate(),
            IsLocked = false,
            CreatedAtUtc = DateTime.UtcNow
        };
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

        if (_members.Count >= MaxMembers)
            throw new SessionDomainException(
                $"El equipo '{Name}' ya alcanzó el límite máximo de {MaxMembers} jugadores.");

        bool alreadyMember = _members.Any(m => m.PlayerRef == member.PlayerRef);
        if (alreadyMember)
            throw new SessionDomainException(
                $"El jugador {member.PlayerRef} ya es miembro del equipo '{Name}'.");

        _members.Add(member);
    }

    /// <summary>
    /// Elimina un jugador del equipo.
    /// INVARIANTE: no se puede eliminar si el equipo está bloqueado (IsLocked).
    /// </summary>
    public void RemoveMember(Guid playerRef)
    {
        if (IsLocked)
            throw new SessionDomainException(
                $"No se puede eliminar al jugador {playerRef} del equipo '{Name}' " +
                $"porque el equipo está bloqueado durante una sesión activa.");

        var member = _members.FirstOrDefault(m => m.PlayerRef == playerRef)
            ?? throw new SessionDomainException(
                $"El jugador {playerRef} no es miembro del equipo '{Name}'.");

        _members.Remove(member);
    }

    /// <summary>
    /// Asigna el equipo a una sesión.
    /// Llamado desde LiveSession.RegisterTeam().
    /// </summary>
    internal void AssignToSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId no puede ser vacío.", nameof(sessionId));

        CurrentSessionRef = sessionId;
    }

    /// <summary>
    /// Bloquea el equipo al iniciar la sesión.
    /// Reacción al evento SessionStarted (consumido por el handler de equipo).
    /// </summary>
    public void Lock()
    {
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
        if (!IsLocked)
            throw new SessionDomainException(
                $"El equipo '{Name}' no está bloqueado.");

        IsLocked = false;
        CurrentSessionRef = null;
    }
}