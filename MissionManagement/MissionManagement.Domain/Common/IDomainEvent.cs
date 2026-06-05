namespace MissionManagement.Domain.Common;

/// <summary>
/// Marca un objeto como un Evento de Dominio.
/// Los eventos de dominio son hechos que ocurrieron en el pasado dentro del dominio
/// y que otros Bounded Contexts pueden escuchar de forma asíncrona (vía RabbitMQ).
/// </summary>
public interface IDomainEvent
{
    /// <summary>Identificador único del evento para trazabilidad.</summary>
    Guid EventId { get; }

    /// <summary>Momento exacto en que ocurrió el evento (siempre UTC).</summary>
    DateTime OccurredOnUtc { get; }
}