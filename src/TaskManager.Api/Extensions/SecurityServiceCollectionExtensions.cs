using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using TaskManager.Api.Errors;
using TaskManager.Api.Settings;
using TaskManager.Application.Common.Constants;

namespace TaskManager.Api.Extensions;

public static class SecurityServiceCollectionExtensions
{
    private const string UnknownClientPartition = "unknown";

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        services.AddCors(options => options.AddPolicy(CorsSettings.PolicyName, policy => policy
            .WithOrigins(corsSettings.AllowedOrigins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithHeaders(HeaderNames.ContentType)));

        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(PartitionByClientIp);
        });

        return services;
    }

    // Fixed window per client IP. Settings are read from IOptions so they can change per environment.
    private static RateLimitPartition<string> PartitionByClientIp(HttpContext httpContext)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClientPartition;

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = 0
        });
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse(ErrorCodes.TooManyRequests), cancellationToken);
    }
}
