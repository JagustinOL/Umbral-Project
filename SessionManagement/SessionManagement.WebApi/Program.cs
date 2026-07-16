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
using SessionManagement.WebApi;
using SessionManagement.WebApi.Auth;
using SessionManagement.WebApi.Hubs;
using SessionManagement.WebApi.Realtime;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceSerilog("SessionManagement");
var frontendCorsPolicy = "FrontendDevPolicy";

builder.Services.AddOpenApi();
builder.Services.AddServiceControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();

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

builder.Services.AddUserServiceAuthentication(builder.Configuration);
builder.Services.AddServiceCrossCutting(typeof(GetActiveSessionsQuery));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetActiveSessionsQuery).Assembly));

builder.Services.AddDbContext<SessionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SessionManagement")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontrÃ³ cadena de conexiÃ³n. Configure ConnectionStrings:SessionManagement o ConnectionStrings:DefaultConnection.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<IDomainEventPublisher, RabbitMqDomainEventPublisher>();
builder.Services.AddScoped<ILiveSessionRealtimeNotifier, SignalRLiveSessionRealtimeNotifier>();
builder.Services.AddHostedService<SessionScoreUpdateRabbitMqConsumer>();
builder.Services.AddSignalR();

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
    throw new InvalidOperationException("No se encontrÃ³ MissionManagement:BaseUrl para configurar la integraciÃ³n.");

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

    try
    {
        dbContext.Database.ExecuteSqlRaw(
            "ALTER TABLE evidence_submissions ADD COLUMN IF NOT EXISTS question_index integer NULL");
    }
    catch (PostgresException)
    {
        // La columna ya existe o la tabla aún no fue creada.
    }

    try
    {
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS session_join_requests (
                "Id" uuid NOT NULL PRIMARY KEY,
                "LiveSessionId" uuid NOT NULL REFERENCES live_sessions("Id") ON DELETE CASCADE,
                team_id uuid NOT NULL,
                status character varying(16) NOT NULL,
                requested_at_utc timestamp with time zone NOT NULL,
                resolved_at_utc timestamp with time zone NULL,
                resolved_by_operator_id uuid NULL
            );
            CREATE TABLE IF NOT EXISTS team_participations (
                "Id" uuid NOT NULL PRIMARY KEY,
                "LiveSessionId" uuid NOT NULL REFERENCES live_sessions("Id") ON DELETE CASCADE,
                team_id uuid NOT NULL,
                status character varying(16) NOT NULL,
                completed_at_utc timestamp with time zone NULL
            );
            """);
    }
    catch (PostgresException)
    {
        // Tablas ya existen o live_sessions aún no está disponible.
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(frontendCorsPolicy);
app.UseHttpsRedirection();
app.UseServiceCrossCutting();
app.MapControllers();
app.MapHub<LiveSessionHub>("/hubs/live-session");

app.Run();

