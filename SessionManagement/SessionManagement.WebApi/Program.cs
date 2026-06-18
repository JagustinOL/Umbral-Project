using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Evidence.Processing;
using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Application.Evidence.Validation.Handlers;
using SessionManagement.Application.Facades;
using SessionManagement.Application.Hints;
using SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;
using SessionManagement.Domain.Repositories;
using SessionManagement.Infrastructure.Integrations;
using SessionManagement.Infrastructure.Messaging;
using SessionManagement.Infrastructure.Persistence;
using SessionManagement.Infrastructure.Repositories;
using Umbral.Shared;
using Umbral.Shared.Auth;
using Umbral.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddUmbralSerilog("SessionManagement");
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
builder.Services.AddUmbralCrossCutting(typeof(GetActiveSessionsQuery));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetActiveSessionsQuery).Assembly));

builder.Services.AddDbContext<SessionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SessionManagement")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontró cadena de conexión. Configure ConnectionStrings:SessionManagement o ConnectionStrings:DefaultConnection.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IDomainEventPublisher, RabbitMqDomainEventPublisher>();

builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

builder.Services.AddScoped<SessionActiveValidationHandler>();
builder.Services.AddScoped<TeamRegisteredValidationHandler>();
builder.Services.AddScoped<NodeAllowedValidationHandler>();
builder.Services.AddScoped<SequentialProgressValidationHandler>();
builder.Services.AddScoped<AnswerCorrectnessValidationHandler>();
builder.Services.AddScoped<EvidenceValidatorService>();
builder.Services.AddScoped<TriviaEvidenceSubmissionProcessor>();
builder.Services.AddScoped<TreasureHuntEvidenceSubmissionProcessor>();
builder.Services.AddScoped<ISessionOperationFacade, SessionOperationFacade>();
builder.Services.AddScoped<IPlayerHintPanelService, PlayerReleasedHintsProxy>();

var missionManagementBaseUrl = builder.Configuration["MissionManagement:BaseUrl"];
if (string.IsNullOrWhiteSpace(missionManagementBaseUrl))
    throw new InvalidOperationException("No se encontró MissionManagement:BaseUrl para configurar la integración.");

builder.Services.AddHttpClient<IMissionIntegrationService, HttpMissionIntegrationService>(client =>
{
    client.BaseAddress = new Uri(missionManagementBaseUrl, UriKind.Absolute);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SessionManagementDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw("SELECT 1 FROM teams LIMIT 1");
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
app.UseUmbralCrossCutting();
app.MapControllers();

app.Run();
