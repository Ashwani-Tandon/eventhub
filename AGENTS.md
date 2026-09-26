# AGENTS.md — how to work on EventHub

Instructions for any coding agent (Claude Code, Codex, Cursor, Copilot agent mode, …) and for a human
following the same process.

## The three files that drive this project

| File | Role |
|---|---|
| `docs/SPEC.md` | **What** to build. Source of truth. Requirement IDs (`FR-*`, `AR-*`, `SD-*`, `NFR-*`). |
| `docs/EXECUTION_PLAN.md` | **In what order**, and how each step is proven. The board is authoritative; detail status is synchronized with it. |
| `AGENTS.md` | **How** to work (this file). |

Supporting files created along the way live in `docs/`: `DECISIONS.md` (why things are the way they
are), `LEARNING.md` (what the owner learned per step), `PROJECT_STRUCTURE.md`, `README.md`, `DEMO.md`.

## Purpose — this is a learning project

The owner is building this to understand microservices, auth and AI agents deeply, not just to have
working code. So:

- Follow **Clean Architecture, CQRS with our own mediator, SOLID and the coding practices** in
  `docs/SPEC.md` §15 — every use case is a command or query with its handler and validator; endpoints stay
  thin; business rules live in the Domain.
- Prefer **clear, explicit code** over clever abstractions. Manual mapping, no reflection magic beyond
  handler registration.
- Resilience rules in `docs/SPEC.md` §14 apply to every outbound call.
- After each step, **explain** what was built and why (see "Finishing a step").
- Never hide a concept behind a library without saying what the library does.

## Working one step at a time

When asked to "execute Step-N" (or "do the next step"):

1. **Check readiness.** Open the board in `docs/EXECUTION_PLAN.md`. The step's status must be `Not Started`
   and all its dependencies `Completed`. If not, say which dependency is missing and stop.
2. **Read the spec.** Read every `docs/SPEC.md` section and requirement ID listed for the step, plus
   "Rules that apply to every step" in the plan.
3. **Claim it.** Set the step's status to `In Progress (<agent or name>, <date>)` on the board **and** in
   its detail section.
4. **Build.** Work through the step's task checklist, ticking `[x]` as each is done. Stay inside the
   step's scope; the ⛔ line is a hard boundary.
5. **Verify.** Prove every acceptance criterion: run the command / `.http` request / test and show the
   actual output. A criterion is ticked only with evidence. Criteria marked 👤 need the owner to check
   in the browser — ask them and wait.
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
- Append 3–5 lines to `docs/LEARNING.md` for that step.

## When the spec and reality disagree

Stop and tell the owner. If they agree to a change: update `docs/SPEC.md` first, add a `docs/DECISIONS.md` entry
(Decision · Why · Trade-off), then change the code. Never silently diverge from the spec.

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
- Backend: `dotnet run --project src/Aspire/EventHub.AppHost` · Frontend: `cd web && npm start` (http://localhost:4200)
- Ollama must be running (menu-bar icon) before the Agent API is used.
- Demo users: `admin@demo.com`, `organizer@demo.com`, `organizer2@demo.com`, `attendee@demo.com`,
  `attendee2@demo.com` — password `Demo@123`.
