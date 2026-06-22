using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace UserService.Infrastructure.Auth;

public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddOptions<KeycloakAuthOptions>()
            .Bind(configuration.GetSection(KeycloakAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(KeycloakAuthOptions.SectionName).Get<KeycloakAuthOptions>()
            ?? new KeycloakAuthOptions();

        var validIssuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { options.Authority };
        if (!string.IsNullOrWhiteSpace(options.PublicAuthority))
            validIssuers.Add(options.PublicAuthority);

        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.WebClientId;
                jwt.RequireHttpsMetadata = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuers = validIssuers,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role",
                    IssuerValidator = (issuer, _, _) =>
                        ValidateIssuer(issuer, options.Realm, validIssuers, isDevelopment)
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                            return Task.CompletedTask;

                        var realmAccess = context.Principal.FindFirst("realm_access")?.Value;
                        if (string.IsNullOrWhiteSpace(realmAccess))
                            return Task.CompletedTask;

                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(realmAccess);
                            if (doc.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var value = role.GetString();
                                    if (!string.IsNullOrWhiteSpace(value))
                                        identity.AddClaim(new System.Security.Claims.Claim("role", value));
                                }
                            }
                        }
                        catch
                        {
                            // Ignorar parseo invÃ¡lido; roles pueden venir en otros claims.
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

    private static string ValidateIssuer(
        string issuer,
        string realm,
        IReadOnlySet<string> validIssuers,
        bool allowDevelopmentLanIssuers)
    {
        if (validIssuers.Contains(issuer))
            return issuer;

        if (allowDevelopmentLanIssuers && IsAllowedDevelopmentIssuer(issuer, realm))
            return issuer;

        throw new SecurityTokenInvalidIssuerException(
            $"Issuer validation failed. Issuer: '{issuer}'. Valid issuers: {string.Join(", ", validIssuers)}.");
    }

    private static bool IsAllowedDevelopmentIssuer(string issuer, string realm)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedPath = $"/realms/{realm}";
        if (!uri.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (uri.Host is "localhost" or "127.0.0.1")
            return true;

        if (!IPAddress.TryParse(uri.Host, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var bytes = ip.GetAddressBytes();
        if (bytes[0] == 10)
            return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;

        return bytes[0] == 192 && bytes[1] == 168;
    }
}
