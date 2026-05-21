using SessionManagement.Domain.Common;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Domain.Entities;

public class TeamSessionProgress : Entity
{
    public Guid TeamId { get; private set; }
    public Points TotalPoints { get; private set; }

    internal TeamSessionProgress(Guid teamId)
    {
        Id = Guid.NewGuid();
        TeamId = teamId;
        TotalPoints = Points.Zero;
    }

    internal void AddPoints(Points points)
    {
        TotalPoints += points;
    }

    internal void DeductPoints(Points points)
    {
        TotalPoints -= points;
    }
}