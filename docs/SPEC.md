# EventHub — Specification

> **This file is the source of truth.** Code is built to match it, and every step in
> `EXECUTION_PLAN.md` is verified against the requirement IDs below.
> If the implementation needs to differ from this spec, update the spec **first** (and record why in
> `DECISIONS.md`), then change the code. Never the other way round.

---

## 1. Purpose

EventHub is a learning project that is also a presentable demo. People discover events and book tickets;
organizers publish events and track sales; an AI assistant can search and book on the user's behalf.

It exists to understand, hands-on:

- microservices with a gateway and one database per service
- authentication (JWT) and authorization (roles **and** ownership)
- synchronous service-to-service calls and their failure modes
- an AI agent embedded in .NET, using a free local LLM, acting with the user's own permissions

## 2. Scope

**In scope:** everything in sections 4–10.

**Out of scope (deliberately):**

| Not built | Why | Talking point |
|---|---|---|
| Pub/sub, message broker (RabbitMQ) | Keep the core simple; REST only | When and why you would add it (DECISIONS) |
| Saga / distributed transactions | Replaced by manual compensation | Where compensation breaks down |
| External identity provider (Keycloak, Entra ID) | Own Identity service teaches how tokens work | Production would use one |
| Refresh tokens, password reset, email | Time | — |
| Real payments | Fake payment only | — |
| RAG / document search, MCP | Time | Natural next agent features |
| Cloud deployment | Everything runs locally | — |

## 3. Technology (all free, all local, Mac M1)

| Area | Choice |
|---|---|
| Backend | .NET 10, ASP.NET Core Minimal APIs, EF Core |
| Orchestration | .NET Aspire (AppHost + ServiceDefaults + dashboard) |
| Gateway | YARP with Aspire service discovery |
| Database | SQL Server 2022 container (via Aspire, runs under Rosetta), one database per service |
| Frontend | Angular (standalone components, signals), Angular Material, ngx-echarts |
| LLM | Ollama, model `qwen2.5:7b` (or `qwen2.5:3b` on 8 GB RAM) |
| Agent library | Microsoft.Extensions.AI + OllamaSharp (Microsoft Agent Framework comparison is a "Later" item, L-1) |
| Resilience | Microsoft.Extensions.Http.Resilience (Polly v8), EF Core retrying execution strategy (chaos strategies and gateway rate limiting are "Later" items L-4, L-2) |
| Architecture | Clean Architecture per service, CQRS with a hand-written mediator (`EventHub.BuildingBlocks`), Result pattern |
| Validation | FluentValidation (Apache 2.0) |
| API testing | `.http` files (VS Code REST Client) |

**Package rule:** no commercially-licensed packages. Avoid MediatR v13+ (we write our own mediator —
§15), AutoMapper v15+ (map by hand), MassTransit v9+.

