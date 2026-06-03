namespace Common;

/// <summary>
/// Clase base para todos los Aggregates Root del dominio.
/// Un Aggregate Root es la única entidad a través de la cual el exterior
/// puede interactuar con el agregado. Es el guardián de las invariantes.
///
/// Los Domain Events se acumulan en una lista interna y son
/// despachados por la capa de Aplicación DESPUÉS de persistir los cambios,
/// garantizando consistencia entre la base de datos y el bus de mensajes.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Colección de eventos de dominio pendientes de despachar.
    /// Solo lectura desde el exterior; se modifica internamente.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    /// <summary>
    /// Registra un nuevo evento de dominio para ser despachado.
    /// Llamado únicamente desde los métodos de comportamiento del agregado.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>
    /// Limpia la lista de eventos tras haber sido procesados por la capa de Aplicación.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}