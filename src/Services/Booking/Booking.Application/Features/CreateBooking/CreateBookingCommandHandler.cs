// Handles a user's purchase: check the event, hold seats, process payment, and save the booking.
// When the user sends the same purchase reference again, return the booking without buying twice.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Features.CreateBooking;

/// <summary>Guides a purchase from the user's request to a saved booking, including repeats and failures.</summary>
public sealed class CreateBookingCommandHandler(
    ICatalogClient catalog,
    IPaymentGateway payments,
    IBookingRepository bookings,
    IBookingRequestRepository requests,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<CreateBookingCommandHandler> logger) : ICommandHandler<CreateBookingCommand, BookingDto>
{
    // Give the first request two minutes to work without another request taking over.
    // After that, a repeat can try to finish interrupted work. This assumes the first worker stopped;
    // it does not automatically stop a booking when two minutes pass.
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(2);
    // If the user clicks Book again, that second request waits up to five seconds for the first result.
    private static readonly TimeSpan ReplayWait = TimeSpan.FromSeconds(5);
    // While waiting, ask "is the first booking finished?" every 100 ms, rather than asking nonstop.
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);

    private static readonly Action<ILogger, Guid, string, Exception?> LogCompensationFailure =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(5101, nameof(LogCompensationFailure)),
            "Compensation for reservation {ReservationId} failed with {ErrorCode}");

    /// <summary>Books tickets once; a repeated purchase reference returns the booking already made.</summary>
    public async Task<Result<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<BookingDto>.Failure(BookingErrors.Unauthenticated);
        }

        // The key is the user's purchase reference. Sending it again means "this is the same purchase."
        // Without a reference, each request is treated as a separate purchase.
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();
        BookingRequest? claim = null;
        if (idempotencyKey is not null)
        {
            // Write "we are handling this purchase" before holding seats or paying.
            // Otherwise two clicks could both buy tickets before either booking is saved.
            var claimResult = await AcquireOrObserveAsync(userId, idempotencyKey, request, cancellationToken);
            if (claimResult.IsFailure)
            {
                return Result<BookingDto>.Failure(claimResult.Error!);
            }

            if (claimResult.Value.Booking is not null)
            {
                // The first click already made this booking. Show it again; do not hold more seats or pay again.
                return Result<BookingDto>.Success(BookingMappings.ToDto(claimResult.Value.Booking));
            }

            claim = claimResult.Value.Request;
            // The app may have stopped after saving the booking but before marking the purchase finished.
            // Look for that booking first, so restarting the purchase does not buy more tickets.
            var recovered = await bookings.FindByReservationIdAsync(claim.ReservationId, cancellationToken);
            if (recovered is not null)
            {
                var completion = await requests.CompleteAsync(claim, recovered.Id, clock.GetUtcNow(), cancellationToken);
                return completion.IsFailure
                    ? Result<BookingDto>.Failure(completion.Error!)
                    : Result<BookingDto>.Success(BookingMappings.ToDto(recovered));
            }
        }

        // Keep the same seat-hold reference when continuing interrupted work.
        // Catalog remembers it and will not hold another set of seats for the same reference.
        var reservationId = claim?.ReservationId ?? Guid.NewGuid();
        // If the user sent no purchase reference, use the seat-hold reference to identify this payment.
        var paymentKey = idempotencyKey ?? reservationId.ToString("N");
        var eventResult = await catalog.GetEventAsync(request.EventId, cancellationToken);
        if (eventResult.IsFailure)
        {
            return await ReturnFailureAsync(eventResult.Error!, claim, cancellationToken);
        }

        var eventItem = eventResult.Value;
        if (eventItem.StartsAt <= clock.GetUtcNow())
        {
            return await ReturnFailureAsync(BookingErrors.EventStarted, claim, cancellationToken);
        }

        var reservation = await catalog.ReserveAsync(eventItem.Id, reservationId, request.Quantity, cancellationToken);
        if (reservation.IsFailure)
        {
            return await ReturnFailureAsync(reservation.Error!, claim, cancellationToken);
        }

        // Remember whether the booking was saved. Once it exists, these seats belong to the user
        // and must not be returned just because a later bookkeeping step fails.
        var bookingSaved = false;
        try
        {
            var payment = await payments.PayAsync(userId, paymentKey, request.Quantity * eventItem.Price,
                request.SimulatePaymentFailure, cancellationToken);
            if (!payment.Succeeded)
            {
                return await CompensateAndFailAsync(reservationId, BookingErrors.PaymentFailed, claim, cancellationToken);
            }

            var creation = Domain.Booking.Create(userId, eventItem.Id, eventItem.Title, eventItem.StartsAt,
                eventItem.OrganizerId, request.Quantity, eventItem.Price, payment.Reference!, reservationId,
                idempotencyKey, clock.GetUtcNow());
            if (creation.IsFailure)
            {
                return await CompensateAndFailAsync(reservationId, creation.Error!, claim, cancellationToken);
            }

            // Add prepares the booking for saving. SaveAsync actually writes it to the database.
            bookings.Add(creation.Value);
            var save = await unitOfWork.SaveAsync(cancellationToken);
            if (save.IsFailure)
            {
                return await CompensateAndFailAsync(reservationId, save.Error!, claim, cancellationToken);
            }
            bookingSaved = true;

            if (claim is not null)
            {
                var completion = await requests.CompleteAsync(claim, creation.Value.Id, clock.GetUtcNow(), cancellationToken);
                if (completion.IsFailure)
                {
                    return Result<BookingDto>.Failure(completion.Error!);
                }
            }

            return Result<BookingDto>.Success(BookingMappings.ToDto(creation.Value));
        }
        catch
        {
            // If something unexpected fails before saving, try to return the seats.
            // If the booking was already saved, keep its seats; a repeat can finish marking it complete.
            if (!bookingSaved)
            {
                var release = await CompensateAsync(reservationId);
                if (release.IsSuccess && claim is not null)
                {
                    await requests.DeleteAsync(claim, CancellationToken.None);
                }
            }

            throw;
        }
    }

    /// <summary>Decides whether to start this purchase, show its existing booking, or wait for the first click.</summary>
    private async Task<Result<ObservedClaim>> AcquireOrObserveAsync(Guid userId, string idempotencyKey,
        CreateBookingCommand command, CancellationToken cancellationToken)
    {
        // Set a five-second deadline once. Moving it forward after each check would mean waiting forever.
        var stopAt = clock.GetUtcNow().Add(ReplayWait);
        while (true)
        {
            var claim = await requests.ClaimAsync(userId, idempotencyKey, command.EventId, command.Quantity,
                command.SimulatePaymentFailure, clock.GetUtcNow(), ClaimLease, cancellationToken);
            switch (claim.Outcome)
            {
                case BookingRequestClaimOutcome.Acquired:
                    // No other request is handling this purchase now. Go ahead and book the tickets.
                    return Result<ObservedClaim>.Success(new ObservedClaim(claim.Request, null));
                case BookingRequestClaimOutcome.Mismatch:
                    // The user reused a purchase reference but changed the event or tickets.
                    // Reject it: we cannot treat two different purchases as the same one.
                    return Result<ObservedClaim>.Failure(BookingErrors.IdempotencyMismatch);
                case BookingRequestClaimOutcome.Completed:
                    // This purchase is finished. Read its saved booking and show the user that result.
                    var booking = claim.Request.BookingId is { } bookingId
                        ? await bookings.FindAsync(bookingId, cancellationToken)
                        : null;
                    return booking is null
                        ? Result<ObservedClaim>.Failure(BookingErrors.PersistenceUnavailable)
                        : Result<ObservedClaim>.Success(new ObservedClaim(claim.Request, booking));
                case BookingRequestClaimOutcome.Processing:
                    // The first click is still being handled. Wait a little for its result.
                    // After five seconds, tell the user to try again; the first request can keep working.
                    if (clock.GetUtcNow() >= stopAt)
                    {
                        return Result<ObservedClaim>.Failure(BookingErrors.RequestInProgress);
                    }

                    // Pause for 100 ms, then check again. Stop waiting if the user cancels this request.
                    await Task.Delay(PollDelay, cancellationToken);
                    break;
                default:
                    // This answer is not one the program understands; report it as a coding mistake.
                    throw new InvalidOperationException("Unknown booking request claim outcome.");
            }
        }
    }

    /// <summary>Reports a failed attempt and decides whether to keep its purchase record for a later retry.</summary>
    private async Task<Result<BookingDto>> ReturnFailureAsync(Error error, BookingRequest? claim,
        CancellationToken cancellationToken)
    {
        if (claim is null || !IsKnownRetryableFailure(error))
        {
            // If another service did not answer, we may not know what it finished.
            // Keep the purchase record so the next attempt can use the same references.
            return Result<BookingDto>.Failure(error);
        }

        var deletion = await requests.DeleteAsync(claim, cancellationToken);
        return Result<BookingDto>.Failure(deletion.IsSuccess ? error : deletion.Error!);
    }

    /// <summary>Returns seats after a failed purchase, then removes its unfinished purchase record.</summary>
    private async Task<Result<BookingDto>> CompensateAndFailAsync(Guid reservationId, Error error,
        BookingRequest? claim, CancellationToken cancellationToken)
    {
        var release = await CompensateAsync(reservationId);
        if (release.IsFailure)
        {
            // Returning the seats failed. Keep the purchase record so we remember which seats need attention.
            return Result<BookingDto>.Failure(release.Error!);
        }

        if (claim is null)
        {
            return Result<BookingDto>.Failure(error);
        }

        var deletion = await requests.DeleteAsync(claim, cancellationToken);
        return Result<BookingDto>.Failure(deletion.IsSuccess ? error : deletion.Error!);
    }

    /// <summary>Asks Catalog to make the reserved seats available again and records an error if that fails.</summary>
    private async Task<Result> CompensateAsync(Guid reservationId)
    {
        // Try to return seats even if the user closed the page. The Catalog call has its own time limit.
        var release = await catalog.ReleaseAsync(reservationId, CancellationToken.None);
        if (release.IsFailure)
        {
            LogCompensationFailure(logger, reservationId, release.Error!.Code, null);
        }

        return release;
    }

    /// <summary>Checks for a clear rejection, such as missing event, insufficient seats, or failed payment.</summary>
    private static bool IsKnownRetryableFailure(Error error)
    {
        return error.Type is ErrorType.Validation or ErrorType.NotFound or ErrorType.Conflict or ErrorType.Unprocessable;
    }

    private sealed record ObservedClaim(BookingRequest Request, Domain.Booking? Booking);
}
