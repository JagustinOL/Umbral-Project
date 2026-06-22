namespace UserService.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; init; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El Id de una entidad no puede ser un Guid vacío.", nameof(id));
        Id = id;
    }

    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity? left, Entity? right) =>
        !(left == right);
}
