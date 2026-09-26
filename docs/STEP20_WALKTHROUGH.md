# Step-20 — Follow a request through the mediator

The mediator runs inside each API process. It connects a request to its handler; the gateway connects HTTP requests to services. These are two different parts of the journey.

## Read the code in this order

1. `Identity.Api/Program.cs` registers the mediator and the development endpoint.
2. `Debugging/DebugEndpoints.cs` reads HTTP input, creates `EchoCommand`, calls `sender.Send`, and maps the Result to HTTP.
3. `Debugging/EchoCommand.cs` declares the request data and response. A command represents write intent; a query represents read intent. This demo command simply echoes data.
4. `Messaging/MediatorServiceCollectionExtensions.cs` scans the API assembly at startup, registers handlers and validators with dependency injection (DI), and creates typed dispatch adapters.
5. `Messaging/Sender.cs` looks up the adapter for `EchoCommand` and `Result<EchoResponse>`.
6. `Messaging/RequestDispatcher.cs` gets the handler and behaviors from DI and wraps the handler with them.
7. `LoggingBehavior` calls `ValidationBehavior`, which calls `PerformanceBehavior`, which calls `EchoCommandHandler`.
8. The Result returns through the behaviors to the endpoint. `ResultHttpExtensions.cs` translates it to HTTP.

The behavior's `continuation` is the next operation in the chain. Calling it moves forward; returning without calling it stops execution. Validation uses this to reject invalid input before the handler runs. Logging is outside validation, so it still records a failed outcome.

`Result` means success/failure without data; `Result<T>` adds a success value. `Error` describes an expected failure. Unexpected exceptions travel to `GlobalExceptionHandler`, which logs the stack and returns a generic 500 response with a trace id.

`Entity<TId>` and `ICurrentUser` are shared contracts for later steps. The current-user implementation and real Identity use cases arrive in Step-3.

## Start and inspect the system yourself

1. Open Docker Desktop and wait until its engine is running.
2. Open a terminal in the repository root:

   ```bash
   cd "/Users/apple/Personal Projects/Event Booking Platform"
   dotnet run --project src/Aspire/EventHub.AppHost
   ```

3. Keep this terminal running. Open the dashboard URL printed by Aspire, including its login token if shown. The dashboard port can change between runs.
4. On **Resources**, wait for Identity and Gateway to be Running and SQL/database resources to be healthy. The SQL container may take time on this Mac.
5. Select **Console**, choose **identity** in the Resource selector, and watch its logs. **Structured logs** supports filtering; **Traces** shows the HTTP request journey.
6. In a second terminal, send a valid request:

   ```bash
   curl -i -H 'Content-Type: application/json' \
     -d '{"message":"hello mediator"}' \
     http://localhost:5100/identity/debug/echo
   ```

   Expect 200 and the echoed message. Identity logs show Logging, Validation, Performance, then the echo handler, followed by completion.
7. Change the body to `{"message":""}` and send again. Expect 400 with a `Message` field error. Logs show Logging and Validation, then failure; the handler is skipped.
8. Add `?fail=NotFound` to the URL to get 404. Add `?throw=true` to get a safe 500 response; inspect the exception in Identity logs.
9. Press **Control+C** in the first terminal to stop AppHost. The persistent SQL container/storage can remain; stopping the API processes does not erase the databases.

The reusable requests are in `src/Services/Identity/Identity.Api/debug.http`. If your editor has an HTTP client extension, use its Send Request action; otherwise use the terminal commands above.

Application logs are emitted through .NET logging to the service console and OpenTelemetry, which Aspire displays. We have not configured an application log-file sink. Aspire CLI diagnostic files are under `/Users/apple/.aspire/logs/`; they primarily diagnose orchestration, while the dashboard is the easiest place to inspect API logs.

## Local Git and GitHub

Git history currently lives in this repository's `.git` directory on your Mac. A commit records a local snapshot; it does not upload anything. `git status`, `git log --oneline`, and `git show <commit>` let you inspect changes and history.

There is currently no Git remote configured, so nothing has been pushed to GitHub. To publish, first choose the GitHub account, repository name, and visibility. Create an empty repository on GitHub, copy its URL, then connect and push:

```bash
git remote add origin <your-repository-url>
git push -u origin main
```

GitHub authentication must be available through SSH or an authenticated Git client. Do not put an access token into a committed file or remote URL. Future local commits can then be uploaded with `git push`.
