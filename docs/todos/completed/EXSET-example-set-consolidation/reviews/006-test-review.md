# EXSET-006 test review — 2026-10-15

## Round 1 — CONCERNS (no veto-tier; one callout)

**Reviewer:** test-reviewer, against `e6ac58b`, diffed from `82580c3`. Logs green:
- main build (`006-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`006-design-build.log`): 0 errors, 0 warnings. It builds `Design.Server`, `Design.Client.Blazor` and `Design.Tests` on both TFMs.
- tests (`006-test.log`, `006-design-test.log`): unit 839, integration 644 + 5 pre-existing skips, Design 151, each on both TFMs. No failures.

The counts went up by what was added. Unit went 835 → 839, from `MakeRemoteDelegateRequestHttpCallTests` (three theory cases and one fact). Design went 141 → 151: 6 in `AspNetCoreHttpTests`, 3 in `ClientErrorHandlingTests` and 1 in `ClientServerTestingTests`. Integration is unchanged.

Every cited test exists, asserts what its bullet says and runs at the declared tier, except where the table says otherwise:
- **1. HTTP: save/fetch, 500, format.**
  - `Order_SavesAndFetches_OverHttp` asserts the fetched order is not new and checks its customer and its single line's quantity.
  - `ServerException_…With500` deletes the order from one copy and then saves a stale copy, so the server's Update throws. It asserts `HttpRequestException` with `StatusCode == InternalServerError`.
  - `Server_AnnouncesItsFormat…` asserts the server's options are `Ordinal` and the response header is `ordinal`.
  - The host is `WebApplicationFactory<Program>` over Design.Server's real `Program`, started in the assembly fixture's `InitializeAsync`. If Development's startup validation fails, every HTTP test fails.
- **2. Auth denials.** These go over HTTP, against the host's `SwitchableOrderAuth`.
  - `DeniedRead_ReturnsNull` asserts a null order and the not-found message. `AuthorizedOrder.Fetch` never returns null when access is allowed, so the null comes from the denial.
  - `DeniedSave_…` asserts the error text, which only the `NotAuthorizedException` catch sets. The `HttpRequestException` branch sets different text.
  - `AllowedSave_…` covers the success path.
- **3. Correlation and cancellation.**
  - Correlation: the client scope's ID equals the operation's return value and the echoed `X-Correlation-Id`, and the server's `ISupportLog` holds an entry with that reference.
  - Cancellation: the gated store holds the server's read open. The client cancels, the client call throws `OperationCanceledException`, and the server's token `IsCancellationRequested`. Both waits are capped at 10 s, so a broken pipe fails the test rather than hanging it.
- **4. Unit.** The theory asserts `ex.StatusCode` for 500, 403 and 404, and that the body text is carried. The fact asserts POST to `/api/neatoo` with the correlation header.
- **6. Testing region.** `aspnetcore-testing` is inside the test. The test asserts 2 remote calls, the fetched customer, and the row in the server's store.

**Sacred-test check: clean.**
- `DesignClientServerContainers.cs` gains one `AddSingleton<ISupportLog, InMemorySupportLog>()` each on server and local. Nothing was removed.
- `TestingSamples.cs` changes one comment.
- `EmployeeManagement.Tests/Samples/AspNetCore/TwoContainerTestingSamples.cs` is deleted. That is the `aspnetcore-testing` sample test the plan's Scope and Step 5 retire. Its region is re-homed in, and re-pinned by, `ClientServerTestingTests`, which is stronger: it asserts the remote-call count and the store row, and resolves from the client scope instead of local.
- No other existing test changed.

**Order-dependence over the shared host: no live risk found.**
- `host.Auth` is mutated only by `ClientErrorHandlingTests`. Its tests run serially within the class, and each restores the flag in `finally`. No other class uses `AuthorizedOrder` over the host.
- Holds on `host.Store` are keyed by an order id the test has just created, and `_nextId` is assigned under the store's lock, so no two tests share an id.
- The correlation and logging tests filter what they read by the scope's own correlation ID.
- The host is started eagerly in `InitializeAsync`, so the lazy double-start the amendment describes cannot happen.

Explicit-skip spot checks (bullet 7):
- Every `cs` fence on `aspnetcore-integration.md` is inside an embed. The only `bash` block is at `:14`.
- Embed sources are all under `src/Design`, apart from `aspnetcore-middleware-order` (EXSET-009).
- Each of the seven live `aspnetcore-*` regions has exactly one `#region`.
- None of the 8 retired names is defined or embedded anywhere.
- setup.md's only reference-app embed is `skill-blazor-usage` (EXSET-010).

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Callout | **Bullet 5 (logging, `· Should`): the test does not pin "the region enables".**<br>`ServerLogging_WritesTheConfiguredCategories` passes with the `aspnetcore-logging` region deleted. Design.Server's `appsettings.Development.json` already sets `Default: Information`. That matches the region's `Neatoo.RemoteFactory` and `Design` rules, and every `RemoteFactory.Trace` message is `LogLevel.Trace` (`Log.cs:357-477`), which Information already drops. So the `DoesNotContain` check on Trace is vacuous too.<br>What the test pins is "an HTTP call logs under these categories". It does not pin that the region is what turns them on.<br>Suggested shape (integration, same host): assert that the host's `IOptions<LoggerFilterOptions>` holds the region's three rules, or otherwise make one rule load-bearing.<br>Tiered as a callout because `Should` reads as Optional (the conversion checklist's default). If Keith reads `Should` as Required, this is veto-tier. | AC-3 | **Punch** (fixed): the test also asserts the host's `LoggerFilterOptions` holds the region's three rules |
| 2 | Tech-debt | `DeniedSave_…` does not assert that `loaded` is non-null before it calls `Assert.Same(loaded, screen.Order)`. Today, if `Load` returned null, `Save(null!)` would throw first. | — | **Punch** (fixed): `Assert.NotNull(loaded)` before the save |

Could not verify (orchestrator: both logs are now in `reviews/006-mdsnippets.log` and `reviews/006-refapp-build.log`):
- There is no mdsnippets log for 006 in `reviews/`, so I could not check bullet 8's "no missing snippet". I relied on the rendered embeds matching their sources, as in 005.
- There is no `EmployeeManagement.sln` build log for 006 (005 had `005-refapp-build.log`), so I could not check bullet 7's "the reference app builds".

Theoretical (not triaged):
- The `HttpRequestException` branch of `aspnetcore-error-handling` is reached by no input over the host, because `AuthorizedOrder`'s operations never throw. The 500 itself is pinned on `OrderFactory` (bullet 1).
- No test reads `IHostEnvironment`. Development is pinned only through `WebApplicationFactory`'s default and the startup validation it triggers.
- The `SwitchableOrderAuth` state is safe only while a single test class uses it. A second class that uses `AuthorizedOrder` over the host would race.
