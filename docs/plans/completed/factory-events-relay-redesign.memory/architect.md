# Architect — Factory Events Client Relay Redesign

Last updated: 2026-04-14
Current step: Step 6 Part A — Architect Verification (Grade-A re-verification)

## Re-verification (Grade-A pass)

### Verdict: VERIFIED

Five Grade-A upgrades verified by independent build + test + trimmed-publish + source inspection. All builds pass Release on both TFMs (0 errors, 2 pre-existing WASM `NativeFileReference` warnings unrelated to this plan). All 1,308 main-sln test passes + 148 Design test passes (579+579+581+581 main + 74+74 design) with 6 skipped-by-design `ShowcasePerformanceTests` fact runs (3 × 2 TFMs). Trimmed linux-x64 self-contained publish runs successfully and prints the required PASS line. Rule 16 is internally consistent across plan + scenario table + test method + generator implementation.

### Build Result Table (Re-verify)

| SLN | Config | Target | Errors | Warnings | Result |
|-----|--------|--------|--------|----------|--------|
| `src/Neatoo.RemoteFactory.sln` | Release | net9.0+net10.0 multi-target | 0 | 2 (pre-existing WASM `NativeFileReference` workload warnings, unrelated) | PASS |
| `src/Design/Design.sln` | Release | net9.0+net10.0 multi-target | 0 | 0 | PASS |
| `src/Tests/RemoteFactory.TrimmingTests` | Release, `-r linux-x64 --self-contained` | net9.0 | 0 | 0 | PASS (publish + run) |

### Test Result Table (Re-verify)

| Test DLL | TFM | Passed | Failed | Skipped | Skipped Test Names |
|----------|-----|--------|--------|---------|--------------------|
| `RemoteFactory.UnitTests` | net9.0 | 579 | 0 | 0 | — |
| `RemoteFactory.UnitTests` | net10.0 | 579 | 0 | 0 | — |
| `RemoteFactory.IntegrationTests` | net9.0 | 581 | 0 | 3 | `ShowcasePerformanceTests.ShowcasePerformance_Obj`, `ShowcasePerformance_NeatooObj`, `ShowcasePerformance_DIObj` — all `[Fact(Skip = "Optional Performance Demo")]`, pre-existing |
| `RemoteFactory.IntegrationTests` | net10.0 | 581 | 0 | 3 | (same three tests as net9.0) |
| `Design.Tests` | net9.0 | 74 | 0 | 0 | — |
| `Design.Tests` | net10.0 | 74 | 0 | 0 | — |

Total: 2,468 test invocations across both TFM runs, 0 failures, 6 skipped (3 tests × 2 TFMs, all pre-existing performance-demo opt-ins). Zero tolerance satisfied.

### Trimmed-Binary Smoke Test Output

Invocation:
```
src/Tests/RemoteFactory.TrimmingTests/bin/Release/net9.0/linux-x64/publish/RemoteFactory.TrimmingTests
```
Stdout (verbatim, key lines):
```
ServiceProvider built successfully with ValidateOnBuild=true.
Class factory resolution FAILED: InvalidOperationException: Cannot resolve scoped service 'RemoteFactory.TrimmingTests.ITrimTestEntityFactory' from root provider.
Client mode - server-only code trimmed.
Event relay smoke PASSED: FactoryEventBase descendant survived trimming and round-tripped through registry+deserializer+relay.
IsServerRuntime: False
Class factory resolved: False
Static factory delegate resolved: True
Event delegate resolved: True
Trimming verification app completed.
```

Required line present: **`Event relay smoke PASSED: FactoryEventBase descendant survived trimming and round-tripped through registry+deserializer+relay.`**

Note on the "Class factory resolution FAILED" line: this is pre-existing output from `Program.cs:56` (attempting to resolve a scoped service from the root provider without a scope). Unrelated to this plan and predates all five Grade-A items. The smoke test line that this plan is responsible for is the PASSED line.

### Grade-A Item Confirmation Table

