# Presenting EventHub

This script helps the owner demonstrate EventHub through user journeys and explain the code behind them.
Use it with the running portal and Aspire dashboard; it is a rehearsal guide, not a claim that the final rehearsal has passed.

## Before presenting

Start Docker, Ollama and AppHost using [README.md](README.md). Wait for all resources to become healthy.
Open the portal and Aspire dashboard in separate tabs. Check an upcoming event has seats; note its ID, title,
price and organizer. Clear the chat and sign out. Keep `resilience.http` open in VS Code REST Client.
Rehearse your chosen prompts once to warm the model; clear history afterwards. Allow time for local inference.

Use a full-size browser window so the chat and confirmation card are easy to read. Avoid changing event data
or resetting SQL during the presentation. Fake payment has a 10% decline rate: a decline is expected behavior,
not a broken demo. After a definite payment decline, make a new attempt; after an uncertain network error,
check My Bookings and retry the same card/key rather than creating another purchase.

Opening sentence: **“EventHub is a local event-booking platform that demonstrates service ownership,
role-based security, safe booking workflows and an AI assistant using the same APIs as the user.”**

## Eight-minute main demo

| Time | Show | Say / expected result |
| --- | --- | --- |
| 0:00–0:45 | Browse Events logged out; login as attendee | Browsing is public. Booking requires login. Menus reflect roles; backend authorization enforces them. |
| 0:45–1:45 | Open an upcoming event and book one ticket | Show quantity/total and the saved row in My Bookings. Booking reads price from Catalog, reserves seats, processes fake payment, then saves. |
| 1:45–2:30 | Repeat using the development payment-failure checkbox | The decline returns 422 and releases reserved seats. Refresh details and show seats unchanged by the failed attempt. This checkbox exists only in development. |
| 2:30–4:00 | Resilience experiment below | Show retries in a trace, then the circuit opening, My Bookings still available, and recovery. |
| 4:00–4:45 | Organizer login → dashboard | Sales are scoped to that organizer's events. Booking stores event snapshots, so history/stats do not require live Catalog reads. |
| 4:45–6:30 | Attendee login → chat search, proposal, Yes | The model selects a described tool; C# executes a read. Card facts come from the service. Nothing is purchased until UI Yes. Show the saved booking. |
| 6:30–7:00 | Ask attendee chat for sales stats | It must refuse. If it calls the stats tool, Booking returns 403; a model-only refusal may have no downstream trace. |
| 7:00–8:00 | Aspire trace of the normal booking; briefly open code | Follow gateway → Booking → Catalog. Explain endpoint → mediator/validation → handler → ports/adapters. End with the read-only Agent and explicit UI confirmation boundary. |

For an agent cancellation demo, use an extra minute: get the exact newly saved booking ID from My Bookings,
ask to prepare its cancellation, inspect the card, click Yes and show the saved Cancelled status and returned seats.

## Chat prompts that make the flow clear

Replace bracketed IDs with the event/booking you actually inspected; do not paste the brackets literally.

1. “Show details for event [event ID], including title, city, price and available seats.”
2. “Fetch the city for that same event using its event ID.”
3. “Prepare a new booking for 1 ticket for event [event ID].” Inspect ID/title/date/total, then click Yes.
4. “Prepare cancellation for my booking [booking ID].” Inspect the card before Yes.
5. As attendee: “Fetch my sales statistics.” As organizer: “Fetch sales statistics for my events.”

You can also demonstrate “Find upcoming Tech events in Delhi under 4000 rupees.” For “cheapest event”,
explain that the model must search, compare results and use the selected event ID. Search is paginated;
a returned page is not proof of the global cheapest event. Avoid claiming guaranteed optimal selection.

If the model misunderstands, say: “The local model can choose a tool incorrectly or phrase a response badly.
The card shows service facts, and the assistant has no write tools. I can correct the request before confirming.”
Do not treat model prose such as “booked” as proof: the authoritative saved result and My Bookings are proof.

## Resilience experiment

Use [Booking resilience requests](src/Services/Booking/Booking.Api/resilience.http). Login before stopping Catalog.
Stop only Catalog in Aspire. Submit its outage booking request and inspect Booking logs for retries sharing
one trace ID; submit another until the breaker opens. Subsequent requests should fail quickly with 503.
Show `GET /booking/bookings/mine` still returns saved records while Catalog is unavailable.
Restart Catalog, wait at least 15 seconds, and send recovery with a **fresh Idempotency-Key**. Observe recovery.

Practical caveat: Aspire's service proxy can hold a connection while its target is stopped. On this machine,
that can demonstrate attempt timeouts instead of an immediate connection refusal; exact timings vary.
Describe the actual trace. If recovery takes longer than the allotted segment, restart Catalog, show the
existing retry/breaker logs and continue only once it is healthy. Always rehearse this segment locally first.

