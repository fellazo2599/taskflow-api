using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace TaskFlow.Api.Middlewares;

// Tags every log line of a request with a correlation id and returns it to the client,
// so an error reported by a caller can be found in the logs.
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    private const string LogPropertyName = "CorrelationId";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    // Reuses a well-formed id sent by the caller (never raw input into the logs); otherwise the trace id.
    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        return incoming.Length is > 0 and <= MaxLength && SafeId().IsMatch(incoming)
            ? incoming
            : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeId();
}
