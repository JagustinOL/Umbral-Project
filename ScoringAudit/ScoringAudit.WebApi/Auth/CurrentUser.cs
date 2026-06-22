using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ScoringAudit.WebApi.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue("sub")
                ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username =>
        Principal?.FindFirstValue("preferred_username")
        ?? Principal?.Identity?.Name;

    public IReadOnlyList<string> Roles
    {
        get
        {
            var realmRoles = Principal?
                .FindAll("realm_access")
                .SelectMany(_ => Array.Empty<string>())
                .ToList() ?? [];

            var roleClaims = Principal?
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat(Principal.FindAll("role").Select(c => c.Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            return roleClaims;
        }
    }

    public bool IsInRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
