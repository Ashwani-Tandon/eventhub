namespace EventHub.BuildingBlocks.Results;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } =
        new Dictionary<string, string[]>();

    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(code, message, ErrorType.Validation)
        {
            ValidationErrors = validationErrors ?? new Dictionary<string, string[]>()
        };

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Unavailable(string code, string message) =>
        new(code, message, ErrorType.Unavailable);

    public static Error Unprocessable(string code, string message) =>
        new(code, message, ErrorType.Unprocessable);
}
