// Handles the user's Cancel request and asks Catalog to make their seats available again.
// Remember unfinished seat returns so clicking Cancel again can finish them after a service failure.
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
            // The booking is already cancelled and its seats returned. Show that result again.
            return Result<BookingDto>.Success(BookingMappings.ToDto(booking));
        }

        if (!booking.SeatReleasePending)
        {
            // Check whether cancellation is allowed only the first time; an accepted cancellation
            // only needs to finish returning seats when the user asks again.
            var cancellation = booking.BeginCancellation(clock.GetUtcNow());
            if (cancellation.IsFailure)
            {
                return Result<BookingDto>.Failure(cancellation.Error!);
            }

            // Save "cancelled" and "seats still need returning" together, so a restart does not lose the work.
            var pendingSave = await unitOfWork.SaveAsync(cancellationToken);
            if (pendingSave.IsFailure)
            {
                return Result<BookingDto>.Failure(pendingSave.Error!);
            }
        }

        // The cancellation was already accepted. Finish returning seats even if the event has since started.
        var release = await catalog.ReleaseAsync(booking.ReservationId, cancellationToken);
        if (release.IsFailure)
        {
            return Result<BookingDto>.Failure(release.Error!);
        }

        // Mark "seats returned" only after Catalog confirms it. If saving this marker fails,
        // another Cancel request can finish it without returning seats twice.
        booking.CompleteSeatRelease();
        var finalSave = await unitOfWork.SaveAsync(cancellationToken);
        if (finalSave.IsFailure)
        {
            return Result<BookingDto>.Failure(finalSave.Error!);
        }

        return Result<BookingDto>.Success(BookingMappings.ToDto(booking));
    }
}
