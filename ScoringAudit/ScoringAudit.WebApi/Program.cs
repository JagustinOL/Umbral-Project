using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScoringAudit.Application.Events;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Infrastructure.Messaging;
using ScoringAudit.Infrastructure.Persistence;
using ScoringAudit.Infrastructure.Repositories;
using Umbral.Shared;
using Umbral.Shared.Auth;
using Umbral.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddUmbralSerilog("ScoringAudit");

builder.Services.AddOpenApi();
builder.Services.AddUmbralControllers();
builder.Services.AddUmbralAuthentication(builder.Configuration);
builder.Services.AddUmbralCrossCutting(typeof(ProcessEvidenceValidatedHandler));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ProcessEvidenceValidatedHandler).Assembly));

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<ScoringAuditRabbitMqConsumer>();

builder.Services.AddDbContext<ScoringAuditDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection es requerida.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ITeamLedgerRepository, TeamLedgerRepository>();
builder.Services.AddSingleton<RankingManagerService>();
builder.Services.AddSingleton<ScoreCalculatorService>();
builder.Services.AddSingleton<IScoreCalculationStrategy, TriviaScoreStrategy>();
builder.Services.AddSingleton<IScoreCalculationStrategy, TreasureHuntScoreStrategy>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScoringAuditDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseUmbralCrossCutting();
app.MapControllers();
app.MapGet("/health", [AllowAnonymous] () => Results.Ok("ScoringAudit Service is running"));

app.Run();
