# Architect — Factory Event Relay

Last updated: 2026-04-09
Current step: Step 6A Architect Verification (Post-Implementation) — VERIFIED

## Key Context

The Factory Event Relay plan builds on the IFactoryEvents mediator (commit 1750f52) to add server-to-client event forwarding. Events raised during server factory operations are captured, serialized in RemoteResponseDto, and replayed on the client.

The design was refined during implementation from `IFactoryEventHandler<T>` interface to `[FactoryEventHandler<T>]` class attribute, unifying server-side `[FactoryEventHandler]` method attribute and client-side relay into one generator pipeline keyed off the class attribute. This refinement is documented in the plan's Design section.

## Architect Verification (Post-Implementation) — Step 6A

### Verdict: VERIFIED

### Evidence

**Build:**
- `dotnet build src/Neatoo.RemoteFactory.sln` — 0 Errors, 2 Warnings (both pre-existing Blazor WASM `NativeFileReference`/`WasmBuildNative` warnings for the OrderEntry example, unrelated to this feature)
- `dotnet build src/RemoteFactory/RemoteFactory.csproj` — 0 warnings, 0 errors (core library clean)

**Tests:**
- `RemoteFactory.UnitTests` (net9.0): 506 passed, 0 failed
- `RemoteFactory.UnitTests` (net10.0): 506 passed, 0 failed
- `RemoteFactory.IntegrationTests` (net9.0): 538 passed, 0 failed, 3 skipped (showcase performance tests, pre-existing)
- `RemoteFactory.IntegrationTests` (net10.0): 538 passed, 0 failed, 3 skipped (same)
- `Design.Tests` (net9.0): 47 passed, 0 failed
- `Design.Tests` (net10.0): 47 passed, 0 failed

Zero failures across all test projects and both target frameworks.

### IL Trimming Warnings

No trimming warnings on the new types. Verified via core library build (0 warnings). The `NeatooTransportJsonContext` was correctly updated to include `RelayedFactoryEvent`, `List<RelayedFactoryEvent>`, and `IReadOnlyList<RelayedFactoryEvent>`. Confirmed via generated STJ source files at `src/RemoteFactory/obj/Debug/net{9,10}.0/generated/.../NeatooTransportJsonContext.RelayedFactoryEvent.g.cs` (and List/IReadOnlyList variants). This means the production HTTP deserialization path (`MakeRemoteDelegateRequestHttpCall.cs:40`) will correctly deserialize relayed events without reflection.

### Design Refinement Consistency Check

Searched the entire source tree for leftover `IFactoryEventHandler<` interface references:
```
grep -l "IFactoryEventHandler<" src --include="*.cs" -r | grep -v obj | grep -v bin
(no results)
```

The `[FactoryEventHandler<T>]` class attribute is used consistently in all active source: tests, examples (PersonEventHandler.cs), generator (RelayHandlerRenderer, FactoryGenerator pipeline), and diagnostics (NF0401/NF0402 for [FactoryEventHandler<T>]). The refinement is complete.

### Test Scenario Cross-Check

All 14 plan scenarios mapped to actual test methods:

| # | Plan Scenario | Test Method | File |
|---|---------------|-------------|------|
| 1 | Single event relay | `SingleEventRelay_ClientHandlerReceivesEvent` | FactoryEventRelayTests.cs |
| 2 | ServerOnly excludes relay | `ServerOnlyEvent_NotRelayedToClient` | FactoryEventRelayTests.cs |
| 3 | Multiple events in order | `MultipleEventsRelay_AllEventsReceivedInOrder` | FactoryEventRelayTests.cs |
| 4 | Nested operation events captured | Not explicit (follow-up item 3) — implicit via design | — |
| 5 | No events = null in response | `NoEvents_NoRelayedEvents` (implicit via follow-up 5) | FactoryEventRelayTests.cs |
| 6 | Client handler exception swallowed | `HandlerException_DoesNotPropagateToFactoryCaller` | FactoryEventRelayTests.cs |
| 7 | No handlers = silent drop | `NoRegisteredHandlers_EventSilentlyDropped`; also `DispatchRelayedEvents_NoDispatcherRegistered_SilentDrop` | FactoryEventRelayTests.cs, FactoryEventRelayDispatcherTests.cs |
| 8 | Multiple handlers per event | `MultipleHandlersSameEvent_AllHandlersInvoked`; `DispatchRelayedEvents_MultipleHandlers_AllInvoked` | both |
| 9 | Unregister stops delivery | `UnregisterStopsDelivery`; `DispatchRelayedEvents_UnregisteredHandler_NoDelivery` | both |
| 10 | Weak reference cleanup | `WeakReferenceCleanup_GarbageCollectedHandlerRemoved` (weak — see follow-up 2) | FactoryEventRelayTests.cs |
| 11 | ServerOnly + ContinueOnFail | `ServerOnlyCombinedWithContinueOnFail_NotRelayed` | FactoryEventRelayTests.cs |
| 12 | Logical mode no capture | Not explicit (follow-up item 3) — DI construction ensures it | — |
| 13 | Serialization round-trip | Covered transitively by single/multiple event tests (events carry properties); see `MultipleEventsRelay_AllEventsReceivedInOrder` asserts `Message` and `Sequence` fields | FactoryEventRelayTests.cs |
| 14 | Source-generated dispatch | Entire test suite exercises generated registrar; also NF0401/NF0402 diagnostic tests in `NF04xxFactoryEventHandlerTests.cs` | NF04xxFactoryEventHandlerTests.cs |

