# Static-Factory Delegates Obey [Remote]; Weld Removed

**Plan #:** 001
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2
**Status:** Done
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the generated public surface changes shape (registration per factory mode) and the wire-refusal step is a security seam. The plan contradicts the documented "decorative" rule by design; that contradiction is the todo's Goal, user-authorized, so it is not veto-tier here.
**Code-review opt-in:** Yes — behavior-changing generator and runtime work.
**Branch:** exrm-001-static-delegates-obey-remote — cut from the arc 2026-09-08
**PR:** —

---

## Scope

Remove the generator's forced-remote treatment of `[Execute]` and give the static-factory delegate path a real remote flag: the delegate model carries it, the builder derives it from `[Remote]` the way class methods already do, the registrar renders a remote registration only for remote delegates, and a bare delegate gets one unguarded local registration in every factory mode while remote delegates keep the guarded v1.8.1 shape. The server-side delegate handler refuses a request naming a bare delegate, so `[Remote]` is not decorative at the wire either (user decision 2026-09-08, folded in here as a Should bullet under AC-1). Preserve the intent of existing tests that assumed always-remote: the combination generator emits `[Remote]` for its Remote-mode Execute targets, and any bare test target that is invoked from a client scope gains `[Remote]` only with the user's recorded sign-off. Does not add local-mode coverage (EXRM-002), change class-level rendering (none is needed: its public method, local guard, and registrar filters already branch on the flag), touch the trimming harness, or edit documentation prose.

---

## Intent

- A developer who omits `[Remote]` on a static `[Execute]` gets a method that runs where it is called: on the client with client services, on the server with server services, exactly like a bare `[Create]`. This is the shape zTreatment needs for its WASM-resident engines.
- A developer who writes `[Remote, Execute]` sees no change in generated code or runtime behavior.
- `[Remote]` stops being decorative at the wire: the server does not run a bare delegate because a request named it.
- The existing Execute test surface keeps meaning what it says; no test goes green for a reason other than the one in its name.

---

## Framework & Architectural Alignment

- Mirrors the bare-`[Create]` local emission on class factories: the builder derives the flag from `[Remote]`, the renderer omits the remote leg, and the local leg carries no server-runtime guard.
- Keeps the TRIM-009 guard shape for remote delegates: the guard wraps the registration itself, outside any async state machine, so the trimmer removes the whole registration on a client publish.
- The local delegate keeps routing through the factory entry call (PHASE-003) on both tiers, so entry marking and AfterCommit drain semantics hold wherever it runs.
- The wire refusal uses no reflection: the generated registrar declares which delegate types are remote-callable, and the handler consults that declaration before resolving the delegate. Client-raised events keep their marker delegate on the allow-list.
- Static factories carry no authorization rendering today; the builder derives the static remote flag with the same three-way rule as class methods (`[Remote]`, a remote auth method, or an ASP authorize attribute) so the flag is consistent across shapes, without adding enforcement this plan does not own.
- Interface factories are untouched; NF0106 keeps them always-remote.

---

## Constraints & Invariants

- `[Remote, Execute]` output is byte-for-byte the v1.8.1 shape: remote delegate through `IMakeRemoteDelegateRequest`, guarded local registration, entry-call routing.
- NF0102 (Execute must return Task) fires for bare and remote Execute alike; NF0103 (must be static) is unchanged; the NF0105 static exemption is unchanged in this plan (AC-6 belongs to EXRM-004).
- The server handler still serves every remote delegate of every factory shape, and every client-raised event, after the refusal lands.
- Class-level Execute rendering is not edited; every existing class-level Execute test stays green.
- Every existing Execute test keeps its intent: a client-scope test proves a wire crossing, a local- or server-scope test proves local execution. No out-of-scope test is edited without recorded sign-off; none is foreseen, because the ten bare integration targets are invoked only from local or server scopes.
- Incremental generator caching stays correct: the cached method record already includes the remote flag in its equality.
- Both solutions build and test green at the gate.

---

## Steps

