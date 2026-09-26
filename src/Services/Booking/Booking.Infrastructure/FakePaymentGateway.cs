// Simulates payment without external financial infrastructure or real charges.
// A reservation's hash determines its stable 90-percent-success outcome and payment reference.
using System.Security.Cryptography;
using Booking.Application.Ports;
using Microsoft.Extensions.Hosting;

namespace Booking.Infrastructure;

/// <summary>Deterministic fake payment behavior; repeating an identity produces the same outcome and reference.</summary>
public sealed class FakePaymentGateway(IHostEnvironment environment) : IPaymentGateway
{
    /// <summary>Declines roughly one in ten identities, or forces a decline for the Development demo switch.</summary>
    public Task<PaymentResult> PayAsync(
        Guid requestId,
        decimal amount,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = SHA256.HashData(requestId.ToByteArray());
        var declined = hash[0] % 10 == 0 || (environment.IsDevelopment() && simulateFailure);
        var reference = declined ? null : $"PAY-{Convert.ToHexString(hash.AsSpan(0, 4))}";
        return Task.FromResult(new PaymentResult(!declined, reference));
    }
}
