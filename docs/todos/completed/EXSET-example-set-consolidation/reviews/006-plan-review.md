# EXSET-006 plan review — 2026-10-15

**Reviewer:** plan-reviewer. **Verdict:** APPROVED. No veto-tier findings.

Pass A: Keith's 2026-10-15 rulings are kept. The HTTP tier runs over Design.Server; CORS, the second assembly and the environment-chosen format get Design code; authentication, authorization and `aspnetcore-middleware-order` stay with EXSET-009. Two passages lean into EXSET-009's ground, the 403 claim and a denied Save (callout 5). The plan holds to R-A, R-K, R-M, R-O, the snippet rule (pairs, log kept outside the tree, the diff check, delete in the same change) and CLAUDE.md's `[Remote] internal`. The client `Program` stays build-pinned, as ruled in EXSET-005. Deferrals trace to EXSET-008, EXSET-009 and EXSET-010 Plan Index rows and to Todo 2. The Notes' park candidates are still open questions, so they are not deferrals yet.

Pass B reality check:
- **WebApplicationFactory is workable on both TFMs.**
  - Design.Tests already references Design.Server and gets `Design.Server.deps.json`, both `staticwebassets.*.json` files and `Design.Client.Blazor.dll` copied into `bin/Debug/{net9.0,net10.0}`.
  - Mvc.Testing 10.x ships `lib/net10.0` only, so net9 needs a `VersionOverride`. 9.0.13 is on NuGet, which matches the 9.0.13 pattern in `Design.Tests.csproj`. 10.0.3 (the central version) is listed as well.
  - CI's Design build does not pass `--no-restore` (`build.yml:95`), so the new package restores there.
- **`Program` visibility.** The 10.x ASP.NET Core ref pack ships `Microsoft.AspNetCore.App.SourceGenerators.dll`, which makes a top-level `Program` public. The 9.x packs do not. So `WebApplicationFactory<Program>` compiles on net10 and fails with CS0122 on net9 until `Program` is declared public. Step 2 covers this as intent.
- **Library claims hold.** I spot-checked these and they hold as cited:
  - `ClassFactoryRenderer.cs:263-275` and `:1083-1095`
  - `HandleRemoteDelegateRequest.cs:253-258`
  - `MakeRemoteDelegateRequestHttpCall.cs:33-37`
  - `MakeRemoteDelegateRequest.cs:258-265`
  - `WebApplicationExtensions.cs:33-50` and `:62`
  - `NeatooJsonSerializer.cs:67`
  - `FactoryGenerator.Types.cs:410-419` and `:436-444`
  - `OrdinalRenderer.cs:355-362`
  - `NeatooInterfaceJsonTypeConverter.cs:83-94`
  - the logger-category usage. `Remote` and `Authorization` are used only in `GetCategoryName`.

  The client does send `X-Correlation-Id` (`MakeRemoteDelegateRequestHttpCall.cs:29`).
- **The retire list is complete.**
  - The 15 + 4 + 6 embeds are all on the plan's pages, plus `advanced-patterns.md:148` and setup's three embeds.
  - No page outside the scope embeds a region being deleted. `serialization-caveat-*` lives in `Domain/Samples/Collections/`, which is not deleted.
  - The deletion folders hold no region from another family.
  - None of their types or extension methods is used elsewhere in the reference app; the only hits are generated files and a comment at `TestingSamples.cs:362`.
- **mdsnippets and `.razor`.** No region exists in a Design `.razor` file and the plan adds none.

## Callouts (all affect a Must criterion)

