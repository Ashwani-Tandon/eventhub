// Binds the secret that proves Booking may call Catalog's internal seat endpoints.
// AppHost supplies this value from user-secrets; normal public event reads never send it.
using System.ComponentModel.DataAnnotations;

namespace Booking.Infrastructure;

/// <summary>Validated configuration shared by the internal Catalog client and Catalog's credential guard.</summary>
public sealed class BookingServiceOptions
{
    public const string SectionName = "BookingService";
    public const string HeaderName = "X-EventHub-Service";

    [Required, MinLength(16)]
    public string Key { get; set; } = "";
}
