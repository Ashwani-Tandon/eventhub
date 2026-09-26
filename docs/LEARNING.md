# EventHub — Learning Notes

These notes follow the execution steps in `EXECUTION_PLAN.md`; the project and layer overview is in `PROJECT_STRUCTURE.md`.

## Step-13 — Local project setup

The four planning files are in `/Users/apple/Personal Projects/Event Booking Platform`.
Local Git records project history; GitHub is unnecessary for development on this Mac.
VS Code and its Codex extension are installed, and the agent can read the execution board.
Break it on purpose: `git log` before the first commit reports no commits; the owner has deferred that commit.

## Step-1a — Base tools

`sysctl hw.memsize` reported 8589934592 bytes (8 GB), so Step-1e should use `qwen2.5:3b`.
Rosetta is installed (`pkgutil --pkg-info com.apple.pkg.RosettaUpdateAuto`).
Apple Git 2.39.5 is available; this repository has the owner's name and email configured locally.
The four VS Code development extensions are installed and visible in the extension list.
Break it on purpose: before setup, `command -v code` found nothing; after the VS Code shell command was installed, it found `/usr/local/bin/code`.

## Step-1b — .NET SDK and Aspire

.NET SDK 10.0.301 was already installed, so reinstalling the SDK would have added no value.
Aspire.ProjectTemplates 13.5.3 adds the AppHost and Service Defaults templates used to orchestrate the services later.
The trusted localhost HTTPS certificate lets ASP.NET Core serve local HTTPS without browser trust warnings.
Break it on purpose: the certificate check initially returned “No valid certificate found”; after trusting it, the same check found the valid `CN=localhost` development certificate.

## Step-1c — Docker and SQL Server

Docker Desktop 4.92.0 runs an ARM64 Linux engine with about 4.1 GB available; Rosetta lets it run the AMD64 SQL Server image.
The `hello-world` container proved the client could pull and start an image, while `SELECT @@VERSION` proved SQL Server 2022 actually answered a query.
The temporary `sqltest` container stayed up over a minute, then was removed; the SQL image remains cached for Aspire.
Break it on purpose: a Docker client without permission to reach its socket prints its version but cannot query the engine; granting local socket access made the same check succeed.

## Step-1d — Node and Angular CLI

Homebrew Node 24.21.0 and Angular CLI 22.2.0 are installed and available on the terminal PATH.
The throwaway Angular app built, served HTTP 200 on port 4200, and loaded in the owner's browser; it was then deleted.
This proves the frontend toolchain before creating the real `web/` app in a later step.
Break it on purpose: CLI 22 initially warned that Node 24.14.0 was too old; updating to 24.21.0 removed the mismatch and let the app serve.

## Step-1e — Ollama and model

Ollama 0.32.15 was already installed; opening it once completed local-only onboarding and exposed its API on port 11434.
The 8 GB machine uses `qwen2.5:3b`; its warm one-sentence response took 1.49 seconds, well below the 20-second limit.
The model returned a structured `get_weather` tool call with `city: "Delhi"`, proving that it can drive the agent loop instead of merely generating text.
Break it on purpose: before Ollama was running, `/api/tags` could not connect; after local startup, it returned the model inventory and advertised the `tools` capability.

## Step-2 — Solution skeleton

The solution now separates each business service into Domain, Application, Infrastructure, and API projects; project references make dependencies point inward and prevent accidental database or framework coupling.
Aspire starts SQL Server, three persistent service-owned databases, four APIs, and the YARP gateway as one dependency graph; `WaitFor` keeps dependent resources from racing SQL during startup.
Secrets live in .NET user-secrets, while central package/build files make nullable analysis, analyzers, warnings-as-errors, and dependency versions consistent across every project.
Break it on purpose: request `http://localhost:5100/catalog/internal/health`; the gateway should return 404 because Catalog's internal surface is deliberately not public.

## Step-20 — Mediator, CQRS, and Result plumbing

The hand-written mediator finds each command/query handler through DI and wraps it with logging, validation, and performance behaviors in a visible, deterministic order.
Expected failures use `Result` and map centrally to consistent HTTP ProblemDetails; only unexpected exceptions reach the global handler, which logs the stack but returns a safe 500 body.
Handler dispatch adapters are built during assembly registration, so the request path uses an explicit dictionary lookup instead of runtime reflection.
Break it on purpose: send an empty message to `/identity/debug/echo`; expect 400 with a `Message` field error and no performance or handler entry in the Aspire logs.

## Step-3 — Identity and shared JWT validation

Identity now implements registration, login, token profile, user listing, and role changes as five explicit CQRS slices, with ports keeping SQL and authentication libraries outside Application.
ASP.NET Core PasswordHasher stores salted hashes; JWT signs readable profile claims, and every API independently checks signature, issuer, audience, and expiry before role policies run.
Issued tokens are snapshots: promoting attendee2 changed a fresh login to Organizer while the old token stayed Attendee; the demo role was then restored.
The committed EF migration runs before development seeding; restarting preserved the registered user and left six users and one migration, proving persistence and seed idempotency.
Break it on purpose: use an attendee token on /identity/users or alter a token signature; expect 403 for insufficient role and 401 for an invalid token.

## Step-4 — Catalog and atomic seat reservations

Catalog now separates event invariants, CQRS handlers, untracked read projections, and EF adapters so each layer has one visible responsibility.
A conditional SQL update and reservation row share one transaction, making capacity enforcement atomic while the reservation GUID makes safe retries idempotent—even during a parallel duplicate race.
Internal seat routes require both the user's JWT and Booking's constant-time checked credential; excluding them from YARP reduces exposure but is not itself authentication.
The shared fixed-seed generator creates 40 events and 300 future Booking records, so Catalog's booked-seat totals and later Booking data start from the same source.
Break it on purpose: reserve one seat beyond sold-out event 1; expect 409 and verify `seatsBooked` remains 20.

## Step-5 — Booking and compensating workflows

Booking reserves seats before fake payment and releases them if payment or local persistence fails; this compensates across independent service databases without a distributed transaction.
Cancellation saves Cancelled with SeatReleasePending first, then releases seats; after a Catalog outage, replay finishes that pending work without applying cancellation rules again.
Stored event and price snapshots let My Bookings and organizer-scoped statistics work while Catalog is unavailable; read queries use untracked DTO projections.
The Catalog client forwards the caller's JWT and adds Booking's service credential only to internal seat operations; bounded calls exist now, while creation idempotency and retry pipelines come later.
Break it on purpose: force payment failure and expect 422 with unchanged seats; stopping Catalog during cancellation gave 503, and restarting then replaying returned seats exactly once.
