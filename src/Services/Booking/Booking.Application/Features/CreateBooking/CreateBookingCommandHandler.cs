// Coordinates a purchase in the required order: event, reservation, payment, then local save.
// A failed payment or local save releases the reservation to compensate for the completed seat hold.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Features.CreateBooking;

/// <summary>Coordinates the write workflow while keeping HTTP, payment mechanics, and EF Core behind ports.</summary>
public sealed class CreateBookingCommandHandler(
    ICatalogClient catalog,
    IPaymentGateway payments,
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CreateBookingCommandHandler> logger) : ICommandHandler<CreateBookingCommand, BookingDto>
{
    private static readonly Action<ILogger, Guid, string, Exception?> LogCompensationFailure =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(5101, nameof(LogCompensationFailure)),
            "Compensation for reservation {ReservationId} failed with {ErrorCode}");

    /// <summary>Confirms a purchase only after Catalog holds its seats and payment succeeds.</summary>
    public async Task<Result<BookingDto>> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<BookingDto>.Failure(BookingErrors.Unauthenticated);
        }

        var eventResult = await catalog.GetEventAsync(request.EventId, cancellationToken);
        if (eventResult.IsFailure)
        {
            return Result<BookingDto>.Failure(eventResult.Error!);
        }

        var eventItem = eventResult.Value;
        if (eventItem.StartsAt <= clock.GetUtcNow())
        {
            return Result<BookingDto>.Failure(BookingErrors.EventStarted);
        }

        var reservationId = Guid.NewGuid();
        var reservation = await catalog.ReserveAsync(
            eventItem.Id, reservationId, request.Quantity, cancellationToken);
        if (reservation.IsFailure)
        {
            return Result<BookingDto>.Failure(reservation.Error!);
        }

        try
        {
            var payment = await payments.PayAsync(
                reservationId, request.Quantity * eventItem.Price,
                request.SimulatePaymentFailure, cancellationToken);
            if (!payment.Succeeded)
            {
                var release = await CompensateAsync(reservationId);
                return Result<BookingDto>.Failure(
                    release.IsSuccess ? BookingErrors.PaymentFailed : release.Error!);
            }

            var creation = Domain.Booking.Create(
                userId, eventItem.Id, eventItem.Title, eventItem.StartsAt, eventItem.OrganizerId,
                request.Quantity, eventItem.Price, payment.Reference!, reservationId, clock.GetUtcNow());
            if (creation.IsFailure)
            {
                var release = await CompensateAsync(reservationId);
                return Result<BookingDto>.Failure(
                    release.IsSuccess ? creation.Error! : release.Error!);
            }

            bookings.Add(creation.Value);
            var save = await unitOfWork.SaveAsync(cancellationToken);
            if (save.IsFailure)
            {
                var release = await CompensateAsync(reservationId);
                return Result<BookingDto>.Failure(
                    release.IsSuccess ? save.Error! : release.Error!);
            }

            return Result<BookingDto>.Success(BookingMappings.ToDto(creation.Value));
        }
        catch
        {
            // Request cancellation must not skip cleanup; the HTTP adapter still bounds the release call.
            await CompensateAsync(reservationId);
            throw;
        }
    }

    /// <summary>Attempts bounded cleanup even after client cancellation and logs a recoverable hold if Catalog is down.</summary>
    private async Task<Result> CompensateAsync(Guid reservationId)
    {
        var release = await catalog.ReleaseAsync(reservationId, CancellationToken.None);
        if (release.IsFailure)
        {
            LogCompensationFailure(logger, reservationId, release.Error!.Code, null);
        }

        return release;
    }
}
