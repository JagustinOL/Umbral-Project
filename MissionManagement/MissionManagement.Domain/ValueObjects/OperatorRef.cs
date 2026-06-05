namespace MissionManagement.Domain.ValueObjects;

public sealed record OperatorRef
{
    public Guid OperatorId { get; init; }

    public OperatorRef(Guid operatorId)
    {
        if (operatorId == Guid.Empty)
            throw new ArgumentException("El operatorId no puede ser vacio.", nameof(operatorId));

        OperatorId = operatorId;
    }
}

