using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using TaskFlow.Api.Constants;

namespace TaskFlow.Api.Extensions;

public static class LoggingExtensions
{
    private const string DevelopmentOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

    // Levels and properties come from the "Serilog" section; the sink format depends on the environment:
    // readable text locally, one JSON object per line elsewhere so log aggregators can index the fields.
    public static IServiceCollection AddApiLogging(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        => services.AddSerilog((serviceProvider, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext();

            if (environment.IsDevelopment())
                logger.WriteTo.Console(outputTemplate: DevelopmentOutputTemplate);
            else
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        });

    // One line per request. Successful health probes are dropped to Verbose so they don't flood the logs.
    public static WebApplication UseApiRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel);
        return app;
    }

    private static LogEventLevel RequestLogLevel(HttpContext httpContext, double elapsedMilliseconds, Exception? exception)
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        return httpContext.Request.Path.StartsWithSegments(HealthEndpoints.BasePath)
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    }
}
