using Microsoft.EntityFrameworkCore;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Domain.Repositories;
using MissionManagement.Infrastructure.External.Keycloak;
using MissionManagement.Infrastructure.External.SessionManagement;
using MissionManagement.Infrastructure.Messaging;
using MissionManagement.Infrastructure.Persistence;
using MissionManagement.Infrastructure.Repositories;
using MissionManagement.WebApi.Hosting;

var builder = WebApplication.CreateBuilder(args);
var frontendCorsPolicy = "FrontendDevPolicy";

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("UmbralDev", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:19000",
                "http://localhost:19001",
                "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateMissionCommand).Assembly));

builder.Services.AddScoped<MissionManagement.WebApi.Middleware.ExceptionHandlingMiddleware>();

builder.Services.AddDbContext<MissionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MissionManagement");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IMissionRepository, MissionRepository>();
builder.Services.AddScoped<IDomainEventPublisher, LoggingDomainEventPublisher>();

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
{
    app.MapOpenApi();
}

app.UseCors("UmbralDev");
app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.UseMiddleware<MissionManagement.WebApi.Middleware.ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
