# ASP.NET Core integration over HTTP, and setup

**Plan #:** 006
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** Yes (a new test tier over HTTP, a second Design domain assembly, and Design.Server configuration that later plans build on). Reviewed 2026-10-15, APPROVED; re-split, with serialization and modes moved to EXSET-013 ([review](../reviews/006-plan-review.md))
**Code-review opt-in:** Yes (a page rewritten against library behaviour; new Design server and client configuration)
**Branch:** EXSET-006-aspnetcore-modes-serialization
**PR:** —

---

## Scope

Re-point every C# block on `docs/aspnetcore-integration.md` to `src/Design`, together with the skill's `references/setup.md` `aspnetcore-*` embeds:
- the `aspnetcore-*` regions the page embeds. `aspnetcore-correlation-id` also renders in the skill's `advanced-patterns.md`, and re-points with it;
- the hand-written blocks: the `AddNeatooAspNetCore`/`UseNeatoo`/`IAspAuthorize` signatures, the request and response DTOs, and the JSON error bodies.

Add what Design lacks for them (Keith, 2026-10-15):
- **An HTTP test tier.** Design.Tests runs `WebApplicationFactory` over Design.Server, with a Remote client whose HttpClient goes to it. This pins what the in-process harness cannot: status codes, middleware, the correlation header, the request's cancellation token.
- **Real examples:**
  - CORS for a client served from another origin;
  - a second domain assembly that both sides register;
  - the serialization format chosen by environment, on the server and the client together.
- A server operation that takes a `CancellationToken`, and one that reads `ICorrelationContext`.
- Server logging configuration that names the categories the library actually creates.
- Client error handling.
- The testing region on Design's real three-container helper.

Delete the reference-app `AspNetCore` samples and their region markers.

Fix the audit findings on the page:
- C-19/C-20 public concretes and concrete-typed members;
- I-01 private setters;
- I-12 hand-written signature illustrations;
- T-09 event-handler hosting ("hosted service", "isolated scopes", "fire-and-forget");
- T-10 the request/response DTO shapes, the error bodies, the log categories;
- T-22 "guaranteed to compile";
- T-25 the two-container sample that round-trips nothing.

Also fix what recon found wrong beyond the audit:
- **Error handling.** A denied read returns null rather than throwing. A server exception arrives as `HttpRequestException` (500), not as "server unavailable".
- **Correlation.** The page says "per request"; the ID is per client scope.
- **Endpoint wording.** "Automatic endpoint generation" and "simulates client/server" are wrong.

Does NOT touch:
- `factory-modes.md`, `serialization.md`, setup's factory-modes block and their regions. These are EXSET-013's (Keith, 2026-10-15), and that plan reuses this one's format and logging regions.
- Authentication, authorization, the 403 path and the `aspnetcore-middleware-order` region. These are EXSET-009's (Keith, 2026-10-15), and its embed stays on the reference app until then.
- `save-operation.md`'s 409 claim (EXSET-008).
- Setup's static-commands and events block, and `skill-blazor-usage` (EXSET-010).
- The skill's prose (Todo 2).

---

## Intent

- A reader setting up the server sees compiled, tested Design code.
- Every behaviour the page states over HTTP was watched over HTTP by a Design test: status codes, the correlation header, cancellation, CORS and the format header.
- README and getting started still show a short registration line. The options and the assembly list behind it have their own regions on the ASP.NET Core page (Keith, 2026-10-15).
- The testing section shows Design's real three-container helper, resolving from the client.

---

## Framework & Architectural Alignment

- **Rulings.**
  - D18: `[Remote]` marks a client entry point.
  - R-M: public setters on state that crosses.
  - R-O: migrated regions take the Order domain.
  - R-A: embeds come only from Design; WRONG blocks are the one exception.
- **`.claude/rules/design-snippets.md`.**
  - Regions are tight, globally unique and pinned by a Design test.
  - A migrating region's old definition is deleted in the same change.
  - Placeholders come in pairs.
  - mdsnippets' log is kept outside the tree, followed by a `git diff --stat` check.