| # | Finding | Affects | Reachable by | Triage |
|---|---|---|---|---|
| 1 | **The setup regions shared with EXSET-005's pages change shape.**<br>• Three regions sit on the lines Step 3 changes: `getting-started-server-program` (`Design.Server/Program.cs:58-76`), `readme-server-setup` (`ServerServices.cs:68-70`) and `getting-started-client-program` (`Design.Client.Blazor/Program.cs:63-71`). README (`:196, :206, :232`), getting started (`:49, :59, :89`) and `setup.md:107` embed them.<br>• `UseCors` must sit before `UseNeatoo()` (`:71`), inside the server-program region.<br>• The second assembly must go in the *same* `AddNeatooAspNetCore`/`AddNeatooRemoteFactory` call. A second call re-adds the options and replaces `IServiceAssemblies` (`AddRemoteFactoryServices.cs:57, :62`, last `AddSingleton` wins). That breaks the interface converter's `HasType` for the first assembly (`NeatooInterfaceJsonConverterFactory.cs` `CanConvert`).<br>• The environment-chosen format is that same call's options argument.<br>• The plan's region map has the same problem. In the reference app, `aspnetcore-service-registration`, `-multi-assembly`, `-development`, `-production`, `-custom-serialization`, `serialization-config` and `serialization-debug-named` all show this one call, so "the rest keep their names" puts seven names on one Design line. EXSET-005 chose reuse over duplicate regions (its Plan Amendments). `aspnetcore-minimal-api` also needs a custom route in the same `Program`.<br>• The Constraints protect only `readme-domain-model`. What README and getting started show after this plan is Keith's call. | AC-3 | Step 3 (CORS, second assembly, format) and Step 4 (re-pointing) | **Ask** → Keith: separate regions. README's registration stays one short line; the options and the assembly list are built just above it in their own regions; the server Program gains one `UseCors()`. The seven names map onto distinct Design regions or retire into existing ones |
| 2 | **The process-wide logger factory outlives the test host.**<br>• `UseNeatoo` stores the host's `ILoggerFactory` in a static (`WebApplicationExtensions.cs:23-27` → `NeatooLogging.cs:12, :19-22`).<br>• `NeatooOrdinalConverterFactory` reads it on every `CreateConverter` (`:26`, cache-hit log at `:81`). That runs once per new scoped serializer (`AddRemoteFactoryServices.cs:107-116`).<br>• Once a `WebApplicationFactory` is disposed, the static points at a disposed `LoggerFactory`, and its `CreateLogger` throws `ObjectDisposedException`.<br>• The harness defaults to Ordinal (`DesignClientServerContainers.cs:242`) and never sets the static today. Harness classes running after the HTTP class can therefore fail depending on order.<br>• Two hosts (Development and Production, for the format test) also race for the static, so the logging test's `Neatoo.RemoteFactory.Serialization` entries can land in the other host's factory.<br>• One option: a single host for the assembly's lifetime. The library side joins the Notes' logger park candidates. | AC-3 | Step 2's fixture disposing while later Design.Tests classes serialize Ordinal; Step 3's format and logging tests | **Amend**: the hosts live for the assembly's lifetime (an xunit v3 assembly fixture), so none is disposed while harness tests run; the logging test asserts only categories that do not go through the static. If order-dependent failures appear anyway, STOP. The static joins the park candidates |
| 3 | **The fixture's client format must follow the host's environment.**<br>• `WebApplicationFactory` starts the host in Development, so under the plan the server answers Named.<br>• `AddNeatooRemoteFactory` without options is Ordinal (`AddRemoteFactoryServices.cs:37-40`, `NeatooSerializationOptions.cs:12`).<br>• The ordinal reader demands `StartArray` (`OrdinalRenderer.cs:115-117`), and the client never reads `X-Neatoo-Format`. A mismatched fixture fails every call (500 → `HttpRequestException`).<br>• The fixture needs a test-side copy of the client `Program`'s rule, keyed to the environment each factory runs. | AC-3 | Step 2's first save/fetch test once Step 3's environment switch lands | **Amend**: one format rule in Design.Domain, called by the server, the client `Program` and the fixture |
| 4 | **(Possible) The hosted client's environment on net10.**<br>• The client can read its environment (`builder.HostEnvironment`). On net9 hosted, the server's `UseBlazorFrameworkFiles` sends `Blazor-Environment`, so both sides agree.<br>• If the net10 client's environment is fixed at build time (see Could not verify), a Debug build served with `ASPNETCORE_ENVIRONMENT=Production` pairs a Named client with an Ordinal server.<br>• Every call then fails, and the client half's `[explicit-skip]` build-pin cannot see it. This bears directly on the Constraint "the server and the client pick the same format in the same environment". | AC-3 | Running Design.Server on net10 outside Development from a Debug build | **Amend**: before building the switch, verify how the hosted client learns its environment on each TFM; if the two sides can disagree, STOP and ask |
| 5 | **The 403 and denied-Save outcomes are not pinned, and 403 is authentication.**<br>• Step 5 says the error section states the outcomes "the HTTP tests pin", including `NotAuthorizedException` on a denied Save and "403 from the interface-factory forbid path". Acceptance bullet 1 pins neither.<br>• 403 comes only from the `[AspAuthorize]` path (`AspAuthorize.cs:87-104` → `HandleRemoteDelegateRequest.cs:246-252`). Current State `:181` itself says an interface factory's `NotAuthorizedException` is a 500.<br>• `ForbidAsync` needs an authentication scheme, and the status code is the scheme's (bearer 403, cookie 302). Authentication is EXSET-009's (Keith, 2026-10-15).<br>• Intent `:62` says every status code the page states is one a test watched. | AC-3, AC-4 | Step 5's error section | **Amend**: a denied read (null) and a denied Save (`NotAuthorizedException`) are pinned over HTTP on a class factory with `[AuthorizeFactory]`, which needs no scheme; the 403 path is EXSET-009's |
| 6 | **Watching cancellation conflicts with the no-swap constraint.**<br>• To watch the operation's token cancel over HTTP, the operation must still be running when the client cancels.<br>• The in-memory store (`ServerServices.cs:106`) returns immediately, so a gate is needed: a store that waits on the token, or one the test registers.<br>• Constraint `:87` forbids swapping "anything the page describes", and the page describes passing the token to the store. | AC-3 | Step 3 bullet 1; Acceptance bullet 2 | **Amend**: the cancellation test registers a store that waits on the token (a stand-in for the database); the constraint protects the configuration the page shows, not the store |
| 7 | **Order has no non-public setter to leave out.**<br>• Step 3 pins that the ordinal shape "leaves out a non-public setter". Every Order property is a public `get; set;` (`Order.cs:86-89, :109-110`); the only excluded member is the get-only `Total` (`:90`, `IOrder :44`).<br>• Adding a private setter to Order goes against R-M, and Order's slices (`order-interface`, `readme-domain-model`) are embedded elsewhere. | AC-3, AC-4 | Step 3's serialization bullet | **Re-split** → EXSET-013 |
| 8 | **The Named-fallback conditions are paraphrased short.**<br>• Current State `:197` paraphrases `FactoryModelBuilder.cs:308-313` as "partial, non-nested, non-collection, usable parameterless constructor".<br>• The code also requires at least one ordinal property, and for a record, a primary constructor. | AC-4 | Step 7's "when a type falls back to the Named shape" | **Re-split** → EXSET-013 |
| 9 | **More stale `SerializationTests.cs` claims than the plan lists.**<br>• Step 9 and Current State `:209` cite `:35, :58-66, :95-114, :195-199`. Two more go stale under this plan:<br>• `:41` says interface-typed collections will NOT serialize, against the planned `Order.Lines` interface round-trip.<br>• `:47-49` says circular references are "Handled for mutable types", which is exactly what Step 1 probes. | AC-4 | Step 9 | **Re-split** → EXSET-013 |
| 10 | **A missing embed only warns.**<br>• `mdsnippets.json` sets `"TreatMissingAsWarning": true`, so a page still embedding a deleted region does not fail the run.<br>• "ran clean" needs the log searched for missing-snippet warnings, not just the exit code. | AC-3 | Step 4's deletions, then Step 10 | **Amend**: verification searches the mdsnippets log for missing-snippet warnings |
| 11 | **setup.md has three `aspnetcore-*` embeds, not two.**<br>• They are `:18` `basic-setup` (retires to `getting-started-server-program`), `:46` `multi-assembly` and `:68` `cors`.<br>• Step 8 says "Its two `aspnetcore-*` embeds re-point". | AC-3 | Step 8 | **Amend**: Step 8 names three |
| 12 | **Size: this reads as two deliverables.**<br>• 241 lines, and Acceptance has 13 bullets against a recommended 8.<br>• Half A is the HTTP tier and the server/client configuration: Steps 2, 3 (cancellation, correlation, errors, CORS, format, second assembly, logging), 5 and 8.<br>• Half B is serialization and modes: Steps 1, 3 (Logical, testing region, ordinal/interface/references), 6, 7 and 9. It needs only the in-process harness.<br>• Step 1's STOP would hold all of A hostage.<br>• The seam: B's `serialization-config`, `-debug-named`, `-logging` and `modes-logging` reuse A's format and logging regions, so B follows A.<br>• 12 of 18 plan numbers are issued. | AC-3 | Step 1 stopping on a library ruling | **Re-split** (Keith, 2026-10-15): 006 keeps the HTTP tier, the server/client configuration, `aspnetcore-integration.md` and setup; EXSET-013 takes serialization and modes |