1. Stop forcing Execute remote in the factory-method model so the `[Remote]` attribute read is what the builder sees, and drop the dead clause on the record-constructor path.
2. Give the static-factory delegate model a remote flag and derive it in the builder with the same three-way rule the class path uses.
3. Render the remote delegate registration only for remote delegates.
4. Render the local delegate registration unguarded and in every factory mode for bare delegates, and keep the guarded server-only shape for remote delegates.
5. Have the static-factory registrar declare its bare, local-only delegate types in a process-global registry mirroring the DTO constructor registry, and make the server handler refuse a request naming one of them as if the delegate were unknown, logging the real reason server-side; class-factory, interface-factory, and client-raise delegates are served exactly as today. _(Amended 2026-09-08; was an allow-list.)_
6. Make the combination generator honour the execution mode for Execute so Remote-mode targets carry `[Remote]`; leave the Execute dimension Remote-only until EXRM-002 adds Local.
7. Give the integration containers' serialized stand-in an observable per-scope call count, correct the Execute behavior tests' header, and make their client-scope legs assert one remote request per call and their local-scope legs none. _(Amended 2026-09-08; no hook existed.)_
8. Add bare static integration targets and client-scope tests for the local shape, using the existing client-resolvable service and the server-only negative control.
9. Add unit tests for the three-mode registration shape and for the handler refusal.
10. Build and test both solutions once for the gate; logs to `reviews/001-*.log`.

---

## Acceptance

- [ ] A bare static `[Execute]` invoked from a Remote-mode client container runs there, resolves its `[Service]` parameters from that container, and makes no remote request. `[integration]` · Must
- [ ] A `[Remote, Execute]` static delegate invoked from a Remote-mode client container goes through the remote request path and executes on the server, unchanged from v1.8.1. `[integration]` · Must
- [ ] A bare delegate resolves in Remote, Logical, and Server containers, and a remote delegate's remote registration exists only in the Remote container. `[integration]` · Must
- [ ] The server handler refuses a remote request naming a bare static delegate as an unknown delegate, and still serves class, interface, and static remote delegates and client-raised events. `[integration]` · Should
- [ ] Every existing client-scope Execute behavior test observes exactly one remote request per call, and every local-scope one observes none. `[integration]` · Must
- [ ] A bare static `[Execute]` that takes a server-only service fails on the client at call time with a service-resolution error and no remote request. `[integration]` · Must
- [ ] Both solutions build and test green. `[explicit-skip: meta-bullet]` · Must

_(Bullets 3, 4, 5 amended and bullet 6 added 2026-09-08 at plan-review triage; see Plan Amendments.)_

---

## Current State (Pre-Flight)

Walked 2026-09-08 on `exrm-001-static-delegates-obey-remote` at arc `EXRM` (= main @ eb63a94), before any edit.

**Model layer.** `FactoryGenerator.Types.cs:703` reads `IsRemote` from the `[Remote]` attribute in the `MethodInfo` base. `TypeFactoryMethodInfo` overwrites it at `:582` (method/ctor form) and `:629` (record primary-ctor form; that ctor only ever sees `Create`, so the Execute clause is dead). `IsStaticFactory = methodSymbol.IsStatic` at `:581`. `AspAuthorizeCalls` is populated for every factory method, static included, at `FactoryGenerator.Transform.cs:265`. `TypeFactoryMethodInfo.IsRemote` participates in the cached record's equality (`:787`), so the cache key changes correctly.

**Static builder and model.** `FactoryModelBuilder.cs:82-112` (`BuildStaticFactory`) emits NF0102 for non-Task and calls `BuildExecuteDelegate` at `:489-524`, which reads neither `IsRemote`, `AuthMethodInfos`, nor `AspAuthorizeCalls`. `Model/ExecuteDelegateModel.cs:8-38` carries Name, DelegateName, ReturnType, IsNullable, Parameters, ServiceParameters, HasCancellationToken — no remote flag. The class path's three-way rule to mirror is `FactoryModelBuilder.cs:458-460` (`method.IsRemote || AuthMethodInfos.Any(m => m.IsRemote) || AspAuthorizeCalls.Any()`); `BuildReadMethod` uses the same at `:333-335`.

