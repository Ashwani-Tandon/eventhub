// Isolates payment behavior from the purchase handler.
// The demo implementation uses the caller and idempotency key to return a stable outcome and reference.
namespace Booking.Application.Ports;

/// <summary>Payment acknowledgement; a declined payment has no reference.</summary>
public sealed record PaymentResult(
    bool Succeeded,
    string? Reference);

/// <summary>Payment port that can later be implemented by another gateway without changing the use case.</summary>
public interface IPaymentGateway
{
    /// <summary>Charges the amount once per request identity; development can explicitly demonstrate a decline.</summary>
    Task<PaymentResult> PayAsync(
        Guid userId,
        string requestKey,
        decimal amount,
        bool simulateFailure,
        CancellationToken cancellationToken);
}
