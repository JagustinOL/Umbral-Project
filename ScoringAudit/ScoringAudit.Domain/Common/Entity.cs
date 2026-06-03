namespace Common;

/// <summary>
/// Clase base para todas las Entidades del dominio.
/// Una Entidad tiene identidad propia (Id) que la distingue de otras,
/// incluso si sus atributos son idénticos.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; init; }

    /// <summary>
    /// Constructor protegido. Las entidades solo se crean desde
    /// sus propios métodos de fábrica o constructores concretos.
    /// </summary>
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El Id de una entidad no puede ser un Guid vacío.", nameof(id));

        Id = id;
    }

    /// <summary>
    /// Constructor sin parámetros requerido por algunos ORMs (EF Core).
    /// No debe usarse directamente en el código de dominio.
    /// </summary>
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