# Developer — Factory Events Client Relay Redesign

Last updated: 2026-04-14
Current step: Step 5 — Code Review (fresh run)

## Verdict: Approved (with minor notes)

The implementation matches the plan's Business Rules and Test Scenarios. Every rule 1-18 traces to concrete code paths with assertions. Every scenario 1-17 (plus 7b) has a corresponding test method (Scenario 13 "trimming preservation" is covered only by attribute-presence tests; there is no runtime-trimmed test, but the annotation on `FactoryEventBase` is the mechanism and it is verified — this matches the plan's risk posture). No design drift detected. The two specific scrutiny areas (Task.Run + Task.Yield dispatch, and the `hasStaticCandidate` gate) both hold up. Minor notes captured below for the orchestrator — none block verification.

## Code Review Trace (Business Rules)

| # | Rule summary | File:line(s) | Verified? | Notes |
|---|-------------|--------------|-----------|-------|
| 1 | Remote mode + no consumer relay → NoOp singleton registered | `src/RemoteFactory/AddRemoteFactoryServices.cs:124` (`TryAddSingleton<IFactoryEventRelay, NoOpFactoryEventRelay>()` inside `if (remoteLocal == NeatooFactory.Remote)`) | Y | Implementation: `NoOpFactoryEventRelay.Relay => Task.CompletedTask` at `src/RemoteFactory/NoOpFactoryEventRelay.cs:13` |
| 2 | Server mode → IFactoryEventRelay NOT registered | `AddRemoteFactoryServices.cs:136-146` (Server branch registers only `IFactoryEventCollector` + `HandleRemoteDelegateRequest`, no relay) | Y | Also covered by explicit test `ServerMode_IFactoryEventRelay_NotRegistered` |
| 3 | Logical mode → IFactoryEventRelay NOT registered | `AddRemoteFactoryServices.cs:147-148` (comment "Logical mode: No IMakeRemoteDelegateRequest registered."; no relay registration path for Logical) | Y | |
| 4 | Consumer registers BEFORE AddNeatooRemoteFactory → TryAdd keeps it | `AddRemoteFactoryServices.cs:124` uses `TryAddSingleton` | Y | Standard DI semantics; test `RemoteMode_ConsumerRegistersBeforeAdd_TryAddKeepsConsumerRegistration` proves it |
| 5 | Consumer registers AFTER AddNeatooRemoteFactory → replaces NoOp | `AddRemoteFactoryServices.cs:124` + DI last-writer-wins on non-TryAdd | Y | Test `RemoteMode_ConsumerRegistersAfterAdd_OverridesNoOp` proves it |
| 6 | Caller continuation begins before Relay is invoked | `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:114-150` (Task.Run with `await Task.Yield()` as first statement inside; `return deserialized;` follows immediately) | Y | `Task.Run` queues to pool; caller's awaiter continuation runs on its own sync context/thread. The `await Task.Yield()` inside forces an additional yield before deserialization/Relay |
| 7 | Relay invoked after caller continuation completes/yields | Same as rule 6 — the fire-and-forget lambda runs through pool + yield | Y | Verified by `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` which writes state synchronously on continuation and expects Relay to observe it |
| 8 | Remote call success + deserialization success → Relay called exactly once; deserialization fail → Relay NOT called, logged | `MakeRemoteDelegateRequest.cs:122-146`: Task.Run fires regardless; inside the lambda, if `FactoryEventDeserializer.Deserialize` throws, the `return` (line 135) short-circuits before `relay.Relay` is ever called (line 140). Relay exception path logs at line 144. Deserialization failure logs at line 134. | Y | `FactoryEventDeserializationFailed` (event 3009) and `FactoryEventRelayFailed` (event 3008) in `src/RemoteFactory/Internal/Log.cs:146-162` |
| 9 | Relay exception doesn't propagate | Same site: inner try/catch wraps `await relay.Relay(...)` with `catch (Exception ex) { relayLogger.FactoryEventRelayFailed(...); }` — lambda is discarded `_ = Task.Run(...)` | Y | Integration test `RelayException_DoesNotPropagateToFactoryCaller` exercises this |
| 10 | Event order preserved | `FactoryEventDeserializer.cs:33-46` iterates `events` by index into `result[i]`, preserving input order. Server-side collector uses `List<FactoryEventBase>` (unchanged, per reviewer notes) | Y | Unit test `Deserialize_MultipleEvents_PreservesOrder`; integration test `MultipleEventsRelay_ArriveInServerRaiseOrder` |
| 11 | ServerOnly events excluded from batch | Unchanged server-side behavior in `FactoryEventsDispatcher.DispatchToHandlers` (not modified) | Y | Integration test `ServerOnlyEvent_ExcludedFromRelayBatch` + `ServerOnlyCombinedFlags_NotRelayed` |
| 12 | Zero events → Relay invoked once with empty list | `MakeRemoteDelegateRequest.cs:128-130`: `rawEvents is { Count: > 0 } ? Deserialize(...) : Array.Empty<FactoryEventBase>()` — the Task.Run fires whenever `_relay != null`, independent of event count | Y | Integration test `NoEvents_RelayInvokedOnceWithEmptyBatch` |
| 13 | Events delivered as fully deserialized FactoryEventBase instances | `FactoryEventDeserializer.cs:39-45` deserializes each payload and type-checks via `is not FactoryEventBase evt` | Y | Unit test `Deserialize_SingleEvent_RoundTripsThroughRegistry` + integration `SingleEventRelay_ConsumerReceivesEvent` |
| 14 | FactoryEventBase carries attributes with Inherited=true; descendants preserved through trimming | `src/RemoteFactory/FactoryEventBase.cs:15-17` applies both `[FactoryEvent]` and `[DynamicallyAccessedMembers(PublicConstructors\|PublicProperties)]`. `FactoryEventAttribute` declares `Inherited = true` (`FactoryEventAttribute.cs:17`). `[DynamicallyAccessedMembers]` metadata semantics inherit to derived type metadata for trim analysis | Y | Attribute presence + inheritance verified by `FactoryEventBaseAttributeTests` (all 5 facts). Runtime trimmed scenario not directly tested (no dedicated trimming test for events). |
| 15 | Unknown event type at deserialization → throws, batch fails fast, caught + logged | `FactoryEventDeserializer.cs:36-37` throws `UnknownFactoryEventTypeException` with type name + full batch names. Caught at `MakeRemoteDelegateRequest.cs:132-136` → `FactoryEventDeserializationFailed` log; `return` prevents Relay | Y | Unit tests `Deserialize_UnknownTypeFullName_...`, `Deserialize_UnknownTypeInMiddleOfBatch_...` verify exception shape |
| 16 | Instance-only [FactoryEventHandler<T>] → no code emitted, no diagnostic | `src/Generator/FactoryGenerator.RelayHandler.cs:74-75` (`if (!member.IsStatic) continue;`) filters instance methods. Line 106-128 emits NF0501 only when `hasStaticCandidate` is true (any static ordinary method present). Line 184 gates the entire return on `entries.Count == 0 && diagnostics.Count == 0 → return null;`. `FactoryGenerator.cs:101 if (model.Entries.Count == 0) return;` gates emission | Y | Unit tests `InstanceOnlyHandler_SilentlyUnused_NoDiagnostic`, `EmptyHandler_SilentlyUnused_NoDiagnostic` |
| 17 | Static-method [FactoryEventHandler<T>] unchanged | `FactoryGenerator.RelayHandler.cs` still produces `EventHandlerEntry` for matching static methods; `RelayHandlerRenderer.cs:32, 45-53, 68-94` still emits `FactoryEventHandlerRegistry.RegisterHandler<T>(...)` wrapped in `NeatooRuntime.IsServerRuntime` | Y | Unit tests `Valid_StaticMethod_NoDiagnostics`, `Valid_StaticMethodWithServices_NoDiagnostics`, `Valid_MultipleEventTypes_NoDiagnostics`; existing server-side integration tests unmodified |
| 18 | Register/Unregister, FactoryEventRelayRegistry, FactoryEventRelayDispatcher removed | `src/RemoteFactory/IFactoryEventRelay.cs` contains only `Relay(...)` (no Register/Unregister). `ls src/RemoteFactory/FactoryEventRelay*.cs` → only exception + attribute files exist (no Dispatcher/Registry). Grep for `FactoryEventRelayDispatcher\|FactoryEventRelayRegistry\|DispatchRelayedEvents` finds only CLAUDE-DESIGN.md (docs, out-of-scope for this phase) and a single comment in RelayTimingTests.cs | Y | |

