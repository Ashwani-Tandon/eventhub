// Translates Booking's three Catalog ports into HTTP requests with bounded waiting.
// Expected HTTP and connection failures become Results so use cases can compensate or expose 503.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Results;
using EventHub.ServiceDefaults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Booking.Infrastructure;

/// <summary>Typed Catalog adapter; service discovery selects the endpoint and the token handler supplies user identity.</summary>
public sealed class CatalogHttpClient(
    HttpClient client,
    IOptions<BookingServiceOptions> options,
    ILogger<CatalogHttpClient> logger) : ICatalogClient
{
    private static readonly Action<ILogger, Exception?> LogCatalogFailure =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(5103, nameof(LogCatalogFailure)),
            "Catalog service unavailable during a Booking request");

    /// <summary>Reads public event fields and translates an unknown event to Booking's 404 error.</summary>
    public async Task<Result<CatalogEvent>> GetEventAsync(
        int eventId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/events/{eventId}");
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<CatalogEvent>.Failure(await ReadErrorAsync(response, cancellationToken));
            }

            var eventItem = await response.Content.ReadFromJsonAsync<CatalogEvent>(cancellationToken);
            return eventItem is null
                ? Result<CatalogEvent>.Failure(BookingErrors.CatalogUnavailable)
                : Result<CatalogEvent>.Success(eventItem);
        }
        catch (Exception exception) when (IsDependencyFailure(exception, cancellationToken))
        {
            LogUnavailable(exception);
            return Result<CatalogEvent>.Failure(BookingErrors.CatalogUnavailable);
        }
    }

    /// <summary>Posts the stable reservation identity and quantity with Booking's internal credential.</summary>
    public async Task<Result> ReserveAsync(
        int eventId,
        Guid reservationId,
        int quantity,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/internal/events/{eventId}/reservations");
        request.Content = JsonContent.Create(new ReservationInput(reservationId, quantity));
        // Catalog uses reservationId as its deduplication key, so a lost response can be retried safely.
        request.MarkAsIdempotent();
        return await SendInternalAsync(request, cancellationToken);
    }

    /// <summary>Requests a one-time release; Catalog recognizes both already-released and unknown reservations.</summary>
    public async Task<Result> ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/internal/reservations/{reservationId}/release");
        // Releasing an already released reservation is a no-op, so this POST is also safe to replay.
        request.MarkAsIdempotent();
        return await SendInternalAsync(request, cancellationToken);
    }

    /// <summary>Adds the service credential only to internal seat operations and handles bounded dependency failure.</summary>
    private async Task<Result> SendInternalAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Send the user's login token to identify who is buying, plus Booking's secret to prove
        // this seat change comes from Booking. A customer cannot skip payment and change seats directly.
        request.Headers.Add(BookingServiceOptions.HeaderName, options.Value.Key);
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await ReadErrorAsync(response, cancellationToken));
        }
        catch (Exception exception) when (IsDependencyFailure(exception, cancellationToken))
        {
            LogUnavailable(exception);
            return Result.Failure(BookingErrors.CatalogUnavailable);
        }
    }

    /// <summary>Maps Catalog's expected status codes while preserving its problem code and readable detail.</summary>
    private static async Task<Error> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if ((int)response.StatusCode >= 500 || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests)
        {
            // Catalog could not complete the request. Tell the user the service is unavailable (503).
            return BookingErrors.CatalogUnavailable;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BookingErrors.EventNotFound;
        }

        var problem = await response.Content.ReadFromJsonAsync<CatalogProblem>(cancellationToken);
        var code = problem?.Code ?? "Catalog.RequestRejected";
        var detail = problem?.Detail ?? "Catalog rejected the request.";
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => Error.Validation(code, detail, problem?.Errors),
            HttpStatusCode.Unauthorized => Error.Unauthorized(code, detail),
            HttpStatusCode.Forbidden => Error.Forbidden(code, detail),
            HttpStatusCode.Conflict => Error.Conflict(code, detail),
            _ => BookingErrors.CatalogUnavailable
        };
    }

    /// <summary>Distinguishes dependency timeout from the caller deliberately cancelling their request.</summary>
    private static bool IsDependencyFailure(
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A call taking too long and a user cancelling can look similar here.
        // Report Catalog as unavailable only for the timeout, not when the user cancelled.
        return exception is HttpRequestException or JsonException or BrokenCircuitException or TimeoutRejectedException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>Records the unavailable dependency without printing authentication or service credentials.</summary>
    private void LogUnavailable(Exception exception)
    {
        LogCatalogFailure(logger, exception);
    }

    // These transport shapes describe only fields needed from Catalog's HTTP contract.
    private sealed record ReservationInput(
        Guid ReservationId,
        int Quantity);
    private sealed record CatalogProblem(
        string? Code,
        string? Detail,
        Dictionary<string,
        string[]>? Errors);
}