- **Substrate from EXSET-002–005:** `Order`, `IOrderDbContext`, `AuthorizedOrder`, `ServerServices.AddDesignServer`, `DesignClientServerContainers`, and the `getting-started-*` and `readme-server-setup` regions.
- **Pinning.** EXSET-005 ruled `Program.cs` build-pinned (Keith, 2026-10-14). The HTTP tier now runs the server's `Program`, so the server regions become test-pinned. The client `Program` stays build-pinned, but its format choice calls a Design.Domain rule that the tests also call.

---

## Constraints & Invariants

- **Every C# block on `aspnetcore-integration.md` is an embed from `src/Design` or a short block labelled WRONG**, except the `aspnetcore-middleware-order` embed (EXSET-009).
- **Region map.**
  - These retire into Design's existing regions, together with their old definitions:
    - `aspnetcore-basic-setup` and `aspnetcore-minimal-api` → `getting-started-server-program`;
    - `aspnetcore-addneatoo` → `readme-server-setup`.
  - The development/production pair becomes one region: the format rule. One of the two names retires.
  - The other names migrate as they are.
- **README's lines.** `readme-server-setup` and `getting-started-client-program` stay one registration call each. That call takes the options and the assembly list, which are built just above it in their own regions. `getting-started-server-program` gains `UseCors()` before `UseNeatoo()`.
- **One registration call per side.** The second assembly and the options go in the single `AddNeatooAspNetCore` and `AddNeatooRemoteFactory` call on each side, because a second call replaces the assembly list.
- **The format rule lives in Design.Domain, in one place.** The server, the client `Program` and the HTTP fixture all call it, so the two sides pick the same format in the same environment.
- **The HTTP tier runs Design.Server's real `Program`, configuration and services.** A test does not replace the configuration the page shows. The cancellation test may replace the store with one that waits on the token, standing in for a database.
- **The test hosts live for the test assembly's lifetime.** `UseNeatoo` stores the host's logger factory in a static, so a host disposed mid-run would break later Ordinal harness tests. If order-dependent failures appear anyway, STOP: that is a library behaviour.
- **Tiers.** The in-process harness stays the tier for domain behaviour. The HTTP tier covers only what the harness cannot see.
- **New Design operations.**
  - They are `[Remote] internal` only where a client calls them (D18).
  - They are interface-first, with public setters.
  - There is no `new` on a `[Factory]` type outside its own factory methods.
  - `readme-domain-model` does not change shape.
- **The second assembly is real and small:** one `[Factory]` type the Order domain does not depend on.
- **The whole still works.** `Design.Server` composition still resolves every service, the hosted Blazor app still starts, mdsnippets swallows no section, both solutions are green, and the reference app builds.

---

## Steps

1. **HTTP tier.**
   - Design.Server becomes testable: `Program` is public on net9 too.
   - Design.Tests gains `WebApplicationFactory`: Mvc.Testing, with the net9 override pattern Design already uses.
   - The hosts are assembly-lifetime fixtures. Each has a Remote-mode client container whose keyed HttpClient goes to the test server, using the format rule for that host's environment.
   - First test: an Order saves and fetches over HTTP.
2. **Format and assemblies.**
   - First verify how the hosted client learns its environment on net9 and net10. If the two sides can disagree, STOP and ask.
   - Add the format rule to Design.Domain and the second domain assembly. Both sides register the second assembly in their one call, through the options and assembly-list regions.
   - Over HTTP: the server answers Named in Development and Ordinal otherwise (the format header), and the second assembly's factory round-trips over HTTP and through the harness.
3. **Server examples**, each pinned over HTTP:
   - **Cancellation.** A `[Remote]` operation takes the request's `CancellationToken` and passes it to the store; a cancelled call cancels it.
   - **Correlation.** A server operation reads `ICorrelationContext`; an `X-Correlation-Id` the caller sends reaches it and comes back on the response.
   - **CORS** for another origin: an allowed origin gets the CORS headers on the endpoint, another origin does not.
   - **Logging.** Server logging enables the categories the library creates, including `RemoteFactory.Trace`; a call is shown to log them.
   - **Service registration.** The region shows Design's explicit registrations before `RegisterMatchingName`.
