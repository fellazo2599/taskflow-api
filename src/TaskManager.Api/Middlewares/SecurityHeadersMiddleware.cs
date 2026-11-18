namespace TaskManager.Api.Middlewares;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ApiPathPrefix = "/api";
    private const string ReferrerPolicyHeader = "Referrer-Policy";

    public Task InvokeAsync(HttpContext context)
    {
        // Applied when the response starts: the exception handler clears headers set earlier.
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context);
            return Task.CompletedTask;
        });

        return next(context);
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers[ReferrerPolicyHeader] = "no-referrer";

        // JSON responses never load resources nor should be cached. Swagger UI is left out
        // because it needs scripts and styles.
        if (context.Request.Path.StartsWithSegments(ApiPathPrefix))
        {
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            headers.CacheControl = "no-store";
        }
    }
}
