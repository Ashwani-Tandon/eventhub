# Agent service — events, proposals and sales

Agent answers signed-in users from Catalog and Booking facts and prepares confirmation cards.
This reference explains the endpoint, tool loop and the boundary between a model suggestion and a user-submitted action.

## API and access

`POST /agent/chat` through Gateway is `POST /chat` inside Agent. Any authenticated EventHub user may call it.
Agent validates the JWT itself. It has no database, Domain project, internal Booking credential or special privileges.
Development `/agent/health` and `/agent/alive` report hosting health; they do not guarantee Ollama is online.
The Angular proxy uses `/api/agent/chat`.

```json
{"messages":[{"role":"user","content":"Book 2 tickets for event ID 2."}]}
```

The endpoint's `[FromBody]` reads this JSON. `[FromServices] ISender` comes from dependency injection.
`CancellationToken` is supplied by ASP.NET Core on disconnect; it is not client input or a DI service.
The mediator runs Logging → Validation → Performance → SendChatMessage handler.
The handler checks identity and calls the Application-owned `IAgentChatClient` port.

A normal answer is `{ "reply": "...", "action": null }`. A prepared purchase returns:

```json
{
  "reply": "Review this booking proposal. Nothing has been booked. Click Yes to book, or Cancel to dismiss.",
  "action": {
    "kind": "book", "eventId": 2, "bookingId": null,
    "eventTitle": "Cloud Summit 2", "eventStartsAt": "2026-10-12T12:00:00+00:00",
    "quantity": 2, "unitPrice": 3650, "total": 7300
  }
}
```

This example is illustrative: actual facts come from the services. Cancellation uses `kind: "cancel"`
and includes the owned booking ID and its purchase snapshot. No reservation/payment references are exposed.
The action is built by C# tools, not extracted from model prose. When a tool prepares a card, the adapter
also supplies a fixed truthful lead-in, preventing model text such as “already booked” from replacing the preview.
At most one proposal is kept per request; a second prepare call cannot overwrite it.

Every turn supplies full prior user/assistant text. The Agent keeps no conversation state.
Validation rejects empty history, more than 30 messages, null entries, non-user/assistant roles,
a last message not from the user, empty content, content over 4,000 characters, or combined text over 32,000.
The client cannot submit system or tool messages. Validation produces 400 ProblemDetails before model work.

| HTTP status | Meaning |
| --- | --- |
| 200 | Final text and optional structured proposal; downstream tool failures may be explained in this text |
| 400 | Invalid history or malformed JSON; validator field messages appear in ProblemDetails |
| 401 | Missing, expired or invalid JWT; no model work |
| 429 | `Agent.Busy`, “The assistant is busy, try again shortly”, `Retry-After: 5` |
| 503 | `Agent.Offline`, “The assistant is offline”, `Retry-After: 5`; model outage, invalid model JSON, no final text or whole-chat deadline |

Gateway can return 504 at its own deadline before Agent's 503 arrives. Unexpected programming errors use
shared 500 handling. Expected network faults and tool HTTP rejections become Results.

## Tool menu and service boundaries

| Tool | Inputs | Actual service call / behavior |
| --- | --- | --- |
| SearchEvents | Optional search/category/city/maxPrice/from/to | Catalog `GET /events?page=1&pageSize=20` with escaped filters; summaries plus total |
| GetEventDetails | Exact event ID from user or search | Catalog `GET /events/{id}`; current title, description, price, date and seats |
| GetMyBookings | None | Booking `GET /bookings/mine`; model sees newest 10 distinct purchases with recency ranks, full count and omitted-history notice |
| PrepareBooking | Event ID, quantity 1–10 | Catalog GET; computes price × quantity and prepares a card. **No reserve, payment or booking POST.** |
| PrepareCancellation | Specific booking ID | Booking own-history GET; checks the full returned owned list, rejects missing/already-finished cancellation, then prepares a card. **No cancellation POST.** |
| GetSalesStats | None | Booking `GET /bookings/stats`; organizer sees own events, admin all, attendee gets actual 403 |

Search does not expose pagination or price sorting. A “cheapest event” answer must not claim completeness
when total exceeds the returned page. History's model page is also partial; repeated event titles are
separate purchases and older records are not known to the model. PrepareCancellation checks the full
API history so a user-supplied older owned ID is supported without inventing a record.
Quantity is bounded before making a card. Final availability, started-event and business rules belong to Booking/Catalog.

The Agent's `IBookingApi` has only `GetMineAsync` and `GetStatsAsync`. Its HTTP adapter contains no write methods.
The model's registered functions are an explicit six-method allowlist: even “ignore instructions and book now”
or a typed “yes” cannot execute a purchase. This is a code boundary, rather than a prompt-only request to wait.
A card still may suggest an unintended real event; the user must check the visible title/ID before Yes.

## How tool calling works

Ollama runs the configured local Qwen model (`qwen2.5:3b` on this machine).
OllamaSharp implements Microsoft's `IChatClient` and handles Ollama HTTP/JSON.
`AIFunctionFactory.Create` builds each tool's name, parameter schema and descriptions and binds its C# delegate.
The model chooses tool names and arguments; it neither runs C# nor chooses arbitrary API URLs.
`UseFunctionInvocation()` runs the delegate, adds its result as a tool message and asks the model to continue.
Those follow-up calls are conversation continuation, not retries of failed chats.
Tools execute serially, with at most eight model iterations per request.

