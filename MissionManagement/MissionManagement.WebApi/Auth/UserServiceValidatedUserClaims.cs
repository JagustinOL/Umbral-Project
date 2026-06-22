using System.Security.Claims;

namespace MissionManagement.WebApi.Auth;

public static class UserServiceValidatedUserClaims
{
    public static IEnumerable<Claim> BuildRoleClaims(string role, IReadOnlyList<string> roles)
    {
        foreach (var normalized in CollectRoleNames(role, roles))
            yield return new Claim(ClaimTypes.Role, normalized);
    }

    internal static IEnumerable<string> CollectRoleNames(string role, IReadOnlyList<string> roles)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(role))
            normalized.Add(role.Trim().ToLowerInvariant());

        foreach (var entry in roles)
        {
            if (!string.IsNullOrWhiteSpace(entry))
                normalized.Add(entry.Trim().ToLowerInvariant());
        }

        return normalized;
    }
}
