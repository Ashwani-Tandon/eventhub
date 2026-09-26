# Step-4 — Catalog Service Evidence

This file records the live build and HTTP proof used to complete Step-4.
It complements `catalog.http` with the observed results from the local Aspire stack on 2026-09-26.

## Build and migration

`dotnet build EventHub.sln --no-restore --disable-build-servers --verbosity minimal`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

EF Core generated and committed `InitialCatalog`. Its migration creates `Events` and
`SeatReservations`, including capacity, price, category, quantity, and status constraints.
The application started through Aspire, applied the migration, seeded successfully, and `/catalog/health`
returned `Healthy` with HTTP 200. A second startup retained 40 events, proving the persistent seed did
not duplicate rows.

## HTTP acceptance run

The local verification script logged in through Identity, called public routes through YARP, and called
internal routes directly on Catalog where required. Tokens and service credentials were never printed.

```text
SEARCH default=30 tech=6 title=3 free=3 page2Items=7 page2Total=30
AUTH attendeeCreate=403 organizer2Edit=403 adminEdit=200
VALIDATION status=400 fields=Capacity,Category,Price,StartsAt,Title
INTERNAL gateway=404 directWithoutCredential=403
OVERBOOK status=409 seatsBefore=20 seatsAfter=20
IDEMPOTENCY reserve=200/200 seats=16->18 mismatch=409 wrongUserRelease=403 release=200/200 finalSeats=16
PARALLEL statuses=200/200 seats=35->38 delta=3
STEP4_HTTP_ACCEPTANCE=PASS
```

The successful CRUD cleanup produced:

```text
CRUD create=201 id=41 delete=204 seededBefore=40 afterCleanup=40
STEP4_CRUD_AND_SEED=PASS
```

These results prove filters and paging totals, role and ownership enforcement, field validation, internal
route isolation, overbooking protection, sequential and parallel idempotency, one-time release, and the
deterministic 40-event seed.
