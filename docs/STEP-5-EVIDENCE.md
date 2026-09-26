# Step-5 — Booking verification evidence

This records the live HTTP checks used to prove Booking's purchase, cancellation, and read flows.
It accompanies `Booking.Api/booking.http`; no automated tests were added.

Verified on 2026-09-27 with Aspire, SQL Server, and gateway `http://localhost:5100`.

| Check                          | Observed result                                                                                                                                  |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Migration-first seed           | Initial Admin listing: 300 bookings, 29 Cancelled; Booking health 200 Healthy                                                                    |
| Purchase two seats             | 201, booking 301; event 2 seatsLeft 233 → 231                                                                                                    |
| Forced payment decline         | 422; seatsLeft remained 231 after compensation                                                                                                   |
| Cancellation ownership         | attendee2 cancelling attendee's booking: 403                                                                                                     |
| Cancel and replay              | Both 200; Cancelled, pending false; seatsLeft restored to 233                                                                                    |
| Statistics scope               | Organizer 154 bookings; organizer2 147; Admin 301; six monthly buckets; attendee 403                                                             |
| Statistics aggregates          | Organizer2 matched Admin rows scoped to organizer ID 3; both organizers' revenue and tickets summed to Admin totals                              |
| Input/business failures        | Quantity 0: 400; unknown event: 404; past event 31: 400; sold-out event 1: 409                                                                   |
| Catalog outage                 | Stopped only Catalog using Aspire's Stop resource action; cancellation 503, creation 503, detail `Catalog service unavailable`, `Retry-After: 5` |
| Persisted pending cancellation | My Bookings 200 exposed booking 302 as Cancelled with SeatReleasePending true; statistics remained 200                                           |
| Recovery and replay            | Restarted Catalog using Aspire; cancel retry 200 and replay 200; pending false; seatsLeft 233 on both, matching pre-purchase baseline            |

The temporary live-request driver reported `BASELINE_ACCEPTANCE=PASS`, `OUTAGE_ACCEPTANCE=PASS`,
and `RECOVERY_ACCEPTANCE=PASS`. The two successful verification purchases remain as Cancelled
audit records, so the database now contains the original 300 seed records plus those two records.
Catalog was restored after the outage check; no seat holds remain from these Booking purchases.

Build and formatting were verified with:

```sh
dotnet build EventHub.sln --no-restore
dotnet format EventHub.sln --no-restore --verify-no-changes --verbosity minimal
node /Users/apple/.vscode/extensions/esbenp.prettier-vscode-12.4.0/node_modules/prettier/bin/prettier.cjs --check docs/BOOKING_GUIDE.md docs/STEP-5-EVIDENCE.md docs/DECISIONS.md docs/LEARNING.md docs/EXECUTION_PLAN.md
git diff --check
```

Final build: 0 warnings, 0 errors. Human-authored Booking source files were reviewed for purpose
headers and method explanations; generated migrations remain under EF's control. Commands,
handlers, and validators are separate files. Creation idempotency and retry/breaker tuning remain
explicitly outside this step (Step-16 and Step-17).
