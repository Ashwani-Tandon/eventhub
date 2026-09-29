// Gives callers a short retry hint when the assistant is offline or its queue is full.
// It adds response metadata only; the shared mapper still decides status and ProblemDetails.
namespace Agent.Api;

public static class AgentAvailabilityHeaders
{
    private const string RetryAfterSeconds = "5";
    /// <summary>Suggests waiting five seconds before manually trying an unavailable or busy assistant again.</summary>
    public static void UseAgentAvailabilityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (context.Response.StatusCode is StatusCodes.Status429TooManyRequests or StatusCodes.Status503ServiceUnavailable)
                    context.Response.Headers.RetryAfter = RetryAfterSeconds;
                return Task.CompletedTask;
            });
            await next(context);
        });
    }
}
