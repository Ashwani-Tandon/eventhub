// Composes Booking's purchase and cancellation workflows with its SQL and Catalog adapters.
// Development startup applies migrations before importing the shared demo purchase dataset.
using Booking.Api;
using Booking.Application.Features.CreateBooking;
using Booking.Infrastructure;
using EventHub.BuildingBlocks.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEventHubAuth();
builder.AddBookingInfrastructure();
builder.Services.AddMediator(typeof(CreateBookingCommand).Assembly);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseBookingAvailabilityHeaders();
app.MapBookingEndpoints();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeBookingAsync(app.Lifetime.ApplicationStopping);
}

app.MapDefaultEndpoints();

app.Run();
