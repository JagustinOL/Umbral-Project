using SessionManagement.Domain.Common;
using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Domain.Entities;

/// <summary>
/// Solicitud formal de un equipo para unirse a una LiveSession (HU-49 / RN-15).
/// Pending → Approved|Rejected por el Operador.
/// </summary>
public sealed class SessionJoinRequest : Entity
{
    public Guid TeamId { get; private set; }
    public JoinRequestStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByOperatorId { get; private set; }

    private SessionJoinRequest() { }

    public static SessionJoinRequest Create(Guid teamId)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("TeamId no puede ser vacío.", nameof(teamId));

        return new SessionJoinRequest
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Status = JoinRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };
    }

    public void Approve(Guid operatorId)
    {
        EnsurePending();
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));

        Status = JoinRequestStatus.Approved;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByOperatorId = operatorId;
    }

    public void Reject(Guid operatorId)
    {
        EnsurePending();
        if (operatorId == Guid.Empty)
            throw new ArgumentException("OperatorId no puede ser vacío.", nameof(operatorId));

        Status = JoinRequestStatus.Rejected;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByOperatorId = operatorId;
    }

    private void EnsurePending()
    {
        if (Status != JoinRequestStatus.Pending)
            throw new SessionDomainException("La solicitud de unión a la sesión ya fue procesada.");
    }
}
