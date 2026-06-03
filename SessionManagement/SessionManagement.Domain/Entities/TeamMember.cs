using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Entities;

/// <summary>
/// Entidad que representa a un jugador dentro de un Equipo.
///
/// PlayerRef es el ID proveniente de Keycloak (sistema externo).
/// Este contexto no conoce los detalles del jugador — solo su identidad.
/// </summary>
public sealed class TeamMember : Entity
{
    /// <summary>
    /// Referencia al jugador en el sistema de identidad (Keycloak).
    /// Este contexto no gestiona la autenticación — solo referencia el ID.
    /// </summary>
    public Guid PlayerRef { get; private set; }

    /// <summary>Alias o nombre del jugador para mostrar en el tablero.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    public TeamMemberRole Role { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    private TeamMember() { }

    public static TeamMember Create(
        Guid playerRef,
        string displayName,
        TeamMemberRole role = TeamMemberRole.Member)
    {
        if (playerRef == Guid.Empty)
            throw new ArgumentException("PlayerRef no puede ser un Guid vacío.", nameof(playerRef));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("El nombre del jugador no puede estar vacío.", nameof(displayName));

        return new TeamMember
        {
            Id = Guid.NewGuid(),
            PlayerRef = playerRef,
            DisplayName = displayName.Trim(),
            Role = role,
            JoinedAtUtc = DateTime.UtcNow
        };
    }

    internal void PromoteToLeader()
    {
        Role = TeamMemberRole.Leader;
    }

    internal void DemoteToMember()
    {
        Role = TeamMemberRole.Member;
    }
}