// Carries the current user's bearer token from the chat request to Catalog and Booking.
// Downstream APIs authorize the real caller; the assistant receives no privileged service credential.
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
namespace Agent.Infrastructure;

public sealed class ForwardTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    /// <summary>Reads the current request at send time, so pooled HTTP handlers never cache one user's token.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var value)) request.Headers.Authorization = value;
        return base.SendAsync(request, cancellationToken);
    }
}