## Test Coverage Trace (Test Scenarios)

| # | Plan scenario | Test file:method | Covered? | Notes |
|---|--------------|-------------------|----------|-------|
| 1 | NoOp in Remote mode | `FactoryEventRelayRegistrationTests.RemoteMode_NoConsumerRelay_ResolvesNoOpDefault` + integration `FactoryEventRelayTests.NoConsumerRegistration_NoOpRelayResolved` | Y | Asserts type name "NoOpFactoryEventRelay" |
| 2 | Server mode null | `FactoryEventRelayRegistrationTests.ServerMode_IFactoryEventRelay_NotRegistered` | Y | |
| 3 | Logical mode null | `FactoryEventRelayRegistrationTests.LogicalMode_IFactoryEventRelay_NotRegistered` | Y | |
| 4 | Consumer Before → kept | `FactoryEventRelayRegistrationTests.RemoteMode_ConsumerRegistersBeforeAdd_TryAddKeepsConsumerRegistration` | Y | |
| 5 | Consumer After → replaces | `FactoryEventRelayRegistrationTests.RemoteMode_ConsumerRegistersAfterAdd_OverridesNoOp` | Y | |
| 6 | Post-return assignment visible | `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` | Y | Writes `callerState` synchronously on continuation, asserts SnapshotRelay observes it |
| 7 | Single invocation — 3 events | `FactoryEventRelayTests.MultipleEventsRelay_ArriveInServerRaiseOrder` | Y | Verifies both count and order |
| 7b | Single invocation — 0 events | `FactoryEventRelayTests.NoEvents_RelayInvokedOnceWithEmptyBatch` | Y | Explicit `InvocationCount == 1` assertion + empty batch |
| 8 | Relay exception isolation | `FactoryEventRelayTests.RelayException_DoesNotPropagateToFactoryCaller` | Y | |
| 9 | Ordering preserved | `Deserialize_MultipleEvents_PreservesOrder` (unit) + `MultipleEventsRelay_ArriveInServerRaiseOrder` (integration) | Y | |
| 10 | ServerOnly excluded | `ServerOnlyEvent_ExcludedFromRelayBatch` + `ServerOnlyCombinedFlags_NotRelayed` | Y | |
| 11 | Empty batch delivered | `NoEvents_RelayInvokedOnceWithEmptyBatch` | Y | Same as 7b |
| 12 | Deserialized instances with state | `Deserialize_SingleEvent_RoundTripsThroughRegistry` + integration `SingleEventRelay_ConsumerReceivesEvent` | Y | Both verify property values |
| 13 | Trimming preservation | `FactoryEventBaseAttributeTests.*` (attribute presence + inheritance). No `PublishTrimmed` runtime test for events. | Partial | Annotation is present and correct; no end-to-end trimmed validation. Acceptable per plan — trimming scenario deferred to manual Blazor WASM publish verification per plan Risk #2 |
| 14 | Unknown type aborts batch | `Deserialize_UnknownTypeFullName_ThrowsUnknownFactoryEventTypeException` + `Deserialize_UnknownTypeInMiddleOfBatch_PreservesAllBatchNamesForDiagnostics` | Y | Dispatch-site catch path is exercised indirectly by integration tests (no regression) — no integration test specifically forces unknown-type delivery, but the deserializer unit tests prove the throw + the catch block is mechanically straight-forward |
| 15 | Instance-method silently unused | `NF05xxFactoryEventHandlerTests.InstanceOnlyHandler_SilentlyUnused_NoDiagnostic` + `EmptyHandler_SilentlyUnused_NoDiagnostic` | Y | |
| 16 | Static-method unchanged | `NF05xxFactoryEventHandlerTests.Valid_StaticMethod_NoDiagnostics` + `Valid_StaticMethodWithServices_NoDiagnostics` + `Valid_MultipleEventTypes_NoDiagnostics` + pre-existing server-side integration tests | Y | |
| 17 | Removed surface | Implicit: compilation of `IFactoryEventRelay.cs` without Register/Unregister proves removal; orchestrator summary confirms 579 unit + 581 integration + 74 Design tests all pass | Y | No explicit test, but the whole codebase failing to compile would be immediate |

