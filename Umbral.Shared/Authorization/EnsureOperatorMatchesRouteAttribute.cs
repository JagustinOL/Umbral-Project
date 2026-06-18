using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Filters;
using Umbral.Shared.Auth;

namespace Umbral.Shared.Authorization;

/// <summary>
/// Valida que el usuario autenticado coincida con el operatorId de la ruta,
/// salvo que tenga rol admin.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class EnsureOperatorMatchesRouteAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            return;

        var currentUser = context.HttpContext.RequestServices.GetService(typeof(ICurrentUser)) as ICurrentUser;
        if (currentUser is null || !currentUser.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Usuario no autenticado." });
            return;
        }

        if (currentUser.IsInRole("admin"))
            return;

        if (!context.RouteData.Values.TryGetValue("operatorId", out var routeValue) ||
            !Guid.TryParse(routeValue?.ToString(), out var routeOperatorId))
            return;

        if (currentUser.UserId != routeOperatorId)
        {
            context.Result = new ObjectResult(new { error = "No autorizado para operar en nombre de otro operador." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
