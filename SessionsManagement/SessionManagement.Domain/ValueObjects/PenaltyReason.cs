namespace SessionManagement.Domain.ValueObjects;

public record PenaltyReason
{
    public string Description { get; init; }

    public PenaltyReason(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("RN-10: Toda penalización debe ir acompañada obligatoriamente de un motivo.");
            
        Description = description;
    }
}