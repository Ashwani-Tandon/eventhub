# EventHub — Execution Plan

> Built to `SPEC.md` (spec-driven: every step names the requirement IDs it implements, and its
> acceptance criteria prove them). Workflow rules for whoever executes the plan — you or a coding
> agent — are in [`../AGENTS.md`](../AGENTS.md).
>
> **No time boxes.** Steps are ordered by dependency only. Do as many as you like in one sitting —
> one step or all of them. The board below is authoritative; the repeated status in each detail
> section is kept synchronized for readability.

**Status values (exactly these six):**
`Not Started` — ready now · `In Progress` — being built (who, since when) · `Completed` — built **and**
every acceptance criterion verified (date + evidence) · `Blocked` — waiting on an Open Issue (`OI-##`) ·
`Dependent` — waiting on the named steps · `Retired` — deliberately removed from v1.0, retained only
so historical references still resolve.

**Legend in task lists:** 👤 = needs you (password, GUI click, browser check) · everything else can be
done by a coding agent.

## Phases

| Phase               | Goal                                                                                           | Steps (in board order)                                                                                                  |
| ------------------- | ---------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| **0 — Setup**       | Every tool installed and verified before any code                                              | Step-13, Step-1a, Step-1b, Step-1c, Step-1d, Step-1e                                                                    |
| **A — Application** | The working product: architecture, APIs, resilience, Angular UI                                | Step-2, Step-20, Step-3, Step-4, Step-5, Step-16, Step-17, Step-6, Step-7a, Step-7b, Step-8a, Step-8b, Step-8c, Step-19 |
| **B — AI agent**    | Built by the coding agent like Phase A; the owner studies the code and walkthroughs afterwards | Step-14, Step-9, Step-10, Step-11                                                                                       |
| **C — Finish**      | Demo-ready                                                                                     | Step-12                                                                                                                 |
| **Later**           | Extras for deeper understanding, after v1.0                                                    | See "Later — for understanding" at the end (Step-15, Step-18, architecture checks, chaos testing)                       |

Step numbers are permanent and never reused. Steps added after the first draft took the next free
number, which is why the board is not in numeric order — **execution order is the board order**.

---

## Part 1 — The board