**No tests of any kind in v1.0.** Verification is done by running the system: `.http` requests, the
Aspire dashboard, and browser checks (see each step's acceptance criteria).

## 4. Architecture

```
Angular (4200) ──/api/* dev proxy──► Gateway (5100)
                                        ├─ /identity/* ─► Identity API ─► identitydb
                                        ├─ /catalog/*  ─► Catalog API  ─► catalogdb
                                        ├─ /booking/*  ─► Booking API  ─► bookingdb
                                        │                    └─REST (user token)─► Catalog API
                                        └─ /agent/*    ─► Agent API ─► Ollama (11434)
                                                             └─REST (user token)─► Catalog, Booking
Aspire AppHost starts all of the above and the SQL Server container.
```

**Architecture rules**

| ID | Rule |
|---|---|
| AR-01 | Each service owns exactly one database. No service references another service's DbContext, tables or connection string. |
| AR-02 | Every service validates the JWT itself. The gateway routes; it is not the security boundary. |
| AR-03 | The UI and all external callers go through the gateway only. |
| AR-04 | Service-to-service calls use Aspire service discovery names (`http://catalog`), never hard-coded ports. |
| AR-05 | Service-to-service calls forward the **calling user's** JWT, so every downstream check applies to the real user. |
| AR-06 | The Agent has no database and no special privileges; it can only do what the logged-in user can do. |
| AR-07 | Secrets (JWT key, persistent SQL password, internal service credential) live in user-secrets / Aspire parameters, never in committed files. |
| AR-08 | Inside each service the code follows Clean Architecture + CQRS (§15). Services share only `EventHub.BuildingBlocks` (technical plumbing, no business rules), `EventHub.ServiceDefaults`, and the dev-only `EventHub.SeedData`. |
| AR-09 | Catalog seat reservation endpoints are internal: the gateway does not route them, and Catalog requires both the calling user's JWT and an `X-EventHub-Service` credential supplied only by Booking. The user JWT remains the authorization/audit identity; the service credential proves the caller is Booking. |

## 5. Roles and permissions

Roles: **Guest** (not logged in), **Attendee**, **Organizer**, **Admin**.

| Capability | Guest | Attendee | Organizer | Admin |
|---|:-:|:-:|:-:|:-:|
| Browse / search / view events | ✅ | ✅ | ✅ | ✅ |
| Register, log in | ✅ | — | — | — |
| Book tickets, view & cancel **own** bookings | — | ✅ | ✅ | ✅ |
| Create events | — | — | ✅ | ✅ |
| Edit / delete an event | — | — | **own only** | any |
| Sales dashboard | — | — | **own events** | all events |
| View all bookings | — | — | — | ✅ |
| List users, change roles | — | — | — | ✅ |
| Use the AI assistant | — | ✅ | ✅ | ✅ |

Authorization policies: `Organizer` = role Organizer **or** Admin. `Admin` = role Admin.
Ownership is checked in code (resource-based), in addition to the policy.

## 6. Identity service

### 6.1 Data — `identitydb`

**Users**: `Id` (GUID, PK) · `Email` (unique, max 256) · `FullName` (max 100) · `PasswordHash` · `Role` (`Attendee` | `Organizer` | `Admin`) · `CreatedAt`

### 6.2 Requirements

| ID | Requirement |
|---|---|
| FR-ID-01 | Register with email, full name, password (min 8 chars). New users are always `Attendee`. Duplicate email → 409. |
| FR-ID-02 | Login with email + password returns an access token. Wrong email **or** password → 401 with the same generic message. |
| FR-ID-03 | Token: JWT, HMAC-SHA256, issuer `eventhub-identity`, audience `eventhub`, lifetime 2 h, claims `sub` (user id), `email`, `name`, `role`. |
| FR-ID-04 | `GET /auth/me` returns the current user from the token. |
| FR-ID-05 | Admin can list users and change a user's role. A role change takes effect at that user's next login (existing tokens keep the old role). |
| FR-ID-06 | Passwords are stored only as hashes (ASP.NET Core `PasswordHasher`). |

### 6.3 API (behind gateway prefix `/identity`)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| POST | `/auth/register` | Public | `{ email, fullName, password }` | 201 `LoginResponse` | 400, 409 |
| POST | `/auth/login` | Public | `{ email, password }` | 200 `LoginResponse` | 400, 401 |
| GET | `/auth/me` | Logged in | — | 200 `UserDto` | 401 |
| GET | `/users` | Admin | — | 200 `UserDto[]` | 401, 403 |
| PUT | `/users/{id}/role` | Admin | `{ role }` | 200 `UserDto` | 400, 401, 403, 404 |

`LoginResponse` = `{ accessToken, expiresAt, user: UserDto }` · `UserDto` = `{ id, email, fullName, role }`

## 7. Catalog service

### 7.1 Data — `catalogdb`

**Events**: `Id` (int, identity) · `Title` (max 120) · `Description` (max 2000) · `Category` · `Venue` · `City` · `StartsAt` (UTC) · `Price` (decimal 10,2, INR) · `Capacity` (int) · `SeatsBooked` (int) · `OrganizerId` (GUID) · `CreatedAt` · `RowVersion` (SQL `rowversion`, concurrency token)

**SeatReservations**: `Id` (GUID, PK — supplied by the caller) · `EventId` · `UserId` · `Quantity` · `Status` (`Held` | `Released`) · `CreatedAt` · `ReleasedAt`
This table is what makes reserve/release **idempotent**: the same reservation id applied twice changes seats only once.

Categories (fixed): `Music`, `Tech`, `Sports`, `Comedy`, `Workshop`.

### 7.2 Requirements

| ID | Requirement |
|---|---|
| FR-CAT-01 | Anyone can list events with filters: `search` (title/description contains), `category`, `city`, `from`, `to`, `maxPrice`, `page` (default 1), `pageSize` (default 12, max 50). Default shows upcoming events only, ordered by date. Response `{ items, total }`. |
| FR-CAT-02 | Anyone can get one event, including `seatsLeft = Capacity − SeatsBooked`. Unknown id → 404. |
| FR-CAT-03 | Organizer creates an event; `OrganizerId` = caller's `sub`. |
| FR-CAT-04 | Only the owner or an Admin can update or delete an event (else 403). |
| FR-CAT-05 | `GET /events/mine` returns the caller's events (all events for Admin), including past ones. |
| FR-CAT-06 | Validation: title required; price ≥ 0; capacity 1–10000; `StartsAt` in the future on create, and on update only when the date is changed; known category; capacity never below `SeatsBooked`. Violations → 400 with field errors. |
| FR-CAT-07 | Reserve seats is **atomic and idempotent**. Booking supplies a `reservationId`; Catalog records the user `sub`. If that id already exists with the same user, event and quantity → return the existing result (200) without touching seats; a mismatched replay → 409. Otherwise, in one transaction: a single conditional update (`Capacity − SeatsBooked ≥ quantity`) plus insert of the `Held` reservation; not enough seats → 409. A concurrent duplicate-key insert rolls back its seat update and loads the winning reservation. Two concurrent requests for the last seat cannot both succeed. |
| FR-CAT-08 | Release is **idempotent** by `reservationId` and user: a `Held` reservation becomes `Released` and its seats are returned once; releasing an already-released or unknown reservation → 200 with no change. A reservation owned by a different user → 403. `SeatsBooked` never goes negative. |
| FR-CAT-09 | An event with `SeatsBooked > 0` cannot be deleted → 409. |
| FR-CAT-10 | Event updates use **optimistic concurrency**: `EventDto` carries `rowVersion`; an update sent with a stale `rowVersion` → 409 "This event was changed by someone else — reload and try again". |

### 7.3 API

Public routes are behind gateway prefix `/catalog`. `/internal/*` routes are reachable only by service discovery and are not configured in YARP.

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/events` | Public | query filters | 200 `{ items: EventDto[], total }` | 400 |
| GET | `/events/{id}` | Public | — | 200 `EventDto` | 404 |
| GET | `/events/mine` | Organizer | — | 200 `EventDto[]` | 401, 403 |
| POST | `/events` | Organizer | `EventInput` | 201 `EventDto` | 400, 401, 403 |
| PUT | `/events/{id}` | Organizer + owner/Admin | `EventInput` + `rowVersion` | 200 `EventDto` | 400, 401, 403, 404, 409 |
| DELETE | `/events/{id}` | Organizer + owner/Admin | — | 204 | 401, 403, 404, 409 |
| POST | `/internal/events/{id}/reservations` | User JWT + Booking credential; not routed by gateway | `{ reservationId, quantity }` | 200 `{ reservationId, status }` | 401, 403, 404, 409 |
| POST | `/internal/reservations/{reservationId}/release` | User JWT + Booking credential; not routed by gateway | — | 200 `{ reservationId, status }` | 401, 403 |

`EventInput` = `{ title, description, category, venue, city, startsAt, price, capacity }`
`EventDto` = `EventInput` + `{ id, seatsBooked, seatsLeft, organizerId, rowVersion }` (`rowVersion` base64)

## 8. Booking service

### 8.1 Data — `bookingdb`

**Bookings**: `Id` (int, identity) · `UserId` (GUID) · `EventId` (int) · `EventTitle` · `EventStartsAt` · `OrganizerId` (GUID) · `Quantity` · `UnitPrice` · `Total` · `Status` (`Confirmed` | `Cancelled`) · `PaymentRef` · `ReservationId` (GUID) · `IdempotencyKey` (string, nullable, unique per `UserId`) · `SeatReleasePending` (bool) · `CreatedAt`

**BookingRequests**: `Id` (GUID) · `UserId` · `IdempotencyKey` · `EventId` · `Quantity` · `ReservationId` · `State` (`Processing` | `Completed`) · `BookingId` (nullable) · `CreatedAt` · unique (`UserId`, `IdempotencyKey`). This is the atomic claim made **before** Catalog or payment is called. It gives every retry the same reservation/payment identity and prevents concurrent requests from performing side effects twice.

`EventTitle`, `EventStartsAt` and `OrganizerId` are **copied** from Catalog at booking time, so Booking can
list and aggregate without calling Catalog for every row (deliberate data duplication).

### 8.2 Requirements

| ID | Requirement |
|---|---|
| FR-BKG-01 | After any idempotency claim required by FR-BKG-08, create a booking in this exact order: (1) get the event from Catalog — missing → 404, already started → 400; (2) use the claim's stable `reservationId` (or generate one when no key was supplied) and reserve seats in Catalog — no seats → 409; (3) fake payment; (4) on payment failure **release the reservation (compensation)** and return 422; (5) save the booking as `Confirmed` with its `reservationId` → 201; if saving fails, release the reservation before returning the error. Quantity 1–10. |
| FR-BKG-02 | Fake payment succeeds 90% of the time and returns a reference like `PAY-XXXXXXXX`. It is idempotent for the booking request identity: repeating the same claimed request returns the same payment result/reference. In Development only, request field `simulatePaymentFailure: true` forces failure (for demos). |
| FR-BKG-03 | `GET /bookings/mine` returns the caller's bookings, newest first. |
| FR-BKG-04 | Cancel: owner only (else 403); only `Confirmed` bookings for events that have not started can begin cancellation (else 400). In one local transaction set `Cancelled` and `SeatReleasePending = true`, then release seats idempotently in Catalog, then clear the flag. If Catalog is unavailable, return 503 and leave the flag true; repeating cancel on that booking retries only the release. A completed cancellation replay returns the existing cancelled booking. This deliberately provides retryable eventual consistency without a distributed transaction. |
| FR-BKG-05 | Admin can list all bookings, optional `status` filter. |
| FR-BKG-06 | Stats: Organizer sees only bookings for their events (`OrganizerId` = caller); Admin sees all. Returns totals, revenue per month (last 6 months, zero-filled), top 10 events by tickets, and counts per status. Revenue counts `Confirmed` only. |
| FR-BKG-07 | If Catalog is unreachable (after retries) or its circuit is open, return 503 `"Catalog service unavailable"` with a `Retry-After` header — never an unhandled 500. |
| FR-BKG-08 | `POST /bookings` accepts an `Idempotency-Key` header. Before any external call, atomically claim (`UserId`, key) in `BookingRequests`, with one stable `ReservationId`. A repeat with the same event and quantity waits briefly if the winner is still processing, then returns the **original** booking (same 201 and body) without reserving or charging again; the fake payment also uses the key. Reusing a key with a different event or quantity → 409. Known failed attempts (400/404/409/422) compensate and remove the claim so the same key can be tried again. A stale `Processing` claim is safely resumable with the same reservation/payment identities. |
| FR-BKG-09 | `GET /bookings/mine` and `GET /bookings/stats` do **not** call Catalog, so they keep working while Catalog is down (benefit of the copied data in §8.1). |

### 8.3 API (behind gateway prefix `/booking`)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| POST | `/bookings` | Logged in | header `Idempotency-Key` (optional) · `{ eventId, quantity, simulatePaymentFailure? }` | 201 `BookingDto` | 400, 401, 404, 409, 422, 503 |
| GET | `/bookings/mine` | Logged in | — | 200 `BookingDto[]` | 401 |
| POST | `/bookings/{id}/cancel` | Logged in (owner) | — | 200 `BookingDto` | 400, 401, 403, 404, 503 |
| GET | `/bookings` | Admin | `?status=` | 200 `BookingDto[]` | 401, 403 |
| GET | `/bookings/stats` | Organizer | — | 200 `StatsDto` | 401, 403 |

`StatsDto` = `{ totals: { revenue, ticketsSold, bookings, cancelled }, revenueByMonth: [{ month: "YYYY-MM", revenue }], topEvents: [{ eventId, title, tickets, revenue }], statusCounts: [{ status, count }] }`

## 9. Agent service

### 9.1 What it is

An endpoint that sends the conversation plus a list of **tools** to the local LLM. The LLM either answers
or asks for a tool call; the service runs the tool (a normal HTTP call to Catalog/Booking **with the user's
token**), sends the result back, and repeats until the LLM gives a final answer
(Microsoft.Extensions.AI function-invocation loop).

### 9.2 Tools

| Tool | Calls | Available from |
|---|---|---|
| `SearchEvents(search?, category?, city?, maxPrice?, from?, to?)` | `GET /events` | Step-9 |
| `GetEventDetails(eventId)` | `GET /events/{id}` | Step-9 |
| `GetMyBookings()` | `GET /bookings/mine` | Step-9 |
| `BookTickets(eventId, quantity)` | `POST /bookings` | Step-10 |
| `CancelBooking(bookingId)` | `POST /bookings/{id}/cancel` | Step-10 |
| `GetSalesStats()` | `GET /bookings/stats` | Step-10 |

Tool results are compact JSON (only the fields the model needs). Errors are returned as short text
(`"Forbidden"`, `"Not enough seats"`, `"Payment failed"`) so the model can explain them.

### 9.3 Requirements

| ID | Requirement |
|---|---|
| FR-AGT-01 | `POST /chat` (logged in) receives the full message history `{ messages: [{ role: "user"|"assistant", content }] }` and returns `{ reply }`. The service keeps no conversation state. |
| FR-AGT-02 | System prompt: EventHub assistant; answers only from tool data, never invents events, prices or bookings; knows today's date; currency INR; stays on EventHub topics and politely declines others. |
| FR-AGT-03 | Before `BookTickets` or `CancelBooking`, the assistant states event, quantity and total and waits for the user to confirm. |
| FR-AGT-04 | All tool calls carry the user's JWT. A 403 from a downstream service is reported to the user as "you don't have permission". |
| FR-AGT-05 | Every tool call and its arguments are visible in the logs / Aspire dashboard. |
| FR-AGT-06 | Model name and Ollama endpoint come from configuration (`Ollama:Model`, `Ollama:Endpoint`). Temperature 0.2. |
| FR-AGT-07 | LLM calls have a 120 s timeout and are **never retried automatically** — a retried chat could run a booking tool twice. At most 2 LLM calls run at once (queue 5); beyond that → 429 "The assistant is busy, try again shortly". Ollama unreachable → 503 "The assistant is offline". |
| FR-AGT-08 | `BookTickets` sends an `Idempotency-Key` (one new key per tool call), so a retried tool HTTP call cannot book twice. |

**Security note:** FR-AGT-03 is a user-experience guard only. The real protection is FR-AGT-04 + AR-05/06 —
the APIs enforce permissions no matter what the model decides.

## 10. Frontend (Angular)

| ID | Screen / behaviour | Who |
|---|---|---|
| FR-UI-01 | Login and Register pages with validation and error messages | Guest |
| FR-UI-02 | Token kept in localStorage; attached to every API call by an interceptor; 401 → logout + redirect to login; auto-logout at expiry | All |
| FR-UI-03 | Menu shows only what the role may use (Events · My Bookings · My Events · Dashboard · Users · All Bookings) | All |
| FR-UI-04 | Route guards: logged-in pages redirect guests to login; role pages redirect others to Events | All |
| FR-UI-05 | Events list with filter bar (search debounced, category, city, dates, max price), cards, paging, empty and loading states | All |
| FR-UI-06 | Event details with seats left; "Sold out" badge; Book button (guests are sent to login and returned) | All |
| FR-UI-07 | Booking dialog: quantity 1–10 and ≤ seats left, live total, confirm; clear messages for 201 / 409 / 422 / 503. In Development only, a clearly labelled demo toggle sends `simulatePaymentFailure: true`; it is absent from production builds. | Logged in |
| FR-UI-08 | My Bookings table with cancel + confirmation | Logged in |
| FR-UI-09 | My Events table and create/edit form (same rules as FR-CAT-06), delete with confirmation, 409 message shown | Organizer, Admin |
| FR-UI-10 | Dashboard: KPI tiles (revenue, tickets sold, bookings, average fill % — computed in the UI from `/events/mine`) and 3 charts: revenue by month (bar), top 10 events (horizontal bar), status split (donut) | Organizer, Admin |
| FR-UI-11 | Users page with role dropdown (note: applies at next login); All Bookings page with status filter | Admin |
| FR-UI-12 | Chat widget: floating button, panel, history kept for the session and sent in full, "thinking…" indicator, errors shown, hidden when logged out, **cleared on logout** | Logged in |
| FR-UI-13 | Booking submit sends a new `Idempotency-Key` per booking attempt (reused if the same attempt is re-sent); the Confirm button is disabled while the request is in flight | Logged in |
| FR-UI-14 | GET requests retry up to 2 times with backoff (500 ms, 1 s) on network errors / 502 / 503 / 504; POST/PUT/DELETE are never retried by the UI | All |
| FR-UI-15 | Graceful degradation: each page region handles its own failure — a failed panel shows "Temporarily unavailable" with a Retry button while the rest of the page keeps working; 429 shows "Too many requests, wait a moment"; 409 on event edit shows the reload message | All |

The dev server proxies `/api/*` to `http://localhost:5100` (prefix removed), so no CORS configuration is needed.

## 11. Seed data

| ID | Requirement |
|---|---|
| SD-01 | Seeding runs at startup only when the table is empty (idempotent). |
| SD-02 | Users (fixed GUIDs, password `Demo@123`): `admin@demo.com` (Admin), `organizer@demo.com` (Organizer), `organizer2@demo.com` (Organizer), `attendee@demo.com` (Attendee), `attendee2@demo.com` (Attendee). |
| SD-03 | ~40 events: ~30 upcoming (next 60 days) and ~10 past (last 6 months), split across both organizers, 5 categories, 5 cities, realistic titles, prices ₹0–₹5000. At least one upcoming event is sold out so the UI state is deterministic to demonstrate. |
| SD-04 | ~300 bookings across past and upcoming events and both attendees, ~10% `Cancelled`, spread over 6 months so charts look real. |
| SD-05 | Catalog `SeatsBooked` equals the sum of `Confirmed` quantities in Booking for each event. Both seeds come from one deterministic generator (class library `EventHub.SeedData`, fixed random seed) — the only shared code between services, dev-only. |

## 12. Non-functional

| ID | Requirement |
|---|---|
| NFR-01 | One command starts the backend: `dotnet run --project src/Aspire/EventHub.AppHost`. Angular: `npm start` in `web/`. |
| NFR-02 | Fixed ports: Gateway 5100 (not 5000 — macOS AirPlay), Angular 4200, Ollama 11434. Others assigned by Aspire. |
| NFR-03 | API responses < 500 ms locally (excluding the agent). |
| NFR-04 | Agent reply < 30 s for a warm model. Gateway timeout for `/agent` is 120 s. |
| NFR-05 | Every API step has a `.http` file covering its success and error cases. |
| NFR-06 | Errors use ProblemDetails (`application/problem+json`) with a readable `title`. |

## 13. Glossary

**JWT** — signed token carrying who the user is (`sub`) and their role · **Gateway** — single entry point
that routes by URL prefix · **Service discovery** — finding a service by name instead of address ·
**Compensation** — undoing an earlier step when a later one fails · **Tool calling** — the LLM asking the
application to run a named function with arguments · **Agent** — LLM + tools + the loop that runs them ·
**ReAct** — the reason → act (call a tool) → observe → repeat pattern our agent loop follows ·
**Idempotent** — doing it twice has the same effect as doing it once · **Retry with backoff + jitter** —
try again after a growing, slightly random delay · **Circuit breaker** — stop calling a failing service for
a while and fail fast · **Bulkhead** — cap concurrent work so one slow thing cannot exhaust everything ·
**Rate limiting** — cap requests per caller per time window · **Chaos testing** — injecting faults on
purpose to prove the resilience works.

## 14. Resilience

Failures are normal in a distributed system: a service restarts, the network blips, the database is
briefly busy, the laptop is slow. These rules make EventHub survive them — and make it **observable**
when it does.

### 14.1 Where each pattern is used

```
Angular ──(FR-UI-14 GET retry, FR-UI-15 degradation, FR-UI-13 idempotency key)──►
Gateway ──(RES-04 agent route timeout)──►
Booking ──(RES-01 pipeline: timeout → retry → circuit breaker)──► Catalog
   │                                                                           (RES-06 idempotent reserve/release,
   └─(RES-05 DB retry)──► bookingdb                                             RES-07 optimistic concurrency)
Agent ──(FR-AGT-07 timeout + bulkhead, NO retry)──► Ollama
Agent ──(RES-01 pipeline, FR-AGT-08 idempotency key)──► Catalog / Booking
```

### 14.2 Requirements

| ID | Requirement |
|---|---|
| RES-01 | Every service-to-service `HttpClient` uses one shared resilience pipeline (defined in ServiceDefaults, `Microsoft.Extensions.Http.Resilience`), outermost first: **total timeout 10 s → retry** (max 3, exponential backoff from 200 ms with jitter, on `HttpRequestException`, timeouts, 408, 429, 5xx) **→ circuit breaker** (opens at ≥ 50 % failures over 10 s with at least 5 calls; stays open 15 s; then half-open trial) **→ attempt timeout 2 s**. Values live in configuration. |
| RES-02 | Retries apply only to **safe or idempotent** requests: all GETs; Catalog reserve/release (idempotent by `reservationId`, FR-CAT-07/08); Booking `POST /bookings` **with** an `Idempotency-Key` (FR-BKG-08). Every other POST/PUT/DELETE has retry disabled. |
| RES-03 | When a downstream call fails after retries or the circuit is open, the caller returns 503 ProblemDetails naming the unavailable service, with `Retry-After`. Never an unhandled 500. |
| RES-04 | Nothing waits forever: gateway agent route timeout 120 s (a 30 s default for other routes is part of Later L-2); EF Core command timeout 15 s; `HttpClient` limits per RES-01; LLM per FR-AGT-07. |
| RES-05 | Database transient faults are retried by EF Core's SQL Server retrying execution strategy (on by default in the Aspire SQL integration). Any explicit transaction runs inside `CreateExecutionStrategy().ExecuteAsync(...)`. |
| RES-06 | Idempotency: `reservationId` for seat operations (FR-CAT-07/08); `Idempotency-Key` for bookings (FR-BKG-08, FR-UI-13, FR-AGT-08). |
| RES-07 | Optimistic concurrency on event edits (FR-CAT-10); atomic seat reservation (FR-CAT-07). |
| RES-08 | *Later (L-2):* Gateway rate limiting (ASP.NET Core rate limiter): `POST /identity/auth/login` and `/register` — 10 per minute per IP (fixed window); all other routes — token bucket of 100 per minute per user (or per IP if anonymous); `/agent/*` — 10 per minute per user. Exceeding a limit → 429 ProblemDetails with `Retry-After`. |
| RES-09 | Bulkhead on the Agent: max 2 concurrent LLM calls, queue 5 (FR-AGT-07). |
| RES-10 | Health: every service exposes `/health` (ready — includes a database check where the service has one) and `/alive` (liveness); Aspire shows them. *Later (L-2):* a gateway health endpoint that reports each downstream service. |
| RES-11 | *Later (L-4):* Chaos testing (Development only): configuration `Chaos:Enabled`, `Chaos:FaultRate` (0–1), `Chaos:LatencyMs` adds Polly chaos fault and latency strategies **inside** Booking's Catalog client pipeline, so retries and circuit-breaker behaviour can be triggered on demand and seen in Aspire traces. Off by default. |
| RES-12 | Graceful degradation: My Bookings and stats work while Catalog is down (FR-BKG-09); the Events pages work while Booking is down; the dashboard degrades per panel (FR-UI-15). |
| RES-13 | Every retry attempt, circuit-breaker state change (opened / half-open / closed), timeout and rate-limit rejection is logged with the trace id and visible in the Aspire dashboard. |

### 14.3 Traps to remember

- **Retrying a non-idempotent call is a bug, not resilience.** Without `reservationId`, a retried reserve
  whose first response was lost would hold seats twice. That is why RES-02 exists.
- **Retries multiply load.** 3 retries × every caller hitting a struggling service makes it worse — the
  circuit breaker and jitter exist to stop that.
- **Timeouts must nest.** Attempt timeout (2 s) < total timeout (10 s) < gateway timeout (30 s). An outer
  timeout shorter than the inner ones makes retries pointless.
- **Never retry the LLM chat call** (FR-AGT-07): the model may already have run a booking tool.
- **Out of scope:** the transactional outbox and message-based retries belong with pub/sub (SPEC §2).

## 15. Code architecture and practices

### 15.1 Clean Architecture inside each service

Each business service (Identity, Catalog, Booking) is split into four projects. Dependencies point
**inwards only**:

```
        ┌──────────────────────────────── Api ────────────────────────────────┐
        │  Minimal API endpoints, DI composition root, auth policies            │
        │   ┌────────────────────── Infrastructure ──────────────────────┐     │
        │   │  EF Core DbContext + repositories, HTTP clients, JWT,       │     │
        │   │  password hashing, fake payment, seeding                    │     │
        │   │   ┌──────────────── Application ────────────────┐          │     │
        │   │   │  Commands, Queries, Handlers, Validators,    │          │     │
        │   │   │  DTOs, interfaces (ports) the outside fills  │          │     │
        │   │   │   ┌──────────── Domain ────────────┐         │          │     │
        │   │   │   │  Entities, rules, domain errors │         │          │     │
        │   │   │   └─────────────────────────────────┘         │          │     │
        │   │   └──────────────────────────────────────────────┘          │     │
        │   └─────────────────────────────────────────────────────────────┘     │
        └───────────────────────────────────────────────────────────────────────┘
```

| ID | Rule |
|---|---|
| CA-01 | **Domain** references nothing except `EventHub.BuildingBlocks` (Result, Error, Entity base). No EF Core, no ASP.NET Core, no HTTP. |
| CA-02 | **Application** references Domain and BuildingBlocks only. It defines service-specific interfaces (ports) for anything external: `IEventRepository`, `IEventQueries`, `IUnitOfWork`, `ICatalogClient`, `IPaymentGateway`, `IJwtTokenGenerator`, `IPasswordHasher`. The cross-service technical abstraction `ICurrentUser` lives in BuildingBlocks and is implemented from `HttpContext` by ServiceDefaults, avoiding three incompatible service-specific interfaces. |
| CA-03 | **Infrastructure** implements those interfaces (EF Core, `HttpClient` + resilience, JWT, hashing, fake payment, seed). It references Application. |
| CA-04 | **Api** is the composition root: registers everything, maps endpoints, applies policies. Endpoints are thin — build a command/query, `Send` it, map the `Result` to HTTP. No business logic in endpoints. |
| CA-05 | Entities are never returned from the API; handlers return DTOs (C# `record`s). Mapping is hand-written. |
| CA-06 | Business rules live in the Domain (e.g. `Booking.BeginCancellation()` refuses a new cancellation if it is already cancelled or the event has started, while the handler separately recognizes idempotent/pending-release replays; `Event.Update()` enforces capacity ≥ seats booked). The atomic seat reservation (FR-CAT-07) is enforced in the database for concurrency, behind `IEventRepository.TryReserveAsync` — documented in DECISIONS as a deliberate exception. |
| CA-07 | **Agent** has no Domain layer (no business data of its own): `Agent.Application` (chat use case, tool definitions, ports `ICatalogApi`, `IBookingApi`), `Agent.Infrastructure` (Ollama, HTTP clients), `Agent.Api`. |
| CA-08 | **Gateway** is a single project — it is infrastructure with no business logic. |
| CA-09 | *Later (L-3):* architecture tests (NetArchTest) that fail the build if any of CA-01…CA-04 is broken. In v1.0 the project references themselves are the guard. |

### 15.2 CQRS and the mediator

| ID | Rule |
|---|---|
| CQ-01 | Every use case is either a **Command** (changes state, returns `Result` or `Result<T>`) or a **Query** (reads only, returns `Result<T>`, never changes state). |
| CQ-02 | Commands load aggregates through repositories, call domain methods, and save via `IUnitOfWork`. Query handlers depend on narrow read ports owned by Application (for example `IEventQueries`); Infrastructure implements those ports with EF Core `AsNoTracking()` projections straight to DTOs. No `IQueryable` or entity crosses the port. Same database for both (CQRS-lite; no separate read store). |
| CQ-03 | `EventHub.BuildingBlocks` contains shared technical plumbing: the hand-written mediator (`ICommand`, `ICommand<T>`, `IQuery<T>`, handlers, `ISender.Send(...)`, `IPipelineBehavior<TRequest,TResponse>`), Result/Error, and the small `ICurrentUser` request-context abstraction. It contains no business rules. Handlers are registered by assembly scanning. |
| CQ-04 | Pipeline behaviors, in order: **Logging** (request name, duration, outcome) → **Validation** (runs FluentValidation validators; failure returns `Result` with a validation error, the handler is not called) → **Performance** (warns when a request takes > 500 ms, NFR-03). |
| CQ-05 | One folder per use case (vertical slice inside Application): `Features/Events/CreateEvent/` holds `CreateEventCommand`, `CreateEventCommandHandler`, `CreateEventCommandValidator`. |
| CQ-06 | **Result pattern:** expected failures (not found, validation, conflict, forbidden, unavailable, payment failed) are returned as `Result` with an `Error { Code, Message, Type }` — not thrown. `ServiceDefaults` maps `Error.Type` to HTTP status + ProblemDetails in one place. Exceptions are only for the unexpected and are caught by a global exception handler (→ 500 ProblemDetails, logged). |

**Use cases per service**

| Service | Commands | Queries |
|---|---|---|
| Identity | `RegisterUser`, `Login`, `ChangeUserRole` | `GetCurrentUser`, `ListUsers` |
| Catalog | `CreateEvent`, `UpdateEvent`, `DeleteEvent`, `ReserveSeats`, `ReleaseReservation` | `SearchEvents`, `GetEventById`, `GetMyEvents` |
| Booking | `CreateBooking`, `CancelBooking` | `GetMyBookings`, `ListBookings`, `GetBookingStats` |
| Agent | `SendChatMessage` | — |

### 15.3 SOLID — where each principle shows up

| Principle | Where you will see it |
|---|---|
| **S** — Single responsibility | One handler per use case; validators separate from handlers; endpoints only translate HTTP |
| **O** — Open/closed | New cross-cutting behaviour = a new pipeline behavior, no handler changes; new agent tool = a new method, loop unchanged |
| **L** — Liskov substitution | `FakePaymentGateway` and any real gateway are interchangeable behind `IPaymentGateway`; any implementation of a port can replace another |
| **I** — Interface segregation | Small ports (`ICatalogClient` exposes only the 3 calls Booking needs; `ICurrentUser` only id + role) |
| **D** — Dependency inversion | Application depends on interfaces it owns; Infrastructure implements them; Api wires them |

### 15.4 Coding practices

| ID | Practice |
|---|---|
| CP-01 | `Directory.Build.props`: `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, .NET analyzers on. `Directory.Packages.props`: central package versions. `.editorconfig` for style. |
| CP-02 | Async all the way with `CancellationToken` passed through every layer. No `.Result` / `.Wait()`. |
| CP-03 | Classes `sealed` by default; commands, queries and DTOs are `record`s; no public setters on entities. |
| CP-04 | Configuration through the Options pattern with validation at startup (`ValidateDataAnnotations().ValidateOnStart()`) — JWT, resilience and Ollama settings. |
| CP-05 | Time comes from `TimeProvider` (testable); no `DateTime.Now` in Domain or Application. |
| CP-06 | Structured logging with message templates (`"Booking {BookingId} created"`), never string concatenation; no secrets or passwords in logs. |
| CP-07 | No magic strings: roles, policies, claim names, error codes are constants. |
| CP-08 | ~~Unit tests with every step~~ — **removed by the owner** (adds complexity; this is a learning build). Verification is by running the system. No test projects in v1.0. |
| CP-09 | Angular: standalone components, signals, `OnPush` change detection, strict TypeScript, typed reactive forms, no `any`; folders `core/` (auth, interceptors, guards), `shared/` (reusable UI), `features/` (events, bookings, organizer, admin, chat); one API service per backend service; ESLint clean. |
| CP-10 | Database schemas use committed EF Core migrations and `MigrateAsync()` at development startup; `EnsureCreated()` is not used. Seeding runs only after migrations complete. Each schema-changing step adds its own migration so persistent local data can move forward without deleting the volume. |

### 15.5 Solution layout

```
EventHub/
├─ AGENTS.md · CLAUDE.md                         agent entry points (kept at root for discovery)
├─ Directory.Build.props · Directory.Packages.props · .editorconfig · EventHub.sln
├─ docs/  README.md · SPEC.md · EXECUTION_PLAN.md · DECISIONS.md · LEARNING.md · PROJECT_STRUCTURE.md
├─ src/
│  ├─ BuildingBlocks/EventHub.BuildingBlocks/        mediator, CQRS interfaces, behaviors, Result/Error, Entity base, current-user abstraction
│  ├─ Aspire/EventHub.AppHost/ · EventHub.ServiceDefaults/   (auth, resilience, Result→HTTP mapping, telemetry)
│  ├─ Gateway/EventHub.Gateway/
│  ├─ Services/
│  │  ├─ Identity/  Identity.Domain · Identity.Application · Identity.Infrastructure · Identity.Api
│  │  ├─ Catalog/   Catalog.Domain  · Catalog.Application  · Catalog.Infrastructure  · Catalog.Api
│  │  ├─ Booking/   Booking.Domain  · Booking.Application  · Booking.Infrastructure  · Booking.Api
│  │  └─ Agent/     Agent.Application · Agent.Infrastructure · Agent.Api
│  └─ Tools/  EventHub.SeedData · Agent.Playground
└─ web/   (Angular)
```
