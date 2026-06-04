using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Domain.Repositories;
using SessionManagement.Infrastructure.Integrations;
using SessionManagement.Infrastructure.Persistence;
using SessionManagement.Infrastructure.Repositories;
using SessionManagement.WebApi.Middleware;

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
                "http://localhost:5173",
                "http://localhost:19000",
                "http://localhost:19001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SessionManagement.Application.LiveSessions.Queries.GetActiveSessions.GetActiveSessionsQuery).Assembly));

builder.Services.AddScoped<ExceptionHandlingMiddleware>();

builder.Services.AddDbContext<SessionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SessionManagement")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontró cadena de conexión. Configure ConnectionStrings:SessionManagement o ConnectionStrings:DefaultConnection.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

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
        // Shared database: create SessionManagement tables only when missing.
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
        databaseCreator.CreateTables();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(frontendCorsPolicy);
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

