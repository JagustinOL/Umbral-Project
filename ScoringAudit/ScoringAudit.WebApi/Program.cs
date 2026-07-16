using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScoringAudit.Application.Events;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Infrastructure.Messaging;
using ScoringAudit.Infrastructure.Persistence;
using ScoringAudit.Infrastructure.Repositories;
using ScoringAudit.WebApi;
using ScoringAudit.WebApi.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceSerilog("ScoringAudit");

builder.Services.AddOpenApi();
builder.Services.AddServiceControllers();
builder.Services.AddUserServiceAuthentication(builder.Configuration);
builder.Services.AddServiceCrossCutting(typeof(ProcessEvidenceValidatedHandler));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ProcessEvidenceValidatedHandler).Assembly));

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<ITeamScoreUpdatePublisher, RabbitMqTeamScoreUpdatePublisher>();
builder.Services.AddHostedService<ScoringAuditRabbitMqConsumer>();

builder.Services.AddDbContext<ScoringAuditDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection es requerida.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ITeamLedgerRepository, TeamLedgerRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddSingleton<RankingManagerService>();
builder.Services.AddSingleton<ScoreCalculatorService>();
builder.Services.AddSingleton<IScoreCalculationStrategy, TriviaScoreStrategy>();
builder.Services.AddSingleton<IScoreCalculationStrategy, TreasureHuntScoreStrategy>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScoringAuditDbContext>();
    // Shared DB: EnsureCreated() is a no-op when other schemas already exist.
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS team_ledgers (
            "Id" uuid PRIMARY KEY,
            team_ref uuid NOT NULL,
            session_ref uuid NOT NULL,
            team_name character varying(200) NOT NULL,
            is_closed boolean NOT NULL,
            created_at_utc timestamp with time zone NOT NULL,
            completion_elapsed_seconds double precision NULL
        );

        CREATE TABLE IF NOT EXISTS score_entries (
            "Id" uuid PRIMARY KEY,
            points integer NOT NULL,
            recorded_at_utc timestamp with time zone NOT NULL,
            entry_type text NOT NULL,
            source_event_id uuid NOT NULL,
            team_ledger_id uuid NULL REFERENCES team_ledgers("Id") ON DELETE CASCADE,
            origin jsonb NULL,
            penalty_reason jsonb NULL
        );

        ALTER TABLE team_ledgers
            ADD COLUMN IF NOT EXISTS completion_elapsed_seconds double precision NULL;

        CREATE TABLE IF NOT EXISTS audit_logs (
            "Id" uuid PRIMARY KEY,
            session_ref uuid NOT NULL UNIQUE,
            mission_ref uuid NOT NULL,
            operator_ref uuid NOT NULL,
            started_at_utc timestamp with time zone NOT NULL,
            ended_at_utc timestamp with time zone NULL,
            status character varying(20) NOT NULL,
            is_closed boolean NOT NULL,
            created_at_utc timestamp with time zone NOT NULL
        );

        CREATE TABLE IF NOT EXISTS session_events (
            "Id" uuid PRIMARY KEY,
            session_id uuid NOT NULL,
            event_type text NOT NULL,
            source_event_id uuid NOT NULL,
            team_ref uuid NULL,
            mission_node_ref uuid NULL,
            description character varying(1000) NOT NULL,
            metadata jsonb NULL,
            occurred_at_utc timestamp with time zone NOT NULL,
            audit_log_id uuid NULL REFERENCES audit_logs("Id") ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ix_session_events_session_source
            ON session_events (session_id, source_event_id);
        """);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseServiceCrossCutting();
app.MapControllers();
app.MapGet("/health", [AllowAnonymous] () => Results.Ok("ScoringAudit Service is running"));

app.Run();

