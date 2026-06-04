using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MissionManagement.Infrastructure.External.Keycloak;

internal static class KeycloakJwtReader
{
    public static JwtPayload ReadPayload(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            throw new InvalidOperationException("Keycloak devolvió un token JWT con formato inválido.");

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var payload = JsonSerializer.Deserialize<JwtPayload>(payloadJson)
            ?? throw new InvalidOperationException("Keycloak devolvió un payload JWT inválido.");

        if (string.IsNullOrWhiteSpace(payload.Sub) || !Guid.TryParse(payload.Sub, out _))
            throw new InvalidOperationException("Keycloak devolvió un identificador de usuario no válido.");

        return payload;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var normalized = input.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
        }

        return Convert.FromBase64String(normalized);
    }

    internal sealed class JwtPayload
    {
        [JsonPropertyName("sub")]
        public string Sub { get; init; } = string.Empty;

        [JsonPropertyName("realm_access")]
        public RealmAccess? RealmAccess { get; init; }
    }

    internal sealed class RealmAccess
    {
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; init; }
    }
}
