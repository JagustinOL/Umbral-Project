using SessionManagement.Domain.Common;
using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Domain.Entities;

/// <summary>
/// Seguimiento del estado de un equipo dentro de la sesión (RN-18 / HU-61).
/// </summary>
public sealed class TeamParticipation : Entity
{
    public Guid TeamId { get; private set; }
    public TeamParticipationStatus Status { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private TeamParticipation() { }

    public static TeamParticipation Create(Guid teamId)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));

        return new TeamParticipation
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Status = TeamParticipationStatus.Active
        };
    }

    public void MarkCompleted()
    {
        if (Status == TeamParticipationStatus.Expelled)
            throw new SessionDomainException("Un equipo expulsado no puede marcarse como Completado.");

        if (Status == TeamParticipationStatus.Completed)
            return;

        Status = TeamParticipationStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkExpelled()
    {
        if (Status == TeamParticipationStatus.Completed)
            throw new SessionDomainException("Un equipo que finalizó no puede ser expulsado.");

        Status = TeamParticipationStatus.Expelled;
    }

    public bool CanReceiveSupportMessage =>
        Status is TeamParticipationStatus.Active;
}
