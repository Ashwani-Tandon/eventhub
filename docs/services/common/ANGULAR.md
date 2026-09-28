# Angular shell and authentication

The browser application provides account forms, event discovery, purchases, organizer tools, and administration.
This reference explains the implemented screens through Step-8c and their service boundaries.

## Run and structure

Run `npm install` in `web/` once after cloning. Start backend and Angular together with
`dotnet run --project src/Aspire/EventHub.AppHost`; the dashboard lists Angular as `web`, with its URL and console logs.
Open `http://localhost:4200`. `npm run build` produces the production bundle; `npm run lint` checks TypeScript and accessible templates.

`core/api/identity-api.service.ts` is the single HTTP adapter for Identity. `core/auth/auth.service.ts` owns session state;
`core/models` defines request and response contracts; `core/interceptors` adds authentication; `core/guards` controls navigation.
`shared/layout` owns the header and menu. `features/auth` owns account forms. Events, bookings, organizer, and admin folders own their implemented screens. Chat remains reserved for Step-11.

Standalone components declare their own imports. `OnPush` avoids unnecessary view work, while signals notify Angular when
identity, pending state, or errors change. Typed reactive forms keep fields and validators explicit without untyped values.
Angular Material supplies accessible form-field, input, and button behavior; our styles provide layout and a shared theme.
`jwt-decode` parses readable JWT claims; it does **not** verify a signature.

## HTTP and session flow

