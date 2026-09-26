# Gateway — routing and API entry point

The Gateway gives clients one backend address and forwards requests to the service that owns each API.
This reference explains its actual routes, prefix rewriting, security boundaries, and responses; it owns no business rules or database.

## Public address and routes

Local address: `http://localhost:5100`. Port 5000 is avoided because macOS AirPlay uses it.
YARP is the reverse-proxy library: it matches configured paths and forwards method, body, headers, and response between caller and service.

| Incoming path                                  | Destination | Forwarded path                 |
| ---------------------------------------------- | ----------- | ------------------------------ |
| `/identity/{rest}`                             | Identity    | `/{rest}`                      |
| `/catalog/events` and `/catalog/events/{rest}` | Catalog     | `/events` and `/events/{rest}` |
| `/catalog/health`                              | Catalog     | `/health`                      |
| `/booking/{rest}`                              | Booking     | `/{rest}`                      |
| `/agent/{rest}`                                | Agent       | `/{rest}`                      |

For example, `POST /booking/bookings` becomes `POST /bookings` in Booking; `GET /identity/auth/me` becomes `GET /auth/me` in Identity.
The destinations use Aspire-discovered service names (`http://identity`, `http://catalog`, `http://booking`, `http://agent`) rather than fixed service ports.
No generic `/catalog/*` route is configured: `/catalog/alive` is not exposed, although `/alive` exists directly on Catalog in Development.

## Security and error behavior

The Gateway does not issue or validate JWTs; each destination API validates the forwarded bearer token and applies its own authorization policies.
`/catalog/internal` and every nested path are rejected with 404 before proxying, case-insensitively. Clients must never use the internal service credential through the public gateway.
Unmatched paths return 404. Matched service responses retain their status and validation/business error body; see the service references for exact messages.
Proxy connection failures can produce gateway errors such as 502; they are not the same as Booking's controlled 503 when Booking cannot reach Catalog.
Rate limiting, dedicated proxy timeouts, and business request validation are not currently implemented here.

## Operational endpoints and code location

Gateway's own `/health` and `/alive` are mapped in Development through ServiceDefaults. They describe the Gateway, not an aggregate guarantee that every downstream API is healthy.
`src/Gateway/EventHub.Gateway/Program.cs` composes the proxy and internal-route rejection. `appsettings.json` declares route matches, prefix transforms, and clusters; launch settings select port 5100.

See [all service references](../README.md), [Aspire](ASPIRE.md), and [project structure](PROJECT_STRUCTURE.md) for how routing fits the system.
