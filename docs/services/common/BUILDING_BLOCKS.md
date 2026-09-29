# BuildingBlocks — shared application primitives

BuildingBlocks provides the small technical contracts used by all business services, not shared business entities.
This reference explains the mediator, validation/logging pipeline, Result errors, and caller identity abstractions that make service behavior consistent.

## What belongs here

Project: `src/BuildingBlocks/EventHub.BuildingBlocks`. It has no HTTP endpoints, database, payment adapter, or event/booking rules.
An Event or Booking belongs to its own service; putting business models here would couple services to each other's changes.

| Component                                | Purpose                                               | How a service uses it                                      |
| ---------------------------------------- | ----------------------------------------------------- | ---------------------------------------------------------- |
| `Entity<TId>`                            | Common typed identity base                            | Domain entities inherit it                                 |
| `IRequest<TResponse>`                    | Describes a request and response type                 | Base contract for commands/queries                         |
| `ICommand<T>`, `ICommand`                | Write-use-case contracts                              | Purchase, cancellation, event edits                        |
| `IQuery<T>`                              | Read-use-case contract                                | Lists, profiles, statistics                                |
| Request/command/query handler interfaces | Give each request one implementation                  | Service handlers execute the use case                      |
| `ISender` / `Sender`                     | Dispatch a request to its handler                     | Thin endpoints call `Send`                                 |
| `IPipelineBehavior`                      | Wrap dispatch with shared processing                  | Logging, validation, timing                                |
| `Result`, `Result<T>`, `IResult`         | Success or expected error without throwing            | Handlers return business failures explicitly               |
| `Error`, `ErrorType`                     | Stable code, message, category, optional field errors | APIs later translate to HTTP                               |
| `ICurrentUser`, `ICurrentUserProfile`    | Caller identity/role and token profile contracts      | Handlers access identity without ASP.NET Core dependencies |

## Request flow

An endpoint creates a command/query and calls `ISender.Send` with the framework request cancellation token.
The registered order is Logging → Validation → Performance → Handler. Logging observes entry/outcome; validation can stop processing before business side effects; timing wraps the downstream handler.
FluentValidation supplies validation rules defined in each service. Failure groups messages by property and returns `Validation.Failed`, detail `One or more validation errors occurred.` The handler is not invoked.
Performance uses `TimeProvider` and warns for requests taking more than 500 ms; it does not impose a timeout or retry.

`AddMediator` scans the supplied service assemblies during registration, registers handlers, and builds dispatch adapters.
Runtime dispatch uses a typed adapter/dictionary lookup, not reflection-based mapping of business objects. Mapping between entities and DTOs remains explicit in the owning service.

The mediator is hand-written to keep dispatch and validation visible for this learning project and avoid adding the commercially licensed MediatR v13+ dependency.
CQRS here means separate command/query use cases, not separate read/write databases or event sourcing. The trade-off is maintaining these small dispatch primitives ourselves.

## Expected errors versus exceptions

Result errors express expected conditions such as invalid input, missing event, forbidden ownership, payment decline, or unavailable dependency.
Unexpected exceptions remain exceptions and are handled by the hosting layer. BuildingBlocks does not know HTTP; the translation lives in ServiceDefaults:

| Error type    | HTTP mapping               |
| ------------- | -------------------------- |
| Validation    | 400; includes field errors |
| Unauthorized  | 401                        |
| Forbidden     | 403                        |
| NotFound      | 404                        |
| Conflict      | 409                        |
| Unprocessable | 422                        |
| Unavailable   | 503                        |
| RateLimited   | 429                        |

`Result<T>` success maps to 200 (or 201 for a creation endpoint). A successful result with no payload maps to 204.
These mappings describe endpoint responses, not APIs hosted by BuildingBlocks itself.

## Identity and dependency boundaries

ServiceDefaults implements the current-user contracts using validated JWT claims. Application sees only the interfaces; it does not reference ASP.NET Core.
Domain/Application must not depend on EF Core or concrete HTTP clients. Ports describe needed operations and Infrastructure supplies their implementations.

See [service references](../README.md) for actual validation messages and [Aspire / ServiceDefaults](ASPIRE.md) for authentication, ProblemDetails, health, and telemetry hosting.

Agent uses `ErrorType.RateLimited` when its two active slots and five waiting slots are full. The shared Result mapper translates it to 429 ProblemDetails, so the Application handler remains independent of ASP.NET response types. Agent adds `Retry-After: 5` at its HTTP boundary.
