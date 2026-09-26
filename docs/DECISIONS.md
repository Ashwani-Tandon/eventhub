# EventHub — Architecture Decisions

## 2026-09-27 — Persist pending seat release before contacting Catalog

**Decision.** Cancellation saves Cancelled and SeatReleasePending together before calling Catalog. A pending replay retries only the idempotent release; a completed replay returns the existing booking.

**Why.** Booking and Catalog own separate databases. Recording the local decision first makes an unavailable Catalog recoverable without a distributed transaction, broker, or outbox. The booking snapshot supplies its event date and owner without a Catalog read.

**Trade-off.** Seats may remain held while the booking is already Cancelled. A caller must repeat cancellation after recovery; there is no automatic background retry in v1.0. If the flag-clear save fails after release, replaying the release remains safe.

## 2026-09-27 — Use stable fake-payment outcomes and bounded Catalog calls

**Decision.** Hash the reservation identity to determine the fake payment outcome and reference, with a Development-only forced decline. Give the typed Catalog client a ten-second total timeout; install the configurable resilience pipeline in Step-17.

**Why.** A repeated payment identity has a stable result without a real payment provider. A bounded HTTP call and explicit unavailable Result let Step-5 prove compensation and recovery without prematurely implementing resilience tuning.

**Trade-off.** Payment is a deterministic demonstration rather than a real financial system. Creation has no durable idempotency claim until Step-16, and an interrupted or unavailable compensation requires diagnosis using the logged reservation identity.

## 2026-09-26 — Require two identities for internal seat operations

**Decision.** Catalog's internal reserve and release endpoints require both the calling user's signed JWT and a constant-time checked `X-EventHub-Service` credential known only to Booking. YARP exposes only Catalog's `/events` and `/health` routes, never `/internal`.

**Why.** The JWT preserves the real user's authorization and audit identity, while the second credential proves that the immediate caller is the Booking service. Gateway exclusion reduces exposure but cannot protect direct service-discovery or local network access by itself.

**Trade-off.** Booking must securely receive and send another secret, and Catalog must rotate it consistently. This is intentionally simpler than workload identity for the local learning environment.

## 2026-09-26 — Read the current profile from signed token claims

**Decision.** Keep the id/role `ICurrentUser` port narrow and add a shared technical `ICurrentUserProfile` port for email/name claims. Identity's current-user query reads the authenticated token, not a fresh database profile.

**Why.** FR-ID-04 requests the current user from the token, while FR-ID-05 requires issued tokens to retain their role until the next login. Both the profile response and authorization therefore use the same snapshot.

**Trade-off.** A changed role remains effective in existing tokens until they expire after two hours. Immediate token revocation and refresh tokens remain outside v1.0.

## 2026-09-26 — Keep Aspire projects under `src/Aspire`

**Decision.** The backend startup command is `dotnet run --project src/Aspire/EventHub.AppHost`, matching the solution layout in SPEC §15.5.

**Why.** Keeping the AppHost and ServiceDefaults projects together makes the orchestration boundary explicit and matches Step-2's implementation and acceptance criteria.

**Trade-off.** The command is slightly longer than placing AppHost directly under `src/`, but the repository structure is clearer and remains consistent as more projects are added.

## 2026-09-26 — Keep project documentation under `docs/`

**Decision.** Store the specification, execution plan, decisions, learning notes, and architecture guide in `docs/`. Keep `AGENTS.md` and `CLAUDE.md` at the repository root.

**Why.** One documentation folder makes the growing body of project material easier to browse, while root-level agent files continue to be discovered automatically by coding tools.

**Trade-off.** References from root-level instructions need a `docs/` prefix, and the two tool-discovery files remain intentional exceptions to the documentation-folder rule.

## 2026-09-26 — Use a hand-written mediator

**Decision.** Implement the small `ISender` pipeline required by EventHub instead of adding MediatR. Assembly scanning creates typed dispatch adapters at startup; sending a request is then a dictionary lookup followed by the registered behaviors and handler.

**Why.** MediatR v13+ is commercially licensed, and implementing the mechanism directly makes handler resolution and pipeline ordering visible for this learning project.

**Trade-off.** EventHub owns this plumbing and deliberately supports only the features it needs; it does not inherit notifications, streaming, or other MediatR features.

## 2026-09-26 — Use CQRS-lite in each service

**Decision.** Represent every use case as a command or query with its own handler and validator, while keeping one database per service rather than separate read and write stores.

**Why.** Separate request models make intent and responsibilities clear without introducing operational complexity that the current scale does not require.

**Trade-off.** Reads and writes can be optimized independently in code, but they still share the service database and its availability boundary.

## 2026-09-26 — Results for expected failures, exceptions for unexpected failures

**Decision.** Handlers return `Result`/`Result<T>` with a typed `Error` for validation, missing data, conflicts, authorization, availability, and unprocessable requests. Unexpected faults are allowed to reach the global exception handler.

**Why.** Expected outcomes stay explicit in handler signatures and map consistently to HTTP, while programming and infrastructure faults are logged centrally instead of being mistaken for business outcomes.

**Trade-off.** Callers must inspect a result before reading its value, and every new error category requires an intentional HTTP mapping.