## Could not verify

- That TestServer turns the client's cancellation into `HttpContext.RequestAborted`. TestHost is a binary package and is not in this repo.
- How a net10 hosted WASM client learns its environment: from the build, or from the server's `Blazor-Environment` header. Both the 9.0.13 and the 10.0.3 `WebAssembly.Server` dlls still contain the header name.
- That `LoggerFactory.CreateLogger` throws after `Dispose`. This is from memory of the runtime source; it was not executed here.

## Theoretical (not triaged)

- `UseNeatoo` discards the `MapPost` builder (`WebApplicationExtensions.cs:52`), so the endpoint cannot take `.RequireCors()`. The CORS example has to be a policy applied through `UseCors`.
- Only the `HttpClient` can see the correlation header on the response; `MakeRemoteDelegateRequestHttpCall.cs:40` reads only the body. Pinning "returns on the response" needs a handler on the test client.
- "Runs everything else locally" (Step 6) is true of the mechanism. A plain `internal` method still hits the server-only guard on a client (VisionAlignment section 2, "Visibility decides the guard").
- The error-handling region's denied read would sit on `AuthorizedOrder`/`ParamAuthOrder`, which EXSET-009's Punchlist row reshapes.
- The Development environment turns on `ValidateOnBuild` and `ValidateScopes`. The first HTTP run is the first time Design.Server's composition is validated that way; `DesignServerCompositionTests` builds with the defaults.
- Acceptance bullets carry `· Must`/`· Should`. This is the arc's convention since 002 and was accepted in the earlier reviews.
- `TestingSamples.cs:362` has a comment pointing at the `Serialization` folder being deleted. It goes with the reference app in EXSET-012.

## Read report

- **Read beyond the brief:**
  - `mdsnippets.json`, `src/Directory.Packages.props`, `src/Directory.Build.props`
  - the NuGet registration for Mvc.Testing, and the local ref packs and package cache
  - `NeatooOrdinalConverterFactory.cs`, `NeatooLogging.cs`, `AspAuthorize.cs`, `AddRemoteFactoryServices.cs`
  - `DesignServerCompositionTests.cs`
  - the reference-app sample files in the deletion folders
- **Named but unused:**
  - the audit file (`audit-remotefactory.md`) and `audit-ledger.md`. No finding turned on a row's wording.
  - plans 002–004. Plan 005 and its review were enough for the substrate.
