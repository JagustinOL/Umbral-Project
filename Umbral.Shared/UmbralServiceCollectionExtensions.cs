using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Umbral.Shared.Middleware;
using Umbral.Shared.Validation;

namespace Umbral.Shared;

public static class UmbralServiceCollectionExtensions
{
    public static IMvcBuilder AddUmbralControllers(this IServiceCollection services) =>
        services.AddControllers();

    public static IServiceCollection AddUmbralCrossCutting(
        this IServiceCollection services,
        Type applicationAssemblyMarker)
    {
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<CorrelationIdMiddleware>();
        services.AddValidatorsFromAssembly(applicationAssemblyMarker.Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    public static WebApplication UseUmbralCrossCutting(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    public static WebApplicationBuilder AddUmbralSerilog(this WebApplicationBuilder builder, string serviceName)
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
