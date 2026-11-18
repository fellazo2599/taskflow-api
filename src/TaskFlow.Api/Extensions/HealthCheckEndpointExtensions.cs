using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TaskFlow.Api.Constants;
using TaskFlow.Infrastructure.HealthChecks;

namespace TaskFlow.Api.Extensions;

public static class HealthCheckEndpointExtensions
{
    // live: the process answers (no dependencies). ready: dependencies such as the database are reachable.
    // Probes are frequent, so they are exempt from rate limiting.
    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpoints.Live, new HealthCheckOptions { Predicate = _ => false })
            .DisableRateLimiting();

        app.MapHealthChecks(HealthEndpoints.Ready, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
        })
            .DisableRateLimiting();

        return app;
    }
}
