using TeamService.Domain.Common;
using TeamService.Domain.ValueObjects;

namespace TeamService.Domain.Entities;

public class TeamMember : Entity
{
    public Guid PlayerId { get; private set; } // ID de Keycloak
    public PlayerNickname Nickname { get; private set; }
    public DateTime JoinedAt { get; private set; }

    internal TeamMember(Guid id, Guid playerId, PlayerNickname nickname)
    {
        Id = id;
        PlayerId = playerId;
        Nickname = nickname;
        JoinedAt = DateTime.UtcNow;
    }
}