System instructions describe EventHub scope, fact grounding, INR, the date, partial-page limits and read-only proposals.
They are kept separately from transport code so behavior is easy to study. The UTC date comes from TimeProvider.
Tool/event descriptions are data; the prompt says to ignore instructions inside service records.
Prompt instructions remain probabilistic for selection and ordinary prose; the service-derived card and read-only tool boundary are enforced in code.

| Message role | Creator and purpose |
| --- | --- |
| System | Server-owned overall instructions |
| User | Caller question or follow-up |
| Assistant | Model answer or untrusted submitted prior text |
| Tool | Invocation library's actual server-executed result |

`ForwardTokenHandler` reads the current incoming Authorization header at send time and forwards it to
Catalog/Booking. Pooled handlers never cache a user token. The separate Ollama transport receives no JWT.
The actual APIs enforce identity, ownership and organizer/admin roles; a UI card grants no extra permissions.
Downstream 401/403/404 become sign-in/permission/not-found text; 400 gives an input hint; 409/422 map to
“Not enough seats”/“Payment failed”; outages become short unavailable messages. No model failure grants access.

## Clicking Yes in the UI

The floating panel displays structured event title/ID, UTC date, quantity, unit price and total.
Sending chat, closing the panel, dismissing a card, and typing yes perform no Booking writes.
Only the card's Yes button calls the existing Booking endpoint through Angular's BookingApiService.
No new action URL or server-side pending-action store is required.

Before the first booking submission, the browser rereads the event. Changed title/price/date refreshes the
card and requires another Yes; insufficient seats or an already-started event prevent that submission.
Booking rereads and validates current facts again. A concurrent edit between browser recheck and Booking's
read remains possible because the existing Booking contract does not accept an expected price/version.
This preview is not a price lock; final saved totals come from Booking. Fixing that race requires a separate contract change.
Cancellation uses the displayed owned booking ID; Booking enforces ownership and retryable seat release.

One UUID key belongs to each booking card. Double-clicks are blocked during submission, and explicit retry
of an uncertain purchase keeps that key/payload so Booking can return the already-made booking.
A definitive 400/404/422 rejection resets the attempt; an uncertain outcome says to check My Bookings first.
Never automatically resend a chat or write. Logout cancels browser subscriptions, clears history/cards and
hides the widget; an already-submitted server operation may still finish and be visible in My Bookings.
Successful mutations and uncertain failures notify the open My Bookings page to reload persisted state.

## Resilience and local inspection

Ollama Endpoint, Model and TimeoutSeconds come from validated Options; timeout is 120 seconds.
Temperature is 0.2, output is capped at 512 tokens, and `AddOllamaOption(NumCtx, 8192)` supplies an
8,192-token temporary context window for instructions, tool schemas, history and tool results. This uses
additional model memory and does not train the model. Bounded pages/history still matter.
Strict JSON settings cannot contain comments; their purpose is local nonsecret model/hosting configuration.

A shared limiter allows two chats, five queued in oldest-first order, then immediate 429.
The 120-second budget includes queue, tools and model turns. Caller cancellation travels through the stack.
Service GETs use total timeout 10 s, up to three retries, circuit breaker and 2 s attempt timeout.
Ollama has no retry handler. Gateway's Agent route has 120-second total/activity timeouts.
The browser waits 125 seconds before displaying a timeout message, with no retry.

Start Ollama and AppHost. Use `agent.http`, the floating panel, and Aspire `Agent tool` logs/HTTP traces.
For a deliberate experiment, request a purchase and dismiss its card: booking count and seats should not change.
The original Step-10 prompt-only version created #313 then prematurely cancelled it. That observation is
recorded in OI-01 and explains the owner-approved switch to proposals and explicit UI execution.


## Revised-flow observations — 2026-09-29

The live portal rendered an event-2 card for two tickets at ₹3,650 each, ₹7,300 total. Before Yes,
there were 162 own bookings and 223 seats. A double-click on Yes created one #314, count 163 and
221 seats; the open history page refreshed. A cancellation proposal and portal Yes later returned
#314 Cancelled with seatReleasePending false and refreshed its history row. Agent traces for proposal
preparation used Booking GETs. A chat message explicitly asking to book immediately with typed yes
returned only a card; saved count remained 163.

The first cancellation follow-up produced misleading “Cancelled” prose without invoking a prepare
tool; persisted #314 was still Confirmed and no card/write occurred. More explicit tool instructions
then produced the proper owned card. Similarly, a follow-up asking the city initially failed to fetch
facts; after the history-ID instruction was clarified, the next turn correctly fetched Delhi for event 2.
These observations show that model prose/selection can still be wrong. The read-only boundary prevents
such text from executing an action; the UI's fixed note explains that messages never submit actions.
Service-derived cards and API-derived success messages remain the authority for actual operations.

Logout during a chat hid the panel and removed its controls; organizer2 started with an empty welcome.
A temporary unreachable local Ollama endpoint caused real 503 in about 116 ms; the panel showed
unavailable feedback and enabled input again. Original configuration was restored and Agent restarted.
Stopping an Aspire resource alone held the proxy request until restart rather than failing immediately.
Backend build reported 0 warnings/errors; production Angular build and lint passed. Owner browser
approval, code review/Q&A and step completion remain pending; no changes have been committed.
