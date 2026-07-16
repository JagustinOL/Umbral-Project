using UserService.Application.Common.Interfaces;
using UserService.Infrastructure.External.Keycloak;

namespace UserService.Infrastructure.Auth;

public sealed class KeycloakAccessTokenReader : IAccessTokenReader
{
    public (Guid UserId, IReadOnlyList<string> Roles) Read(string accessToken)
    {
        var payload = KeycloakJwtReader.ReadPayload(accessToken);
        var roles = payload.RealmAccess?.Roles?
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        return (Guid.Parse(payload.Sub), roles);
    }
}
