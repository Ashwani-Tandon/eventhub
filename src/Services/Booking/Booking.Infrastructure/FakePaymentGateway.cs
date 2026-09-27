// Simulates payment without external financial infrastructure or real charges.
// A reservation's hash determines its stable 90-percent-success outcome and payment reference.
using System.Security.Cryptography;
using System.Text;
using Booking.Application.Ports;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure;

/// <summary>Deterministic fake payment behavior; repeating an identity produces the same outcome and reference.</summary>
public sealed class FakePaymentGateway(
    IHostEnvironment environment,
    ILogger<FakePaymentGateway> logger) : IPaymentGateway
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, PaymentResult> results = new();

    private static readonly Action<ILogger, Guid, string, decimal, string, Exception?> LogPayment =
        LoggerMessage.Define<Guid, string, decimal, string>(
            LogLevel.Information,
            new EventId(5103, nameof(LogPayment)),
            "Payment identity for user {UserId} and key {IdempotencyKey} processed amount {Amount} with reference {PaymentReference}");

    /// <summary>Declines roughly one in ten identities, or forces a decline for the Development demo switch.</summary>
    public Task<PaymentResult> PayAsync(
        Guid userId,
        string requestKey,
        decimal amount,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Alice and Bob may use the same purchase reference; their payments must still be separate.
        var identity = $"{userId:N}:{requestKey}";
        if (results.TryGetValue(identity, out var stored))
        {
            return Task.FromResult(stored);
        }

        // Calculate a repeatable payment result from the user and reference; repeats get the same normal result.
        // This only simulates payment; it does not charge money or contact a bank.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        var declined = hash[0] % 10 == 0 || (environment.IsDevelopment() && simulateFailure);
        var reference = declined ? null : $"PAY-{Convert.ToHexString(hash.AsSpan(0, 4))}";
        var candidate = new PaymentResult(!declined, reference);
        if (results.TryAdd(identity, candidate))
        {
            // If two calls reach here together, remember one result and record the payment only once.
            LogPayment(logger, userId, requestKey, amount, candidate.Reference ?? "DECLINED", null);
            return Task.FromResult(candidate);
        }

        return Task.FromResult(results[identity]);
    }
}
