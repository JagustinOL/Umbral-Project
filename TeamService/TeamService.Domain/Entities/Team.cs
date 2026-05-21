using TeamService.Domain.Common;
using TeamService.Domain.ValueObjects;

namespace TeamService.Domain.Entities;

public class Team : AggregateRoot
{
    public string Name { get; private set; }
    public TeamCode JoinCode { get; private set; }
    public Guid CaptainId { get; private set; } // ID de Keycloak
    public TeamState State { get; private set; }

    // Bloqueo por RN-13
    public Guid? CurrentLiveSessionId { get; private set; }
    public bool IsLockedInActiveSession { get; private set; }

    private readonly List<TeamMember> _members = [];
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    public const int MaxMembers = 4; // Ajustado a 4 según diagrama UML del profesor

    private Team(Guid id, string name, Guid captainId, PlayerNickname captainNickname)
    {
        Id = id;
        Name = name;
        JoinCode = TeamCode.Generate(); // Se autogenera al crear el equipo
        CaptainId = captainId;
        State = TeamState.Incomplete;
        IsLockedInActiveSession = false;
        
        // El capitán es el primer miembro
        _members.Add(new TeamMember(Guid.NewGuid(), captainId, captainNickname));
    }

    public static Team Create(string name, Guid captainId, PlayerNickname captainNickname)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del equipo no puede estar vacío.");

        return new Team(Guid.NewGuid(), name, captainId, captainNickname);
    }

    public void AddMember(Guid playerId, PlayerNickname nickname)
    {
        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("El equipo ya está lleno.");

        if (_members.Any(m => m.PlayerId == playerId))
            throw new InvalidOperationException("El usuario ya está en el equipo.");

        if (IsLockedInActiveSession)
            throw new InvalidOperationException("No se pueden unir miembros mientras el equipo juega una sesión activa.");

        _members.Add(new TeamMember(Guid.NewGuid(), playerId, nickname));

        if (_members.Count == MaxMembers)
            State = TeamState.ReadyToPlay;
    }

    public void RemoveMember(Guid playerId)
    {
        if (IsLockedInActiveSession)
            throw new InvalidOperationException("No puedes abandonar el equipo mientras están en una sesión activa.");

        var member = _members.FirstOrDefault(m => m.PlayerId == playerId) 
            ?? throw new InvalidOperationException("El jugador no está en el equipo.");

        if (member.PlayerId == CaptainId)
            throw new InvalidOperationException("El capitán no puede abandonar el equipo. Debe disolverlo o transferir el liderazgo.");

        _members.Remove(member);
        State = TeamState.Incomplete; // Si alguien sale, ya no están listos
    }

    // Métodos para reaccionar a RabbitMQ
    public void LockForSession(Guid sessionId)
    {
        CurrentLiveSessionId = sessionId;
        IsLockedInActiveSession = true;
    }

    public void UnlockFromSession()
    {
        CurrentLiveSessionId = null;
        IsLockedInActiveSession = false;
    }
}