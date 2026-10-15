# EXSET-006 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `e6ac58b` against `82580c3`). **Verdict:** APPROVED. No veto-tier findings.

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`006-build.log`).
- Design solution: 0 warnings (`006-design-build.log`).
- Reference app: 0 warnings (`006-refapp-build.log`).

**Tests.** 0 failed on both TFMs: unit 839, integration 644 + 5 skipped (Showcase performance and RelayTiming, pre-existing), Design 151. Unit rose 835 → 839 (`MakeRemoteDelegateRequestHttpCallTests`) and Design rose 141 → 151 (6 + 3 + 1 new), so both runs exercised this change. `006-mdsnippets.log` shows no missing snippet.

The deliverable landed as scoped:

- **Library fix.** `MakeRemoteDelegateRequestHttpCall.cs:37` passes `response.StatusCode` to the `HttpRequestException(string, Exception?, HttpStatusCode?)` constructor. The message is unchanged. The unit theory pins 500, 403 and 404.
- **HTTP tier.**
  - `DesignHttpHost` is `WebApplicationFactory<Program>` over Design.Server's real `Program`. It is an assembly fixture, started eagerly in `InitializeAsync`.
  - Only the store (gated wrapper) and `IAuthorizedOrderAuth` are replaced, as the amendment records.
  - The client scope registers the same way as `Design.Client.Blazor/Program.cs`.
  - The `public partial class Program;` at `Program.cs:81` sits outside `getting-started-server-program`.
- **Library claims on the page, checked against source.** All of these hold:
  - `UseNeatoo` (`WebApplicationExtensions.cs:18-94`): the fixed `POST /api/neatoo`; correlation middleware for every request that generates an ID when the header is absent and echoes it; `X-Neatoo-Format`; a token linked to `RequestAborted` and `ApplicationStopping`; the static logger factory.
  - `AddNeatooAspNetCore` (`ServiceCollectionExtensions.cs:19-41`): server mode, `TryAddScoped<IAspAuthorize, AspAuthorize>`, the handler, and the options overload.
  - `RegisterMatchingName` (`AddRemoteFactoryServices.cs:197-217`): transient `TryAdd` of the pair and the concrete type.
  - A second call replaces `IServiceAssemblies` (`:62`).
  - The DTO fields.
  - The 200/null and 200/`NotAuthorizedException` denial paths (`ClassFactoryRenderer.cs:313-321, :1083-1091`).
  - The rethrow to 500 (`HandleRemoteDelegateRequest.cs:253-258`).
  - Correlation is per scope (`MakeRemoteDelegateRequest.cs:258-265`) and is never used as a logging scope (no `BeginScope` anywhere).
  - The `Server`, `Factory` and `Serialization` category uses, and `RemoteFactory.Trace` at `LogLevel.Trace` only (`Log.cs:357-477`).
  - Every generated factory method takes an optional `CancellationToken`.
- **Design doctrine.**
  - `SupportRequests._ReportProblem` is `[Remote, Execute] private static _Name`, which is VisionAlignment's command shape (section 2, T19). Its `[Remote]` is justified: the client calls it and it needs the server's `ISupportLog` (D18).
  - `OrderSummary.Fetch` stays the read model's `[Remote]` entry point, and its setters stay public (R-M).
  - `ServerServices.cs`'s header now states the order correctly. "Sets Mode = Server" is gone, and the client `Program`'s "NeatooFactory.Mode" is gone.
  - The `TenantTokenService` registration matches the library: the concrete is registered first, so the convention's `TryAddTransient(t)` skips it.
- **Regions.**
  - Seven live `aspnetcore-*` regions are each defined once. Every embed's line range matches its `#region`/`#endregion` in source at `e6ac58b`.
  - None of the eight retired names is defined or embedded anywhere.
  - `BasicSetupSamples.cs` keeps only `aspnetcore-middleware-order`.
  - Every `cs` fence on the page is an embed.
  - No page links an anchor the rewrite removed.
- **Ledger.**
  - C-19, C-20, I-01, I-12 and T-25 match the diff.
  - C-04 and C-07 move to 013, consistent with the re-split.
  - T-09, T-10 and T-22 overstate slightly; see callouts 2, 3 and 5.

