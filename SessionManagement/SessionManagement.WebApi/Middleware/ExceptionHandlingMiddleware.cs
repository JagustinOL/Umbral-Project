using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SessionManagement.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
            var (statusCode, message) = MapException(ex);
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = message,
                Instance = context.Request.Path
            });
        }
    }

    private static (int StatusCode, string Message) MapException(Exception ex) =>
        ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
            _ when EndsWith(ex, "UnauthorizedException") => (StatusCodes.Status401Unauthorized, ex.Message),
            _ when EndsWith(ex, "NotFoundException") => (StatusCodes.Status404NotFound, ex.Message),
            _ when EndsWith(ex, "ConflictException") => (StatusCodes.Status409Conflict, ex.Message),
            _ when EndsWith(ex, "ExternalDependencyException") => (StatusCodes.Status503ServiceUnavailable, ex.Message),
            _ when EndsWith(ex, "SessionDomainException") => (StatusCodes.Status400BadRequest, ex.Message),
            _ when EndsWith(ex, "ScoringDomainException") => (StatusCodes.Status400BadRequest, ex.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error interno en el servidor.")
        };

    private static bool EndsWith(Exception ex, string suffix) =>
        ex.GetType().Name.EndsWith(suffix, StringComparison.Ordinal);

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Solicitud inválida",
        StatusCodes.Status401Unauthorized => "No autorizado",
        StatusCodes.Status403Forbidden => "Acceso denegado",
        StatusCodes.Status404NotFound => "Recurso no encontrado",
        StatusCodes.Status409Conflict => "Conflicto",
        StatusCodes.Status503ServiceUnavailable => "Dependencia externa no disponible",
        _ => "Error interno"
    };
}
