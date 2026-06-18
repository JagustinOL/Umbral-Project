using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Hints;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Domain.Repositories;
using MissionManagement.Infrastructure.External.Keycloak;
using MissionManagement.Infrastructure.External.SessionManagement;
using MissionManagement.Infrastructure.Messaging;
using MissionManagement.Infrastructure.Persistence;
using MissionManagement.Infrastructure.Repositories;
using MissionManagement.WebApi.Hosting;
using Umbral.Shared;
using Umbral.Shared.Auth;
using Umbral.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddUmbralSerilog("MissionManagement");
var frontendCorsPolicy = "FrontendDevPolicy";

builder.Services.AddOpenApi();
builder.Services.AddUmbralControllers();
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

builder.Services.AddUmbralAuthentication(builder.Configuration);
builder.Services.AddUmbralCrossCutting(typeof(CreateMissionCommand));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateMissionCommand).Assembly));

builder.Services.AddDbContext<MissionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MissionManagement");
    options.UseNpgsql(connectionString);
});

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IDomainEventPublisher, RabbitMqDomainEventPublisher>();

builder.Services.AddScoped<IMissionRepository, MissionRepository>();
builder.Services.AddScoped<MissionHintService>();
builder.Services.AddScoped<IHintAccessService>(sp => new DraftOnlyHintProxy(
    sp.GetRequiredService<MissionHintService>(),
    sp.GetRequiredService<IMissionRepository>(),
    sp.GetRequiredService<ICurrentUser>()));

var sessionManagementBaseUrl = builder.Configuration["SessionManagement:BaseUrl"];
if (string.IsNullOrWhiteSpace(sessionManagementBaseUrl))
    throw new InvalidOperationException("No se encontró SessionManagement:BaseUrl para configurar la validación de sesiones.");

builder.Services.AddHttpClient<ISessionValidationService, HttpSessionValidationService>(client =>
{
    client.BaseAddress = new Uri(sessionManagementBaseUrl, UriKind.Absolute);
});

builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IIdentityService, KeycloakIdentityService>();
builder.Services.AddHttpClient<IPlayerIdentityService, KeycloakPlayerIdentityService>();
builder.Services.AddHttpClient<IAuthService, KeycloakAuthService>();
builder.Services.AddHttpClient(nameof(KeycloakWebClientInitializer));
builder.Services.AddHostedService<KeycloakBootstrapHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MissionManagementDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(frontendCorsPolicy);
app.UseHttpsRedirection();
app.UseUmbralCrossCutting();
app.MapControllers();

app.Run();
