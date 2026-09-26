# Agent service — purpose and current API status

Agent is intended to provide a local-LLM assistant that uses Catalog and Booking through the logged-in user's permissions.
This reference distinguishes its current scaffold from planned chat/tools so callers do not mistake unimplemented APIs for available features.

## What exists now

`Agent.Api` starts a web service, registers shared hosting and JWT validation, and enables authentication/authorization middleware.
Aspire supplies Catalog and Booking discovery references and the JWT signing configuration. Agent waits for both services at startup.
Application and Infrastructure projects exist, but chat handlers, LLM adapters, and API-backed tools are not implemented. There is no Agent database or Domain project.

| Endpoint                                   | Current behavior                                                                     |
| ------------------------------------------ | ------------------------------------------------------------------------------------ |
| Direct `/health`                           | Development-only shared health endpoint; 200 Healthy when checks pass, otherwise 503 |
| Direct `/alive`                            | Development-only liveness endpoint; checks tagged `live`                             |
| Gateway `/agent/health`, `/agent/alive`    | Gateway removes `/agent` and proxies these operational requests                      |
| `/agent/chat` or other chat/business paths | No implemented route; normally 404                                                   |

There are **zero implemented Agent business operations**, so there are no chat request fields or chat-specific validation messages to document yet.
Registering JWT validation does not automatically protect all paths: the current health routes do not require authentication.

## Intended responsibility — not yet available

Planned work adds conversation handling, local Ollama model integration, bounded tool execution, and Catalog/Booking HTTP adapters.
Later action tools will book/cancel tickets or request statistics through the same business APIs and permissions as the user, rather than accessing service databases.
Do not expect conversation memory, chat streaming, booking tools, or Ollama failure messages from the current scaffold.

The [execution plan](../../EXECUTION_PLAN.md) tracks this work. [Catalog](../catalog/API.md) and [Booking](../booking/API.md) describe the APIs future tools must use; [Aspire](../common/ASPIRE.md) explains the existing runtime wiring.