**Static renderer.** `StaticFactoryRenderer.cs:87-100` emits the delegate type unconditionally (`public delegate Task<T> Name(params, CancellationToken cancellationToken = default)`). `RenderFactoryServiceRegistrar` `:136-181`: remote block gated only on `remoteLocal == NeatooFactory.Remote` (`:142-150`), local block only on `Logical || Server` (`:154-162`), DTO registrations after (`:165-178`); nothing branches per delegate. `RenderRemoteDelegateRegistration` `:183-210` binds the delegate to `IMakeRemoteDelegateRequest.ForDelegate{Nullable}<T>(typeof(Type.Delegate), [params], ct)`. `RenderLocalDelegateRegistration` `:212-251`: guard `if (NeatooRuntime.IsServerRuntime)` opens at `:236-237` and closes at `:250`, outside the `AddTransient`; services resolve from `cc` at `:224`; the body routes through `FactoryEntryCall.RunAsync` at `:240`. An unguarded local registration is `:238-249` verbatim without `:236-237`/`:250`, hoisted out of the mode block. `FactoryEntryCall.cs:33-37` resolves the scheduler null-tolerantly; Remote-mode containers register none (`AddRemoteFactoryServices.cs:73-77`).

**Class path, confirmed no change needed.** `ClassFactoryRenderer.cs:798` renders the remote method only when `IsRemote`; `:819` routes the public method to `Local{Name}` when not; `:843-844` passes `isServerOnly: method.IsInternal || method.IsRemote` into the shared non-async wrapper (`:377-407`, guard at `:395-399`), so a bare `public static` Execute gets no guard; the five `m.IsRemote && !(m is WriteMethodModel)` filters (`:179, :187, :255, :270, :1620-1630`) drop the delegate type, property, ctor assignments, and DI registration. `BuildClassExecuteMethod` already computes the three-way rule; `isTask` is hardcoded true at `:479`.

**Handler and wire.** `HandleRemoteDelegateRequest.cs`: `serializer.DeserializeRemoteDelegateRequest` maps `DelegateFullName` to a `Type` through `serviceAssemblies.FindType` (`NeatooJsonSerializer.cs:346`, throwing `MissingDelegateException` at `:350` when unknown), then `serviceProvider.GetRequiredService(remoteRequest.DelegateType)` and `DynamicInvoke`. No remote-callable check exists; any DI-registered delegate type runs. Client-raised events use the library-registered `RaiseFactoryEventRemote` marker delegate (`RemoteFactoryEvents.cs:37`, registered scoped at `AddRemoteFactoryServices.cs:98`). Registrars are invoked by `RegisterFactories` (`AddRemoteFactoryServices.cs:167-182`) from the assembly attribute. Existing registry shapes: `FactoryEventHandlerRegistry` (public static), `DtoConstructorRegistry` (public static, Internal), `FactoryEventTypeRegistry` (internal static). Allow-list candidates: a per-container DI marker record resolved as an enumerable (test-friendly, no static state) or a static registry mirroring the three above. Leaning DI marker; the library registers the raise marker itself. Refusal on the wire: leaning "indistinguishable from an unknown delegate" (`MissingDelegateException`) with the specific reason in the server log, so a crafted request cannot enumerate bare delegates.

**Combination generator.** `CombinationDimensions.json:49-58`: Execute `validExecutionModes: ["Remote"]`, constraints `requires_static_class`, `always_remote`. `CombinationGenerator.cs:552-589` (`GenerateExecuteClassBody`) writes `[Execute] public static` and ignores `combination.ExecutionMode`; the Read body at `:435-441` shows the `[Remote]` emission pattern to copy. Generated targets are gitignored (`.gitignore:405`) and emitted at build; `TargetClassGenerator.cs` is dead code (Dismissed).

