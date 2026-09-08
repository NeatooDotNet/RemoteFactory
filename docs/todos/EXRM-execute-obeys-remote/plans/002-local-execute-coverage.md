# Local Execute Proven on Both Shapes, Plus Auth

**Plan #:** 002
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-4
**Status:** Done
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the Design samples this plan adds are the requirements source of truth that EXRM-004 will document; `plan-reviewer` (Pass A carries the business-requirements check)
**Code-review opt-in:** Yes — user decision 2026-09-08 (orchestrator proposed No: no generator or library change); runs at Step 5 beside the test review
**Branch:** exrm-002-local-execute-coverage — cut from the arc at Step 2
**PR:** [#93](https://github.com/NeatooDotNet/RemoteFactory/pull/93)

---

## Scope

Prove the new contract from the client side for both shapes: the combination generator gains a Local execution mode for Execute and the Execute behavior tests gain local legs with a wire-crossing negative control; class-level bare `[Execute]` gets client round-trip tests beside the existing `[Remote, Execute]` ones; the interplay of `[AspAuthorize]` and `[AuthorizeFactory]` with a bare class-level `[Execute]` is pinned; and the Design projects gain bare `[Execute]` samples with tests, as this repo's requirements verification. Does not change the generator or the library beyond what EXRM-001 landed, and does not touch published docs, the skill, or the trimming harness.

---

## Intent

- After EXRM-001 the static shape is pinned but the class shape rests on the reading that its renderer already obeys `[Remote]`. This plan turns that reading into tests: a bare class-level `[Execute]` observed running on the client, resolving its services there, and never touching the wire.
- The combination suite currently proves Execute only in its `[Remote]` form. Opening a Local mode makes bare Execute a first-class combination like every other operation, so future regressions in either direction are caught by the same generated matrix.
- The authorization terms that force remoteness (`[AspAuthorize]`, a `[Remote]` auth method) and the one that does not (`[AuthorizeFactory]` with a client-side auth method) are pinned on the bare shape, so AC-4 stops being an inference from `BuildClassExecuteMethod`.
- The Design projects show a consumer what a client-runnable `[Execute]` looks like on each shape — zTreatment's driving case — and Design.Tests prove it stays on the client, replacing the "[Remote] is decorative" note with the real rule.

---

## Framework & Architectural Alignment

- Two-container client/server simulation (`ClientServerContainers.Scopes()`), with the `RemoteCallCounter` EXRM-001 added as the wire-crossing discriminator; the Design counterpart uses its `configureClient` hook to install a wire stand-in that fails the test if reached.
- `RegisterMatchingName` name-convention DI: `IService` resolves on the client, `IServerOnlyService` (implementation named `ServerOnly`) does not — the positive/negative pair from `ShowcaseReadTests`.
- `[AuthorizeFactory]` static-flag toggling (`EnforcementTestAuth.ShouldAllow`, Design's `AuthorizedOrderAuth` flags); `IAspAuthorize` is a core-library interface, so a plain fake on the server container exercises `[AspAuthorize]` without the AspNetCore package.
- Config-driven combination targets (`CombinationDimensions.json` → `CombinationGenerator`), mirroring the Local/Remote split the CRUD operations already declare.
- Design projects as requirements verification (repo CLAUDE.md, Step 7B); Design samples carry the "why" in comments, DDD vocabulary unexplained.
- No reflection in tests; sacred tests receive additions only.

---

## Constraints & Invariants

- No source change under `src/Generator` or `src/RemoteFactory`, with one amended exception (2026-09-08, user decision): `BuildClassExecuteMethod`'s nullability under authorization, mirroring the Read path. Any other test that needs a source change has found a bug: stop and report, never work around it in the test.
- `ExecuteBehaviorTests` (ten tests) and `ClassExecuteRoundTripTests` keep every existing test and assertion; new coverage lives in new regions or new files.
- Interface factories stay always-remote and are not touched (todo Out of Scope, NF0106).
- Class-level Execute keeps its return-type rule (the containing type's service type) and its `Task<T>` rule (NF0102).
- `[Remote, Execute]` samples and tests in Design are unchanged; new samples sit beside them.
- Both solutions (`src/Neatoo.RemoteFactory.sln`, `src/Design/Design.sln`) build and test green, with a test-count increase in each.
- Published docs, the skill, `CLAUDE-DESIGN.md`, and the reference app are EXRM-004's; this plan edits Design *code and its comments* only.

---

## Steps

1. Open a Local execution mode for Execute in the combination dimensions so the five parameter shapes exist as bare delegates beside the `[Remote]` five, and retire the "always remote" constraint label the config still carries.
2. Give the Execute behavior tests a new region for the bare targets called from the client scope — each runs there with its declared parameters and service and zero wire crossings — named and prefixed (`BareExecute_…`) so it cannot collide with the existing `_Local_` tests, which read the `[Remote]` targets from the Logical scope and stay as they are.
3. Add class-level bare `[Execute]` targets and client-side tests: client service resolution with no remote call; a server-only service as the call-time negative control on the client and the positive control on the server; a `[Remote, Execute]` sibling that crosses the wire exactly once.
4. Pin the authorization interplay on the class shape: a client-side `[AuthorizeFactory]` check on a bare Execute runs locally, allowed and denied, with no wire; `[AspAuthorize]` on a bare Execute forces the wire and reaches the server's `IAspAuthorize`; a `[Remote]` auth method forces the wire by the same rule.
4a. (Amended 2026-09-08) Fix the generator so a class-level `[Execute]` under authorization declares its factory method nullable, as the Read path does — the shape never compiled before this plan's targets — and pin it on both the bare and the `[Remote]` shape.
5. Add a bare `[Execute]` sample to each Design shape — the static commands class and the class-factory demo — taking a service the client container can resolve, with comments stating the contract. Replace only the decorative-`[Remote]` claim in the adjacent note; its trimming sentence belongs to EXRM-003, which rewrites it after measuring the pair.
6. Add Design tests that call both samples from the Design client scope and prove no remote request was made, leaving the existing `[Remote, Execute]` Design tests as the AC-2 control.
7. Build and test both solutions to log files; confirm the test counts rose.

---

## Acceptance

- [x] Each bare Execute combination target, resolved from the client scope, runs there with its parameters and service and makes zero remote calls `[integration]` · Must
- [x] A bare class-level `[Execute]` called through the client's factory runs on the client with client-resolved `[Service]` and no remote call; the same shape taking a server-only service fails at call time on the client (no remote call) and succeeds on the server `[integration]` · Must
- [x] `[Remote, Execute]` on a class factory, called from the client, crosses the wire exactly once and returns the v1.8.1 result `[integration]` · Must
- [x] A client-side `[AuthorizeFactory]` check on a bare class Execute runs locally: allowed executes with no remote call; denied returns null, as a denied Create does, still with no remote call `[integration]` · Should
- [x] `[AspAuthorize]` on a bare class Execute forces exactly one wire crossing and the server's `IAspAuthorize` is consulted `[integration]` · Should
- [x] A class-level `[Execute]` under authorization compiles under TreatWarningsAsErrors with a nullable factory method, and a denial returns null on both the bare and the `[Remote]` shape `[integration]` · Should
- [x] The Design bare-`[Execute]` samples on both shapes run from the Design client scope with no remote request, and the Design `[Remote, Execute]` samples still round-trip `[integration]` · Must
- [x] Both solutions build and test green with test counts up `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-09-08 on `exrm-002-local-execute-coverage` @ `dc5522b`. No surprise shifts the plan; no amendment.

- **Class Execute already obeys the rule.** `FactoryModelBuilder.BuildClassExecuteMethod` (`:458-460`) sets `isRemote` from `[Remote]`, a `[Remote]` auth method, or `AspAuthorizeCalls`; `ClassFactoryRenderer.RenderClassExecuteLocalMethod` (`:842-844`) passes `isServerOnly: method.IsInternal || method.IsRemote`, so a bare `public static` Execute renders an unguarded `Local{Name}` whose `[Service]` parameters come from the factory's own `ServiceProvider`. Auth methods are matched to Execute by flag at `FactoryGenerator.Types.cs:609` (`FactoryOperation.Execute = Read | Execute`, `FactoryOperation.cs:11`); `AspAuthorizeCalls` is set for every factory method at `FactoryGenerator.Transform.cs:265`.
- **Denial returns null.** `RenderClassExecutePublicMethod` (`ClassFactoryRenderer.cs:827`) returns `(await Local…(…)).Result`; `Authorized<T>.Result` is `T?` (`Authorized.cs:77`) and a denied check constructs `Authorized<T>` from the bare `Authorized` (`RenderAuthorizationChecks`, `:1403-1455`), leaving Result default. `IAspAuthorize.Authorize` returns `Task<string?>`; the implicit `string?`→`Authorized` (`Authorized.cs:41`) treats null/empty as access, so a fake returning null authorizes and one returning text denies.
- **Name-convention DI.** `RegisterMatchingName` (`AddRemoteFactoryServices.cs:197-217`) does `TryAddTransient(i, t)` when a class's full name equals the interface's minus the leading `I`. Integration client therefore has `IService`→`Service` but not `IServerOnlyService`→`ServerOnly` (`Services.cs:61` names it that way on purpose). Design's client likewise gets `IExampleService`→`ExampleService` and `INotificationService`→`NotificationService` by name even though `DesignClientServerContainers.cs:246-255` registers them explicitly on server and local only — so the Design samples take a new, explicitly client-safe service rather than leaning on that.
- **Containers.** `ClientServerContainers.Scopes(configureClient, configureServer, configureLocal)` (`:230-268`) returns `(client, server, local)` — reversed from `Scopes()`. Base config runs first: `RegisterFactoryTypes` adds the scoped `RemoteCallCounter` (`:444`), `RegisterAuthorizationTypes` (`:466-480`) scopes every non-interface `T` named in `[AuthorizeFactory<T>]` on every container. The client's `ServerServiceProvider` is linked to `serverScope.ServiceProvider` (`:264`), so a scoped fake registered via `configureServer` and read back from the returned server scope is the instance the handler used. `EnforcementTestAuth.ShouldAllow` is static and toggled by `AuthorizationEnforcementTests` with no collection fixture — new tests carry their own auth class and flag.
- **`[Remote]` auth precedent.** `ShowcaseAuthRemoteTests.cs:10-35, 73-90`: interface auth method `[Remote][AuthorizeFactory(Create)]`, implementation with a server-only ctor dependency registered through `configureServer`; the client never registers it.
- **Combination generator.** `CombinationDimensions.json:47-56` — Execute `validExecutionModes: ["Remote"]`, `constraints: ["requires_static_class", "always_remote"]`; constraints are parsed into `OperationInfo.Constraints` (`CombinationGenerator.cs:147`) and consumed nowhere. `CombinationInfo.ClassName` (`:21`) ends in `_{ExecutionMode}`, so Local targets are `Comb_Execute_Static_TaskTResult_{None,Single,Multiple,Service,Mixed}_Local` with their own `_Result` classes. Only IntegrationTests references the analyzer.
- **Sacred file.** `ExecuteBehaviorTests.cs` regions: "Remote Mode" (`:33-98`, client scope, counter 1) and "Local/Server Mode" (`:100-173`, `Execute_*_Local_*` names reading the `_Remote` targets from `_localScope`, counter 0). New tests take the `BareExecute_` prefix.
- **Design.** `DesignClientServerContainers.Scopes(configureServer, configureClient)` (`:230-268`, server first); the client's `IMakeRemoteDelegateRequest` is registered at `:259` and `configureClient` runs at `:263`, so a stand-in registered there is the last registration and wins. `ExampleCommands` at `AllPatterns.cs:342-420`: `_SendNotification` is `[Remote, Execute] private static` (`:399`), the decorative note is `:368-371`, the `private static _Name` convention `:380-388`. `ClassExecuteDemo.RunCommand` is `[Remote, Execute] public static async` (`ClassFactoryWithExecute.cs:106-113`). `Design.Server/ServerServices.cs:103,117` registers `IExampleService` explicitly then `RegisterMatchingName`; `Design.Client.Blazor/Program.cs` registers no domain services.
- **Baselines** (`reviews/001-test.log`, per TFM): UnitTests 773; IntegrationTests 613 (608 passed, 5 skipped); Design.Tests 99.

---

## Punchlist

Step 2 triage (2026-09-08): the five open todo-level rows are all AC-5 docs/version rows in EXRM-004's and EXRM-005's paths; none lies in this plan's path, none pulled down.

-

---

## Test Evidence

Namespaces: `IT` = `RemoteFactory.IntegrationTests`, `UT` = `RemoteFactory.UnitTests`, `DT` = `Design.Tests`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| 1 — bare combination targets run on the client, no wire | Must | `[integration]` | `IT.Combinations.ExecuteBehaviorTests.BareExecute_TaskTResult_{None,Single,Multiple,Service,Mixed}_ClientScope_*` (5) | ✓ |
| 2 — bare class Execute local; server-only fails on client, works on server | Must | `[integration]` | `IT.FactoryGenerator.Execute.LocalClassExecuteTests.BareExecute_ClientScope_RunsLocally_WithClientService_NoRemoteRequest`, `.BareExecute_ClientScope_ServerOnlyService_FailsAtCallTime_NoRemoteRequest`, `.BareExecute_ServerScope_ServerOnlyService_Resolves`, `.BareExecute_NoServices_RunsInRemoteLogicalAndServerContainers` | ✓ |
| 3 — `[Remote, Execute]` crosses the wire once | Must | `[integration]` | `IT.…LocalClassExecuteTests.RemoteExecute_ClientScope_CrossesTheWireOnce`, `.RemoteExecute_LogicalScope_RunsLocally_NoWire` | ✓ |
| 4 — client-side `[AuthorizeFactory]` stays local, allowed and denied | Should | `[integration]` | `IT.…LocalClassExecuteTests.BareExecute_ClientSideAuth_Allowed_RunsLocally_NoRemoteRequest`, `.BareExecute_ClientSideAuth_Denied_ReturnsNull_NoRemoteRequest` | ✓ |
| 5 — `[AspAuthorize]` forces one crossing, `IAspAuthorize` consulted | Should | `[integration]` | `IT.…LocalClassExecuteTests.BareExecute_AspAuthorize_Allowed_CrossesTheWireOnce_AndIsConsulted`, `.BareExecute_AspAuthorize_Denied_ReturnsNull_StillCrossesTheWireOnce` | ✓ |
| 6 — authorized class Execute compiles nullable; denial returns null on both shapes | Should | `[integration]` | `IT.…LocalClassExecuteTests.RemoteExecute_WithAuth_Allowed_CrossesTheWireOnce`, `.RemoteExecute_WithAuth_Denied_ReturnsNull_StillCrossesTheWireOnce` (bare shape by bullet 4); at `[unit]` by `UT.FactoryGenerator.Execute.ClassExecuteAuthTests.ClassExecuteWithAuth_Denied_ReturnsNull`, `.ClassExecuteWithAuth_Allowed_ReturnsInstance`, `.ClassExecuteWithAuth_BothOutcomes_ThroughOneFactory`, `.ClassExecuteWithAuth_CanRun_ReflectsAuthState` | ✓ |
| 7 — Design bare samples run on the client; `[Remote]` samples still round-trip | Must | `[integration]` | `DT.FactoryTests.LocalExecuteTests.Execute_StaticFactory_WithoutRemote_RunsOnClient`, `.Execute_ClassFactory_WithoutRemote_RunsOnClient`, `.Execute_WithRemote_StillTriesTheWire`; round-trip control unchanged in `DT.FactoryTests.StaticFactoryTests` and `.ClassFactoryExecuteTests` | ✓ |
| 8 — both solutions build and test green, counts up | Must | `[explicit-skip: meta-bullet]` | `reviews/002-build-main.log`, `002-build-design.log`, `002-test-main.log`, `002-test-design.log`: UnitTests 773→777, IntegrationTests 613→631, Design.Tests 99→102, all per TFM, 0 failed | ✓ |

Bullet 6's compile clause is structural, and only structural: the generated file failing CS8603 under `TreatWarningsAsErrors` (CS8603 is in no `NoWarn`) means the target does not build and no test in the solution runs, so a green build with the auth targets present *is* the assertion. No test method can add to it — an earlier note here claimed one did, by assigning the result to a nullable local; that assignment is legal either way and proves nothing (test-review round 1). The behavior half of the bullet — denial returns null on both shapes — is pinned by the tests cited above.

---

## Gate Record

- Round 1 (2026-09-08): test-review **CLEAN** — all 8 bullets pinned at tier, sacred test additive, no vacuous pin. One weak claim corrected in response (a test comment asserted a nullable-local assignment would not compile against `Task<T>`; it would — the test was renamed to what it does and the false claim removed from the Test Evidence note). Two tech-debt items dismissed: Design `LocalExecuteTests` disposes without `try/finally`; `constraints` in `CombinationDimensions.json` remains parsed and unconsumed. — [`reviews/002-test-review.md`](../reviews/002-test-review.md)
- Round 1 (2026-09-08): code-review **CLEAN** — generator fix verified at all four render sites, no change to non-authorized Execute or any other operation, wire path sound. Three callouts, all stale references to the test renamed mid-review: two already corrected, the third (a dangling `cref` in the class remarks) fixed and the solution rebuilt green. — [`reviews/002-code-review.md`](../reviews/002-code-review.md)
- Both gates CLEAN in one round; no leftovers, no demotions. Done.

---

## Plan Amendments

_(append-only)_

### 2026-09-08 — Plan-review callouts amended before implementation

- **Section affected:** Steps 2 and 5 (was 6); Acceptance bullets 4 and 7
- **Original said:** Step 2 added "a Local-mode region"; Step 6 replaced the whole decorative note; bullet 4 said denial "surfaces as it does for a denied Create"; bullet 7 pinned the visibility rule at `[unit]`.
- **What changed:** Step 2's region is named for bare targets and prefixed `BareExecute_` (B1 — the natural names collide with the existing `_Local_` tests); Step 5 rewrites the decorative claim only and leaves the trimming sentence to EXRM-003 (B5); bullet 4 says "returns null" (B2 — `Authorized<T>.Result` is default on denial); bullet 7 moved to EXRM-004 beside the Punchlist sentence it backs (B4, B3). B6 dismissed.
- **Why:** `reviews/002-plan-review.md`; user decisions 2026-09-08.
- **Discovery Log:** 2026-09-08 / EXRM-002

### 2026-09-08 — Generator fix admitted: class Execute nullability under authorization

- **Section affected:** Constraints (first bullet); Steps (4a); Acceptance (new Should bullet after the `[AspAuthorize]` one)
- **Original said:** No source change under `src/Generator` or `src/RemoteFactory`.
- **What changed:** `BuildClassExecuteMethod` (`FactoryModelBuilder.cs:481`) passed `isNullable: method.IsNullable`, while `BuildReadMethod` (`:343-344`) adds `|| authorization.HasAuth`; the public factory method was declared `Task<T>` yet returned `Authorized<T>.Result` (`T?`) — CS8603 in generated code, fatal under TreatWarningsAsErrors, a null through a non-nullable signature otherwise. Affects bare and `[Remote]` class Execute alike; no in-repo target ever combined the two. Fixed by adding the `HasAuth` term; pinned by the AC-4 targets (compile) and denied→null tests on both shapes.
- **Why:** Step 4's targets exposed it; user chose fix-now over a fix plan or an issue (2026-09-08). Release notes (EXRM-005) carry it as a fix.
- **Discovery Log:** 2026-09-08 / EXRM-002 (second entry)

---

## Notes

Recon (2026-09-08): the pattern for "ran locally on the client" is `Showcase/ShowcaseReadTests.cs`: a bare `[Create]` taking `[Service] IServerOnlyService` whose body is `Assert.Fail()`, asserted to throw `InvalidOperationException` from the client (negative form), and a bare `[Create]` taking `[Service] IService` resolved on the client through `RegisterMatchingName` (positive form). `ClassExecuteRoundTripTests` and `Design.Tests/ClassFactoryExecuteTests` are all `[Remote, Execute]` and become AC-2 evidence. Static factories carry no authorization path in the builder today, so AC-4 is class-level; `[AspAuthorize]` has no Execute precedent in any test (`AspAuthorizeTestObj.cs` covers Create/Insert only) and the AspNetCore test library is consumed by no test project.

Step 2 recon additions: `IAspAuthorize` lives in core `RemoteFactory`, so the integration tests can host a plain fake through `Scopes(configureServer:)` — no AspNetCore reference needed. `DesignClientServerContainers` has no call counter; its `configureClient` hook is the seam for a throwing wire stand-in. `CombinationDimensions.json` lists Execute with `"constraints": ["requires_static_class", "always_remote"]`; no test consumes the constraint strings. `AllPatterns.cs:368-371` carries the "[Remote] is decorative on [Execute]" note beside the static sample. Design registers its services by `RegisterMatchingName` on all three containers, so a Design sample's `[Service]` resolves on the client whenever the implementation name matches — the sample's comments must say so rather than claim a server-only service.

Could-tier candidate from the 001 plan review (B1) was drafted as Acceptance bullet 7 and moved to EXRM-004 at this plan's review (B4/B3): it backs todo Punchlist row 3, which 004 writes, and a single-method internal target renders the whole interface internal rather than a prefixed member. Pre-flight notes from the review: the static Design sample follows `private static _Name` (`AllPatterns.cs:380-388`); the Design wire stand-in throws from `ForDelegate*`, never its constructor, and its test calls only the bare sample.
