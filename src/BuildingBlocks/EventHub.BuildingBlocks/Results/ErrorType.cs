namespace EventHub.BuildingBlocks.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Unavailable,
    Unprocessable
}
// Lists the expected failure categories understood by the shared HTTP mapper.
// This keeps handlers independent of HTTP status codes.
