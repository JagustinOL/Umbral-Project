using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(MissionManagement.Application.Missions.Commands.CreateMission.CreateMissionCommand).Assembly));

builder.Services.AddScoped<MissionManagement.WebApi.Middleware.ExceptionHandlingMiddleware>();

builder.Services.AddDbContext<MissionManagement.Infrastructure.Persistence.MissionManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MissionManagement");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<MissionManagement.Domain.Repositories.IMissionRepository, MissionManagement.Infrastructure.Repositories.MissionRepository>();
builder.Services.AddScoped<MissionManagement.Application.Common.Interfaces.IIdentityService, MissionManagement.Infrastructure.External.Fakes.FakeIdentityService>();
builder.Services.AddScoped<MissionManagement.Application.Common.Interfaces.ISessionValidationService, MissionManagement.Infrastructure.External.Fakes.FakeSessionValidationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<MissionManagement.WebApi.Middleware.ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
