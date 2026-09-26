# Booking operations and code walkthrough

Booking owns purchases and their event snapshots. This guide explains its five operations, payment compensation, and cancellation recovery.
Use the gateway URLs below in Postman and the adjacent `booking.http` collection to run each success and failure case.

| Method | Gateway URL                                               | Operation                                                            | Access             | Success          |
| ------ | --------------------------------------------------------- | -------------------------------------------------------------------- | ------------------ | ---------------- |
| POST   | `http://localhost:5100/booking/bookings`                  | Reserve seats, pay, and save a confirmed purchase                    | Logged-in user     | 201 BookingDto   |
| GET    | `http://localhost:5100/booking/bookings/mine`             | List the caller's purchases, newest first                            | Logged-in user     | 200 BookingDto[] |
| POST   | `http://localhost:5100/booking/bookings/{id}/cancel`      | Cancel an owned purchase or retry a pending seat release             | Booking owner      | 200 BookingDto   |
| GET    | `http://localhost:5100/booking/bookings?status=Cancelled` | List every purchase with an optional status filter                   | Admin              | 200 BookingDto[] |
| GET    | `http://localhost:5100/booking/bookings/stats`            | Read sales totals, six revenue months, top events, and status counts | Organizer or Admin | 200 StatsDto     |

Every protected request supplies `Authorization: Bearer <accessToken>`. The cancellation id is a route value; the optional Admin status filter is a query value. Creation accepts this JSON body:

```json
{
    "eventId": 2,
    "quantity": 2,
    "simulatePaymentFailure": false
}
```

Quantity must be 1–10. A true failure switch forces a declined payment only in Development. Without that switch, the fake gateway accepts approximately 90% of request identities. It does not charge real money. For a declined payment, expect 422 and no net seat change; a missing event is 404, a started event or invalid quantity is 400, and insufficient seats is 409.

## How a purchase moves through the code

`CreateBookingCommand` carries the body. Its validator rejects bad identifiers or quantities before any external call. The handler gets the user id from validated JWT claims and retrieves the event through `ICatalogClient`.

After the event-time check, it creates a reservation GUID and asks Catalog to hold seats under that id. It uses the same GUID as the fake payment identity. Once payment succeeds, `Booking.Create` copies the event title, start time, organizer, and price into the aggregate. The unit of work then saves the confirmed purchase in bookingdb.

If payment or saving fails after the seat hold, the handler asks Catalog to release that same reservation. This is compensation: a separate operation that reverses a completed action in another service. It is not a shared SQL transaction. If compensation itself cannot reach Catalog, the caller receives 503 and the reservation id is logged for diagnosis. Automatic recovery for an interrupted creation is outside this step; Step-16 adds the durable request claim.

## Why cancellation has a pending flag

The handler first checks that the booking belongs to the caller. A new cancellation invokes the Domain rule: the purchase must be Confirmed and the event must not have started. The handler saves `Status = Cancelled` and `SeatReleasePending = true` together in one local transaction before contacting Catalog.

Catalog's successful release lets Booking clear the pending flag. If Catalog is unavailable, the booking remains visibly Cancelled with the flag set, and the response is 503 with `Retry-After: 5`. Repeat the cancellation after Catalog recovers: the handler retries only the release, even if the event has started since cancellation was accepted. A fully completed cancellation replay returns 200 without changing seats again.

There is no background worker or broker in this version. The caller must retry a pending cancellation. The copied event facts let My Bookings show that recovery state even while Catalog is unavailable.

## Where HTTP and SQL belong

`ForwardTokenHandler` copies the incoming bearer token onto outgoing Catalog requests, preserving the attendee's identity. `CatalogHttpClient` sends `X-EventHub-Service` only on the internal reserve/release calls. The public event read uses the JWT without that credential.

The typed client's `https+http://catalog` address is resolved by Aspire service discovery. This step uses a ten-second total HTTP timeout with no explicit retry policy. Step-17 adds the shared retry, circuit-breaker, and attempt-timeout pipeline. The adapter translates dependency failure to a Result; Booking's API supplies the 503 retry header.

`BookingQueries` uses EF Core `AsNoTracking` projections and never calls Catalog. It filters statistics by the copied organizer id, counts revenue and tickets only for Confirmed purchases, fills six calendar months including zero-sales months, and limits top events to ten.

## Migrations, seeds, and project files

`InitialBooking` creates the service-owned table and integrity constraints. Development startup calls `MigrateAsync` before inserting the 300 shared demo purchases into an empty table. Their reservation GUIDs are the same identifiers Catalog used for its seeded holds.

Because Catalog may have started on an earlier day, Booking reads the actual public Catalog snapshots during first seeding rather than regenerating event dates and pretending they match persisted events. Purchase quantities and reservation ids still come from the fixed-seed shared generator.

The existing Booking project files now reference the EF tooling, Aspire SQL integration, ServiceDefaults, and development SeedData library. Application and Domain keep their original inward-only references. Configuration values continue to arrive from AppHost parameters; no secret is added to committed JSON.
