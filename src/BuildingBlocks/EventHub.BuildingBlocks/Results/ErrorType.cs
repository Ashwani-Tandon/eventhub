// Lists the expected failure categories understood by the shared HTTP mapper.
// This keeps handlers independent of HTTP status codes.
namespace EventHub.BuildingBlocks.Results;

/// <summary>
/// Lists the expected failure categories understood by the shared HTTP mapper. This keeps handlers independent of HTTP status codes.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Unavailable,
    Unprocessable,
    // A service has no free capacity right now; callers may try again later.
    RateLimited
}
