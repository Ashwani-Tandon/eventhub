# EventHub — Learning Notes

These notes follow the execution steps in `EXECUTION_PLAN.md`; the project and layer overview is in `PROJECT_STRUCTURE.md`.

## Step-13 — Local project setup

The four planning files are in `/Users/apple/Personal Projects/Event Booking Platform`.
Local Git records project history; GitHub is unnecessary for development on this Mac.
VS Code and its Codex extension are installed, and the agent can read the execution board.
Break it on purpose: `git log` before the first commit reports no commits; the owner has deferred that commit.

## Step-1a — Base tools

`sysctl hw.memsize` reported 8589934592 bytes (8 GB), so Step-1e should use `qwen2.5:3b`.
Rosetta is installed (`pkgutil --pkg-info com.apple.pkg.RosettaUpdateAuto`).
Apple Git 2.39.5 is available; this repository has the owner's name and email configured locally.
The four VS Code development extensions are installed and visible in the extension list.
Break it on purpose: before setup, `command -v code` found nothing; after the VS Code shell command was installed, it found `/usr/local/bin/code`.

## Step-1b — .NET SDK and Aspire

.NET SDK 10.0.301 was already installed, so reinstalling the SDK would have added no value.
Aspire.ProjectTemplates 13.5.3 adds the AppHost and Service Defaults templates used to orchestrate the services later.
The trusted localhost HTTPS certificate lets ASP.NET Core serve local HTTPS without browser trust warnings.
Break it on purpose: the certificate check initially returned “No valid certificate found”; after trusting it, the same check found the valid `CN=localhost` development certificate.

## Step-1c — Docker and SQL Server

Docker Desktop 4.92.0 runs an ARM64 Linux engine with about 4.1 GB available; Rosetta lets it run the AMD64 SQL Server image.
The `hello-world` container proved the client could pull and start an image, while `SELECT @@VERSION` proved SQL Server 2022 actually answered a query.
The temporary `sqltest` container stayed up over a minute, then was removed; the SQL image remains cached for Aspire.
Break it on purpose: a Docker client without permission to reach its socket prints its version but cannot query the engine; granting local socket access made the same check succeed.

## Step-1d — Node and Angular CLI

Homebrew Node 24.21.0 and Angular CLI 22.2.0 are installed and available on the terminal PATH.
The throwaway Angular app built, served HTTP 200 on port 4200, and loaded in the owner's browser; it was then deleted.
This proves the frontend toolchain before creating the real `web/` app in a later step.
Break it on purpose: CLI 22 initially warned that Node 24.14.0 was too old; updating to 24.21.0 removed the mismatch and let the app serve.

## Step-1e — Ollama and model

Ollama 0.32.15 was already installed; opening it once completed local-only onboarding and exposed its API on port 11434.
The 8 GB machine uses `qwen2.5:3b`; its warm one-sentence response took 1.49 seconds, well below the 20-second limit.
The model returned a structured `get_weather` tool call with `city: "Delhi"`, proving that it can drive the agent loop instead of merely generating text.
Break it on purpose: before Ollama was running, `/api/tags` could not connect; after local startup, it returned the model inventory and advertised the `tools` capability.

## Step-2 — Solution skeleton

The solution now separates each business service into Domain, Application, Infrastructure, and API projects; project references make dependencies point inward and prevent accidental database or framework coupling.
Aspire starts SQL Server, three persistent service-owned databases, four APIs, and the YARP gateway as one dependency graph; `WaitFor` keeps dependent resources from racing SQL during startup.
Secrets live in .NET user-secrets, while central package/build files make nullable analysis, analyzers, warnings-as-errors, and dependency versions consistent across every project.
Break it on purpose: request `http://localhost:5100/catalog/internal/health`; the gateway should return 404 because Catalog's internal surface is deliberately not public.