Service policy: 2-second attempt timeout inside a 10-second total budget; up to 3 retries with exponential
backoff starting at 200 ms and jitter. Breaker opens at 50% failures, minimum 5 attempts over 10 seconds,
and stays open 15 seconds. Only safe/idempotent calls retry. The browser retries reads at 500 ms and 1 second;
booking/cancellation/chat writes are never automatically retried by the browser. Gateway rate limiting is deferred.

## Code walkthrough: one booking through chat

1. `web/src/app/features/chat/chat-widget.ts`: `send` sends current text and history through `AgentApiService`.
2. `src/Services/Agent/Agent.Api/Program.cs`: the chat endpoint sends the command through the mediator.
3. `Agent.Application/Features/SendChatMessage/`: validator checks inputs; handler calls `IAgentChatClient`.
4. `Agent.Infrastructure/OllamaChatClient.cs`: instructions, user/history and tool schemas go to Ollama.
   The model returns a tool name/arguments. Microsoft.Extensions.AI function invocation dispatches the registered C# method.
5. `Agent.Application/Tools/EventHubTools.cs`: `PrepareBooking` reads actual Catalog details and builds a proposal;
   tools use the caller's token. It cannot submit a booking. The chat reply contains prose plus an optional structured action.
6. Back in `chat-widget.ts`, UI Yes rereads event facts before a first submission, updates changed details for another Yes,
   then calls the existing Booking endpoint with a stable per-card key. There is still a small price-change race after reread;
   this is not an atomic price lock.
7. `Booking.Application/Features/`: booking handler claims the key before side effects, reads Catalog, reserves,
   pays and saves. Infrastructure implements HTTP/EF; Domain holds booking rules.
8. The UI displays the Booking API result and refreshes My Bookings. Model wording does not generate that success message.

Paths above are repository-relative for a cloned copy; see the [project map](docs/services/common/PROJECT_STRUCTURE.md)
for complete file locations.

## Short answers for questions

| Question | Answer you can give in under a minute |
| --- | --- |
| Why microservices / separate databases? | Each service owns its rules and data. Services collaborate through APIs instead of joining another database. This adds failure modes, which the demo makes visible. |
| Why a gateway if APIs validate JWTs? | YARP provides one browser entry point and routes. Each destination remains responsible for security. Forwarding a token preserves the caller's identity. |
| Why Clean Architecture / your mediator? | Use cases depend on small interfaces, not EF or HTTP. Thin endpoints send commands/queries. Our mediator makes logging and validation explicit without a commercial mediator package. |
| What is CQRS here? | Separate commands and queries with handlers, using the same database. There is no separate read store or message bus. |
| Why Result / migrations? | Expected business failures become structured HTTP errors. Committed EF migrations evolve persistent schemas; startup applies them before seeding. |
| HS256 versus RS256? | Local APIs share a symmetric signing key; each could therefore sign tokens. With RS256, an issuer keeps the private key while APIs validate with a public key. A production identity provider would be preferable. |
| REST versus messaging? | REST keeps this learning scope explicit. Messaging/outbox could improve asynchronous recovery, but introduces delivery, ordering and duplicate handling; it is outside v1.0. |
| Compensation versus saga? | A payment/save failure triggers a seat release; there is no transaction across databases. Pending cancellation is saved and replay retries release. No background recovery worker or saga is claimed. |
| Why copied event fields? | Saved booking snapshots let history and revenue work while Catalog is down. They describe the purchase rather than automatically following every event edit. |
| What prevents double booking? | A durable user/key claim is made before side effects. Reservation IDs deduplicate Catalog writes. UI disables repeated clicks, but the backend also handles concurrent/replayed keys. |
| JWT versus internal credential? | JWT identifies the user. The separate credential proves Booking is calling internal seat operations; these routes are absent from the public gateway. |
| Does C# choose the best tool? | The model chooses a tool name and arguments from schemas/descriptions. The invocation library dispatches it; C# validates and executes it. There is no string-matching router pretending to understand text. |
| What do system instructions / context mean? | Instructions describe the assistant's role and tool rules. The 8192-token context window limits text the model can consider, including history and tool data; it is not an event count. |
| Why local Qwen? | Ollama runs qwen2.5:3b on this memory-constrained Mac. It avoids an external paid model, but tool selection and language are less reliable. |
| Is UI confirmation enough for security? | Backend authorization is the security boundary. Agent ports/tools are read-only; UI Yes performs the real write using the user's token and a booking key. |

Detailed trade-offs and API errors live in [service references](docs/services/README.md).

## Rehearsal and release

Run the script twice. Record actual date, selected IDs, successful/failure observations and any timing adjustments
in Step-12 of the execution plan. Practise each answer aloud, using your own words. A second clean rehearsal and
explaining the design choices are owner acceptance criteria; they are not checked merely because this document exists.

After those criteria and fresh-folder startup/reset verification pass, complete Step-12 and tag the release `v1.0`.
Until then, the reviewed features can be committed while the release step remains In Progress.
