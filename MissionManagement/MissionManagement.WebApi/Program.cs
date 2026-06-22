using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Hints;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Domain.Repositories;
using MissionManagement.Infrastructure.External.SessionManagement;
using MissionManagement.Infrastructure.External.UserService;
using MissionManagement.Infrastructure.Messaging;
using MissionManagement.Infrastructure.Persistence;
using MissionManagement.Infrastructure.Repositories;
using MissionManagement.WebApi;
using MissionManagement.WebApi.Auth;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceSerilog("MissionManagement");
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

builder.Services.AddUserServiceAuthentication(builder.Configuration);
builder.Services.AddServiceCrossCutting(typeof(CreateMissionCommand));

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

var userServiceBaseUrl = builder.Configuration["UserService:BaseUrl"];
if (string.IsNullOrWhiteSpace(userServiceBaseUrl))
    throw new InvalidOperationException("No se encontró UserService:BaseUrl para validar operadores.");

builder.Services.AddHttpClient<IOperatorValidationService, HttpOperatorValidationService>(client =>
{
    client.BaseAddress = new Uri(userServiceBaseUrl, UriKind.Absolute);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MissionManagementDbContext>();
    try
    {
        dbContext.Database.ExecuteSqlRaw("SELECT 1 FROM missions LIMIT 1");
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
app.MapGet("/health", [AllowAnonymous] () => Results.Ok("MissionManagement Service is running"));

app.Run();
