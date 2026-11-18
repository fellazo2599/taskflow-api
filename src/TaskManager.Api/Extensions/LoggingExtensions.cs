using Serilog;
using Serilog.Formatting.Compact;

namespace TaskManager.Api.Extensions;

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
}