4. **Client examples.**
   - **Error handling.** Over HTTP, on a class factory with `[AuthorizeFactory]`: a denied read returns null and a denied Save throws `NotAuthorizedException`. A server exception arrives as `HttpRequestException` with 500.
   - **Testing.** A three-container test saves and fetches from the `client` scope.
5. **Re-point and delete.**
   - The page, setup's three `aspnetcore-*` embeds and `advanced-patterns.md` re-point.
   - The reference app's `AspNetCore` samples and their tests are deleted; `BasicSetupSamples.cs` keeps only `aspnetcore-middleware-order`, for EXSET-009.
   - Build `EmployeeManagement.sln` once.
6. **`aspnetcore-integration.md`:**
   - the signature blocks go to prose;
   - the real `RemoteRequestDto`/`RemoteResponseDto` shapes, in prose;
   - the error section gives the outcomes Step 4 pins, with no JSON error bodies. The `[AspAuthorize]` forbid path is left for EXSET-009;
   - the real log categories;
   - correlation is per client scope and carried as a message parameter;
   - event-handler hosting matches `factory-events.md` (T-09);
   - the endpoint is mapped by `UseNeatoo`, not generated;
   - the testing section is Design's helper.
7. **Design's own statements on these topics agree with the library:**
   - `ServerServices.cs`'s registration-order header and "sets Mode = Server";
   - the `Program.cs` comments on a configurable endpoint path and on `NeatooFactory.Mode`.
8. **Verify and record.**
   - Run mdsnippets with its log outside the tree, search the log for missing-snippet warnings (`TreatMissingAsWarning` is on), and check the diff.
   - Build and test both solutions.
   - Record ledger outcomes for C-19, C-20, I-01, I-12, T-09, T-10, T-22 and T-25.

---

## Acceptance

- [ ] Over HTTP against Design.Server's real `Program`, a Remote client saves and fetches an Order, and a server exception reaches it as `HttpRequestException` with 500 `[integration]` · Must
- [ ] Over HTTP, on a class factory with `[AuthorizeFactory]`, a denied read returns null and a denied Save throws `NotAuthorizedException` `[integration]` · Must
- [ ] Over HTTP:
  - a caller's `X-Correlation-Id` reaches a server operation's `ICorrelationContext` and returns on the response;
  - a cancelled call cancels the operation's token;
  - an allowed origin gets the CORS headers and another origin does not.

  `[integration]` · Must
- [ ] Over HTTP:
  - the server answers in Named in Development and in Ordinal otherwise;
  - the server, the client and the fixture take the format from one Design.Domain rule;
  - a second assembly's factory round-trips over HTTP and through the harness.

  `[integration]` · Must
- [ ] The logging region enables the categories the page names, which an HTTP call is shown to log `[integration]` · Should
- [ ] The testing region saves and fetches from the `client` scope of the three-container helper `[integration]` · Must
- [ ] On the page:
  - every C# block is an embed from `src/Design` or WRONG, except `aspnetcore-middleware-order` (EXSET-009);
  - no region is defined twice;
  - none of the findings' text this plan lists remains;
  - the replaced reference-app sources are gone, and the reference app builds.

  `[explicit-skip: grep of fences, markers, #region names and finding text; EmployeeManagement.sln build]` · Must
- [ ] mdsnippets' log has no missing-snippet warning and no section was swallowed; both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Recon, 2026-10-15. Library behaviour is cited from source; the plan review spot-checked the citations.

- **Page.**
  - `aspnetcore-integration.md` (626 lines) has 15 embeds, 6 hand-written C# blocks, and JSON error bodies at `:449-467`.
  - `setup.md` embeds `aspnetcore-basic-setup` (`:18`), `-multi-assembly` (`:46`) and `-cors` (`:68`).
  - `advanced-patterns.md:148` embeds `-correlation-id`.