## Scrutiny Findings

### Task.Run + Task.Yield dispatch (rules 6, 7, 8, 9)

Reviewed `MakeRemoteDelegateRequest.cs:114-148`:

- Line 122: `_ = Task.Run(async () => {...}, CancellationToken.None);` — runs the lambda on the thread pool, discarded task.
- Line 124: `await Task.Yield();` — first statement in the lambda; re-queues the lambda's continuation onto the pool.
- Lines 125-136: deserialization in its own try/catch with log + `return` on failure (short-circuits before Relay — satisfies rule 8 deserialization-failure branch).
- Lines 138-145: `await relay.Relay(events).ConfigureAwait(false);` in its own try/catch with log (satisfies rule 9 isolation).
- Line 150: `return deserialized;` — immediately follows the fire-and-forget schedule.

Locals are captured by value into the lambda (`rawEvents`, `relay`, `serializer`, `relayLogger`, `relayCorrelationId`) — good practice, avoids closure over mutable `this` state.

The Task.Run + Task.Yield pattern does not provide a hard ordering guarantee on a sync-context-less host — in principle the pool thread executing the lambda could race past the caller's continuation. In practice Task.Yield forces a requeue, which makes caller-continuation-wins heavily favored and the `RelayTimingTests.Relay_FiresAfterCallerContinuation_InNoSyncContextHost` test verifies this empirically. This matches plan Risk #3 which acknowledges the situation.

