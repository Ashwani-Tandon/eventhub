// Describes an expected failure using a stable code, readable message, and category.
// Validation can also carry field errors; HTTP translation belongs in ServiceDefaults.
namespace EventHub.BuildingBlocks.Results;

/// <summary>
/// Describes an expected failure using a stable code, readable message, and category. Validation can also carry field errors; HTTP translation belongs in ServiceDefaults.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } =
        new Dictionary<string, string[]>();

    /// <summary>
    /// Creates a validation failure with optional per-field messages for the client's form.
    /// </summary>
    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        return new(code, message, ErrorType.Validation)
        {
            ValidationErrors = validationErrors ?? new Dictionary<string, string[]>()
        };
    }

    /// <summary>
    /// Creates an expected missing-resource failure that maps to HTTP 404.
    /// </summary>
    public static Error NotFound(
        string code,
        string message)
    {
        return new(code, message, ErrorType.NotFound);
    }

    /// <summary>
    /// Creates an expected state-conflict failure that maps to HTTP 409.
    /// </summary>
    public static Error Conflict(
        string code,
        string message)
    {
        return new(code, message, ErrorType.Conflict);
    }

    /// <summary>
    /// Creates an authorization failure for a known caller that maps to HTTP 403.
    /// </summary>
    public static Error Forbidden(
        string code,
        string message)
    {
        return new(code, message, ErrorType.Forbidden);
    }

    /// <summary>
    /// Creates an authentication failure that maps to HTTP 401.
    /// </summary>
    public static Error Unauthorized(
        string code,
        string message)
    {
        return new(code, message, ErrorType.Unauthorized);
    }

    /// <summary>
    /// Creates a dependency-unavailable failure that maps to HTTP 503.
    /// </summary>
    public static Error Unavailable(
        string code,
        string message)
    {
        return new(code, message, ErrorType.Unavailable);
    }

    /// <summary>
    /// Creates an operation-rejected failure that maps to HTTP 422.
    /// </summary>
    public static Error Unprocessable(
        string code,
        string message)
    {
        return new(code, message, ErrorType.Unprocessable);
    }
}
