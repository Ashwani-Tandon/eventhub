// Validates the Booking-only service credential on Catalog's internal route group.
// SHA-256 normalizes both values before a fixed-time comparison so secret bytes are not compared early.
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Catalog.Api;

public sealed class BookingServiceOptions
{
    public const string SectionName = "BookingService";
    public const string HeaderName = "X-EventHub-Service";
    [Required, MinLength(16)] public string Key { get; set; } = "";
}

public sealed class BookingCredentialFilter(IOptions<BookingServiceOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var supplied = context.HttpContext.Request.Headers[BookingServiceOptions.HeaderName].ToString();
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Key));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash)
            ? await next(context)
            : Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                detail: "A valid Booking service credential is required.");
    }
}

public static class CatalogApiRegistration
{
    public static IServiceCollection AddCatalogApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BookingServiceOptions>().Bind(configuration.GetSection(BookingServiceOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<BookingCredentialFilter>();
        return services;
    }
}
