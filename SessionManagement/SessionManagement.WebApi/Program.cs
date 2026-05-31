using Microsoft.EntityFrameworkCore;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Domain.Repositories;
using SessionManagement.Infrastructure.Integrations;
using SessionManagement.Infrastructure.Persistence;
using SessionManagement.Infrastructure.Repositories;
using SessionManagement.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SessionManagement.Application.LiveSessions.Queries.GetActiveSessions.GetActiveSessionsQuery).Assembly));

builder.Services.AddScoped<ExceptionHandlingMiddleware>();

builder.Services.AddDbContext<SessionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SessionManagement");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
builder.Services.AddScoped<IMissionIntegrationService, FakeMissionIntegrationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

