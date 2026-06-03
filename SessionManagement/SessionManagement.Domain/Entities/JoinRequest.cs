using SessionManagement.Domain.Common;
using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Domain.Entities;

public sealed class JoinRequest : Entity
{
    public Guid PlayerRef { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public JoinRequestStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    private JoinRequest() { }

    public static JoinRequest Create(Guid playerRef, string displayName)
    {
        if (playerRef == Guid.Empty)
            throw new ArgumentException("PlayerRef no puede ser un Guid vacío.", nameof(playerRef));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("El nombre del jugador no puede estar vacío.", nameof(displayName));

        return new JoinRequest
        {
            Id = Guid.NewGuid(),
            PlayerRef = playerRef,
            DisplayName = displayName.Trim(),
            Status = JoinRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };
    }

    public void Approve()
    {
        EnsurePending();
        Status = JoinRequestStatus.Approved;
        ReviewedAtUtc = DateTime.UtcNow;
    }

    public void Reject()
    {
        EnsurePending();
        Status = JoinRequestStatus.Rejected;
        ReviewedAtUtc = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != JoinRequestStatus.Pending)
            throw new SessionDomainException("La solicitud ya fue procesada.");
    }
}
