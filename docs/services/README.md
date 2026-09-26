# Service API references

These references explain what each implemented service does and how to use its APIs.
Each includes endpoint behavior, request and response fields, access rules, and validation/error messages.

| Service  | Responsibility                                                           | Reference                             |
| -------- | ------------------------------------------------------------------------ | ------------------------------------- |
| Catalog  | Discover and manage events; protect capacity and seat reservations       | [Catalog API](catalog/API.md)         |
| Booking  | Purchase tickets, cancel purchases, and report booking history and sales | [Booking API](booking/API.md)         |
| Identity | Registration, login, signed profiles, user listing, and role management  | [Identity API](identity/API.md)       |
| Agent    | Authenticated service scaffold; chat and tools are not implemented yet   | [Agent status and APIs](agent/API.md) |

Public URLs use the gateway at `http://localhost:5100`. Identity issues the bearer token used by protected operations.
Catalog's internal seat APIs are service-to-service operations, not gateway APIs for a browser or attendee.

## Common infrastructure

| Reference                                        | Purpose                                                                               |
| ------------------------------------------------ | ------------------------------------------------------------------------------------- |
| [Project structure](common/PROJECT_STRUCTURE.md) | Repository map, all service projects, and layer responsibilities                      |
| [Gateway](common/GATEWAY.md)                     | Public routing, prefix transforms, and security boundaries                            |
| [BuildingBlocks](common/BUILDING_BLOCKS.md)      | Mediator, CQRS, pipeline behaviors, Result errors, and caller contracts               |
| [Aspire / ServiceDefaults](common/ASPIRE.md)     | Startup resources, dependencies, configuration, health, telemetry, and authentication |

These references describe implemented behavior and explicitly label future work.
