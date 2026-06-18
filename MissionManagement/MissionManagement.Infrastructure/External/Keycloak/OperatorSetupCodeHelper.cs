using System.Security.Cryptography;
using System.Text;

namespace MissionManagement.Infrastructure.External.Keycloak;

internal static class OperatorSetupCodeHelper
{
    public const string SetupCodeHashAttribute = "umbral_setup_code_hash";
    public const string SetupCodeExpiresAttribute = "umbral_setup_code_expires_utc";

    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string GenerateSetupCode()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return $"{chars[..4]}-{chars[4..]}";
    }

    public static string HashSetupCode(string setupCode, string salt)
    {
        var normalized = NormalizeSetupCode(setupCode);
        var payload = Encoding.UTF8.GetBytes($"{normalized}:{salt}");
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash);
    }

    public static string NormalizeSetupCode(string setupCode) =>
        setupCode.Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

    public static bool CodesMatch(string providedCode, string storedHash, string salt)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        var computed = HashSetupCode(providedCode, salt);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHash));
    }

    public static DateTimeOffset ComputeExpiryUtc(int ttlDays) =>
        DateTimeOffset.UtcNow.AddDays(ttlDays);

    public static bool IsExpired(string? expiresUtcRaw)
    {
        if (string.IsNullOrWhiteSpace(expiresUtcRaw))
            return true;

        return !DateTimeOffset.TryParse(expiresUtcRaw, out var expiresUtc)
            || expiresUtc <= DateTimeOffset.UtcNow;
    }
}