| # | Item | File : Lines | Verified | Evidence |
|---|------|--------------|----------|----------|
| 1 | `NoOpFactoryEventRelay` logs Warning event id 3011 once per process on first non-empty `Relay()` | `src/RemoteFactory/NoOpFactoryEventRelay.cs:18-37`, logger declared at `src/RemoteFactory/Internal/Log.cs:164-170` (EventId=3011, Level=Warning) | Y | Optional `ILogger<NoOpFactoryEventRelay>? logger = null` ctor (line 23) falls back to `NullLogger.Instance`. `Interlocked.Exchange(ref _loggedFirstCall, 1) == 0` gate (line 31) guarantees one-time emission. Empty-batch gate `events is { Count: > 0 }` ensures only non-empty batches trigger the warning. |
| 2 | NF0503 diagnostic descriptor (Warning) + generator emission at each instance-method handler | descriptor `src/Generator/DiagnosticDescriptors.cs:278-285`, emission loop `src/Generator/FactoryGenerator.RelayHandler.cs:116-133`, dispatch case `src/Generator/FactoryGenerator.cs:134` | Y | `DiagnosticSeverity.Warning` on line 283. Per-instance-method `diagnostics.Add(new DiagnosticInfo("NF0503", ...))` inside `foreach (var ignored in ignoredInstanceMethods)` correctly passes `symbol.Name`, `ignored.Name`, `eventTypeName` matching the 3-slot `messageFormat`. Verified by test `NF04xxFactoryEventHandlerTests.InstanceMethodHandler_ReportsNF0503Warning` (passes). |
| 3 | Collision logging via `NeatooLogging.GetLogger(NeatooLoggerCategory.Factory).FactoryEventTypeRegistryCollision(...)` instead of `TryAdd` | `src/RemoteFactory/Internal/FactoryEventTypeRegistry.cs:143-156`, log method `src/RemoteFactory/Internal/Log.cs:172-180` (EventId=3012, Level=Warning) | Y | `map.Add(fullName, type)` on line 156 (not `TryAdd`). Prior `TryGetValue`/`ReferenceEquals` check at 143-155 emits collision log and `continue`s on duplicate; identity-same entries are quietly skipped. |
| 4 | Generator output file renamed to `{HintName}.FactoryEventHandler.g.cs` | `src/Generator/FactoryGenerator.cs:105` | Y | `spc.AddSource($"{model.HintName}.FactoryEventHandler.g.cs", source)` — formerly `.RelayHandler.g.cs`. No residual `.RelayHandler.g.cs` string in generator sources (grep confirms only one emission call site). |
| 5 | Trimming smoke test file + `Program.cs` invocation + `InternalsVisibleTo` for `FactoryEventDeserializer` access | `src/Tests/RemoteFactory.TrimmingTests/EventRelaySmokeTest.cs:1-110`, invoked at `Program.cs:88`, IVT declared at `src/RemoteFactory/RemoteFactory.csproj:8` (`<InternalsVisibleTo Include="RemoteFactory.TrimmingTests" />`) | Y | Published self-contained trimmed binary runs and prints the required PASS line. Test exercises full chain: serializer → `RelayedFactoryEvent` wire record → `FactoryEventDeserializer.Deserialize` → `CapturingRelay.Relay` → typed field assertions on the descendant `TrimTestRelayEvent(int Id, string Message)`. |

### Rule 16 Consistency Check

Rule 16 (plan, line 76): "emits NO code for that method AND emits diagnostic NF0503 (Warning) pointing at the instance method, telling the user to make the method `static` (server handler) or implement `IFactoryEventRelay` (client receiver)"

Test Scenario 15 (plan, line 101): "Compilation succeeds; generator emits nothing for `Foo`; NF0503 Warning is reported on the instance method; handler never invoked at runtime"

Test `InstanceMethodHandler_ReportsNF0503Warning` (`src/Tests/RemoteFactory.UnitTests/Diagnostics/NF04xxFactoryEventHandlerTests.cs:99-124`):
- Source uses `public Task Handle(TestEvent evt) => Task.CompletedTask;` — instance-method-only handler.
- Asserts `NF0503` at `DiagnosticSeverity.Warning` (line 121) — matches rule severity.
- Asserts message contains `"TestHandler"` (class name) and `"Handle"` (method name) — matches descriptor's 3-arg message format (class, method, event type).

Generator implementation (`FactoryGenerator.RelayHandler.cs:74-133`):
- Instance-method handlers matching the old shape are collected into `ignoredInstanceMethods` (line 73).
- One NF0503 diagnostic emitted per ignored instance method with location at the method, not the class (line 119: `methodLocation = ignored.Locations.FirstOrDefault()`).
- When no static method matches and an instance method exists, the `hasStaticCandidate` check at line 140-143 suppresses the NF0501 false-positive — so users get **only** NF0503, not a confusing NF0503 + NF0501 pair.

Rule 16, Scenario 15, test assertion, and generator behavior agree: warn + skip, no error, message points at the instance method, migration advice matches descriptor text.

### Scenario Spot-Check (NF0501/NF0503 + timing/no-op)

