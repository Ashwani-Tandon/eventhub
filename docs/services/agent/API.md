# Agent service — read-only assistant

Agent turns a signed-in user's question into an answer grounded in Catalog and Booking data.
This reference explains its API, tool loop, permissions and failure handling so the implementation can be studied alongside the code.

## What is implemented

The service has Application, Infrastructure and Api projects. It owns no database or Domain project.
`qwen2.5:3b`, already installed in Ollama for this 8 GB machine, supplies language understanding and answer writing.
We are integrating a pretrained model, not training one or inserting records into its permanent knowledge.

| Endpoint | Access | Result |
| --- | --- | --- |
| `POST /agent/chat` through Gateway (`POST /chat` inside Agent) | Any authenticated EventHub user | `200 { "reply": "..." }`, or the errors below |
| `/agent/health`, `/agent/alive` | Public, Development only | Shared readiness and liveness checks; no database check and no guarantee that Ollama is online |

Step 9 exposes only read tools. Booking, cancellation and sales tools belong to Step 10; the Angular chat widget belongs to Step 11.
The actual entry point is Gateway at `http://localhost:5100`; the future widget will use Angular's `/api/agent/chat` proxy path.

## Request and response

Postman: select POST, enter `http://localhost:5100/agent/chat`, set Authorization to Bearer Token using Identity's `accessToken`, and send JSON under Body → raw:

```json
{
  "messages": [
    { "role": "user", "content": "Music events under ₹1000?" }
  ]
}
```

The endpoint marks this JSON explicitly `[FromBody]`. `ISender` is `[FromServices]` dependency injection.
`CancellationToken` is supplied by ASP.NET Core when the request disconnects; it is not a JSON field, header or dependency the caller supplies.

A successful response contains only the final natural-language text:

```json
{ "reply": "Jazz Evening 16 is free and has 223 seats available." }
```

That is an illustrative response, not a fixed answer: availability, events and wording can change.
Every subsequent request includes all earlier user/assistant messages plus the new user message. The service stores no history.
The client sends text, not tool-call objects. The Agent creates fresh system instructions and obtains current tool data on each request.

Validation runs before model work through our mediator and FluentValidation:

- `messages` must exist and contain at least one message: `'Messages' must not be empty.`
- At most 30 messages: `Send at most 30 messages.`
- Entries cannot be null: `Messages cannot be null.`
- Roles are exactly `user` or `assistant`: `Role must be user or assistant.` Clients cannot replace system instructions or fabricate tool results.
- The final message must be a user message: `The last message must be from the user.`
- Each `content` is nonempty and at most 4,000 characters; combined text is at most 32,000 characters: `Conversation text must not exceed 32000 characters.`

These bounds keep accidental huge histories from overwhelming the local model. They limit text characters, not token counts: a token is a small piece of text used by the model internally.
Malformed JSON and invalid types also receive HTTP 400 from ASP.NET Core.

| Status | Meaning / body detail |
| --- | --- |
| 200 | Final answer, including a readable explanation if a tool reports no matches or a downstream failure |
| 400 | `Validation.Failed` ProblemDetails with field messages; the model is not called |
| 401 | Missing, invalid or expired bearer token; authentication stops the request before the endpoint |
| 429 | `Agent.Busy`: `The assistant is busy, try again shortly`; `Retry-After: 5` |
| 503 | `Agent.Offline`: `The assistant is offline`; connection/model-service failure, malformed model JSON, no final text, or whole-chat timeout; `Retry-After: 5` |

Unexpected programming errors still go through the shared exception handler as 500; expected network failures are translated.
A 503 is deliberately a simple user message; model connection/timeout information goes into logs without JWTs.

## Follow one question through the code

1. Gateway removes `/agent` and forwards the body and bearer token to Agent.
2. Agent validates the JWT itself. `AgentEndpoints` sends `SendChatMessageCommand` through Logging → Validation → Performance → handler.
3. The handler checks caller identity and calls the Application-owned `IAgentChatClient` port. `OllamaChatClient` implements it in Infrastructure.
4. The adapter starts a 120-second budget and acquires a shared concurrency permit. It builds a system message, including today's UTC date from `TimeProvider`, then appends the submitted history.
5. It supplies three named functions to Microsoft.Extensions.AI. `[Description]` attributes become descriptions of the methods and arguments in the JSON tool menu sent to Ollama.
6. Ollama may return a function request such as `SearchEvents` with `category: "Music"` and `maxPrice: 1000`. This is a request to our program, not direct database access by the model.
7. `UseFunctionInvocation()` runs the corresponding C# method. `EventHubTools` logs the name and arguments, calls its port, and returns compact JSON or a short error.
8. The HTTP adapter resolves Catalog/Booking through Aspire service discovery and forwards the original user's JWT. The downstream API checks that user's permission and reads its own database.
9. The function-invocation layer adds the tool result to the current conversation and asks Ollama to continue. It may request another tool or write the final answer.
10. Only the final assistant text becomes `{ reply }`; the permit is released on success, failure or cancellation.

