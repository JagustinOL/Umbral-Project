using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Umbral.Shared.Auth;

public static class UmbralAuthenticationExtensions
{
    public static IServiceCollection AddUmbralAuthentication(
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
                    ValidIssuer = options.Authority,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role"
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
                            // Ignorar parseo inválido; roles pueden venir en otros claims.
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
}