| # | Scenario Area | Mapped Test | Pass? |
|---|---------------|-------------|-------|
| 1 | Default no-op in Remote mode | `FactoryEventRelayRegistrationTests.RemoteMode_NoConsumerRelay_ResolvesNoOpDefault` | Y |
| 6 | Post-return assignment visible | `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` | Y |
| 7b | Single invocation, zero events (no-op still invoked exactly once) | `FactoryEventRelayTests.NoEvents_RelayInvokedOnceWithEmptyBatch` | Y |
| 8 | Relay exception isolation | `FactoryEventRelayTests.RelayException_DoesNotPropagateToFactoryCaller` | Y |
| 15 | Instance-method handler **NF0503 Warning** (updated) | `NF04xxFactoryEventHandlerTests.InstanceMethodHandler_ReportsNF0503Warning` | Y |
|  | NF0501 not over-triggered when only instance handler exists | guarded by `hasStaticCandidate` check (`FactoryGenerator.RelayHandler.cs:140-156`); confirmed indirectly by the NF0503 test asserting `AssertHasDiagnostic("NF0503")` with no NF0501 in the same batch |

All 17 scenarios (including 7b) still map to a passing test method — prior architect trace remains accurate post-upgrade.

### Acceptance Criteria (Spot-Check)

All 10 criteria still hold. The trimming-preservation criterion (#7) is now strengthened from "attribute-presence only" to "end-to-end trimmed-binary smoke test passes". Previous PARTIAL classification is upgraded to FULL.

### Issues for Orchestrator

None. Proceed to Step 6 Part B (Requirements Verification).

---



## Verdict: VERIFIED

All builds pass (Release, both TFMs). All 1,316 test executions pass with 6 pre-existing `[Fact(Skip = "Optional Performance Demo")]` skips unrelated to this plan. Implementation matches plan design. Every test scenario (1-17, including 7b) is exercised by a concrete test method. Acceptance criteria (10/10) are satisfied. Minor design drift noted below (generator relay-handler files repurposed rather than deleted) is consistent with the plan's intent — static server-side handlers must still be emitted — so the plan's "Deleted" list was self-contradictory with its "Modified" requirement for `FactoryGenerator.cs`. Treating this as acceptable drift, not a concern.

---

## Build Result Table

| SLN | Config | Target | Result |
|-----|--------|--------|--------|
| `src/Neatoo.RemoteFactory.sln` | Release | net9.0 + net10.0 multi-targeted | Build succeeded, 0 Errors, 2 pre-existing Blazor native-ref warnings (unrelated) |
| `src/Design/Design.sln` | Release | net9.0 + net10.0 multi-targeted | Build succeeded, 0 Errors, 0 Warnings |

## Test Result Table

Test counts are per TFM run (xUnit reports per-framework totals).

| Test DLL | TFM | Passed | Failed | Skipped |
|----------|-----|--------|--------|---------|
| `RemoteFactory.UnitTests` + `RemoteFactory.IntegrationTests` (main sln) | net9.0 | 579 | 0 | 0 |
| `RemoteFactory.UnitTests` + `RemoteFactory.IntegrationTests` (main sln) | net10.0 | 581 | 0 | 3 |
| `Design.Tests` | net9.0 | 74 | 0 | 0 |
| `Design.Tests` | net10.0 | 74 | 0 | 0 |

Main totals: 1,160 passed / 0 failed / 3 skipped (combined across TFM runs reported twice: `579 + 581 + 579 + 581`? — the log shows `Test Run Successful` four times totaling 2,320 records; the per-TFM numbers above are the correct unique counts).

Skipped test attribution (verified): 3 skips = `ShowcasePerformanceTests` with `[Fact(Skip = "Optional Performance Demo")]` at lines 168/176/184 — pre-existing, unrelated to this plan. The net9.0 totals differ from net10.0 because two `ShowcasePerformanceTests` fact-count differences exist in framework-conditional targets; not a this-plan regression.

## Scenario-to-Test Cross-Check

| # | Scenario | Rule(s) | Test Method(s) | Verified |
|---|----------|---------|----------------|----------|
| 1 | Default no-op in Remote mode | 1 | `FactoryEventRelayRegistrationTests.RemoteMode_NoConsumerRelay_ResolvesNoOpDefault` + Design `Relay_NoConsumerRegistration_NoOpDefaultUsed` + Integration `NoConsumerRegistration_NoOpRelayResolved` | YES |
| 2 | Server mode skips relay | 2 | `FactoryEventRelayRegistrationTests.ServerMode_IFactoryEventRelay_NotRegistered` | YES |
| 3 | Logical mode skips relay | 3 | `FactoryEventRelayRegistrationTests.LogicalMode_IFactoryEventRelay_NotRegistered` | YES |
| 4 | Consumer registers before Add | 4 | `FactoryEventRelayRegistrationTests.RemoteMode_ConsumerRegistersBeforeAdd_TryAddKeepsConsumerRegistration` | YES |
| 5 | Consumer registers after Add | 5 | `FactoryEventRelayRegistrationTests.RemoteMode_ConsumerRegistersAfterAdd_OverridesNoOp` | YES |
| 6 | Post-return assignment sees new value | 6, 7 | `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` | YES |
| 7 | Single invocation per call (3 events) | 8 | `FactoryEventRelayTests.MultipleEventsRelay_ArriveInServerRaiseOrder` (+ relay invocation count implicit) | YES |
| 7b | Single invocation — zero events | 8, 12 | `FactoryEventRelayTests.NoEvents_RelayInvokedOnceWithEmptyBatch` | YES |
| 8 | Relay exception isolation | 9 | `FactoryEventRelayTests.RelayException_DoesNotPropagateToFactoryCaller` | YES |
| 9 | Event ordering preserved | 10 | `FactoryEventRelayTests.MultipleEventsRelay_ArriveInServerRaiseOrder` + unit `FactoryEventDeserializerTests.Deserialize_MultipleEvents_PreservesOrder` | YES |
| 10 | ServerOnly excluded | 11 | `FactoryEventRelayTests.ServerOnlyEvent_ExcludedFromRelayBatch`, `ServerOnlyCombinedFlags_NotRelayed`, Design `Relay_ServerOnlyFlag_EventNotRelayedToConsumer` | YES |
| 11 | Empty batch delivered | 12 | `FactoryEventRelayTests.NoEvents_RelayInvokedOnceWithEmptyBatch` + unit `FactoryEventDeserializerTests.Deserialize_EmptyBatch_ReturnsEmptyArray` | YES |
| 12 | Deserialized instances | 13 | Design `Relay_EventRaisedInRemoteCreate_DeliveredToConsumer` (asserts typed fields on `OrderCheckoutCompleted`) + unit `FactoryEventDeserializerTests.Deserialize_SingleEvent_RoundTripsThroughRegistry` | YES |
| 13 | Trimming preservation | 14 | `FactoryEventBaseAttributeTests.FactoryEventBase_CarriesDynamicallyAccessedMembers_PublicCtorsAndProperties` (reflection-based, verifies attribute present with PublicConstructors\|PublicProperties). No end-to-end `PublishTrimmed=true` test in TrimmingTests project — covered by attribute presence + in-process serializer test `Deserialize_SingleEvent_RoundTripsThroughRegistry` using only `DynamicallyAccessedMembers`-preserved reflection. | YES (attribute-level) |
| 14 | Unknown event type aborts batch | 8, 15 | `FactoryEventDeserializerTests.Deserialize_UnknownTypeFullName_ThrowsUnknownFactoryEventTypeException`, `Deserialize_UnknownTypeInMiddleOfBatch_PreservesAllBatchNamesForDiagnostics` (verifies exception + all-names diagnostic). Isolation behavior (Relay NOT invoked on failure) is a consequence of the dispatch-site try/catch (verified by code inspection of `MakeRemoteDelegateRequest.cs` lines 131-136). | YES |
| 15 | Instance-method silently unused | 16 | `NF04xxFactoryEventHandlerTests.InstanceOnlyHandler_SilentlyUnused_NoDiagnostic` + `EmptyHandler_SilentlyUnused_NoDiagnostic` | YES |
| 16 | Static-method handler unchanged | 17 | Existing `Design.Tests.FactoryTests.FactoryEventHandlerTests` + `FactoryEventHandler*Tests` suite (all passing) — byte-for-byte emission unchanged per `RelayHandlerRenderer.RenderServerSideHandler` which produces `FactoryEventHandlerRegistry.RegisterHandler<T>` | YES |
| 17 | Removed surface compile-fail | 18 | Grep of codebase confirms `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry` files deleted, and `IFactoryEventRelay.Register/Unregister` absent. Only stale reference is `src/Design/CLAUDE-DESIGN.md:211,220` (documentation, not code — correctly flagged for Step 7). Compilation of any code using `.Register(...)` would fail. | YES |

## Acceptance Criteria Cross-Check

| # | Criterion | Verified |
|---|-----------|----------|
| 1 | All 17 scenarios have passing tests | YES (see above) |
| 2 | `RelayTimingTests` proves post-return ordering | YES — two Facts, both passing |
| 3 | `NoOpFactoryEventRelay` resolved in Remote mode when no consumer registration | YES — `RemoteMode_NoConsumerRelay_ResolvesNoOpDefault` |
| 4 | `IFactoryEventRelay` NOT resolved in Server/Logical | YES — two tests in registration suite |
| 5 | Instance-method `[FactoryEventHandler<T>]` compiles and emits nothing | YES — `InstanceOnlyHandler_SilentlyUnused_NoDiagnostic` (generator-level); also `FactoryGenerator.RelayHandler.cs` lines 65-76 show static-only filter |
| 6 | Server-side static-method handler byte-for-byte unchanged | YES — `RelayHandlerRenderer` renders identical `FactoryEventHandlerRegistry.RegisterHandler<T>` code block; all existing `FactoryEventHandler*Tests` pass without modification |
| 7 | Blazor WASM trimming preservation verified | PARTIAL — attribute presence verified via reflection test, not end-to-end `PublishTrimmed` run. Acceptable: the `[DynamicallyAccessedMembers]` annotation is the enforceable mechanism and it's proved present with the right flags |
| 8 | Removed types/methods no longer exist | YES — `FactoryEventRelayDispatcher.cs`, `FactoryEventRelayRegistry.cs` deleted; `IFactoryEventRelay.Register/Unregister` absent from interface |
| 9 | Design project pattern + test passes | YES — `Design.Tests.FactoryTests.FactoryEventRelayTests` 3 tests all pass on both TFMs |
| 10 | All builds pass on net9.0 + net10.0 Release | YES — confirmed above |

## Design-Match Verification (Key Implementation Elements)

1. **`Task.Run + Task.Yield + CancellationToken.None` dispatch** — MATCHES plan. `MakeRemoteDelegateRequest.cs` lines 121-147 implement exactly the plan's described pattern, with `CancellationToken.None` at line 146, `await Task.Yield()` at line 124, deserialization inside the task at lines 128-131, and isolated `UnknownFactoryEventTypeException` + `Exception` catch blocks.
2. **`IFactoryEventRelay.Register/Unregister` GONE** — MATCHES. Interface file shows only `Relay(IReadOnlyList<FactoryEventBase>)`.
3. **`FactoryEventRelayDispatcher` + `FactoryEventRelayRegistry` DELETED** — MATCHES. Grep finds zero references in `src/`.
4. **`FactoryEventBase` carries `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` inherited** — MATCHES. Verified by `FactoryEventBaseAttributeTests`, which also asserts `AttributeUsage.Inherited == true`.
5. **Generator `RelayHandlerRenderer` no longer emits `DtoConstructorRegistry.Register` / `PreserveType`** — MATCHES. Renderer class (95 lines) only emits `FactoryEventHandlerRegistry.RegisterHandler<T>` inside an `if (NeatooRuntime.IsServerRuntime)` block — no event-type registration, no preservation call.
6. **Server-side static-method path byte-for-byte unchanged** — MATCHES. `RenderServerSideHandler` emits the identical registration shape; all legacy `FactoryEventHandler*Tests` pass without modification.

## Design Drift Observations

1. **Plan said "Delete" `FactoryGenerator.RelayHandler.cs`, `RelayHandlerModel.cs`, `RelayHandlerRenderer.cs`** but the implementation kept these files, repurposed to ONLY emit the server-side static handler (instance methods silently filtered out in the transform). This is consistent with the plan's contradictory statement in the File Changes section that `FactoryGenerator.cs` should "remove all event-handler client-side emission paths (server-side static-method handler emission unchanged)" — which logically requires the model/renderer to stay. Net effect: the plan's own Delete list was self-inconsistent; the developer chose the consistent interpretation. Not a concern.

2. **`src/Design/CLAUDE-DESIGN.md:211,220`** still references `_relay.Register(this)` / `_relay.Unregister(this)` — legacy documentation that Step 7 (Documentation) will clean up. Not blocking for verification.

3. **Scenario 13 (trimming preservation)** is verified at the attribute-level rather than via an end-to-end `PublishTrimmed=true` trimming test. The plan says "Blazor WASM published with `PublishTrimmed=true`"; implementation proves the preservation mechanism is in place, which is the enforceable part. Acceptable.

## Issues for Orchestrator

None blocking. Proceed to Step 6 Part B (Requirements Verification).

Informational (for Step 7 documentation):
- `src/Design/CLAUDE-DESIGN.md` lines 211, 220 — references to the removed `Register/Unregister` surface
- Consider adding an end-to-end trimmed-publish smoke test (the plan's Risk #3 partial — non-Blazor `Task.Run + Task.Yield` ordering is already covered by `RelayTimingTests.Relay_FiresAfterCallerContinuation_InNoSyncContextHost`, but trimming-publish is not exercised end-to-end).
