// Handles unexpected exceptions centrally and logs their full details.
// Clients receive a generic 500 ProblemDetails with a trace id, keeping internal stack traces private.
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EventHub.ServiceDefaults;

/// <summary>
/// Handles unexpected exceptions centrally and logs their full details. Clients receive a generic 500 ProblemDetails with a trace id, keeping internal stack traces private.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception?> LogUnhandledException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(5000, nameof(LogUnhandledException)),
            "Unhandled exception while processing {Method} {Path}");

    /// <summary>
    /// Logs an unexpected exception and returns a safe 500 response with a trace id, without exposing the stack to clients.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(
            logger,
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "The server could not complete the request."
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}
