# EventHub — Project Structure

This guide explains where code belongs and why each project exists. Identity, Catalog, and Booking
have implemented business APIs; the Agent and Angular work remains on the execution board.

For service behavior, endpoints, inputs, and validation/error messages, see the
[service API references](../README.md). ServiceDefaults supplies shared JWT validation,
role policies, and token request context; each implemented business service owns its migrations.

## Repository map

```text
EventHub/
├── AGENTS.md                         Rules coding agents must follow
├── CLAUDE.md                         Points Claude-compatible tools to AGENTS.md
├── EventHub.sln                      The complete .NET solution
├── Directory.Build.props             Build rules shared by every .NET project
├── Directory.Packages.props          Central NuGet package versions
├── docs/                              Specification, plan, decisions, and learning material
└── src/
    ├── Aspire/
    │   ├── EventHub.AppHost/          Starts and connects the local distributed system
    │   └── EventHub.ServiceDefaults/  Shared hosting, health, telemetry, service discovery
    ├── BuildingBlocks/
    │   └── EventHub.BuildingBlocks/   Shared technical primitives; no business rules
    ├── Gateway/
    │   └── EventHub.Gateway/          Public YARP reverse proxy on port 5100
    ├── Services/
    │   ├── Identity/                  Users, passwords, roles, and JWTs
    │   ├── Catalog/                   Events, ownership, capacity, and seat reservations
    │   ├── Booking/                   Bookings, payment simulation, cancellation, and stats
    │   └── Agent/                     Local-LLM chat and HTTP-backed tools
    └── Tools/
        └── EventHub.SeedData/         Deterministic development seed-data definitions
```

`web/` will be added in Step-6 for the Angular application. `Agent.Playground` will be added in
Step-14 as standalone learning material.

## How one business service is divided

Identity, Catalog, and Booking each use four projects. Dependencies point inward:

```text
HTTP request
    ↓
Api → Infrastructure → Application → Domain
          implements       owns ports     owns rules
```

The arrows mean “may reference.” Domain does not know about databases, HTTP, ASP.NET Core, or EF
Core. Application describes use cases and the interfaces it needs. Infrastructure implements those
interfaces. API assembles the pieces and translates HTTP to commands and queries.

| Layer          | Purpose                                     | Contains                                                             | Must not contain                                 |
| -------------- | ------------------------------------------- | -------------------------------------------------------------------- | ------------------------------------------------ |
| Domain         | Protect business rules and state            | Entities, value objects, domain errors and methods                   | EF Core, HTTP, ASP.NET Core, database code       |
| Application    | Express the system's use cases              | Commands, queries, handlers, validators, DTOs, ports                 | Concrete database or HTTP-client implementations |
| Infrastructure | Connect use cases to external technology    | EF Core, repositories, HTTP clients, token/password/payment adapters | HTTP endpoints or business decisions             |
| API            | Expose the service and compose dependencies | Minimal endpoints, authentication policies, dependency registration  | Business rules or direct database manipulation   |

This separation makes the important rules independent of the technologies used to deliver and store
them. The project references act as a compile-time guard against dependencies pointing outward.

## Shared and hosting projects

### `EventHub.AppHost`

The local orchestrator. It declares SQL Server, the three databases, all APIs, the gateway, secrets,
startup dependencies, and persistent container storage. It replaces manually starting each process
and hard-coding local ports. Run it with:

```bash
dotnet run --project src/Aspire/EventHub.AppHost
```

### `EventHub.ServiceDefaults`

Common ASP.NET Core hosting setup used by APIs and the gateway: service discovery, health endpoints,
OpenTelemetry logging/metrics/tracing, shared authentication, and later resilience configuration.
It contains cross-service technical setup, not domain behavior.

### `EventHub.BuildingBlocks`

Small technical abstractions shared by the services. It contains the hand-written mediator,
command/query contracts, logging/validation/performance pipeline behaviors, Result/Error types,
entity base type, and current-user abstraction. Business concepts such as Event or Booking never
belong here.

### `EventHub.Gateway`

The single public backend entry point. YARP maps `/identity`, `/catalog`, `/booking`, and `/agent` to
services discovered through Aspire. It removes those prefixes before forwarding. It deliberately
blocks `/catalog/internal/*`; the gateway routes traffic but is not the services' security boundary.

### `EventHub.SeedData`

Holds deterministic development data definitions that multiple service infrastructure projects can
use without sharing databases or business logic. Actual database seeding is implemented alongside
the service that owns the database.

## Business service projects

### Identity

| Project                   | Purpose                                                                                                                                 |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `Identity.Domain`         | Owns the `User` entity and rules around identity state and roles.                                                                       |
| `Identity.Application`    | Registration, login, current-user lookup, user listing, and role-change use cases; owns token, password-hashing, and persistence ports. |
| `Identity.Infrastructure` | User database access, password hashing, JWT generation, migrations, and demo-user seeding.                                              |
| `Identity.Api`            | `/auth/*` and `/users/*` endpoints, authentication/authorization setup, and dependency composition.                                     |

### Catalog

| Project                  | Purpose                                                                                       |
| ------------------------ | --------------------------------------------------------------------------------------------- |
| `Catalog.Domain`         | Owns events, capacity rules, ownership-sensitive state, and reservation state.                |
| `Catalog.Application`    | Event CRUD/search and reserve/release use cases; owns repository and query ports.             |
| `Catalog.Infrastructure` | Catalog database, EF migrations, atomic seat updates, projections, and event seeding.         |
| `Catalog.Api`            | Public `/events/*` endpoints plus protected service-only `/internal/*` reservation endpoints. |

### Booking

| Project                  | Purpose                                                                                                             |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| `Booking.Domain`         | Owns booking and cancellation state transitions and their invariants.                                               |
| `Booking.Application`    | Create/cancel/list/statistics use cases; owns Catalog-client, payment, repository, and query ports.                 |
| `Booking.Infrastructure` | Booking database, migrations, Catalog HTTP adapter, fake payment adapter, pending-release persistence, and seeding. |
| `Booking.Api`            | `/bookings/*` endpoints, policies, and dependency composition.                                                      |

### Agent

The Agent has no Domain project because it owns no business entities or database. It currently has
only hosting/authentication and development health routes; chat and tools remain planned.
Its future tools must act through the same Catalog and Booking APIs as the logged-in user.

| Project                | Purpose                                                                                                               |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `Agent.Application`    | Scaffold; planned chat use case, tool definitions, conversation contracts, and API ports.                             |
| `Agent.Infrastructure` | Scaffold; planned Ollama integration and authenticated HTTP clients.                                                  |
| `Agent.Api`            | Hosting/authentication and development health routes; chat, timeouts, and concurrency limits are not implemented yet. |

## Request flow examples

A public event search follows this path (Angular is the planned UI; Postman/curl can call it now):

```text
Angular → Gateway `/catalog/events` → Catalog.Api
        → Catalog.Application query → Catalog.Infrastructure projection → catalogdb
```

A booking already crosses a service boundary without sharing databases:

```text
Angular → Gateway → Booking.Api → Booking.Application
                             ├── Booking.Infrastructure → bookingdb
                             └── Catalog HTTP API → catalogdb
```

Booking never references Catalog's DbContext or tables. It calls Catalog by HTTP using the caller's
JWT, which keeps database ownership and authorization boundaries explicit.