The browser calls `/api/identity/auth/login`, `/api/identity/auth/register`, and `/api/identity/auth/me`.
The development proxy matches `/api/**`, forwards to `http://localhost:5100`, and removes the leading `/api` through
`pathRewrite`. Angular's Vite proxy loader translates that JSON setting into its rewrite function. Restart the Aspire `web` resource after
changing proxy configuration. The gateway then removes `/identity` before forwarding to Identity. The browser sees the
same origin, so development needs no CORS configuration. Production hosting must provide the equivalent `/api` routing.
See [Angular's dev-server proxy documentation](https://angular.dev/tools/cli/serve).

Login submits email and password; registration additionally submits full name. The browser validates required values,
email format and length (256), full name length (100), and registration password length (8 minimum).
Identity still validates every request and always registers an Attendee. Pending forms disable submission. Field errors
appear beneath inputs; API validation and business errors appear in an alert. Wrong credentials show the generic
`Invalid email or password.`; duplicate registration shows Identity's detail; network/server failure asks the user to retry.
There are no automatic HTTP retries yet; they belong to Step-19.

The access token is stored under `eventhub.accessToken` in localStorage. AuthService exposes read-only `currentUser` and
`role` signals. It decodes the `sub`, `email`, `name`, `role`, and `exp` claims to restore the menu after refresh. Missing,
expired, or unsupported-role claims clear the session. An unreadable token supplies no identity and is sent to `/auth/me`
for rejection and removal through 401 handling. On startup, a stored token calls `/auth/me` so the
backend verifies its signature; a network outage alone does not discard the session. A late profile response after logout
is ignored. Login and registration restore the session from the returned token before navigating.

The interceptor reads storage for each relative `/api/` call and sets `Authorization: Bearer ...`. It does not send the
credential to external URLs. A 401 clears storage and signals and redirects to login. A failed login already on that page
keeps its form and return destination visible so its credential error is readable. Expiry schedules automatic logout at
JWT `exp`; logout clears that timer. The explicit Sign out button returns to Events. Logout removes the browser token;
there is no server-side token revocation in v1.0.

A guest visiting `/my-bookings` goes to login with `returnUrl=/my-bookings`; signing in returns there. Only local return
URLs are accepted, and login/register destinations fall back to Events. Wrong-role navigation goes to Events. All role
routes run the authentication guard first. Guards and menus are convenience controls; each backend independently
validates JWTs and authorizes requests. Altering browser state cannot grant API permissions.

| Identity  | Visible menu                                                   |
| --------- | -------------------------------------------------------------- |
| Guest     | Events                                                         |
| Attendee  | Events, My Bookings                                            |
| Organizer | Events, My Bookings, My Events, Dashboard                      |
| Admin     | Events, My Bookings, My Events, Dashboard, Users, All Bookings |

## localStorage versus HttpOnly cookies

localStorage makes token persistence and Bearer forwarding explicit for this learning project, as required by FR-UI-02.
JavaScript can read it, so injected scripts could steal a token. Avoid rendering untrusted HTML; Angular templates escape
ordinary text bindings. An HttpOnly cookie prevents JavaScript from reading the credential, but requires a different
server session/credential flow and consideration of cookie scope, SameSite, and cross-site request forgery. That is a
trade-off rather than a drop-in replacement for this Bearer-token API.

Roles are claims recorded at login. After Admin changes a stored role, an existing token and menu retain the previous
role until another login. `/auth/me` also reads the signed token, rather than reloading the current database role.

## Owner browser acceptance checks

AGENTS.md requires owner confirmation for the execution plan's 👤 criteria. Keep Step-6 In Progress until these are confirmed.

1. Sign in as each of the five demo users with `Demo@123`. Check the menu against the table above; sign out between accounts.
2. Refresh a signed-in page; the name and menu should remain.
3. Sign out and open `/my-bookings`; expect login. Sign in as an Attendee and open `/dashboard`; expect Events.
4. While signed in, use browser developer tools to set `localStorage.setItem('eventhub.accessToken', 'garbage')`.
   Refresh: `/auth/me` sends the garbage token, receives 401, and navigates to login. The header must lose the user's identity.
5. Register a unique email and a password of at least eight characters; expect automatic sign-in with the Attendee menu.
   Reusing the email should show the duplicate-email error. `npm run lint` must pass separately.

**Break it on purpose:** replace the stored token with `garbage` and refresh. The API rejects it, and the browser clears
its identity and displays login. This demonstrates why readable claims and a visible menu are not proof of authorization.

## Event discovery — Step-7a

`/events` is public. Search waits 300 ms while typing; category, city, local date bounds, and maximum INR price are sent
as query parameters to `GET /catalog/events`. Empty filters are omitted, preserving the API's upcoming-events default.
The date range includes the whole chosen local end day. Invalid price/date ranges show feedback without sending a request.
Changing a filter resets paging to one; clearing restores defaults. Twelve cards are requested per page, and the server's
`total` determines page count. An older request is cancelled before a new search so it cannot overwrite newer results.
Cards show price, venue, local time, current seats, and a sold-out badge. Category artwork is decorative CSS, not a supplied
photo of the venue. `/events/:id` reads Catalog details directly and displays API availability; past events remain readable.
Both routes show loading, empty, or API error feedback. See [Catalog APIs](../catalog/API.md) for filtering rules.

## Purchases and history — Step-7b

Book tickets sends guests to login with a local return URL. A signed-in user opens a Material dialog with an integer
quantity from one through the smaller of ten or current seats. Total updates from quantity times displayed price; Booking
still fetches the trusted event price and owns payment/reservation decisions. Started/sold-out events cannot begin a booking.
`POST /booking/bookings` sends event ID, quantity, and the development failure switch. The pending request disables submit
and dialog dismissal. Success shows a snackbar, closes the dialog, and reloads Catalog seats. 409 shows the business conflict,
422 explains payment failure, and 503 explains temporary unavailability; the dialog stays open on error.

The clearly labelled payment-failure checkbox is guarded by Angular's `isDevMode()`; production builds hide it and always
send false. Payments are simulated by the configured backend `FakePaymentGateway`, with no external charge.
Step-19 will add a stable per-attempt idempotency key and GET retry policy; this batch does not claim those protections.

`/my-bookings` reads only `GET /booking/bookings/mine`, using copied event title/date/price rather than requiring Catalog.
Confirmed and cancelled rows remain visible. Cancellation asks for confirmation and calls `POST /bookings/{id}/cancel`.
Pending cancellation disables other cancel actions. A cancellation error still refreshes history because the cancellation
may already be durably saved with `seatReleasePending=true`; that row offers Retry seat return, including after its event
has started. Completed cancellation reloads server state. Event details fetch fresh seats when revisited, and a successful
purchase reloads the currently open details page. See [Booking APIs](../booking/API.md) for compensation and pending release.

## Event management — Step-8a

`/my-events` calls `GET /catalog/events/mine`; Organizer sees their own events, Admin sees all and an Organizer ID column.
The table includes date, price, capacity, sold seats and actions. The Admin column displays the owner GUID rather than
pretending Catalog contains an organizer name. Create and edit are `/my-events/new` and `/my-events/:id/edit`.
Typed forms validate title/description length, category, nonnegative price, whole-number capacity (1–10000 and at least
already booked seats), and a future create/changed date. Unchanged historical dates are allowed on edit. Displayed input
is local time; submissions use UTC. An unchanged date keeps the original server timestamp including its seconds.

Create calls POST; edit calls PUT with the rowVersion originally read. A 409 displays the server's stale-edit message
and disables saving until Reload latest event, which explicitly discards unsaved edits. No silent overwrite is attempted.
Deletion uses a named confirmation dialog and DELETE; a booked-seat 409 is shown and the row remains. Successful changes
return to/refetch the managed list. The public list fetches current data when revisited. Server validators and ownership
remain authoritative even if browser controls are bypassed.

## Dashboard — Step-8b

`/dashboard` combines `GET /booking/bookings/stats` with `GET /catalog/events/mine`. Both scopes are enforced by the backend.
Revenue and tickets include Confirmed purchases; bookings include cancellations. Average fill is the arithmetic mean of
each managed event's seatsBooked/capacity ratio, displayed as a percent (zero with no events). It is not a capacity-weighted
average. Revenue is INR. Independent loads let sales remain visible if the event-capacity request fails.

Three charts show six calendar months of revenue, the top ten events by confirmed tickets, and confirmed/cancelled counts.
`ngx-echarts` owns Angular chart lifecycle and resize handling; Apache ECharts draws bars and doughnut segments. Only bar,
pie, canvas, tooltip, grid, legend, and accessibility modules are registered. The dashboard route lazy-loads these libraries,
keeping them out of the initial account/browsing bundle. Charts resize with their containers; screen-reader summaries and
monthly numeric values accompany canvas output. Refresh reloads both sources; no-data and failed-request feedback are explicit.
The shared panel-state/retry treatment remains Step-19 work.

## Administration — Step-8c

`/admin/users` lists Identity safe profiles. Each row has a typed role selector and explicit Save, which calls
`PUT /identity/users/{id}/role`. Saving one row disables other save actions, and success updates the stored-role display.
The note "applies at next login" is visible before saving: the menu of an already signed-in user retains its token's role.
`/admin/bookings` calls `GET /booking/bookings`, with the optional Confirmed/Cancelled status parameter. The table is read-only;
Admin cannot cancel another person's purchase. Purchaser IDs are shown because Booking owns snapshots, not Identity profiles.
Both routes require authGuard followed by Admin roleGuard; the services independently return 403 to non-Admin callers.

## Batch verification and review

The owner requested all screens through Step-8c before reviewing features together (2026-09-27),
then confirmed the built UI works on 2026-09-28 and requested a commit. That owner confirmation
provides the browser acceptance evidence for Steps 6–8c. Production build and lint passed;
the solution build reported zero warnings and errors. Earlier agent verification of booking writes
was stopped by automatic approval review; the owner subsequently verified the application directly.
Build/lint, earlier live API/UI observations, and final owner approval are recorded in the execution plan.
No tests were added. Step-19 and chat are outside this batch.
