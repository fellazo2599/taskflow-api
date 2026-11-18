using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using TaskManager.Api.Errors.Adapters;
using TaskManager.Application.Common.Constants;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.Errors;

// Single place where exceptions are translated into HTTP status codes.
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, error) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, validation.ToErrorResponse()),
            NotFoundException notFound => (StatusCodes.Status404NotFound, new ErrorResponse(notFound.Code)),
            // Raised by Kestrel while reading the request, e.g. a body over the size limit (413).
            BadHttpRequestException badRequest => (badRequest.StatusCode, badRequest.ToErrorResponse()),
            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse(ErrorCodes.ServerError))
        };

        // Expected business errors are not logged; only unexpected failures are.
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