| Step            | Scope                                                                                                                                                                                | Implementation details                                                                                                                   | Spec                                           | Dependencies                                                           | Acceptance criteria                                                                                                                                                           | Status                                                                                                                                                                                                                         |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Step-13**     | Local project folder, plan files, VS Code + agent extension, local Git. ⛔ No other dev tools or remote repository                                                                   | chosen local folder, the 4 plan files, VS Code and Codex extension installed, `git init`; owner deferred first commit                    | —                                              | —                                                                      | 4 files present · local Git initialized · agent summarises the board                                                                                                          | Completed (2026-09-24 — evidence: `ls`, `git rev-parse --show-toplevel`, empty `git remote -v`, VS Code/Codex installation; first commit deferred by owner)                                                                    |
| **Step-1a**     | Homebrew, Rosetta, Git, VS Code extensions. ⛔ No project code                                                                                                                       | 👤 Homebrew · Rosetta · `code` in PATH · 4 extensions                                                                                    | §3                                             | Step-13                                                                | `brew -v` · `git --version` · `code .` · 4 extensions                                                                                                                         | Completed (2026-09-24 — evidence: Homebrew 7.0.6, Apple Git 2.39.5, `code` 1.139.0, extension list, owner confirmed `code .`; commit deferred by owner)                                                                        |
| **Step-1b**     | .NET 10 SDK, Aspire templates, dev cert. ⛔ No solution                                                                                                                              | `brew install --cask dotnet-sdk` · templates · 👤 cert trust                                                                             | §3                                             | Step-1a                                                                | SDK 10.x · templates listed · cert trusted                                                                                                                                    | Completed (2026-09-24 — evidence: `dotnet --list-sdks`, `dotnet new list aspire`, `dotnet dev-certs https --check --trust`; commit deferred with initial repository commit)                                                    |
| **Step-1c**     | Docker Desktop + SQL Server on M1. ⛔ No project DB                                                                                                                                  | 👤 Rosetta on, ≥ 4 GB · test-run SQL image · remove                                                                                      | §3                                             | Step-1a                                                                | hello-world · SQL up > 1 min · `SELECT @@VERSION` · test container gone                                                                                                       | Completed (2026-09-24 — evidence: `docker version`, `docker info`, `docker run --rm hello-world`, `docker ps`, `SELECT @@VERSION`, empty `docker ps -a --filter name=sqltest`; commit deferred with initial repository commit) |
| **Step-1d**     | Node LTS + Angular CLI. ⛔ No project app                                                                                                                                            | `brew install node@24` · `npm i -g @angular/cli@22` · throwaway app                                                                      | §3                                             | Step-1a                                                                | Node 24.x LTS · Angular CLI 22.x · :4200 test page · deleted                                                                                                                  | Completed (2026-09-25 — evidence: `node -v`, `ng version`, `ng serve`, HTTP 200 from :4200, owner browser confirmation, absent `/tmp/ngtest`; commit deferred with initial repository commit)                                  |
| **Step-1e**     | Ollama + model + tool calling. ⛔ No agent code                                                                                                                                      | 👤 open Ollama · pull model by RAM · curl tool-call test                                                                                 | §3, FR-AGT-06                                  | Step-1a                                                                | model listed · warm answer < 20 s · `tool_calls` returned · :11434 up                                                                                                         | Completed (2026-09-25 — evidence: `ollama list`, warm `ollama run` 1.49 s, `/api/tags`, `/api/chat` returned `get_weather` for Delhi)                                                                                          |
| **Step-2**      | Solution skeleton in Clean Architecture layout; Aspire, SQL + 3 DBs, gateway. ⛔ No business code, no test projects                                                                  | All projects of §15.5 with CA-01…04 references, Directory.Build/Packages.props, AppHost wiring, YARP :5100                               | §4, §15.5, AR-01..09, CP-01, NFR-01/02         | Step-1b, Step-1c, Step-1d, Step-1e                                     | builds with 0 warnings · dashboard all Running · 3 DBs survive a restart · 4 health checks via :5100 · no committed secrets                                                   | Completed (2026-09-26 — evidence: `dotnet build EventHub.sln`, Aspire dashboard, `gateway.http`, SQL query, restart persistence check)                                                                                         |
| **Step-20**     | BuildingBlocks: mediator, CQRS contracts, behaviors, Result; Result→HTTP mapping. ⛔ No service use cases, no architecture checks (Later)                                            | `ISender`, `ICommand/IQuery` + handlers, 3 behaviors, `Result/Error`, `ToHttpResult()`, global exception handler, dev-only `/debug/echo` | §15.1–15.2, CQ-03/04/06                        | Step-2                                                                 | echo logs behaviors in order · invalid echo → 400, handler not run · each ErrorType → right status · unexpected exception → 500 ProblemDetails                                | Completed (2026-09-26 — evidence: `dotnet build EventHub.sln`, `Identity.Api/debug.http`, Aspire console logs)                                                                                                                 |
| **Step-3**      | Identity service (all 4 layers) + shared JWT validation. ⛔ No UI                                                                                                                    | `User` entity, 3 commands / 2 queries, JWT + hasher ports, `AddEventHubAuth()`, seed                                                     | §6, SD-02, §15.2, CP-10                        | Step-20                                                                | 5 users get tokens · claims correct · generic 401 · 403/200 on `/users` · `/auth/me` + 409 duplicate                                                                          | Completed (2026-09-26 — evidence: `docs/STEP-3-EVIDENCE.md`, `identity.http`; build 0 warnings)                                                                                                                                |
| **Step-4**      | Catalog service: events CRUD, search, ownership, internal idempotent seat reservations. ⛔ No bookings, no rowVersion                                                                | `Event`, `SeatReservation`, 5 commands / 3 queries, `TryReserveAsync`, Booking-only internal endpoints, seed generator                   | §7 (FR-CAT-01..09), AR-09, SD-03, SD-05, CP-10 | Step-3                                                                 | filters + total · 403 attendee / other owner, 200 admin · public/internal authorization proven · 409 overbook · same reservationId twice → seats change once · 400 invalid    | Completed (2026-09-26 — evidence: `docs/STEP-4-EVIDENCE.md`, `Catalog.Api/catalog.http`; build 0 warnings)                                                                                                                     |
| **Step-5**      | Booking service: book with compensation, mine, retryable cancel, stats. ⛔ No UI, no idempotency key                                                                                 | `Booking` entity, cancellation release-pending state, 2 commands / 3 queries, `ICatalogClient`, `IPaymentGateway`, seed                  | §8 (FR-BKG-01..07, 09), SD-04, SD-05, CP-10    | Step-4                                                                 | seats −2 · 422 keeps seats · cancel restores / retry after Catalog outage / 403 other · stats scoped · 503 when Catalog down, My Bookings still works                         | Not Started                                                                                                                                                                                                                    |
| **Step-16**     | Idempotent booking creation + optimistic concurrency on event edits. ⛔ No retry policies, no UI                                                                                     | atomic `BookingRequest` claim with stable reservation/payment IDs; `RowVersion` on `Event`; migrations                                   | FR-BKG-08, FR-CAT-10, RES-06/07, CP-10         | Step-5                                                                 | same key twice → one booking · parallel same key → same booking and one payment/reservation · mismatched replay → 409 · different keys → two · stale rowVersion → 409         | Dependent (Step-5)                                                                                                                                                                                                             |
| **Step-17**     | Resilience pipeline for service-to-service calls, DB retry. ⛔ No chaos toggle (Later), no gateway limits, no UI                                                                     | `AddEventHubResilience()` in ServiceDefaults, safe-method retry rule, breaker → 503, EF execution strategy                               | RES-01/02/03/04/05/13, FR-BKG-07               | Step-5                                                                 | Catalog stopped → retries in trace, then 503 · repeated failures → circuit opens, fast 503 · restart → recovers after break · breaker transitions logged                      | Dependent (Step-5)                                                                                                                                                                                                             |
| ~~**Step-18**~~ | ~~Gateway rate limiting, timeouts, aggregated health~~ — **retired by the owner; moved to "Later — for understanding" (L-2)**. The `/agent` route timeout moved into Step-9.         | —                                                                                                                                        | —                                              | —                                                                      | —                                                                                                                                                                             | Retired                                                                                                                                                                                                                        |
| **Step-6**      | Angular shell + auth, clean folder structure. ⛔ No feature screens                                                                                                                  | `web/` with core/shared/features, Material, dev proxy, AuthService, interceptor, guards, role menu                                       | FR-UI-01..04, CP-09                            | Step-3                                                                 | 5 users log in, menus differ · refresh keeps login · guards redirect · bad token → login · register works                                                                     | Not Started                                                                                                                                                                                                                    |
| **Step-7a**     | Events list + details. ⛔ Book button inert                                                                                                                                          | filter bar, cards, paging, details page                                                                                                  | FR-UI-05/06                                    | Step-4, Step-6                                                         | filters work · seats left matches API · sold-out badge · guest can browse                                                                                                     | Dependent (Step-4, Step-6)                                                                                                                                                                                                     |
| **Step-7b**     | Book tickets + My Bookings. ⛔ No organizer screens                                                                                                                                  | booking dialog, messages per status, bookings table, cancel                                                                              | FR-UI-07/08                                    | Step-5, Step-7a                                                        | book end-to-end · seats update · payment failure message · cancel works                                                                                                       | Dependent (Step-5, Step-7a)                                                                                                                                                                                                    |
| **Step-8a**     | Organizer: My Events + create/edit/delete. ⛔ No charts                                                                                                                              | typed reactive form, validation, rowVersion, delete confirm                                                                              | FR-UI-09, FR-CAT-10                            | Step-4, Step-6, Step-16                                                | new event in both lists · field errors block submit · edits persist · stale edit and delete conflicts show 409 messages                                                       | Dependent (Step-4, Step-6, Step-16)                                                                                                                                                                                            |
| **Step-8b**     | Dashboard: KPI tiles + 3 charts. ⛔ No admin screens                                                                                                                                 | ngx-echarts, `/bookings/stats` + `/events/mine`                                                                                          | FR-UI-10                                       | Step-5, Step-6                                                         | 3 charts from seed · organizer2 ≠ organizer, admin = all · refresh reflects new booking · resizes                                                                             | Dependent (Step-5, Step-6)                                                                                                                                                                                                     |
| **Step-8c**     | Admin: users + roles, all bookings. ⛔ No agent                                                                                                                                      | users table + role dropdown, bookings table + filter                                                                                     | FR-UI-11, FR-ID-05                             | Step-3, Step-5, Step-6                                                 | promote attendee2 → organizer after re-login · old rights before re-login · non-admin blocked                                                                                 | Dependent (Step-3, Step-5, Step-6)                                                                                                                                                                                             |
| **Step-19**     | UI resilience: GET retry, booking idempotency key, per-panel degradation, 429/409 messages. ⛔ No new features                                                                       | retry interceptor (GET only), `crypto.randomUUID()` key, `PanelState` component                                                          | FR-UI-13/14/15, RES-12                         | Step-7b, Step-8a, Step-8b, Step-16                                     | Booking down → Events work, dashboard panels show Retry · Catalog down → My Bookings works · double-click → one booking · GET retried, POST not · stale edit → reload message | Dependent (Step-7b, Step-8a, Step-8b, Step-16)                                                                                                                                                                                 |
| **Step-14**     | The agent loop by hand — raw Ollama API, hand-written tool schemas, manual loop — built by the coding agent as study material. ⛔ No M.E.AI, no write tools, not part of the product | console app `Agent.Playground`, 2 tools, prints every round, heavily commented, `WALKTHROUGH.md`                                         | §9.1, glossary "ReAct"                         | Step-1e, Step-4                                                        | round 1 shows `tool_calls` · round 2 answer uses real events · tool error explained by model · loop guard stops at 5                                                          | Not Started                                                                                                                                                                                                                    |
| **Step-9**      | Agent service (3 layers), read-only tools, timeout + bulkhead. ⛔ Cannot book/cancel/stats                                                                                           | M.E.AI + OllamaSharp, `SendChatMessage` command, 3 tools, ports `ICatalogApi`/`IBookingApi`, limiter, `/agent` route timeout 120 s       | §9, FR-AGT-01/02/04/05/06/07, CA-07, NFR-04    | Step-1e, Step-5, Step-17                                               | real events + tool calls in logs · nothing invented, off-topic declined · warm reply < 30 s · Ollama down → 503, overload → 429                                               | Dependent (Step-1e, Step-5, Step-17)                                                                                                                                                                                           |
| **Step-10**     | Agent action tools + permissions + idempotent booking tool. ⛔ No UI                                                                                                                 | `BookTickets` (with key), `CancelBooking`, `GetSalesStats`, confirmation rule                                                            | FR-AGT-03/04/08                                | Step-9, Step-16                                                        | confirms then books · attendee stats refused (403 in logs) · organizer stats match · cancel by name                                                                           | Dependent (Step-9, Step-16)                                                                                                                                                                                                    |
| **Step-11**     | Chat widget. ⛔ No new agent features                                                                                                                                                | `features/chat`, floating panel, session history, indicator, clear on logout                                                             | FR-UI-12                                       | Step-6, Step-9                                                         | multi-turn memory · hidden logged out · empty for next user · slow reply shows indicator                                                                                      | Dependent (Step-6, Step-9)                                                                                                                                                                                                     |
| **Step-12**     | Demo readiness. ⛔ No new features                                                                                                                                                   | README, DECISIONS, DEMO script (incl. resilience demo), data reset, tag v1.0                                                             | §2, all                                        | Step-7b, Step-8a, Step-8b, Step-8c, Step-10, Step-11, Step-17, Step-19 | fresh clone runs from README · build 0 warnings, `ng lint` passes · demo runs clean twice · every decision explainable                                                        | Dependent (Step-7b, Step-8a, Step-8b, Step-8c, Step-10, Step-11, Step-17, Step-19)                                                                                                                                             |
| ~~**Step-15**~~ | ~~Same agent rebuilt on Microsoft Agent Framework, compared~~ — **retired by the owner; moved to "Later — for understanding" (L-1)**                                                 | —                                                                                                                                        | —                                              | —                                                                      | —                                                                                                                                                                             | Retired                                                                                                                                                                                                                        |

**Parallel paths.** Step-1b…1e run in any order after 1a, but all must finish before Step-2. After
Step-3, Step-6 can run alongside the API track (Step-4 → 5 → 16/17); Step-7a additionally needs
Step-4, and Step-8a additionally needs Step-16. Step-14 needs only Step-1e and Step-4, so agent
learning can start before the UI is finished.