- **DTOs.** The real types are `RemoteRequestDto { DelegateFullName, Parameters, Target }` and `RemoteResponseDto(Json, RelayedEvents)`.
- **What the endpoint returns.**
  - 200 on success, and also for a class-factory auth denial. The client then returns null on a read and throws `NotAuthorizedException` on Save (`ClassFactoryRenderer.cs:299-341, :1083-1095`).
  - 500 for any other exception, because the handler rethrows (`HandleRemoteDelegateRequest.cs:253-258`). This includes an interface factory's `NotAuthorizedException`.
  - 403 only from the `[AspAuthorize]` forbid path, which needs a scheme (EXSET-009).
  - Any non-2xx reaches the client as `HttpRequestException` (`MakeRemoteDelegateRequestHttpCall.cs:33-37`).
- **Events.** There is no hosted service and no `CreateScope`; handlers are awaited in the caller's scope.
- **Log categories.**
  - There is no `Client` category, and `Remote` and `Authorization` are never used.
  - Serialization trace goes to `RemoteFactory.Trace`, outside the `Neatoo.RemoteFactory` prefix.
  - Generated factories log under the user's namespace.
  - The ordinal converter's logger is a static, set by `UseNeatoo` (`NeatooLogging`).
- **Correlation.**
  - The client generates the ID once per scope and sends `X-Correlation-Id` (`MakeRemoteDelegateRequestHttpCall.cs:29`).
  - The server middleware fills `ICorrelationContext` from it.
  - Logs carry it only as a message parameter.
  - The in-process harness carries none.
- **Registration.** `AddNeatooAspNetCore` takes options and `params Assembly[]`; a second call replaces `IServiceAssemblies` (`AddRemoteFactoryServices.cs:62`). The client never reads `X-Neatoo-Format`.
- **Private setters.** `aspnetcore-cancellation` and `-correlation-id` set a private-setter `Id` in a `[Create]` constructor, so a fetched object reaches the client with a fresh `Id` (I-01).
- **Design.Server and the client.**
  - `ServerServices.cs:11` states the registration order backwards, and `:61` claims "sets Mode = Server".
  - The client `Program.cs:26` says "NeatooFactory.Mode".
  - No Design operation takes a `CancellationToken` or reads `ICorrelationContext`.
- **HTTP testing.**
  - Neither solution has any.
  - Design.Tests already references Design.Server.
  - Mvc.Testing 10.x is net10-only; 9.0.13 is on NuGet.
  - On net10 a top-level `Program` is public; on net9 it is not.
  - `WebApplicationFactory` starts in Development by default.
- **Reference app.**
  - The page's regions live in `Server.WebApi/Samples/AspNetCore`, `Domain/Samples/AspNetCore`, `Client.Blazor/Samples/AspNetCore` and `Tests/Samples/AspNetCore`.
  - The `aspnetcore-testing` helper returns three scopes of one Logical container (T-25).

---

## Punchlist

---

## Test Evidence

---

## Gate Record

---

## Plan Amendments

- **2026-10-15 · Re-split** (Keith).
  - `factory-modes.md`, `serialization.md`, setup's factory-modes block, the reference-preservation probe and reviewer callouts 7–9 move to EXSET-013.
  - Callouts 1–6, 10 and 11 are amended into this plan (triage in the review).

---

## Notes

- **Keith, 2026-10-15.**
  - HTTP behaviour is pinned by `WebApplicationFactory` tests over Design.Server.
  - CORS, multi-assembly registration and the environment-chosen format get real Design examples.
  - Authentication, authorization and the middleware-order region are EXSET-009's.
  - The plan is split: serialization and modes go to EXSET-013.
  - README keeps a short registration line, with the options and assemblies in their own regions.
- **Candidates to park as issues.** These are library behaviour, not this plan's to change; Keith decides at the gate.
  - **Logger categories:** `RemoteFactory.Trace` sits outside the prefix, the `Remote`/`Authorization` constants are unused, and the client HTTP call has no logger.
  - **Static logger factory:** the one `UseNeatoo` sets outlives its host.
  - **Format header:** the client never reads `X-Neatoo-Format`.
