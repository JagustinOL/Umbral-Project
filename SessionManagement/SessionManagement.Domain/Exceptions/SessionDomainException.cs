namespace SessionManagement.Domain.Exceptions;

/// <summary>
/// Excepción base para todas las violaciones de invariantes
/// del dominio SessionManagement. Permite al middleware de la
/// capa API distinguir errores de negocio (400) de errores
/// inesperados del sistema (500).
/// </summary>
public class SessionDomainException : Exception
{
    public SessionDomainException(string message)
        : base(message) { }

    public SessionDomainException(string message, Exception inner)
        : base(message, inner) { }
}