namespace ScoringAudit.Domain.Exceptions;

/// <summary>
/// Excepción base para todas las violaciones de invariantes
/// del dominio ScoringAudit. Permite al middleware de la API
/// devolver un 400 en lugar de un 500 ante errores de negocio.
/// </summary>
public sealed class ScoringDomainException : Exception
{
    public ScoringDomainException(string message)
        : base(message) { }

    public ScoringDomainException(string message, Exception inner)
        : base(message, inner) { }
}