**Sacred tests.** No existing test in `src/Tests` or Design.Tests lost an assertion. `DesignClientServerContainers.cs` gains two registrations. The deleted `TwoContainerTestingSamples.cs` is the `aspnetcore-testing` sample the plan retires, and it was re-pinned more strongly (the test review's finding). The test review's two punches are not re-raised.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **The skill's server setup no longer shows the RemoteFactory registration.**<br>• `skills/RemoteFactory/references/setup.md:20-38` now embeds `getting-started-server-program`, whose registration line is `builder.Services.AddDesignServer();` with the comment "(ServerServices.cs)".<br>• Nothing in the skill defines `AddDesignServer` or shows `AddNeatooAspNetCore` in code. The section after it (`:44`) refers to "the **one** `AddNeatooAspNetCore` call" that the reader never sees.<br>• Before this change, the skill showed `AddNeatooAspNetCore(typeof(Employee).Assembly)`.<br>• CLAUDE.md requires the skill to be self-contained. README and the page both explain `AddDesignServer` (page `:48`, which embeds `readme-server-setup`); setup.md does not.<br>• Confidence High. Reachable by: an agent following the skill's "Server Setup" writes `AddDesignServer()`, which does not exist in its app.<br>• One option: embed `readme-server-setup` after it, as the page does. | AC-3 · Must | **Punch** (fixed): setup.md embeds `readme-server-setup` after the server Program, introduced as the example's composition method |
| 2 | **The event-handler sentence overstates exception propagation.**<br>• `aspnetcore-integration.md:281` says "an exception from one propagates to the operation that raised the event", without qualification.<br>• `factory-events.md:184` and `:250` say post-completion (`AfterCommit`) handler exceptions are logged (9003), swallowed, and "never fail the response". Only `Immediate` and `AfterFlush` propagate.<br>• The ledger's T-09 outcome ("handlers stated as factory-events.md has them") therefore does not quite hold.<br>• Confidence High. Reachable by: a reader with an `AfterCommit` handler who expects its failure to reach the client. | AC-4 · Must (T-09) | **Punch** (fixed): in-transaction handler exceptions fail the operation; `AfterCommit` exceptions are logged and never fail the response, as factory-events.md has it |
| 3 | **The "server shuts down" half of the failure table's last row is likely wrong, and nothing pins it.**<br>• `:156` gives "The client cancels, or the server shuts down → — → `OperationCanceledException`".<br>• On shutdown, `ApplicationStopping` cancels the linked token (`WebApplicationExtensions.cs:71-76`), and the handler rethrows the `OperationCanceledException` (`HandleRemoteDelegateRequest.cs:240-244`). But `RequestAborted` is not cancelled, so ASP.NET Core treats this as an unhandled exception: a 500 while the response has not started. The client then gets `HttpRequestException` (500), not a cancellation.<br>• Only the client-cancel half is pinned (`CancelledCall_CancelsTheServerOperationsToken`). Plan Intent says every HTTP behaviour the page states was watched, and the ledger's T-10 outcome says the table is "pinned over HTTP".<br>• Confidence Medium: reasoned from ASP.NET Core's exception handling, not executed. Reachable by: a reader handling a deploy or restart in the middle of a call.<br>• One option: state the shutdown outcome as a 500, or drop it from the row. | AC-4 · Must (T-10) | **Punch** (fixed): the row is client cancellation only; the Cancellation section says a shutdown cancellation is a server exception, so the call fails as one |
| 4 | **"The generated factories" log is true only of class factories.**<br>• The page (`:262`) and `ServerServices.cs:90-92` say each generated factory logs under its own type name.<br>• Only `ClassFactoryRenderer.cs` emits a logger (`:431`, `:906`, the start and finish lines at `:637-676`). `StaticFactoryRenderer` and `InterfaceFactoryRenderer` emit none. So `SupportRequests.ReportProblem`, the page's own correlation example, and every interface factory write no entry under the user's namespace.<br>• T-10 is recorded as "real log categories".<br>• Confidence High. Reachable by: a reader who filters on their namespace to trace a static command or a domain service and finds nothing.<br>• One option: "each class factory". | AC-4 · Must (T-10) | **Punch** (fixed): "generated class factories" on the page and in `ServerServices.cs`; static and interface factories write no entries |
| 5 | **Two recorded statements disagree with what landed.**<br>• `audit-ledger.md:77` gives T-22's outcome as "aspnetcore-integration and setup's server embeds come from Design". The page still embeds `aspnetcore-middleware-order` from the reference app (`:114-129`), which is correctly deferred to EXSET-009. The row reads as done for this page.<br>• `Design.Server/Program.cs:12-13` still says `AddNeatooAspNetCore()` registers the factory services "and endpoints". The new `ServerServices.cs:62-64` comment and the page (`:61`) both say `UseNeatoo()` maps the endpoint. Step 7 is where Design's statements were to agree with the library.<br>• Confidence High (both); low consequence. Reachable by: the close-out AC-4 trace, which reads ledger outcomes; a reader of Design's server header. | AC-4 · Must | **Punch** (fixed): T-22 row names the middleware-order exception; `Program.cs` says the endpoint is mapped by `UseNeatoo` |

## Could not verify

- The test review's two punches are uncommitted working-tree edits, made at 11:01, after every build and test log (10:55): `AspNetCoreHttpTests.cs` (the `LoggerFilterOptions` rules) and `ClientErrorHandlingTests.cs` (`Assert.NotNull(loaded)`). No log covers them.
  - The second one moves `aspnetcore-error-handling` to `:92-118`, while the page's attribution still reads `#L91-L117`.
  - These edits need a Design build and test, and an mdsnippets run, before they are committed.
- The shutdown outcome in callout 3 was not executed. TestHost and Kestrel are binary packages.

## Theoretical (not triaged)

- The page's denied-read row names `Create` and `Fetch`. Only `Fetch` is watched over HTTP, though both take the same generated read path (`ClassFactoryRenderer.cs:313-321`).
- `bin/Release/net10.0/wwwroot/_framework/dotnet.js` also carries `"applicationEnvironment": "Development"`. So on .NET 10 the split is a build against a publish rather than Debug against Release. The page's sentence ("a Debug build is Development") is not wrong, only narrower.
- `ServerServices.cs:136-138` justifies the `InMemory*` names with "the convention also runs on clients". `Design.Client.Blazor` never calls `RegisterMatchingName`; only the harness's containers do (`DesignClientServerContainers.cs:309`). This is pre-existing reasoning, now extended to `ISupportLog`.
- `advanced-patterns.md:148`'s correlation embed shows `ISupportLog`/`SupportEntry`, which the skill never defines. This is skill prose (Todo 2).
- The library fix is recorded in Plan Amendments, but not in the todo's Discovery Log, where EXSET-004's library fix was. The release notes for the arc's merge will need it.

## Read report

- **Read beyond the brief:**
  - `WebApplicationExtensions.cs`, `ServiceCollectionExtensions.cs` and `AspAuthorize.cs` (constructors)
  - `AddRemoteFactoryServices.cs` in full
  - `MakeRemoteDelegateRequest.cs:100-140, :255-265`
  - `NeatooLoggerCategories.cs`, the `NeatooLogging` call sites, and the `Log.cs` levels
  - `HandleRemoteDelegateRequest.cs:230-258`
  - `ClassFactoryRenderer.cs:290-345, :1075-1095` and its logger emission; the other renderers by grep
  - `factory-events.md` by grep
  - Design.Server's `appsettings*.json`
  - the net10 `dotnet.js` build outputs
  - `CLAUDE-DESIGN.md` by grep
  - `OrderPersistence.cs` (`FindAsync`) and `AuthorizedOrder.cs` (header)
  - `006-test-review.md`, plus `006-mdsnippets.log` and `006-refapp-build.log`, which appeared during the review
  - the working-tree diff
- **Named but unused:**
  - VisionAlignment 0.1/0.2, read by grep only (D12, R-M, section 2's visibility rows)
  - the audit file
  - plan 013
