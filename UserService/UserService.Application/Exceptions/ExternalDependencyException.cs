namespace UserService.Application.Exceptions;

public sealed class ExternalDependencyException : Exception
{
    public ExternalDependencyException(string message)
        : base(message)
    {
    }

    public ExternalDependencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
