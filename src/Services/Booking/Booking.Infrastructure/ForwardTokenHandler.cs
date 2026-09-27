// Forwards the inbound caller's bearer token on Booking's outgoing Catalog requests.
// Catalog can then authorize and audit the real user instead of a privileged service identity.
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Booking.Infrastructure;

/// <summary>Copies request authentication without logging tokens or adding the Booking credential to public reads.</summary>
public sealed class ForwardTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    /// <summary>Attaches the incoming Authorization header if present, then invokes the HTTP transport.</summary>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var value))
        {
            request.Headers.Authorization = value;
        }

        // Now send the request onward with the user's login token attached.
        return base.SendAsync(request, cancellationToken);
    }
}
