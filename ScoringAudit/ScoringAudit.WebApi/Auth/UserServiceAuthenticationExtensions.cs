using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ScoringAudit.WebApi.Auth;

public static class UserServiceAuthenticationExtensions
{
    public const string SchemeName = "UserService";

    public static IServiceCollection AddUserServiceAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddOptions<UserServiceAuthOptions>()
            .Bind(configuration.GetSection(UserServiceAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration.GetSection(UserServiceAuthOptions.SectionName).Get<UserServiceAuthOptions>()
            ?? new UserServiceAuthOptions();

        services.AddHttpClient(nameof(UserServiceAuthenticationHandler), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        });

        services.AddAuthentication(SchemeName)
            .AddScheme<AuthenticationSchemeOptions, UserServiceAuthenticationHandler>(SchemeName, _ => { });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(SchemeName)
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
