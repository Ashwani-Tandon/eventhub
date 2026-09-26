using EventHub.BuildingBlocks.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace EventHub.BuildingBlocks.Messaging;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    private static readonly Action<ILogger, string, Exception?> LogPipelineEntered =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1100, nameof(LogPipelineEntered)),
            "Mediator pipeline: Validation entered for {RequestName}");

    private static readonly Action<ILogger, string, int, Exception?> LogValidationFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1101, nameof(LogValidationFailed)),
            "Mediator validation failed for {RequestName} with {ErrorCount} field errors");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerContinuation<TResponse> continuation,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        LogPipelineEntered(logger, requestName, null);

        if (!validators.Any())
        {
            return await continuation(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(static result => result.Errors)
            .Where(static failure => failure is not null)
            .GroupBy(static failure => failure.PropertyName)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .Select(static failure => failure.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());

        if (errors.Count == 0)
        {
            return await continuation(cancellationToken);
        }

        LogValidationFailed(
            logger,
            requestName,
            errors.Count,
            null);

        // Do not call continuation: the remaining behaviors and handler are skipped.
        return TResponse.Failure(
            Error.Validation(
                "Validation.Failed",
                "One or more validation errors occurred.",
                errors));
    }
}
// Runs FluentValidation validators and groups failures by field.
// Invalid input returns a failed Result immediately, preventing the handler from executing.
