// Persists cancellation locally before returning held seats through Catalog.
// A pending flag survives dependency failures; repeated requests resume only the unfinished release.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;

namespace Booking.Application.Features.CancelBooking;

/// <summary>Implements owner-only cancellation with an explicit recovery state and idempotent replay.</summary>
public sealed class CancelBookingCommandHandler(
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    ICatalogClient catalog,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<CancelBookingCommand, BookingDto>
{
    /// <summary>Saves the pending cancellation first, releases seats once, then saves acknowledgement.</summary>
    public async Task<Result<BookingDto>> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<BookingDto>.Failure(BookingErrors.Unauthenticated);
        }

        var booking = await bookings.FindAsync(request.Id, cancellationToken);
        if (booking is null)
        {
            return Result<BookingDto>.Failure(BookingErrors.NotFound);
        }

        if (booking.UserId != userId)
        {
            return Result<BookingDto>.Failure(BookingErrors.Forbidden);
        }

        if (booking.Status == BookingStatus.Cancelled && !booking.SeatReleasePending)
        {
            return Result<BookingDto>.Success(BookingMappings.ToDto(booking));
        }

        if (!booking.SeatReleasePending)
        {
            var cancellation = booking.BeginCancellation(clock.GetUtcNow());
            if (cancellation.IsFailure)
            {
                return Result<BookingDto>.Failure(cancellation.Error!);
            }

            // Both fields are persisted together in one local SaveChanges transaction.
            var pendingSave = await unitOfWork.SaveAsync(cancellationToken);
            if (pendingSave.IsFailure)
            {
                return Result<BookingDto>.Failure(pendingSave.Error!);
            }
        }

        // Pending replays bypass the date/status rules because the cancellation was already accepted.
        var release = await catalog.ReleaseAsync(booking.ReservationId, cancellationToken);
        if (release.IsFailure)
        {
            return Result<BookingDto>.Failure(release.Error!);
        }

        booking.CompleteSeatRelease();
        var finalSave = await unitOfWork.SaveAsync(cancellationToken);
        if (finalSave.IsFailure)
        {
            return Result<BookingDto>.Failure(finalSave.Error!);
        }

        return Result<BookingDto>.Success(BookingMappings.ToDto(booking));
    }
}
