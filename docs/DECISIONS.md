# EventHub — Architecture Decisions

## 2026-09-26 — Keep Aspire projects under `src/Aspire`

**Decision.** The backend startup command is `dotnet run --project src/Aspire/EventHub.AppHost`, matching the solution layout in SPEC §15.5.

**Why.** Keeping the AppHost and ServiceDefaults projects together makes the orchestration boundary explicit and matches Step-2's implementation and acceptance criteria.

**Trade-off.** The command is slightly longer than placing AppHost directly under `src/`, but the repository structure is clearer and remains consistent as more projects are added.

## 2026-09-26 — Keep project documentation under `docs/`

**Decision.** Store the specification, execution plan, decisions, learning notes, and architecture guide in `docs/`. Keep `AGENTS.md` and `CLAUDE.md` at the repository root.

**Why.** One documentation folder makes the growing body of project material easier to browse, while root-level agent files continue to be discovered automatically by coding tools.

**Trade-off.** References from root-level instructions need a `docs/` prefix, and the two tool-discovery files remain intentional exceptions to the documentation-folder rule.

## 2026-09-26 — Use a hand-written mediator

**Decision.** Implement the small `ISender` pipeline required by EventHub instead of adding MediatR. Assembly scanning creates typed dispatch adapters at startup; sending a request is then a dictionary lookup followed by the registered behaviors and handler.

**Why.** MediatR v13+ is commercially licensed, and implementing the mechanism directly makes handler resolution and pipeline ordering visible for this learning project.

**Trade-off.** EventHub owns this plumbing and deliberately supports only the features it needs; it does not inherit notifications, streaming, or other MediatR features.

## 2026-09-26 — Use CQRS-lite in each service

**Decision.** Represent every use case as a command or query with its own handler and validator, while keeping one database per service rather than separate read and write stores.

**Why.** Separate request models make intent and responsibilities clear without introducing operational complexity that the current scale does not require.

**Trade-off.** Reads and writes can be optimized independently in code, but they still share the service database and its availability boundary.

## 2026-09-26 — Results for expected failures, exceptions for unexpected failures

**Decision.** Handlers return `Result`/`Result<T>` with a typed `Error` for validation, missing data, conflicts, authorization, availability, and unprocessable requests. Unexpected faults are allowed to reach the global exception handler.

**Why.** Expected outcomes stay explicit in handler signatures and map consistently to HTTP, while programming and infrastructure faults are logged centrally instead of being mistaken for business outcomes.

**Trade-off.** Callers must inspect a result before reading its value, and every new error category requires an intentional HTTP mapping.