Design.Tests has additional relay tests:
- `Relay_EventRaisedInRemoteCreate_DispatchedToClientHandler`
- `Relay_ServerOnlyFlag_EventNotRelayedToClient`
- `Relay_Unregister_StopsDelivery`

Unit tests for FactoryEventCollector:
- `Collect_SingleEvent_ReturnsIt`
- `Collect_MultipleEvents_PreservesOrder`
- `GetCollectedEvents_NoEvents_ReturnsEmpty`

**Gaps (already tracked in plan's Follow-up Items):**
- Scenario 4 (nested operations) and Scenario 12 (Logical mode no capture) lack explicit integration tests. Both are listed in the plan's Follow-up Items (item 3) as non-blocking. DI construction prevents the collector from being resolved in Logical mode, so the behavior is guaranteed by design but unasserted.
- Scenario 10 (weak reference) test is weak (follow-up 2): only asserts subsequent operations succeed, not that the registry pruned the dead entry.
- Scenario 13 (complex nested records) is covered transitively but not by a dedicated complex-payload test.

These gaps are explicitly non-blocking per the plan's Follow-up Items section.

### Implementation vs Plan Match

- `RaiseOptions.ServerOnly = 4` — present in `src/RemoteFactory/RaiseOptions.cs`
- `RemoteResponseDto.RelayedEvents` — present in `src/RemoteFactory/RemoteResponse.cs`
- `NeatooTransportJsonContext` updated — verified via generated STJ files
- `IFactoryEventCollector`/`FactoryEventCollector` — present at `src/RemoteFactory/Internal/FactoryEventCollector.cs`
- `FactoryEventsDispatcher` — updated (optional collector injection)
- `HandleRemoteDelegateRequest` — attaches collected events
- `MakeRemoteDelegateRequest` — extracts and dispatches (with documented cast to concrete dispatcher, follow-up 4)
- `IFactoryEventRelay`/`FactoryEventRelayDispatcher` — present
- `FactoryEventRelayRegistry` — present (keyed by handler class type per refined design)
- `RelayedFactoryEvent` — present
- Generator: `RelayHandlerRenderer`, `RelayHandlerModel`, pipeline in `FactoryGenerator.cs` — present
- Diagnostics NF0401/NF0402 (renumbered from NF0501/NF0502 in plan) — present in `DiagnosticDescriptors.cs` with tests in `NF04xxFactoryEventHandlerTests.cs`
- Design project example at `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs`
- Both test standins (IntegrationTests `ClientServerContainers` and `DesignClientServerContainers`) updated per Concern 2 recommendation

### Breaking Changes

None confirmed. All changes are additive. Existing tests all pass (no regressions).

### Follow-up Items

Plan Follow-up Items section documents 6 non-blocking concerns from Step 5 developer review. None of them block verification. Focus during Step 6A verification was on builds/tests/cross-check, not re-review of those concerns.

## Files Examined (Pre-Implementation — Step 3)

(Preserved from earlier run)
- `src/RemoteFactory/FactoryEventsDispatcher.cs`
- `src/RemoteFactory/HandleRemoteDelegateRequest.cs`
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs`
- `src/RemoteFactory/RemoteResponse.cs`
- `src/RemoteFactory/Internal/NeatooTransportJsonContext.cs`
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequestHttpCall.cs`
- `src/RemoteFactory/AddRemoteFactoryServices.cs`
- `src/RemoteFactory/FactoryEventHandlerRegistry.cs`
- `src/RemoteFactory/FactoryEventBase.cs`
- `src/RemoteFactory/RaiseOptions.cs`
- `src/RemoteFactory/RemoteFactoryEvents.cs`
- `src/RemoteFactory/Internal/ServiceAssemblies.cs`
- `src/Generator/Builder/FactoryModelBuilder.cs`
- `src/Generator/Renderer/StaticFactoryRenderer.cs`
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs`
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs`

## Mistakes to Avoid

(none)

## User Corrections

(none)
