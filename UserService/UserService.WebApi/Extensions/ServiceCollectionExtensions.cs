using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using UserService.WebApi.Middleware;
using UserService.WebApi.Validation;

namespace UserService.WebApi;

public static class ServiceCollectionExtensions
{
    public static IMvcBuilder AddServiceControllers(this IServiceCollection services) =>
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

    public static IServiceCollection AddServiceCrossCutting(
        this IServiceCollection services,
        Type applicationAssemblyMarker)
    {
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<CorrelationIdMiddleware>();
        services.AddValidatorsFromAssembly(applicationAssemblyMarker.Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    public static WebApplication UseServiceCrossCutting(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    public static WebApplicationBuilder AddServiceSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((_, _, config) => config
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}"));

        return builder;
    }
}