**Tests.** `ClientServerContainers.cs`: client container is `NeatooFactory.Remote` with `MakeSerializedServerStandinDelegateRequest` as `IMakeRemoteDelegateRequest` (serializes, calls the server scope's `HandleRemoteDelegateRequest`, deserializes, dispatches relay); it has no call counter today, and a per-scope counter incremented in `ForDelegateNullable`/`ForDelegateEvent` is the smallest hook. `RegisterMatchingName` on the client maps `IService`/`IService2`/`IService3` to their implementations; `IServerOnlyService` (impl `ServerOnly`) is unmatched, which is the negative control. `ExecuteBehaviorTests.cs`: ten tests, client-scope `:29-88`, local-scope `:94-153`, all on the five `Comb_Execute_Static_TaskTResult_*_Remote` targets. `ShowcaseReadTests.cs:60-73` and `:83-88` are the positive and negative local-proof patterns. The ten bare integration targets: `RemoteExecuteTargets.cs` (8, local scope only) and `FactoryEventTransactionTargets.cs:154,173` (class-level, server scope only). UnitTests has `ServerContainerBuilder` and `LogicalContainerBuilder` only; a Remote-mode container for the three-mode check needs `AddNeatooRemoteFactory(NeatooFactory.Remote, …)` directly or lives in IntegrationTests. DI resolution is not pure logic under this template's tiers, so Acceptance bullet 3 should read `[integration]`; amendment pending with the plan-review triage.

---

## Punchlist

- [x] Region header "Execute is always remote" · `src/Tests/RemoteFactory.IntegrationTests/Combinations/ExecuteBehaviorTests.cs` · header and region names now state the [Remote]-follows rule · this plan's PR · AC-2 · Must
- [x] Dead Execute clause in the record-primary-constructor ctor · `src/Generator/FactoryGenerator.Types.cs` · removed with the weld · this plan's PR · AC-2 · Must
- [x] Server-only failure test asserted only the exception type (test-review tech-debt 1) · `LocalExecuteTests.BareExecute_ClientScope_ServerOnlyService_FailsAtCallTime_NoRemoteRequest` · now also asserts the message names `IServerOnlyService` · this plan's PR · AC-1 · Must

---

## Test Evidence

Filled 2026-09-08 after implementation. Logs: `reviews/001-build.log` (both solutions, 0 errors, 4 pre-existing warnings in Examples/TrimmingTests), `reviews/001-test.log` (UnitTests 773 ×2 TFMs, IntegrationTests 608 + 5 pre-existing skips ×2 TFMs, Design.Tests 99 ×2 TFMs, all passing; plus a `verbosity=normal` run of the new and rewritten tests by name). All named tests are in `RemoteFactory.IntegrationTests` unless stated.

| # | Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|---|
| 1 | Bare static Execute runs on the client with client services, no remote request | Must | `[integration]` | `FactoryGenerator.Execute.LocalExecuteTests.BareExecute_ClientScope_RunsLocally_WithClientService_NoRemoteRequest`; "runs wherever called" half: `BareExecute_ServerScope_ServerOnlyService_Resolves` | ✓ |
| 2 | `[Remote, Execute]` unchanged through the remote path, incl. the wrapper guard | Must | `[integration]` | `LocalExecuteTests.RemoteExecute_ClientScope_CrossesTheWireOnce`; `Combinations.ExecuteBehaviorTests.Execute_TaskTResult_{None,Single,Multiple,Service,Mixed}_Remote_*`; pre-existing `RemoteExecuteServiceTests` and `ClassExecuteRoundTripTests` green unchanged. Guard: the guarded renderer branch emits the v1.8.1 text (generated `LocalExecuteTargetFactory.g.cs` inspected: `if (NeatooRuntime.IsServerRuntime)` wraps only `RunRemote`); absence on a trimmed client is measured by `verify-trimmed.sh` in CI, which EXRM-003 extends | ✓ behavior; guard by inspection + existing CI gate |
| 3 | Bare delegate resolves in Remote, Logical, Server; remote registration only in Remote | Must | `[integration]` | `LocalExecuteTests.BareExecute_ResolvesAndRuns_InRemoteLogicalAndServerContainers`; `RemoteExecute_LogicalScope_RunsLocally_NoWire` with `RemoteExecute_ClientScope_CrossesTheWireOnce` | ✓ |
| 4 | Handler refuses a bare static delegate as unknown; still serves class, interface, static remote, client-raised events | Should | `[integration]` | `LocalExecuteTests.Handler_RefusesCraftedRequest_ForBareDelegate_AsUnknownDelegate`; `Handler_StillServes_RemoteDelegate_NamedTheSameWay`; class: `ClassExecuteRoundTripTests.ClassExecute_Remote_*`; interface: `FactoryGenerator.InterfaceFactory.InterfaceFactoryAuthTests`; client raise: `Events.FactoryEventHandler.FactoryEventHandlerClientServerTests.ClientRaise_*` — all green after the refusal landed | ✓ |
| 5 | Client-scope behavior tests observe one remote request per call; local-scope none | Must | `[integration]` | `Combinations.ExecuteBehaviorTests` — all ten, each asserting `RemoteCallCounter` (1 for client scope, 0 for local scope) | ✓ |
| 6 | Bare static Execute with a server-only service fails on the client at call time, no remote request | Must | `[integration]` | `LocalExecuteTests.BareExecute_ClientScope_ServerOnlyService_FailsAtCallTime_NoRemoteRequest` | ✓ |
| 7 | Both solutions build and test green | Must | `[explicit-skip: meta-bullet]` | `reviews/001-build.log`, `reviews/001-test.log` | ✓ |

Supporting `[unit]`: `RemoteFactory.UnitTests.Internal.LocalOnlyDelegateRegistryTests` (4 tests) pins the registry the refusal relies on.

---

## Gate Record

- Round 1 (2026-09-08): test-reviewer **CLEAN** — 0 must-cover, 0 should-cover; 2 tech-debt: one punched inline (server-only failure test now asserts the message names the service), one dismissed (probe types left in the process-global registry) — `reviews/001-test-review.md`
- Code review (2026-09-08): code-reviewer **CLEAN** — 0 veto-tier; 2 callouts: local-scope zero counts documented as non-discriminating (comments punched inline), public registry register accepted on the DTO-registry precedent — `reviews/001-code-review.md`
- Done 2026-09-08. No leftovers accepted; no Must demoted.

---

## Plan Amendments

### 2026-09-08 — Plan-review triage: refuse-list, wire hook, bullet shapes, tier

- **Section affected:** Steps 5 and 7; Acceptance bullets 3, 4, 5 and new bullet 6 (the server-only failure); Constraints & Invariants.
- **Original said:** Step 5 declared remote-callable delegates and refused all others; Step 7 assumed a wire-crossing hook existed; bullet 3 was `[unit]`; bullet 5 asserted `[Remote]` in generated source; no bullet named the server-only failure on the client.
- **What changed:** the refusal is a refuse-list of bare static delegates, mirroring the DTO constructor registry, served as unknown on the wire; the test stand-in gains a per-scope call count; bullet 3 is `[integration]`; bullet 5 is behavioral (one remote request per client-scope call, none per local-scope call); new bullet 6 pins the server-only failure; bullet 4 names all three shapes.
- **Why:** an allow-list scoped to the static registrar would refuse every class- and interface-factory remote call (veto V1, `reviews/001-plan-review.md`); the rest are callouts B2, B3, B4 and the orchestrator's tier correction, all accepted by the user 2026-09-08.
- **Discovery Log:** 2026-09-08 / EXRM-001

---

## Notes

Recon (2026-09-08): the weld is `FactoryGenerator.Types.cs:582` and `:629` (the latter is dead for Execute; record primary ctors are Create-only). `ExecuteDelegateModel` has no remote flag; `BuildExecuteDelegate` reads neither `[Remote]` nor auth, although the transform populates ASP authorize calls for static methods too; `StaticFactoryRenderer.RenderFactoryServiceRegistrar` emits both registrations per factory mode with no per-delegate branch, and the local registration is always guarded. `FactoryEntryCall.RunAsync` tolerates a missing scheduler, so an unguarded local delegate is safe on a Remote-mode client. `CombinationDimensions.json` allows only Remote for Execute and `GenerateExecuteClassBody` never writes `[Remote]`, so the five `Comb_Execute_*_Remote` targets go vacuous the moment the weld is removed unless the generator emits `[Remote]` for them. The handler resolves any registered delegate by type from DI and invokes it; client-raised events arrive through the library-registered `RaiseFactoryEventRemote` marker delegate.

Open for the plan review: `[AspAuthorize]` on a static-factory Execute is collected by the transform and ignored by the builder today (no enforcement is rendered for static factories). That is pre-existing and undocumented; this plan only folds its presence into the remote-flag derivation and does not add enforcement. Proposed Dismiss on the todo.
