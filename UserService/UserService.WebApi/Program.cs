using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using UserService.Application.Admins.Commands.CreateAdmin;
using UserService.Application.Common.Interfaces;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Auth;
using UserService.Infrastructure.External.Keycloak;
using UserService.Infrastructure.External.SessionManagement;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Sync;
using UserService.WebApi.Hosting;
using UserService.WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceSerilog("UserService");
var frontendCorsPolicy = "FrontendDevPolicy";

builder.Services.AddOpenApi();
builder.Services.AddServiceControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;
                return uri.Scheme == "http"
                    && (uri.Host is "localhost" or "127.0.0.1");
            });
        }
        else
        {
            policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:5173",
                "http://localhost:19000",
                "http://localhost:19001");
        }
    });
});

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddServiceCrossCutting(typeof(CreateAdminCommand));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateAdminCommand).Assembly));

builder.Services.AddDbContext<UserServiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("UserService")
        ?? throw new InvalidOperationException("ConnectionStrings:UserService es requerida.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccessTokenReader, KeycloakAccessTokenReader>();

builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<KeycloakIdentityService>();
builder.Services.AddHttpClient<KeycloakPlayerIdentityService>();
builder.Services.AddHttpClient<KeycloakAuthService>();
builder.Services.AddHttpClient(nameof(KeycloakWebClientInitializer));
builder.Services.AddScoped<UserSyncIdentityService>();
builder.Services.AddScoped<UserSyncPlayerIdentityService>();
builder.Services.AddScoped<IIdentityService>(sp => sp.GetRequiredService<UserSyncIdentityService>());
builder.Services.AddScoped<IPlayerIdentityService>(sp => sp.GetRequiredService<UserSyncPlayerIdentityService>());
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<KeycloakAuthService>());
builder.Services.AddScoped<DefaultAdminDirectorySync>();
builder.Services.AddHostedService<KeycloakBootstrapHostedService>();
builder.Services.AddHostedService<DefaultAdminDirectoryBootstrapHostedService>();

var sessionManagementBaseUrl = builder.Configuration["SessionManagement:BaseUrl"];
if (string.IsNullOrWhiteSpace(sessionManagementBaseUrl))
    throw new InvalidOperationException("No se encontró SessionManagement:BaseUrl para configurar la validación de sesiones.");

builder.Services.AddHttpClient<ISessionValidationService, HttpSessionValidationService>(client =>
{
    client.BaseAddress = new Uri(sessionManagementBaseUrl, UriKind.Absolute);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw("SELECT 1 FROM users LIMIT 1");
    }
    catch (PostgresException ex) when (ex.SqlState == "42P01")
    {
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
        databaseCreator.CreateTables();
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(frontendCorsPolicy);
app.UseHttpsRedirection();
app.UseServiceCrossCutting();
app.MapControllers();
app.MapGet("/health", [AllowAnonymous] () => Results.Ok("UserService is running"));

app.Run();
