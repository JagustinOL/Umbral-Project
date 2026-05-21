namespace AdminService.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
}