# AGENTS.md — how to work on EventHub

Instructions for any coding agent (Claude Code, Codex, Cursor, Copilot agent mode, …) and for a human
following the same process.

## The three files that drive this project

| File                     | Role                                                                                                               |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------ |
| `docs/SPEC.md`           | **What** to build. Source of truth. Requirement IDs (`FR-*`, `AR-*`, `SD-*`, `NFR-*`).                             |
| `docs/EXECUTION_PLAN.md` | **In what order**, and how each step is proven. The board is authoritative; detail status is synchronized with it. |
| `AGENTS.md`              | **How** to work (this file).                                                                                       |

Supporting documentation lives under `docs/services/`: one API reference per service and common
references for project structure, Gateway, BuildingBlocks, and Aspire. `docs/README.md` is the index.

### Service documentation preference

Maintain service API references under `docs/services/<service>/API.md`, linked from `docs/services/README.md`.
Explain the service's purpose, every implemented endpoint, inputs, access rules, success responses,
and validation/business-error messages so a reader understands its behavior without reading code.
Do not create standalone step-evidence reports, step walkthroughs, `DECISIONS.md`, or `LEARNING.md`.
Put design reasons, trade-offs, and practical usage explanations in the relevant service/common reference.
Still run and show verification; keep concise acceptance observations and `.http`/commit pointers in the execution plan.

## Purpose — this is a learning project

The owner is building this to understand microservices, auth and AI agents deeply, not just to have
working code. So:

- Follow **Clean Architecture, CQRS with our own mediator, SOLID and the coding practices** in
  `docs/SPEC.md` §15 — every use case is a command or query with its handler and validator; endpoints stay
  thin; business rules live in the Domain.
- Prefer **clear, explicit code** over clever abstractions. Manual mapping, no reflection magic beyond
  handler registration.
- For new or modified HTTP endpoints, declare parameter sources explicitly with `[FromBody]`,
  `[FromRoute]`, `[FromQuery]`, `[FromHeader]`, and `[FromServices]` as appropriate so the owner can
  see where to enter values in Postman. `CancellationToken` remains framework-provided request context;
  never label it as client input or a DI service. Explain that distinction beside the endpoint.
- Resilience rules in `docs/SPEC.md` §14 apply to every outbound call.
- After each step, **explain** what was built and why (see "Finishing a step").
- Never hide a concept behind a library without saying what the library does.
- In every step, start each new human-authored file with a **2–3 line purpose explanation** in plain
  language: what the file/class does, why it exists, and where it fits in the application flow.
  Use the file format's supported comments (before imports/usings where permitted), or opening prose
  for documentation. For formats that forbid comments, such as strict JSON, explain the file in the
  nearest README instead of making it invalid. Leave generated files under their generator's control.
  Explain non-obvious functions or control flow with concise comments, and keep explanations accurate
  when the file's responsibilities change.
- Explain each function's purpose in plain language, including private helpers. Put comments beside
  complex branches, loops, switches, transactions, concurrency checks, recovery paths, and timing values:
  describe why the logic exists and what happens next. Explain unfamiliar framework/library behavior
  where it is used. Do not narrate obvious variable assignments or merely repeat the code in English.
  Use everyday language and user journeys: "the user clicks Book again", "return the seats", or
  "show the booking already made". Explain technical terms before relying on them. Describe the
  situation, the action, and the reason, so a newcomer can follow the code without knowing the architecture.

## Working one step at a time

When asked to "execute Step-N" (or "do the next step"):

1. **Check readiness.** Open the board in `docs/EXECUTION_PLAN.md`. The step's status must be `Not Started`
   and all its dependencies `Completed`. If not, say which dependency is missing and stop.
2. **Read the spec.** Read every `docs/SPEC.md` section and requirement ID listed for the step, plus
   "Rules that apply to every step" in the plan.
3. **Claim it.** Set the step's status to `In Progress (<agent or name>, <date>)` on the board **and** in
   its detail section.
4. **Build.** Work through the step's task checklist, ticking `[x]` as each is done. Stay inside the
   step's scope; the ⛔ line is a hard boundary. Add the required 2–3 line purpose explanation when
   creating each new human-authored file.
5. **Verify.** Prove every acceptance criterion: run the command / `.http` request / test and show the
   actual output. A criterion is ticked only with evidence. Criteria marked 👤 need the owner to check
   in the browser — ask them and wait. Review every new human-authored file for its purpose explanation
   before finishing the step; use the format-specific handling described above.
6. **Finish** (below), or record a blocker.

Do **not** start the next step unless asked.

## Finishing a step

- Mark it `Completed (<date> — evidence: <file/.http/commit>)` on the board and in the detail section.
- Any step whose dependencies are now all `Completed` changes from `Dependent` to `Not Started`.
- Commit: `Step-N: <scope>`, unless the owner explicitly defers commits. Record the deferral on the
  board and leave the work available for the owner's later commit.
- Give the owner a short summary:
    - what was built (files, endpoints)
    - the 2–4 concepts it demonstrates and **why** it was done this way
    - one "break it on purpose" experiment to try, and what should happen
- Update the relevant service/common reference to explain newly implemented behavior and its trade-offs.

## When the spec and reality disagree

Stop and tell the owner. If they agree to a change: update `docs/SPEC.md` first, explain the reason
and trade-off in the relevant service/common reference, then change the code. Never silently diverge from the spec.

## When stuck

After 3 genuine attempts (or ~1 hour): add a row `OI-##` to the Open Issues log in
`docs/EXECUTION_PLAN.md`, set the step to `Blocked (OI-##)`, explain the problem, and suggest a step that
does not depend on it.

## Things the agent must not do

- Run `sudo`, enter passwords, or change macOS system settings. Ask the owner to run those commands
  themselves (tasks marked 👤).
- Commit secrets. The JWT key, persistent SQL password and internal service credential go in user-secrets / Aspire parameters (AR-07).
- Add commercially-licensed packages (MediatR v13+, AutoMapper v15+, MassTransit v9+,
  FluentAssertions v8+).
- Add pub/sub, message brokers, sagas, Keycloak or anything in SPEC §2 "Out of scope".
- Let one service touch another service's database (AR-01).
- Break a layer rule (CA-01…CA-04): no EF Core or ASP.NET Core in Domain/Application, no business logic
  in endpoints.
- Retry a non-idempotent call, or retry the LLM chat call (SPEC §14.3).
- Write tests of any kind — the owner has ruled them out for v1.0 (SPEC CP-08). Prove steps by
  running the system.
- Build anything from the "Later — for understanding" list (L-1…L-4) unless the owner asks for it.
- Mark a step `Completed` with build warnings.
- Tick a checkbox or mark a step `Completed` without evidence. An owner-approved deferred task must
  be removed from the step's completion criteria or recorded separately; never tick it as done.
- Renumber steps. New work takes the next free step number; a step that grows gets a letter (`Step-7c`).

## Environment

- macOS on Apple Silicon (M1). SQL Server runs as an amd64 container under Rosetta (slow first start).
- Port 5000 is taken by macOS AirPlay — the gateway uses **5100**.
- Start backend + Angular together: `dotnet run --project src/Aspire/EventHub.AppHost`; Aspire manages `web` at http://localhost:4200. Run `npm install` in `web/` once after cloning.
- Ollama must be running (menu-bar icon) before the Agent API is used.
- Demo users: `admin@demo.com`, `organizer@demo.com`, `organizer2@demo.com`, `attendee@demo.com`,
  `attendee2@demo.com` — password `Demo@123`.