These follow-up model calls are conversation continuation, not automatic retries of a failed chat.
The loop allows at most eight model iterations, with serial tool execution inside each request. This caps accidental repeated tool calls; the total deadline still applies.
Ollama receives tool facts in the current request context. Its installed weights are not changed by these HTTP calls.

## Message roles and library responsibilities

| Role | Who creates it | Purpose |
| --- | --- | --- |
| System | Agent | Grounding rules, supported scope, INR currency, current date |
| User | Caller | The question or follow-up |
| Assistant | Model, or caller's prior history | Earlier answers; submitted history is not verified business data |
| Tool | Function-invocation layer | Actual result of a server-executed tool |

`OllamaSharp.OllamaApiClient` handles Ollama's HTTP/JSON protocol and implements Microsoft's `IChatClient` interface.
`Microsoft.Extensions.AI` supplies that provider-independent interface, `AIFunctionFactory` and the invocation loop.
We explicitly bind three methods; we do not scan all public methods and expose them to the model.
`UseLogging()` adds library request diagnostics. Application tool logs at Information level ensure names and arguments appear in Aspire; HTTP bearer tokens are never logged by our code.
See Microsoft's [IChatClient and tool-calling reference](https://learn.microsoft.com/en-us/dotnet/ai/ichatclient) and [OllamaSharp's integration reference](https://github.com/awaescher/OllamaSharp#usage-with-microsoftextensionsai).

## Tool contracts and permissions

| Tool | Arguments | Downstream call | Result |
| --- | --- | --- | --- |
| `SearchEvents` | Optional `search`, `category`, `city`, `maxPrice`, `from`, `to` | `GET Catalog /events?page=1&pageSize=20` plus escaped filters | `{ items, total }`; event IDs, titles, city, category, dates, prices, seats left; descriptions and venue come from details |
| `GetEventDetails` | `eventId` from a real search | `GET Catalog /events/{id}` | Current event facts, or `Not found` |
| `GetMyBookings` | None | `GET Booking /bookings/mine` | Newest 10 caller-owned snapshots plus total and `olderBookingsOmitted`: recency rank, booking ID, event ID/title, quantity, total, status, pending seat release |

`search` means title/description text, while `category` means Music, Tech, Sports, Comedy or Workshop.
A category-only search must not add the category word as a text filter; doing so would omit events whose edited title and description do not contain that word.
Dates use ISO 8601 with a timezone; no `from` means Catalog's upcoming-event default. Prices use invariant formatting in URLs and INR in answers.
At most 20 matches are returned; `total` describes all matches. Pagination beyond that first page is not exposed as a tool in Step 9.
Booking retrieves the caller’s history through its existing endpoint, then the tool sends only the newest 10 records to Ollama with the full count and `olderBookingsOmitted`. The demo user has 161 bookings; sending all 161 overwhelmed the default 4,096-token context and hit the deadline. Twenty records also led to incorrect latest-record selection. The final ten-record page includes explicit `recencyRank` and `bookingId` fields; event dates can be obtained with `GetEventDetails`. This is a deliberate local-model trade-off: the assistant must say when an older record is outside the available page. Search summaries likewise omit long descriptions; use details for one event.
No organizer IDs, user IDs, payment references, reservation IDs or event row versions are sent as tool facts.

The model does not supply a user ID or bearer token. `ForwardTokenHandler` reads the current HTTP request when sending each tool call; it does not cache the identity in the pooled HTTP handler.
The Ollama transport is separate and does not receive the user's JWT. The Agent has no Booking internal service credential and no access to any service database.
A tool's downstream 403 becomes `You don't have permission`; 401 becomes `Please sign in again`; 404 becomes `Not found`; invalid filters and outages become short readable errors.
The model can explain those errors in a 200 chat answer; it must not invent missing data.

The system prompt tells the model to obtain facts through tools, treat event descriptions as data, and decline unrelated requests.
This is a language-model instruction, not a deterministic security boundary or a guarantee against every hallucination. Actual access control is the three-tool allowlist plus JWT checks in each API.

## Resilience and configuration

`Ollama:Endpoint`, `Ollama:Model` and `Ollama:TimeoutSeconds` are in Agent's `appsettings.json`; environment variables may override them.
Endpoint must be an absolute HTTP(S) URL, model must be present, and timeout is validated as 120 seconds. Temperature is 0.2, and each model answer is limited to 512 output tokens.
JSON has no comment syntax, so this reference supplies its purpose explanation: it contains local nonsecret model/hosting settings; JWT secrets still come from Aspire.

A singleton `ConcurrencyLimiter` is shared across users: two whole chat workflows can run, five may queue in oldest-first order, and excess requests receive 429.
The budget includes queue time, model turns and tool calls. Client cancellation also cancels queued/running work; the acquired permit is disposed in all cases.
Serial invocation within one chat prevents concurrent access to that request's tool context; separate permitted chats still run concurrently.
This is a limit for this Agent instance, not a distributed limit across multiple servers.

Catalog and Booking GET clients use `AddEventHubResilience`: total timeout 10 s, up to three retries with backoff/jitter, circuit breaker and 2 s attempt timeout, from shared configuration.
Ollama uses a separate transport with no retry pipeline. Repeating a whole chat automatically could repeat future action tools; manual retry is the caller's choice.
The Gateway Agent route has a 120-second total timeout and a 120-second activity timeout, backed by ASP.NET Core request-timeout middleware. Other routes have no newly added timeout policy.
Both Agent and Gateway start their budgets independently; at the deadline the Gateway can return 504 before Agent's 503 arrives. See [YARP timeout documentation](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/timeouts).

## Running and inspecting it

Start Ollama, then `dotnet run --project src/Aspire/EventHub.AppHost`. Send [agent.http](../../../src/Services/Agent/Agent.Api/agent.http) requests through Gateway.
In Aspire select Agent console/structured logs and search for `Agent tool`. Each log shows the selected function and its arguments; HTTP traces show Catalog/Booking as separate service calls.
Run `python3 src/Services/Agent/Agent.Api/parallel-chat.py` to send eight real chats at once and inspect 200/429 outcomes without printing tokens.
For an outage experiment, stop Ollama, send a chat, observe 503 `The assistant is offline`, then restart Ollama and repeat manually.

## Runtime observations — 2026-09-29

Real Gateway requests and Aspire logs proved these behaviors using the persisted development data:

- `SearchEvents` logged `{ "search": null, "category": "Music", "city": null, "maxPrice": 1000, "from": null, "to": null }`. Catalog and the answer both contained Jazz Evening 16 at ₹0 with 223 seats, and Admin verified event at ₹100 with 0 seats. Warm search completed in 9.02 seconds in an earlier pass and 11.42 seconds on the final build.
- A nonexistent Purple Moon Unicorn Festival search returned no matches; the assistant reported not found. A poem request was declined.
- `GetEventDetails` logged `{ "eventId": 2 }`; the answer matched Cloud Summit 2 at ₹3,650, 223 seats, and October 12 at 12:00 UTC.
- `GetMyBookings` logged `{}` and Booking returned 200 for the caller. The underlying Booking data contains distinct purchases #312 (1 ticket), #311 (1), and #310 (2), all Confirmed. Explicit rank labels in the final tool projection distinguish their ordering from repeated event titles; the final answer reproduced all three correctly in 21.05 seconds without unsupported claims about other records.
- A follow-up with submitted user/assistant history fetched event ID 2’s current ₹3,650 price through `GetEventDetails` in 5.59 seconds.
- Missing JWT returned 401. Empty history, client-supplied system role and null messages each returned 400 before any tool ran.
- Stopping the actual local Ollama server produced 503 `Agent.Offline`, `The assistant is offline`, `Retry-After: 5` in 0.13 seconds. The macOS Ollama application restored its server automatically; `/api/tags` responded afterwards.
- Eight simultaneous chats produced seven 200 responses and one 429 `Agent.Busy` in 0.03 seconds. Accepted requests took 9.47–69.14 seconds, including queue wait, with no 500 or crashes. The warm under-30-second observation is for one chat without an overloaded queue.

These are observed outcomes, not promises of identical wording or timing on every run. The model initially combined repeated event titles and inferred absence from a partial booking page; bounded data and explicit instructions improved the observed answer. Prompt rules are still probabilistic. API permissions and the read-only tool list are enforced in code regardless of the wording the model produces.
