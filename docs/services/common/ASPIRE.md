# Aspire — orchestration and shared hosting

Aspire starts the local distributed system and supplies service discovery, resource connections, and configuration.
This reference explains AppHost and ServiceDefaults, their resources and startup dependencies, and how to inspect the running application.

## AppHost: what starts and connects

Project: `src/Aspire/EventHub.AppHost`. Run from the repository root:

```sh
dotnet run --project src/Aspire/EventHub.AppHost
```

| Resource     | Responsibility                                            | Dependencies/reference wiring                         |
| ------------ | --------------------------------------------------------- | ----------------------------------------------------- |
| `sql`        | SQL Server container with persistent lifetime/data volume | Secret SQL password parameter                         |
| `identitydb` | Identity-owned database                                   | SQL Server                                            |
| `catalogdb`  | Catalog-owned database                                    | SQL Server                                            |
| `bookingdb`  | Booking-owned database                                    | SQL Server                                            |
| `identity`   | User and token APIs                                       | References/waits for identitydb                       |
| `catalog`    | Event and seat APIs                                       | References/waits for catalogdb                        |
| `booking`    | Purchase/cancellation/statistics APIs                     | References/waits for bookingdb and Catalog            |
| `agent`      | Current Agent scaffold                                    | References/waits for Catalog and Booking; no database |
| `gateway`    | Public entry point on 5100                                | References/waits for all four services                |

`WithReference` supplies connections/discovery configuration. `WaitFor` declares startup readiness dependencies; it is not a transaction, runtime retry policy, or guarantee a dependency will never fail later.
Booking waits for Catalog because initial development seeding reads actual event snapshots through HTTP. Each service migrates/seeds only its own database.
Persistent SQL storage survives application restarts; it must not be removed merely to apply a migration.

## Configuration and secrets

| AppHost parameter     | Supplied to                                  | Purpose                                                   |
| --------------------- | -------------------------------------------- | --------------------------------------------------------- |
| `sql-password`        | SQL resource and database connections        | SQL authentication                                        |
| `jwt-key`             | All four APIs as `Jwt__Key`                  | Shared signing/validation key                             |
| `booking-service-key` | Catalog and Booking as `BookingService__Key` | Proves trusted Booking access to internal seat operations |

Use AppHost user-secrets / Aspire parameters for values; never commit credentials to JSON or documentation.
Environment double underscores represent configuration nesting. Database references provide each service its own connection string, not permission to query another service's database.
Service ports are dynamically assigned; read them from the dashboard rather than hard-coding them. Gateway stays on 5100. Ollama uses 11434 but is not started by the current AppHost; the owner must run it before future Agent usage.

The local development machine is Apple Silicon with 8 GB RAM. SQL Server runs as an amd64 container under Rosetta; initial startup can be slow.
The selected local Ollama model is `qwen2.5:3b`, chosen for that memory budget; the current Agent scaffold does not call it yet.

## Dashboard and operation

The startup output provides the current dashboard URL and login link. Do not copy its authentication token into permanent documents.
Resources shows service/container state and current URLs. Console and structured logs expose service output; traces show HTTP calls and timing; metrics show collected measurements.
Stop/start an individual service there to observe dependency failures without deleting data. Booking's pending cancellation can be completed after Catalog is restarted; stopping a database is a different failure scenario.

## ServiceDefaults: common API hosting, not an orchestrator

Project: `src/Aspire/EventHub.ServiceDefaults`. APIs and Gateway call shared setup rather than duplicating technical hosting configuration.

| Feature           | Current behavior                                                                                                               |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Discovery         | Resolves outbound HTTP clients through Aspire service names                                                                    |
| Telemetry         | OpenTelemetry logging, ASP.NET/HTTP tracing, HTTP/server/runtime metrics; OTLP export when configured                          |
| Health            | Development-only `/health` runs registered checks; `/alive` runs checks tagged `live`; healthy gives 200, unhealthy 503        |
| Authentication    | APIs validate JWT signature, issuer `eventhub-identity`, audience `eventhub`, expiry, and HS256 algorithm with zero clock skew |
| Authorization     | Organizer policy accepts Organizer or Admin; Admin policy accepts only Admin                                                   |
| Caller context    | Implements BuildingBlocks user/profile interfaces from signed claims                                                           |
| HTTP results      | Translates Results into 200/201/204 or typed ProblemDetails; validation includes `errors`                                      |
| Unexpected errors | Logs exception details and returns a safe 500 body with trace ID instead of leaking a stack trace                              |

Health polling is excluded from normal server tracing. Health routes are not automatically authenticated. The Gateway does not call shared JWT setup; destination APIs validate tokens.
Shared HTTP discovery exists now; the configurable retry/circuit-breaker/attempt-timeout pipeline is planned, not implemented. Booking currently sets its own ten-second Catalog HTTP bound.

## Boundaries

AppHost assembles resources; ServiceDefaults supplies technical API hosting; BuildingBlocks supplies application contracts; services own business behavior.
Aspire does not merge databases, implement business authorization, or automatically provide creation idempotency.
See [Gateway](GATEWAY.md), [BuildingBlocks](BUILDING_BLOCKS.md), and [project structure](PROJECT_STRUCTURE.md) for their separate responsibilities.
