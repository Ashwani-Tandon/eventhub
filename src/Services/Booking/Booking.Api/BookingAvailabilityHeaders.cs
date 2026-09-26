// Adds Retry-After to Booking's temporary-unavailability responses before HTTP headers are sent.
// The shared Result mapper still owns ProblemDetails; this middleware supplies the retry hint required by the spec.
namespace Booking.Api;

/// <summary>Response metadata for callers that receive an unavailable dependency or database response.</summary>
public static class BookingAvailabilityHeaders
{
    private const string RetryAfterSeconds = "5";

    /// <summary>Sets the retry hint only for HTTP 503, without altering successful or validation responses.</summary>
    public static void UseBookingAvailabilityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (context.Response.StatusCode == StatusCodes.Status503ServiceUnavailable)
                {
                    context.Response.Headers.RetryAfter = RetryAfterSeconds;
                }

                return Task.CompletedTask;
            });

            await next(context);
        });
    }
}