### `hasStaticCandidate` gate (rule 16)

Reviewed `FactoryGenerator.RelayHandler.cs:106-128`:

```csharp
if (matchingMethods.Count == 0)
{
    var hasStaticCandidate = symbol.GetMembers().OfType<IMethodSymbol>().Any(m =>
        m.MethodKind == MethodKind.Ordinary
        && m.IsStatic);
    if (hasStaticCandidate)
    {
        diagnostics.Add(new DiagnosticInfo("NF0501", ...));
    }
    continue;
}
```

The gate fires NF0501 only when:
1. The class declares `[FactoryEventHandler<T>]` but no method matched the "static + returns Task + first non-service/non-CT param is T" shape, AND
2. At least one ordinary static method exists on the class.

This correctly distinguishes:
- "User intended a static handler but got the shape wrong" (e.g., wrong return type or wrong param type) → diagnose. Covered by `NF0501_StaticMethod_WrongReturnType_ReportsDiagnostic` and `NF0501_StaticMethod_WrongParamType_ReportsDiagnostic`.
- "User has an instance-only handler class" or "User declared an empty handler class" → silent per Rule 16. Covered by `InstanceOnlyHandler_SilentlyUnused_NoDiagnostic` and `EmptyHandler_SilentlyUnused_NoDiagnostic`.

Edge case considered: could `MethodKind.Ordinary && IsStatic` falsely flag something that isn't a user-declared method (e.g., compiler-generated static members)? `MethodKind.Ordinary` excludes constructors, property accessors, operators, destructors. A static field initializer does not produce a MethodKind.Ordinary method. The gate is tight. 

One minor concern: if the user has a static utility method completely unrelated to event handling (e.g., `public static int Helper() => 0;`), that would count as a static candidate and trigger NF0501 when the class has a `[FactoryEventHandler<T>]` but no matching handler. This is intentional and matches the plan's Rule 16 text ("only instance methods (no matching static handler)") — a static helper that isn't a handler means the user probably intended one of the other static methods to be the handler and got the shape wrong. Acceptable.

## Minor Notes (not blockers)

1. **Scenario 13 (trimming) is partial.** Only attribute-presence verification exists; no runtime-trimmed event deserialization test. Plan's Acceptance Criteria says "Blazor WASM trimming preservation verified" — this is satisfied by annotation correctness, but a dedicated smoke test inside `RemoteFactory.TrimmingTests/` would harden the guarantee. Not a blocker because the mechanism is declarative and unit-tested.

2. **`UnknownFactoryEventTypeException` inherits from `Exception` directly.** Plan does not mandate a base class, and `Exception` is correct for a "fail-loud, log-isolated" failure mode. XML doc says "Caught inside the relay dispatch isolation block" — verified to match.

3. **`FactoryEventTypeRegistry` Scan catches general `Exception`** (line 118-121): justified by the suppression comment — `Assembly.GetTypes()` can throw varied reflection/load exceptions and the scanner must skip malformed assemblies rather than abort. Acceptable.

4. **`FactoryEventDeserializer.Deserialize` null-checks with `ArgumentNullException.ThrowIfNull`.** Good — but these throws occur OUTSIDE the dispatch-site try/catch boundary? Reviewed: `MakeRemoteDelegateRequest.cs:128-136` wraps the entire `FactoryEventDeserializer.Deserialize` call in try/catch that catches `Exception` — so `ArgumentNullException` from the deserializer would also be caught and logged via `FactoryEventDeserializationFailed`. Defensive; no propagation hazard.

5. **`RecordingFactoryEventRelay.OnRelay` increments `InvocationCount` before throwing** (TestTargets/Events/FactoryEventRelayTargets.cs:122-133). Matches the RelayException test's assertion `InvocationCount == 1` — the "relay was called" signal is recorded even though the handler threw. Good test design.

6. **`FactoryEventTypeRegistryTests` does not explicitly reset state between tests** via `Reset()`, but the registry uses first-writer-wins caching and the test types are uniquely named probe types, so there's no cross-test contamination. Fine.

## Key Context

- Plan scope: replace instance-method client-side `[FactoryEventHandler<T>]` + `FactoryEventRelayRegistry` + `Register/Unregister` with single-method `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)` + fire-and-forget Task.Run dispatch.
- Breaking change: instance-method handlers silently become inert post-upgrade (Rule 16, no diagnostic — user's call).
- Preserves: server-side static-method handlers, wire format, ServerOnly filtering, handler-exception isolation.

## Mistakes to Avoid

None so far (first run).

## User Corrections

None so far.
