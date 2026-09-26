# EventHub — Architecture Decisions

## 2026-09-26 — Keep Aspire projects under `src/Aspire`

**Decision.** The backend startup command is `dotnet run --project src/Aspire/EventHub.AppHost`, matching the solution layout in SPEC §15.5.

**Why.** Keeping the AppHost and ServiceDefaults projects together makes the orchestration boundary explicit and matches Step-2's implementation and acceptance criteria.

**Trade-off.** The command is slightly longer than placing AppHost directly under `src/`, but the repository structure is clearer and remains consistent as more projects are added.
