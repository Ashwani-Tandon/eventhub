// Claims an idempotency identity before coordinating Catalog, payment, and local persistence.
// Replays either observe the original booking or safely resume stale work with the stored identities.
using Booking.Application.Contracts;
using Booking.Application.Ports;
using Booking.Domain;
using EventHub.BuildingBlocks.Messaging;
using EventHub.BuildingBlocks.Results;
using EventHub.BuildingBlocks.Security;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Features.CreateBooking;

/// <summary>Coordinates the recoverable purchase workflow while external mechanics stay behind ports.</summary>
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
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ReplayWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(100);

    private static readonly Action<ILogger, Guid, string, Exception?> LogCompensationFailure =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(5101, nameof(LogCompensationFailure)),
            "Compensation for reservation {ReservationId} failed with {ErrorCode}");

    /// <summary>Returns an original booking on replay and lets only the claim owner perform side effects.</summary>
    public async Task<Result<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<BookingDto>.Failure(BookingErrors.Unauthenticated);
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();
        BookingRequest? claim = null;
        if (idempotencyKey is not null)
        {
            var claimResult = await AcquireOrObserveAsync(userId, idempotencyKey, request, cancellationToken);
            if (claimResult.IsFailure)
            {
                return Result<BookingDto>.Failure(claimResult.Error!);
            }

            if (claimResult.Value.Booking is not null)
            {
                return Result<BookingDto>.Success(BookingMappings.ToDto(claimResult.Value.Booking));
            }

            claim = claimResult.Value.Request;
            var recovered = await bookings.FindByReservationIdAsync(claim.ReservationId, cancellationToken);
            if (recovered is not null)
            {
                var completion = await requests.CompleteAsync(claim, recovered.Id, clock.GetUtcNow(), cancellationToken);
                return completion.IsFailure
                    ? Result<BookingDto>.Failure(completion.Error!)
                    : Result<BookingDto>.Success(BookingMappings.ToDto(recovered));
            }
        }

        var reservationId = claim?.ReservationId ?? Guid.NewGuid();
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
            // Once the booking exists, recovery must preserve its reservation and only finish the claim link.
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

    private async Task<Result<ObservedClaim>> AcquireOrObserveAsync(Guid userId, string idempotencyKey,
        CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var stopAt = clock.GetUtcNow().Add(ReplayWait);
        while (true)
        {
            var claim = await requests.ClaimAsync(userId, idempotencyKey, command.EventId, command.Quantity,
                command.SimulatePaymentFailure, clock.GetUtcNow(), ClaimLease, cancellationToken);
            switch (claim.Outcome)
            {
                case BookingRequestClaimOutcome.Acquired:
                    return Result<ObservedClaim>.Success(new ObservedClaim(claim.Request, null));
                case BookingRequestClaimOutcome.Mismatch:
                    return Result<ObservedClaim>.Failure(BookingErrors.IdempotencyMismatch);
                case BookingRequestClaimOutcome.Completed:
                    var booking = claim.Request.BookingId is { } bookingId
                        ? await bookings.FindAsync(bookingId, cancellationToken)
                        : null;
                    return booking is null
                        ? Result<ObservedClaim>.Failure(BookingErrors.PersistenceUnavailable)
                        : Result<ObservedClaim>.Success(new ObservedClaim(claim.Request, booking));
                case BookingRequestClaimOutcome.Processing:
                    if (clock.GetUtcNow() >= stopAt)
                    {
                        return Result<ObservedClaim>.Failure(BookingErrors.RequestInProgress);
                    }

                    await Task.Delay(PollDelay, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException("Unknown booking request claim outcome.");
            }
        }
    }

    private async Task<Result<BookingDto>> ReturnFailureAsync(Error error, BookingRequest? claim,
        CancellationToken cancellationToken)
    {
        if (claim is null || !IsKnownRetryableFailure(error))
        {
            return Result<BookingDto>.Failure(error);
        }

        var deletion = await requests.DeleteAsync(claim, cancellationToken);
        return Result<BookingDto>.Failure(deletion.IsSuccess ? error : deletion.Error!);
    }

    private async Task<Result<BookingDto>> CompensateAndFailAsync(Guid reservationId, Error error,
        BookingRequest? claim, CancellationToken cancellationToken)
    {
        var release = await CompensateAsync(reservationId);
        if (release.IsFailure)
        {
            return Result<BookingDto>.Failure(release.Error!);
        }

        if (claim is null)
        {
            return Result<BookingDto>.Failure(error);
        }

        var deletion = await requests.DeleteAsync(claim, cancellationToken);
        return Result<BookingDto>.Failure(deletion.IsSuccess ? error : deletion.Error!);
    }

    private async Task<Result> CompensateAsync(Guid reservationId)
    {
        var release = await catalog.ReleaseAsync(reservationId, CancellationToken.None);
        if (release.IsFailure)
        {
            LogCompensationFailure(logger, reservationId, release.Error!.Code, null);
        }

        return release;
    }

    private static bool IsKnownRetryableFailure(Error error)
    {
        return error.Type is ErrorType.Validation or ErrorType.NotFound or ErrorType.Conflict or ErrorType.Unprocessable;
    }

    private sealed record ObservedClaim(BookingRequest Request, Domain.Booking? Booking);
}