**Retired steps** (Step-15, Step-18) stay on the board struck through so references still resolve;
their work is in the "Later — for understanding" list at the end.

---

## Rules that apply to every step

_Stated once here; not repeated in the steps below._

1. **Spec first.** Read the step's Spec column in `SPEC.md` before starting. If the code must differ
   from the spec, change `SPEC.md` first and log the reason in `DECISIONS.md`.
2. **Folder layout** is SPEC §15.5. .NET in `src/`, Angular in `web/`, and `.http` files next to each
   `*.Api/Program.cs`. If Later L-3 is activated, its architecture-test project lives in `tests/`.
3. **Clean Architecture + CQRS** (SPEC §15): every use case is a command or query in its own feature
   folder with its handler and validator; endpoints only `Send` and map the `Result`; business rules in
   the Domain; ports in Application, adapters in Infrastructure. The layer references are the guard
   (no automatic architecture checks — see Later L-3).
4. **SOLID and coding practices** CP-01…CP-10 apply to all code: 0 warnings, nullable on,
   `CancellationToken` everywhere, `sealed` + `record`, Options pattern, `TimeProvider`, structured
   logging, constants instead of magic strings.
5. **No tests of any kind** (owner's decision, CP-08). A step is proven by running the system —
   `.http` requests, the Aspire dashboard, the browser. `dotnet build` must have 0 warnings.
6. **One database per service** (AR-01). **Every API validates the JWT** (AR-02).
   **Service-to-service calls forward the user's token** (AR-05).
7. **Resilience** (SPEC §14): nothing waits forever; never retry a non-idempotent call; downstream
   failure → 503 ProblemDetails, never an unhandled 500.
8. **Ports:** Gateway 5100 · Angular 4200 · Ollama 11434 · everything else from Aspire (NFR-02).
9. **Evidence.** Every acceptance criterion is proven by a command, `.http` request or screenshot
   before it is ticked; the evidence pointer goes in the Status when `Completed`.
10. **Commit** at the end of every step: `Step-N: <scope>`, unless the owner explicitly defers it.
    Record a deferral without treating an unmade commit as evidence.
11. **Seed data** is idempotent and deterministic (SD-01, SD-05).
12. **No commercial packages** (SPEC §3).
13. **Stuck** after 3 honest attempts or ~1 hour: add an `OI-##` row to the Open Issues log, set the
    step to `Blocked (OI-##)`, continue with a step that does not depend on it.
14. **Update the board** on every status change. When a step completes, any step whose dependencies
    are all `Completed` becomes `Not Started`.
15. **Learn as you go.** After each step, 3–5 lines in `LEARNING.md`: what was built, why, and one
    "break it on purpose" experiment you tried.
16. **File purpose explanations.** Every new human-authored file starts with 2–3 plain-language lines
    explaining what it does, why it exists, and where it fits in the application flow. Use supported
    comments or opening documentation prose; for formats without comments, document it in the nearest
    README. Leave generated files to their generator. Verify these explanations before completing
    each step and keep them accurate when responsibilities change (see `AGENTS.md`).

---

## Part 2 — Steps in detail

## Phase 0 — Setup

### Step-13 — Project folder, plan files, coding agent

**Scope.** A local project folder and Git repository hold the plan. VS Code and the Codex extension
are available for the owner to use. GitHub and other remote repositories can be considered after
the project is ready. ⛔ No other development tools yet.

**Tasks**

- [x] 👤 Create or choose the local project folder; using `/Users/apple/Personal Projects/Event Booking Platform` (verified with `pwd` and `ls`)
- [x] 👤 Confirm `SPEC.md`, `EXECUTION_PLAN.md`, `AGENTS.md`, `CLAUDE.md` are in that folder (`ls` verified)
- [x] 👤 Install VS Code: `/Applications/Visual Studio Code.app` is present
- [x] Codex extension is installed (`openai.chatgpt` in VS Code's extension list)
- [x] Agent can read the project folder and identify the ready step from this board
- [x] Agent: `git init` completed in this folder; no GitHub repository or remote was created

**Deferred by the owner (2026-09-24):** Make the first local Git commit later. The owner may open
this folder in VS Code and sign in to the Codex chat panel at any time; this GUI action does not
block local project setup.

**Note.** Password commands marked 👤 are run by you in VS Code's terminal (Terminal → New Terminal).

**Dependencies.** None.

**Acceptance criteria**

- [x] `ls` shows the 4 files (output verified on 2026-09-24)
- [x] `git rev-parse --show-toplevel` identifies this folder, and `git remote -v` is empty (verified 2026-09-24)
- [x] The agent identified Step-13 as the only ready step; after completion Step-1a is ready

**Status.** Completed (2026-09-24 — evidence: `ls`, `git rev-parse --show-toplevel`, empty `git remote -v`, VS Code/Codex installation; first commit deferred by owner)

---

### Step-1a — Base tools

**Scope.** Package manager, Rosetta, Git, editor extensions. ⛔ No project code.

**Tasks**

- [x] 👤 Install Homebrew from https://brew.sh; `brew -v` verifies it is on PATH (Homebrew 7.0.6)
- [x] 👤 Rosetta is installed (`pkgutil --pkg-info com.apple.pkg.RosettaUpdateAuto` verified 2026-09-24)
- [x] Git is available (`git --version`: Apple Git 2.39.5); owner's name and email are configured for this local repository. No additional Git installation is needed.
- [x] 👤 In VS Code press Cmd+Shift+P → _Shell Command: Install 'code' command in PATH_ (`command -v code` → `/usr/local/bin/code`)
- [x] Extensions: `ms-dotnettools.csdevkit`, `angular.ng-template`, `ms-mssql.mssql`, `humao.rest-client` (`code --list-extensions` verified 2026-09-24)
- [x] Record RAM (`sysctl hw.memsize`: 8589934592 bytes, 8 GB) in `LEARNING.md` — Step-1e uses `qwen2.5:3b`

**Dependencies.** Step-13.

**Acceptance criteria**

- [x] `brew -v` prints `Homebrew 7.0.6`
- [x] `git --version` prints `git version 2.39.5 (Apple Git-154)`
- [x] `code .` opens VS Code (owner confirmed; `code --version` prints 1.139.0)
- [x] `code --list-extensions` shows all 4 required extensions

**Status.** Completed (2026-09-24 — evidence: Homebrew 7.0.6, Apple Git 2.39.5, `code` 1.139.0, extension list, owner confirmed `code .`; commit deferred by owner)

---

### Step-1b — .NET SDK and Aspire

**Scope.** Everything needed to create and run .NET projects. ⛔ No solution.

**Tasks**

- [x] .NET SDK was already installed; `dotnet --list-sdks` reports `10.0.301` from `/usr/local/share/dotnet/sdk`, so no reinstall was needed
- [x] `dotnet new install Aspire.ProjectTemplates::13.5.3` installed the pinned `13.5.3` templates (the CLI notes that `@` is the newer package/version separator)
- [x] 👤 `dotnet dev-certs https --trust` (owner approved the macOS prompt; independent check found the trusted `CN=localhost` certificate)

**Trap.** `dotnet: command not found` → PATH only updates in new terminals.

**Dependencies.** Step-1a.

**Acceptance criteria**

- [x] `dotnet --list-sdks` shows `10.0.301`
- [x] `dotnet new list aspire` lists the Aspire templates
- [x] `dotnet dev-certs https --check --trust` reports trusted certificate `2B196343C501D6B7F86550D30522327ED71FA60C`

**Status.** Completed (2026-09-24 — evidence: `dotnet --list-sdks`, `dotnet new list aspire`, `dotnet dev-certs https --check --trust`; commit deferred with initial repository commit)

---

### Step-1c — Docker and SQL Server

**Scope.** Prove SQL Server runs on this M1. ⛔ The test container is thrown away; Aspire creates the real one in Step-2.

**Tasks**

- [x] `brew install --cask docker` (Docker Desktop 4.92.0 installed; `docker version` shows its engine)
- [x] 👤 Open Docker Desktop once, accept terms; Settings → General → **Use Rosetta for x86_64/amd64 emulation** on; Settings → Resources → memory ≥ 4 GB (owner confirmed; `docker info` reports 4,106,604,544 bytes available to the Linux VM)
- [x] `docker run hello-world` (printed “Hello from Docker!”)
- [x] `docker pull --platform linux/amd64 mcr.microsoft.com/mssql/server:2022-latest` (completed, digest `sha256:4402d880...`)
- [x] `docker run -d --name sqltest --platform linux/amd64 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Test@12345' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest` (container `6abd853e...`)
- [x] Run `SELECT @@VERSION` using the container's `/opt/mssql-tools18/bin/sqlcmd`; returned Microsoft SQL Server 2022 RTM-CU27, 16.0.4295.3 (X64)
- [x] `docker rm -f sqltest` (removed only the temporary test container; the image remains cached)

**Installation note (resolved 2026-09-24).** Homebrew initially needed the owner's macOS password
to create a link in `/usr/local/bin`, so it removed the partial installation. The owner completed
the installation and confirmed the Rosetta and memory settings before the container checks ran.

**Trap.** Container exits immediately → `docker logs sqltest`: Rosetta off, memory too low, or weak password.

**Dependencies.** Step-1a.

**Acceptance criteria**

- [x] `hello-world` prints its success message
- [x] `docker ps` showed `sqltest` still running over 1 minute after its `2026-09-24T18:19:17Z` start (checked at `18:20:57Z`)
- [x] `SELECT @@VERSION` returns SQL Server 2022 RTM-CU27, 16.0.4295.3
- [x] `docker ps -a --filter name=sqltest` returned no containers after removal

**Status.** Completed (2026-09-24 — evidence: `docker version`, `docker info`, `docker run --rm hello-world`, `docker ps`, `SELECT @@VERSION`, empty `docker ps -a --filter name=sqltest`; commit deferred with initial repository commit)

---

### Step-1d — Node and Angular CLI

**Scope.** Frontend tooling. ⛔ The test app is deleted afterwards.

**Tasks**

- [x] `brew install node@24` installed 24.21.0 and linked it into `/opt/homebrew/bin` (already on PATH); this updated a pre-existing 24.14.0 standalone Node that was below Angular CLI 22.2.0's minimum 24.15.0
- [x] `npm i -g @angular/cli@22` installed 22.2.0 under Homebrew's global prefix; removed the earlier duplicate user-prefix copy; generated app has `package-lock.json`
- [x] In `/tmp`: `ng new ngtest --defaults`, installed dependencies, and `ng serve` built successfully at :4200 (`curl` returned HTTP 200)
- [x] 👤 Owner opened http://localhost:4200 and confirmed the starter page loaded; stopped the server and deleted `/tmp/ngtest`

**Dependencies.** Step-1a.

**Acceptance criteria**

- [x] `node -v` shows 24.21.0 LTS
- [x] `ng version` prints Angular CLI 22.2.0 with Node 24.21.0
- [x] The test page loads at :4200 (owner confirmed; `curl` returned HTTP 200)
- [x] `/tmp/ngtest` is deleted (`test ! -e /tmp/ngtest` succeeded)

**Status.** Completed (2026-09-25 — evidence: `node -v`, `ng version`, `ng serve`, HTTP 200 from :4200, owner browser confirmation, absent `/tmp/ngtest`; commit deferred with initial repository commit)

---

### Step-1e — Ollama and model

**Scope.** A local LLM that supports **tool calling**. ⛔ No agent code.

**Tasks**

- [x] Ollama 0.32.15 was already installed; opened the app once and completed its local-only onboarding (no account)
- [x] 8 GB RAM → pulled `qwen2.5:3b` (1.9 GB, digest `357c53fb659c...`)
- [x] `ollama run qwen2.5:3b "What is a microservice? One sentence."` ran twice; timed warm run completed in 1.49 s
- [x] `curl http://localhost:11434/api/tags` returned both installed models and reported `qwen2.5:3b` capabilities `completion` and `tools`
- [x] Tool-call test: `POST http://localhost:11434/api/chat` with `"stream": false` returned `message.tool_calls[0].function.name = "get_weather"` and `arguments.city = "Delhi"`
- [x] Recorded the chosen model in `LEARNING.md` (Step-9 uses it for `Ollama:Model`)

**Trap.** First answer is slow (model loading). Only the warm answer counts.

**Dependencies.** Step-1a.

**Acceptance criteria**

- [x] `ollama list` shows `qwen2.5:3b` (1.9 GB)
- [x] Warm answer arrived in 1.49 s (under 20 s)
- [x] Tool-call test returned `tool_calls` with `get_weather` and `city: "Delhi"`
- [x] `/api/tags` responded on :11434

**Status.** Completed (2026-09-25 — evidence: `ollama list`, warm `ollama run` 1.49 s, `/api/tags`, `/api/chat` returned `get_weather` for Delhi)

> ✅ **Ready-to-code gate:** Step-13 and Step-1a…1e are all `Completed` before Step-2 starts.

---

## Phase A — Application

### Step-2 — Solution skeleton (Clean Architecture layout)

**Scope.** Every project of SPEC §15.5 exists with the correct references, the whole system starts
with one command, SQL has three empty databases, and the gateway routes to every service.
⛔ No business code — only health checks.

**Implementation**

- [x] `.gitignore` (.NET, Node, macOS `.DS_Store`), `.editorconfig`
- [x] `Directory.Build.props` (net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors, analyzers) and `Directory.Packages.props` (central versions) — CP-01
- [x] `dotnet new aspire -n EventHub` → move into `src/Aspire/`
- [x] Per service create `*.Domain`, `*.Application`, `*.Infrastructure` (classlib) and `*.Api` (webapi) for Identity, Catalog, Booking; `Agent.Application`, `Agent.Infrastructure`, `Agent.Api`; `EventHub.Gateway` (web); `EventHub.BuildingBlocks`, `EventHub.SeedData` (classlib). No test projects.
- [x] Project references exactly as CA-01…CA-04 and CA-07 (Domain → BuildingBlocks; Application → Domain; Infrastructure → Application; Api → Infrastructure + ServiceDefaults; ServiceDefaults → BuildingBlocks for Result mapping and `ICurrentUser`)
- [x] AppHost: `AddParameter("sql-password", secret: true)` passed to `AddSqlServer("sql", password: sqlPassword)`, then `.WithDataVolume().WithLifetime(ContainerLifetime.Persistent)`; databases `identitydb`, `catalogdb`, `bookingdb`; each API `WithReference` + `WaitFor` its DB; Booking → Catalog; Agent → Catalog + Booking; Gateway → all four + `WithExternalHttpEndpoints()`
- [x] AppHost: secret parameters `jwt-key` (≥ 32 chars) and `booking-service-key` (≥ 32 random chars) in AppHost user-secrets → `Jwt__Key` on all APIs; the service key only on Booking and Catalog
- [x] Gateway: `Yarp.ReverseProxy` + `Microsoft.Extensions.ServiceDiscovery.Yarp`; routes `/identity`, `/catalog`, `/booking`, `/agent` with `PathRemovePrefix`; destinations `http://<name>`; port 5100. Catalog routing explicitly excludes `/internal/*` (AR-09).
- [x] `gateway.http`: `GET http://localhost:5100/<service>/health` ×4
- [x] Create `DECISIONS.md` (Decision · Why · Trade-off) and `LEARNING.md`

**Trap.** First run takes ~1 min (SQL under Rosetta) — `WaitFor` prevents crash loops.

**Dependencies.** Step-1b, Step-1c, Step-1d, Step-1e.

**Acceptance criteria**

- [x] `dotnet build` succeeds with 0 warnings
- [x] `dotnet run --project src/Aspire/EventHub.AppHost` → every resource `Running` in the dashboard
- [x] `identitydb`, `catalogdb`, `bookingdb` exist in the SQL container
- [x] All 4 health requests through :5100 return `Healthy`
- [x] Start, stop and start AppHost again without deleting the SQL volume; all three databases remain healthy
- [x] `git diff --cached` plus a secret scanner (for example `gitleaks protect --staged`, if installed) shows no secret **values**; configuration key names and documented demo credentials are allowed

**Status.** Completed (2026-09-26 — evidence: `dotnet build EventHub.sln`, Aspire dashboard, `gateway.http`, SQL query, restart persistence check)

---

### Step-20 — BuildingBlocks: mediator, CQRS, Result

**Scope.** The shared plumbing every service uses: a hand-written mediator with pipeline behaviors,
command/query contracts, the Result pattern and its mapping to HTTP. ⛔ No service use cases; no
architecture checks (Later L-3).

**Implementation**

- [x] `EventHub.BuildingBlocks/Messaging`: `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<…>`, `IQueryHandler<…>`, `ISender` + `Sender` (resolves the handler from DI and runs the behavior chain), `IPipelineBehavior<TRequest,TResponse>`
- [x] `AddMediator(params Assembly[])` — registers handlers, validators and behaviors by assembly scanning
- [x] Behaviors in order: `LoggingBehavior` → `ValidationBehavior` (FluentValidation → `Error.Validation`, handler not called) → `PerformanceBehavior` (warn > 500 ms) — CQ-04
- [x] `Results/`: `Result`, `Result<T>`, `Error(Code, Message, ErrorType)`, `ErrorType` = Validation · NotFound · Conflict · Forbidden · Unauthorized · Unavailable · Unprocessable
- [x] `Domain/`: `Entity<TId>` base; `Security/ICurrentUser` holds only authenticated user id and role as shared technical request context
- [x] ServiceDefaults: `result.ToHttpResult()` mapping `ErrorType` → 400/404/409/403/401/503/422 ProblemDetails (validation errors as field dictionary); global `IExceptionHandler` → 500 ProblemDetails, logged
- [x] Proof endpoint (Development only) in `Identity.Api`: `POST /debug/echo` sends an `EchoCommand { message }` with a validator (required, ≤ 20 chars); `?fail=<ErrorType>` makes the handler return that error; `?throw=true` throws — lets you see the mediator working before any real use case exists
- [x] `debug.http` with the echo cases
- [x] DECISIONS: why a hand-written mediator (MediatR v13+ is commercial; learning), CQRS-lite, Result vs exceptions

**Dependencies.** Step-2.

**Acceptance criteria**

- [x] Valid echo → 200, and the Aspire logs show Logging → Validation → Performance → handler, in that order
- [x] Invalid echo (empty message) → 400 ProblemDetails with the field error, and no handler log line
- [x] `?fail=NotFound|Conflict|Forbidden|Unavailable|Unprocessable` → 404 / 409 / 403 / 503 / 422 ProblemDetails
- [x] `?throw=true` → 500 ProblemDetails with no stack trace in the response, and the exception in the logs

**Status.** Completed (2026-09-26 — evidence: `dotnet build EventHub.sln`, `src/Services/Identity/Identity.Api/debug.http`, Aspire console logs)

---

### Step-3 — Identity service

**Scope.** Register, login, me, user admin; JWT issued; every API can validate tokens. Built in all
four layers with commands and queries. ⛔ No UI, no refresh tokens.

**Implementation**

- [x] **Domain:** `User` (private setters, `User.Create(...)` factory, `ChangeRole(...)`), `Roles` constants, `UserErrors`
- [x] **Application:** commands `RegisterUser`, `Login`, `ChangeUserRole`; queries `GetCurrentUser`, `ListUsers` — each with handler + validator; ports `IUserRepository`, `IUserQueries`, `IUnitOfWork`, `IPasswordHasher`, `IJwtTokenGenerator`; use shared BuildingBlocks `ICurrentUser`
- [x] **Infrastructure:** `IdentityDbContext` (`AddSqlServerDbContext`), `UserRepository`, `PasswordHasherAdapter` (ASP.NET Core `PasswordHasher`), `JwtTokenGenerator` (FR-ID-03, `JwtOptions` validated at start), seed SD-02 — fixed GUIDs as constants in `EventHub.SeedData.DemoUsers`
- [x] **Api:** thin endpoints for SPEC §6.3 using `ISender` + `ToHttpResult()`
- [x] ServiceDefaults: `AddEventHubAuth()` (JwtBearer, `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`, policies `Organizer`, `Admin`) and the BuildingBlocks `ICurrentUser` implementation from `HttpContext` — used by all APIs
- [x] Create and commit the initial Identity EF migration; call `MigrateAsync()` before idempotent seeding (CP-10)
- [x] `identity.http` covering every criterion

**Dependencies.** Step-20.

**Acceptance criteria**

- [x] Login succeeds for all 5 demo users; decoded token shows `sub`, `email`, `name`, `role`, `exp` ≈ now + 2 h (FR-ID-02/03)
- [x] Wrong password and unknown email both return 401 with an identical message (FR-ID-02)
- [x] `/users` → 403 with attendee token, 200 with admin token (FR-ID-05)
- [x] `/auth/me` → 401 without token, correct user with it; duplicate register → 409 (FR-ID-01/04)

**Status.** Completed (2026-09-26 — evidence: `docs/STEP-3-EVIDENCE.md`, `src/Services/Identity/Identity.Api/identity.http`; build 0 warnings)

---

### Step-4 — Catalog service

**Scope.** Public search, owner-managed events, atomic and idempotent seat reservations.
⛔ No bookings or payments; no `rowVersion` (Step-16).

**Implementation**

- [x] **Domain:** `Event` (`Create`, `Update` — capacity ≥ seats booked, `CanBeDeleted`), `SeatReservation` (`Hold`, `Release`), `Categories` constants, `EventErrors`
- [x] **Application:** commands `CreateEvent`, `UpdateEvent`, `DeleteEvent`, `ReserveSeats`, `ReleaseReservation`; queries `SearchEvents`, `GetEventById`, `GetMyEvents`; validators per FR-CAT-06; ports `IEventRepository` (incl. `TryReserveAsync`), `IReservationRepository`, `IEventQueries`, `IUnitOfWork`; use shared BuildingBlocks `ICurrentUser`. Infrastructure implements `IEventQueries` with `AsNoTracking()` DTO projections (CQ-02).
- [x] Ownership rule FR-CAT-04 in the Update/Delete handlers (owner `sub` or Admin → else `Error.Forbidden`)
- [x] **Infrastructure:** `CatalogDbContext`, repositories; `TryReserveAsync` = one transaction: check existing reservation id and matching user/event/quantity, conditional `ExecuteUpdateAsync` on seats, insert `Held` reservation — inside the EF execution strategy (RES-05). Catch a concurrent reservation-PK violation outside the rolled-back transaction, load the winner, and return it only if its inputs match.
- [x] `EventHub.SeedData`: deterministic generator (fixed seed) for SD-03 events **and** SD-04 bookings; Catalog seeds events with `SeatsBooked` = sum of confirmed quantities, plus matching `Held` reservations (SD-05)
- [x] **Api:** public endpoints of SPEC §7.3 plus `/internal/*`; the internal group requires a valid user JWT and constant-time validation of `X-EventHub-Service`. YARP has no route to this group.
- [x] Create and commit the initial Catalog EF migration; call `MigrateAsync()` before seeding (CP-10)
- [x] DECISIONS: why internal seat endpoints require both the user token and a Booking credential, and why gateway exclusion alone is insufficient
- [x] `catalog.http`

**Trap.** Seeded event IDs must match the IDs the booking seed refers to — insert in generator order
into an empty table and assert the IDs after seeding; fail loudly if they differ.

**Dependencies.** Step-3.

**Acceptance criteria**

- [x] `search`, `category`, `maxPrice` change results; paging returns the correct `total` (FR-CAT-01)
- [x] Attendee `POST /events` → 403; organizer2 edits organizer's event → 403; admin → 200 (FR-CAT-03/04)
- [x] Reserve more than `seatsLeft` → 409 and seats unchanged (FR-CAT-07)
- [x] Same `reservationId` reserved twice → seats change once; released twice → returned once (FR-CAT-07/08)
- [x] Two parallel reserves with the same `reservationId` → one seat change; a replay with changed user/event/quantity → 409/403 as applicable
- [x] Calling `/catalog/internal/...` through the gateway → 404; calling the service directly without the Booking credential → 403
- [x] Invalid input (negative price, past date) → 400 with field messages (FR-CAT-06)

**Status.** Completed (2026-09-26 — evidence: `docs/STEP-4-EVIDENCE.md`, `src/Services/Catalog/Catalog.Api/catalog.http`; build 0 warnings)

---

### Step-5 — Booking service

**Scope.** Book (reserve → pay → save, with compensation), own bookings, cancel, stats.
⛔ No UI; no `Idempotency-Key` (Step-16); resilience tuning comes in Step-17.

**Implementation**

- [ ] **Domain:** `Booking` (`Create`, `BeginCancellation(now)`, `CompleteSeatRelease()`; pending-release retries do not run cancellation rules twice), `BookingStatus`, `BookingErrors`
- [ ] **Application:** commands `CreateBooking` (FR-BKG-01 order exactly), `CancelBooking` (FR-BKG-04 state machine); queries `GetMyBookings`, `ListBookings`, `GetBookingStats`; ports `ICatalogClient` (`GetEventAsync`, `ReserveAsync(reservationId, qty)`, `ReleaseAsync(reservationId)` — only what Booking needs, ISP), `IPaymentGateway`, `IBookingRepository`, `IBookingQueries`, `IUnitOfWork`; use shared BuildingBlocks `ICurrentUser`. Infrastructure implements `IBookingQueries` with `AsNoTracking()` DTO projections, zero-filled months and organizer scoping.
- [ ] **Infrastructure:** `BookingDbContext`, repository, `CatalogHttpClient` (typed, `https+http://catalog`, `ForwardTokenHandler` plus `X-EventHub-Service` only on internal seat calls), `FakePaymentGateway` (FR-BKG-02), seed SD-04
- [ ] Catalog unreachable → `Error.Unavailable` → 503 (FR-BKG-07); queries never call Catalog (FR-BKG-09)
- [ ] **Api:** endpoints of SPEC §8.3
- [ ] Create and commit the initial Booking EF migration; call `MigrateAsync()` before seeding (CP-10)
- [ ] DECISIONS: cancellation's `SeatReleasePending` eventual-consistency trade-off and retry behavior without a broker/outbox
- [ ] `booking.http`

**Dependencies.** Step-4.

**Acceptance criteria**

- [ ] Book 2 seats → 201; Catalog `seatsLeft` drops by exactly 2 (FR-BKG-01)
- [ ] `simulatePaymentFailure: true` → 422; `seatsLeft` unchanged (FR-BKG-01/02)
- [ ] Cancel → `Cancelled`, seats returned; another user's booking → 403 (FR-BKG-04)
- [ ] Stop Catalog during cancel → 503 and `SeatReleasePending = true`; restart and repeat cancel → seats returned once and flag cleared
- [ ] Stats: organizer2 sees only their events; attendee → 403 (FR-BKG-06)
- [ ] Catalog stopped → `POST /bookings` 503 while `GET /bookings/mine` still 200 (FR-BKG-07/09)

**Status.** Not Started

---

### Step-16 — Idempotent bookings and optimistic concurrency

**Scope.** A repeated or concurrent booking request cannot reserve or pay twice; two people editing
the same event cannot silently overwrite each other. ⛔ No retry policies (Step-17), no UI (Step-19).

**Implementation**

- [ ] Booking: add `BookingRequest` with unique (`UserId`, `IdempotencyKey`), request inputs, stable `ReservationId`, state and nullable `BookingId`; retain the booking's unique filtered index as a second guard
- [ ] `CreateBooking`: atomically insert/claim `BookingRequest` **before** Catalog/payment. The winner uses its stable reservation id and passes the idempotency key to the idempotent fake payment adapter. A matching concurrent loser observes `Processing`, waits with a bounded delay/cancellation token, then returns the completed booking; it never calls Catalog/payment. A different payload with the same key → 409.
- [ ] A stale `Processing` request is resumed with the stored reservation/payment identities. Known failures (400/404/409/422) compensate, then delete the claim; never delete it before compensation succeeds. Document the unavoidable crash window and recovery behavior in `DECISIONS.md`.
- [ ] Fake payment stores/derives one result per (`UserId`, `IdempotencyKey`) so resume cannot charge twice; log one payment invocation/reference per claimed request
- [ ] Catalog: `RowVersion` (`IsRowVersion()`) on `Event`; `EventDto.rowVersion` base64; `UpdateEvent` sets the original value → `DbUpdateConcurrencyException` → `Error.Conflict` (FR-CAT-10)
- [ ] Add and commit Booking and Catalog EF migrations for these schema changes; apply them with `MigrateAsync()` without deleting the persistent volume
- [ ] DECISIONS: why a pre-side-effect request claim is required (a unique index on the final booking is too late), stale-claim recovery, and its limitations
- [ ] `.http` cases for all criteria (parallel test via a small script firing 2 requests at once)

**Dependencies.** Step-5.

**Acceptance criteria**

- [ ] Same `Idempotency-Key` sent twice → same booking id, seats reduced once (FR-BKG-08)
- [ ] Two parallel requests with the same key → both receive the same booking; exactly one booking, one reservation and one payment reference exist
- [ ] Same key with a changed event or quantity → 409 and no new side effect
- [ ] Different keys → two bookings
- [ ] Existing Step-5 data remains after migrations are applied
- [ ] `PUT /events/{id}` with a stale `rowVersion` → 409 reload message; fresh one → 200 (FR-CAT-10)

**Status.** Dependent (Step-5)

---

### Step-17 — Resilience pipeline

**Scope.** Service-to-service calls survive transient failures and fail fast when a service is really
down. ⛔ No chaos toggle (Later L-4), no gateway rate limiting (Later L-2), no UI (Step-19).

**Implementation**

- [ ] ServiceDefaults: `AddEventHubResilience()` — `AddResilienceHandler` with total timeout → retry → circuit breaker → attempt timeout, values bound from `Resilience:*` options (RES-01)
- [ ] Retry `ShouldHandle` honours RES-02: GETs always; POSTs only when the client is marked idempotent (Booking's Catalog client — reserve/release carry `reservationId`) or the request has an `Idempotency-Key` header
- [ ] Replace Step-5's default handler on `CatalogHttpClient` with this pipeline
- [ ] Map `BrokenCircuitException` / `TimeoutRejectedException` / `HttpRequestException` → `Error.Unavailable` → 503 + `Retry-After` (RES-03)
- [ ] Confirm EF Core retrying execution strategy is on for all three DbContexts; explicit transactions wrapped (RES-05)
- [ ] Logging of retries, breaker transitions, timeouts with trace id (RES-13)
- [ ] `resilience.http` + `LEARNING.md` notes with screenshots of the Aspire traces
- [ ] DECISIONS: chosen numbers and why timeouts nest (SPEC §14.3)

**How to see it working without a chaos switch:** stop Catalog from the Aspire dashboard (⏹) and
restart it (▶) while sending bookings from `resilience.http`.

**Dependencies.** Step-5.

**Acceptance criteria**

- [ ] Catalog stopped → one booking request shows 3 retried Catalog calls in the Aspire trace, then returns 503 with `Retry-After` (RES-01/03)
- [ ] Keep sending while stopped → after ~5 failures the circuit opens and bookings return 503 in < 100 ms (RES-03)
- [ ] Restart Catalog, wait 15 s → the next booking succeeds (half-open → closed) (RES-01)
- [ ] The log shows the breaker transitions opened → half-open → closed with trace ids (RES-13)

**Status.** Dependent (Step-5)

---

### ~~Step-18 — Gateway rate limiting, timeouts, health~~

**Retired by the owner** — moved to "Later — for understanding" as **L-2**. The `/agent` route
timeout it contained moved into Step-9.

---

### Step-6 — Angular shell and authentication

**Scope.** App shell with login, register, token handling, guards, role-based menu, and the folder
structure of CP-09. ⛔ Feature pages are placeholders only.

**Implementation**

- [ ] `ng new web --routing --style=scss --ssr=false` (strict); `ng add @angular/material`; ESLint (`ng add @angular-eslint/schematics`)
- [ ] Folders: `core/` (auth service, interceptors, guards, models), `shared/` (layout, reusable UI), `features/` (events, bookings, organizer, admin, chat — placeholders)
- [ ] `proxy.conf.json`: `/api` → `http://localhost:5100`, `pathRewrite` removes `/api`; wired into `npm start`
- [ ] `AuthService` (signals): login, register, logout, `currentUser`, `role`; token in localStorage; `jwt-decode`; auto-logout at `exp` (FR-UI-02)
- [ ] Functional interceptor: Bearer header; 401 → logout + redirect (FR-UI-02)
- [ ] `authGuard`, `roleGuard(roles)` (FR-UI-04); toolbar + role-filtered menu (FR-UI-03)
- [ ] Login and Register pages with typed reactive forms (FR-UI-01)
- [ ] DECISIONS: localStorage vs HttpOnly cookie

**Dependencies.** Step-3.

**Acceptance criteria**

- [ ] 👤 All 5 demo users log in; Attendee, Organizer and Admin menus differ (FR-UI-03)
- [ ] 👤 Browser refresh keeps the user logged in (FR-UI-02)
- [ ] 👤 Logged out → `/my-bookings` redirects to login; attendee → `/dashboard` redirects to Events (FR-UI-04)
- [ ] 👤 Garbage token in localStorage → next API call redirects to login (FR-UI-02)
- [ ] 👤 Register logs the new user in as Attendee; `ng lint` passes (FR-UI-01, CP-09)

**Status.** Not Started

---

### Step-7a — Events list and details

**Scope.** Anyone can browse, filter and view events. ⛔ Book button present but does nothing.

**Implementation**

- [ ] `core/api/catalog-api.service.ts` → `/api/catalog/events`
- [ ] `features/events`: filter bar (search 300 ms debounce, category, city, date range, max price), cards, paginator
- [ ] `/events/:id` details page; "Sold out" badge; loading and empty states

**Dependencies.** Step-4, Step-6.

**Acceptance criteria**

- [ ] 👤 Each filter changes the results; clearing restores them (FR-UI-05)
- [ ] 👤 Details page seats-left equals the API value (FR-UI-06)
- [ ] 👤 A sold-out event shows the badge (FR-UI-06)
- [ ] 👤 Logged-out user can use both pages

**Status.** Dependent (Step-4, Step-6)

---

### Step-7b — Booking and My Bookings

**Scope.** Logged-in users book tickets and manage their bookings. ⛔ No organizer screens.

**Implementation**

- [ ] `core/api/booking-api.service.ts`
- [ ] Book button: guest → login with return URL
- [ ] Booking dialog: quantity 1–10 and ≤ seats left, live total, Confirm (FR-UI-07)
- [ ] Development builds only: clearly labelled "Simulate payment failure" demo toggle; production configuration removes it
- [ ] Snackbar messages for 201 / 409 / 422 / 503
- [ ] `features/bookings`: `/my-bookings` table with Cancel + confirmation (FR-UI-08); re-fetch seats after changes

**Dependencies.** Step-5, Step-7a.

**Acceptance criteria**

- [ ] 👤 attendee books 2 tickets → success message → appears in My Bookings (FR-UI-07/08)
- [ ] 👤 Seats left on the details page drops by 2
- [ ] 👤 Forced payment failure shows a friendly message and no booking is created
- [ ] 👤 Cancel changes status and restores seats

**Status.** Dependent (Step-5, Step-7a)

---

### Step-8a — Organizer: My Events

**Scope.** Organizers create, edit, delete their events. ⛔ No charts.

**Implementation**

- [ ] `features/organizer`: `/my-events` table (title, date, price, capacity, sold, actions); Admin also sees an Organizer column
- [ ] Create/edit typed reactive form with FR-CAT-06 rules; sends `rowVersion` on edit
- [ ] Delete with confirmation; shows the API's 409 message

**Dependencies.** Step-4, Step-6, Step-16.

**Acceptance criteria**

- [ ] 👤 A new event appears in My Events and in the public Events list (FR-UI-09)
- [ ] 👤 Invalid input shows field errors and does not submit
- [ ] 👤 Edits persist after refresh
- [ ] 👤 Open the same event in two tabs; after the first save, the second save shows the stale `rowVersion` reload message
- [ ] 👤 Deleting an event with bookings shows the 409 message

**Status.** Dependent (Step-4, Step-6, Step-16)

---

### Step-8b — Dashboard

**Scope.** KPI tiles and three charts. ⛔ No admin user management.

**Implementation**

- [ ] `ngx-echarts` + `echarts`
- [ ] KPI tiles: revenue, tickets sold, bookings (from `/bookings/stats`); average fill % (from `/events/mine`)
- [ ] Charts: revenue by month (bar), top 10 events (horizontal bar), status split (donut)
- [ ] Loading/empty states, INR formatting, Refresh button, responsive resize

**Dependencies.** Step-5, Step-6.

**Acceptance criteria**

- [ ] 👤 All 3 charts render from seed data (FR-UI-10)
- [ ] 👤 organizer2's numbers differ from organizer's; Admin sees both combined (FR-BKG-06)
- [ ] 👤 After a new booking, Refresh changes the numbers
- [ ] 👤 Charts resize with the window

**Status.** Dependent (Step-5, Step-6)

---

### Step-8c — Admin: users and all bookings

**Scope.** Admin changes roles and sees every booking. ⛔ No agent.

**Implementation**

- [ ] `features/admin`: `/admin/users` table, role dropdown, Save, note "applies at next login"
- [ ] `/admin/bookings`: all bookings with status filter
- [ ] DECISIONS: why a role change needs re-login (stateless tokens)

**Dependencies.** Step-3, Step-5, Step-6.

**Acceptance criteria**

- [ ] 👤 Admin promotes attendee2 to Organizer; after re-login attendee2 sees the Organizer menu (FR-ID-05)
- [ ] 👤 Before re-login attendee2 still gets 403 on `POST /events` (stateless token)
- [ ] 👤 Non-admin cannot open either page and the API returns 403 (FR-UI-11)

**Status.** Dependent (Step-3, Step-5, Step-6)

---

### Step-19 — UI resilience

**Scope.** The UI survives backend trouble gracefully and never double-books. ⛔ No new features.

**Implementation**

- [ ] `core/interceptors/retry.interceptor.ts`: GET only, 2 retries with 500 ms / 1 s delay on status 0 / 502 / 503 / 504 (FR-UI-14)
- [ ] Booking dialog: `crypto.randomUUID()` key created per attempt, reused if the same attempt is re-sent; Confirm disabled in flight (FR-UI-13)
- [ ] `shared/panel-state` component (loading · error + Retry · empty) used by events list, details, My Bookings and each dashboard panel (FR-UI-15)
- [ ] Messages: 429 "Too many requests, wait a moment" (the agent's busy limit), 409 on edit "changed by someone else — reload", 503 "Service temporarily unavailable"

**Dependencies.** Step-7b, Step-8a, Step-8b, Step-16.

**Acceptance criteria**

- [ ] 👤 Stop Booking → Events pages work; dashboard panels show "Temporarily unavailable" + Retry; restart + Retry loads them (RES-12)
- [ ] 👤 Stop Catalog → My Bookings still shows data (FR-BKG-09)
- [ ] 👤 Double-clicking Confirm creates exactly one booking (FR-UI-13)
- [ ] 👤 Browser network tab: a GET is retried on 503, a POST is not (FR-UI-14)
- [ ] 👤 Edit the same event in two tabs → the second save shows the reload message (FR-CAT-10)

**Status.** Dependent (Step-7b, Step-8a, Step-8b, Step-16)

---

## Phase B — AI agent

Built by the coding agent exactly like Phase A — build, verify, update the board, move on. The owner
reads the code later, so Phase B code is written **to be studied**: every file has a short header
comment saying what it does and why, and each step leaves a walkthrough.

### Step-14 — The agent loop by hand (study material)

**Scope.** A small program that shows exactly what an agent is, with no AI library: raw HTTP to
Ollama, hand-written tool definitions, parse the model's tool request, run it, send the result back,
repeat. Built by the coding agent; the owner studies it afterwards.
⛔ No Microsoft.Extensions.AI, no write tools, not part of the product.

**Implementation**

- [ ] `src/Tools/Agent.Playground` console app (~100 lines, plain `HttpClient` + `System.Text.Json`)
- [ ] Two tools as JSON schema by hand: `search_events(search?, category?, maxPrice?)`, `get_event_details(eventId)` → call Catalog through the gateway (public endpoints, no token needed)
- [ ] Loop: send messages + tools → if `tool_calls`, execute each, append `role: "tool"` messages, send again → stop on a plain answer or after 5 rounds (loop guard)
- [ ] Print every round: what was sent, what the model asked for, what the tool returned
- [ ] Comments at each stage of the loop explaining what is happening and why (the code is study material)
- [ ] `src/Tools/Agent.Playground/WALKTHROUGH.md`: how to run it; a real captured run annotated round by round; the four message roles (system, user, assistant, tool); what "ReAct" means here; why the loop guard exists; which lines Microsoft.Extensions.AI replaces in Step-9

**Dependencies.** Step-1e, Step-4.

**Acceptance criteria**

- [ ] "Music events under ₹1000?" → console shows round 1 with a `search_events` tool call and its arguments
- [ ] The final answer lists events that exist in the Catalog database
- [ ] A tool returning an error (unknown event id) is explained by the model, not crashed on
- [ ] Forcing a looping prompt stops at round 5 with a clear message
- [ ] `WALKTHROUGH.md` exists and contains a real annotated run (not an invented one)

**Status.** Not Started

---

### Step-9 — Agent service (read-only)

**Scope.** The real agent inside EventHub, in Clean Architecture, with three read-only tools, a
timeout and a bulkhead. ⛔ Cannot book, cancel or read stats.

**Implementation**

- [ ] **Agent.Application:** command `SendChatMessage` (history in, reply out); `EventHubTools` with `[Description]` on methods and parameters; ports `ICatalogApi`, `IBookingApi`; system prompt (FR-AGT-02) as a constant with the date from `TimeProvider`
- [ ] **Agent.Infrastructure:** `AddChatClient(new OllamaApiClient(...)).UseFunctionInvocation().UseLogging()`; `OllamaOptions` (Endpoint, Model, Timeout = 120 s) validated at start; HTTP adapters for the ports with `ForwardTokenHandler` + `AddEventHubResilience()`
- [ ] Bulkhead: `ConcurrencyLimiter` (2 permits, queue 5) around the LLM call → `Error` → 429 when rejected; Ollama unreachable → 503 "assistant offline"; **no retry** on the chat call (FR-AGT-07)
- [ ] Compact JSON tool results; errors as short text
- [ ] **Agent.Api:** `POST /chat`, `.RequireAuthorization()`
- [ ] Gateway: `/agent` route timeout 120 s (YARP route `Timeout`), so slow model answers are not cut off (NFR-04)
- [ ] `agent.http`: login + 5 questions + a parallel-requests script
- [ ] `src/Services/Agent/WALKTHROUGH.md`: how a chat request flows through the layers; side-by-side of the Step-14 hand-written loop and what `UseFunctionInvocation()` now does for you; where the user's token travels

**Dependencies.** Step-1e, Step-5, Step-17.

**Acceptance criteria**

- [ ] "Music events under ₹1000?" → events that exist, correct prices; Aspire logs show `SearchEvents` and its arguments (FR-AGT-05)
- [ ] Made-up event → "not found"; "write me a poem" → politely declined (FR-AGT-02)
- [ ] Warm reply in under 30 s (NFR-04)
- [ ] Ollama stopped → 503 "assistant offline"; 8 parallel requests → some 429 "busy", none crash (FR-AGT-07)

**Status.** Dependent (Step-1e, Step-5, Step-17)

---

### Step-10 — Agent actions and permissions

**Scope.** The agent books, cancels and reports stats — always as the user, only after confirmation,
never twice. ⛔ No UI.

**Implementation**

- [ ] Tools `BookTickets` (new `Idempotency-Key` per tool call — FR-AGT-08), `CancelBooking`, `GetSalesStats`
- [ ] System prompt: confirmation rule FR-AGT-03
- [ ] Tool results: 403 → "Forbidden", 409 → "Not enough seats", 422 → "Payment failed"
- [ ] DECISIONS: prompt confirmation is UX only; security = API + user token (AR-06)

**Dependencies.** Step-9, Step-16.

**Acceptance criteria**

- [ ] "Book 2 tickets for <event>" → agent asks to confirm; after "yes" the booking is in `/bookings/mine` (FR-AGT-03)
- [ ] Attendee asks for sales stats → refused; logs show 403 from Booking (FR-AGT-04)
- [ ] Organizer asks for sales stats → summary matches `/bookings/stats`
- [ ] "Cancel my booking for <event>" → finds it, confirms, cancels

**Status.** Dependent (Step-9, Step-16)

---

### Step-11 — Chat widget

**Scope.** Floating chat panel connected to the agent. ⛔ No new agent capabilities.

**Implementation**

- [ ] `features/chat`: floating button bottom-right, visible only when logged in
- [ ] Panel: message list (user/assistant styles), input, Enter to send, auto-scroll
- [ ] History in a signal; full history sent each time (FR-AGT-01)
- [ ] "Thinking…" indicator, input disabled while waiting, messages for 429 / 503 / timeout
- [ ] Clear history on logout; refresh My Bookings after the agent books or cancels

**Dependencies.** Step-6, Step-9.

**Acceptance criteria**

- [ ] 👤 A 3-turn conversation works and the agent remembers earlier turns (FR-UI-12)
- [ ] 👤 Widget hidden when logged out
- [ ] 👤 Log out, log in as another user → empty chat
- [ ] 👤 A slow reply shows the indicator; UI stays responsive

**Status.** Dependent (Step-6, Step-9)

---

## Phase C — Finish

### Step-12 — Demo readiness

**Scope.** Anyone can run it; you can explain every choice. ⛔ No new features.

**Implementation**

- [ ] `README.md`: what it is, architecture diagram, Clean Architecture + CQRS overview, Mac setup (Phase 0 summary), run commands, demo users, URLs
- [ ] `DECISIONS.md` complete — at least: microservices, DB per service, gateway, Clean Architecture, CQRS-lite, hand-written mediator, Result pattern, EF migrations, symmetric JWT vs RS256, REST vs pub/sub, compensation vs saga, retryable cancellation, data duplication, token forwarding + internal service credential, idempotency claim, retry/breaker/timeout numbers, agent permissions, local LLM; note gateway rate limiting is deferred to L-2
- [ ] `DEMO.md` (~8 minutes): role menus → book → forced payment failure → stop Catalog (retries in trace) → circuit opens (fast 503) → My Bookings still works → restart, recovers → dashboard → agent search + book → attendee refused stats → Aspire trace of one booking
- [ ] Data reset: stop AppHost, remove the SQL volume, restart → seed returns
- [ ] Final commit, tag `v1.0`

**Dependencies.** Step-7b, Step-8a, Step-8b, Step-8c, Step-10, Step-11, Step-17, Step-19.

**Acceptance criteria**

- [ ] Fresh clone into a new folder runs using only the README
- [ ] `dotnet build` has 0 warnings; `ng lint` passes
- [ ] 👤 `DEMO.md` runs start to finish twice without errors
- [ ] 👤 You can explain each DECISIONS entry aloud in under a minute

**Status.** Dependent (Step-7b, Step-8a, Step-8b, Step-8c, Step-10, Step-11, Step-17, Step-19)

---

### ~~Step-15 — Microsoft Agent Framework comparison~~

**Retired by the owner** — moved to "Later — for understanding" as **L-1**.

---

## Later — for understanding

Removed from v1.0 by the owner to keep the build small. Each item is written so it can be picked
up after Step-12 and bolted on without changing what exists. When one is picked up, it becomes a new
step with the **next free number** (Step-21, Step-22, …) and goes back on the board.

| #       | What                                                                                                                                                                                                   | What you learn                                                                         | How it plugs in                                                                                        | Spec                   |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ---------------------- |
| **L-1** | Rebuild the agent on **Microsoft Agent Framework** at `/agent/v2/chat`, same tools and ports; compare in DECISIONS (lines of code, control over the loop, memory/workflows/multi-agent, debuggability) | When a full agent framework is worth it over Microsoft.Extensions.AI                   | New endpoint in `Agent.Api` reusing `ICatalogApi` / `IBookingApi`; check the framework's licence first | §3                     |
| **L-2** | **Gateway protection:** rate limits (10 logins/min per IP, 100 req/min per user, 10 chats/min), 429 + `Retry-After`, 30 s default route timeout, gateway `/health` that lists every service            | Throttling, abuse protection, where timeouts belong                                    | ASP.NET Core rate limiter + YARP route metadata in `EventHub.Gateway`                                  | RES-04, RES-08, RES-10 |
| **L-3** | **Architecture checks:** a `tests/Architecture.Tests` project (NetArchTest) that fails the build when a layer rule is broken                                                                           | How to enforce Clean Architecture automatically                                        | New test project referencing every layer; no production code changes                                   | CA-09                  |
| **L-4** | **Chaos testing:** `Chaos:Enabled`, `Chaos:FaultRate`, `Chaos:LatencyMs` inject faults/latency into Booking's Catalog client (Polly chaos strategies, Development only)                                | Proving retries, timeouts and the circuit breaker on demand, without stopping services | Two strategies added inside the retry in `AddEventHubResilience()`                                     | RES-11                 |

---

## Open Issues log

| ID    | Step | Problem | Tried | Resolution |
| ----- | ---- | ------- | ----- | ---------- |
| OI-01 |      |         |       |            |
