// Validates the Booking-only service credential on Catalog's internal route group.
// SHA-256 normalizes both values before a fixed-time comparison so secret bytes are not compared early.
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Catalog.Api;

/// <summary>
/// Validates the Booking-only service credential on Catalog's internal route group. SHA-256 normalizes both values before a fixed-time comparison so secret bytes are not compared early.
/// </summary>
public sealed class BookingServiceOptions
{
    public const string SectionName = "BookingService";

    public const string HeaderName = "X-EventHub-Service";
    [Required, MinLength(16)] public string Key { get; set; } = "";
}

/// <summary>
/// Blocks direct seat changes unless the caller supplies Booking's shared secret; JWT authentication separately identifies the reservation owner.
/// </summary>
public sealed class BookingCredentialFilter(IOptions<BookingServiceOptions> options) : IEndpointFilter
{
    /// <summary>
    /// Checks Booking's credential before allowing an internal endpoint to run; rejects a mismatch with 403.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var supplied = context.HttpContext.Request.Headers[BookingServiceOptions.HeaderName].ToString();
        // Check whether the caller knows Booking's secret. Turn both secrets into equal-size fingerprints,
        // then compare without revealing which part was wrong through how long the check takes.
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Key));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash)
            ? await next(context)
            : Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                detail: "A valid Booking service credential is required.");
    }
}

/// <summary>
/// Validates the Booking-only service credential on Catalog's internal route group. SHA-256 normalizes both values before a fixed-time comparison so secret bytes are not compared early.
/// </summary>
public static class CatalogApiRegistration
{
    /// <summary>
    /// Registers and validates the internal credential settings and the endpoint filter.
    /// </summary>
    public static IServiceCollection AddCatalogApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BookingServiceOptions>().Bind(configuration.GetSection(BookingServiceOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<BookingCredentialFilter>();
        return services;
    }
}
