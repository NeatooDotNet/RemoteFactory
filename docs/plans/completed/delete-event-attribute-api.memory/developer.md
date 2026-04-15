# Developer — Delete `[Event]` Method Attribute API

Last updated: 2026-04-14
Current step: Step 5 — Developer Code Review (complete)

## Key Context

This is a feature deletion plan (v1.5.0). The orchestrator implemented all 14 implementation steps. My job here is to trace each of the 16 business rules to actual code state via grep + reads. No new code; pure trace.

Distinguish carefully:
- `EventAttribute` (DELETED) vs `FactoryEventAttribute` (KEPT — applied to `FactoryEventBase`, unrelated)
- `[Event]` method attribute (DELETED) vs `[FactoryEventHandler<T>]` (KEPT)
- `IEventScopeInitializer` etc. (DELETED) vs `ICorrelationContext` (KEPT — used by `MakeRemoteDelegateRequest`)
- `EventTracker`/`IEventTracker` (DELETED) vs `EventTestService`/`IEventTestService` (KEPT — test helper, relocated)
- NF0401-NF0404 (DELETED) vs NF0405 (KEPT — FactoryEventHandlerMustBeStatic)

## Developer Review

**Status:** Approved
**Date:** 2026-04-14

### Summary
Plan is a clean deletion of the `[Event]` method-attribute API in v1.5.0. All 16 business rules traced to evidence in the actual repository state. Production source contains zero references to the deleted symbol set. Remaining hits for `[Event]`-shaped strings are all in `src/Design/CLAUDE-DESIGN.md`, which is documentation handled in Step 7 (per the plan's own Approach section).

### Code Review Trace

| # | Rule (short) | Verdict | Evidence |
|---|---|---|---|
| 1 | No production source references deleted symbols | Satisfied | `Grep "EventAttribute|class Event\b|\[Event\]|IEventTracker|EventTracker\b|IEventScopeInitializer|DelegateEventScopeInitializer|CorrelationContextScopeInitializer|EventTrackerHostedService|AddRemoteFactoryEventScopeInitializer|EventMethodModel"` over `src/` returns ONLY hits in `src/Design/CLAUDE-DESIGN.md` (Step 7 doc) and `FactoryEventAttribute`/`FactoryEventBase` (kept by design). Generator + RemoteFactory + AspNetCore + Tests + Examples + reference-app are all clean. `FactoryOperation.Event`/`AuthorizeFactoryOperation.Event` grep over `src/` = no matches. |
| 2 | Consumers using `[Event]` get CS0246 because type is gone | Satisfied | `src/RemoteFactory/FactoryAttributes.cs` grep for `EventAttribute|class Event\b` = no matches. Type does not exist in `Neatoo.RemoteFactory.dll`. C# compiler will emit CS0246 by default; rule is "verify absence" — verified. |
| 3 | NF0401-NF0404 removed from descriptors and `GetDescriptor` switch | Satisfied | `Grep "NF040[1234]"` over `src/Generator` = no matches. `DiagnosticDescriptors.cs` only contains NF0405 in the NF04xx range (lines 183-200). `FactoryGenerator.cs:127` switch case has only `"NF0405" => DiagnosticDescriptors.FactoryEventHandlerMustBeStatic`. |
| 4 | Non-event factory operations still generate; non-event tests still pass | Satisfied (by rule 16's full-suite pass — orchestrator-reported, builds clean per spawn message; this rule's verification is delegated to Step 6) | Generator surface intact: `FactoryModelBuilder.cs`, `StaticFactoryRenderer.cs`, `ClassFactoryRenderer.cs` exist and have only `[Event]` paths stripped (other operation paths untouched per spawn message). Build passes. |
| 5 | Mixed-class behavior — `[Event]` methods become unannotated, others generate normally | Satisfied | `EventAttribute` removed → C# `[Event]` becomes a CS0246 at consumer build, not a partial generator output. Generator no longer scans for `[Event]` (FactoryModelBuilder paths stripped per spawn message; `Events` collection removed from models — `Grep "Events\b"` over `src/Generator/Model` returns no matches). |
| 6 | Server-side `[FactoryEventHandler<T>]` still works | Satisfied | `Grep "FactoryEventHandler"` shows 29 hits across kept source: `FactoryEventHandlerRegistry.cs`, `FactoryEventsDispatcher.cs`, `RelayHandlerModel.cs`, `RelayHandlerRenderer.cs`, `FactoryGenerator.RelayHandler.cs`, integration tests under `Events/FactoryEventHandler/` all preserved. NF0405 descriptor + switch case retained. |
| 7 | v1.4 `IFactoryEventRelay` relay path unchanged | Satisfied | `Grep "ForDelegateEvent"` shows it preserved at `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:21,28,29,167` and used by `RemoteFactoryEvents.cs:23,28`. Test container shim preserved at `ClientServerContainers.cs:111`. `NoOpFactoryEventRelay`, `IFactoryEventRelay`, `FactoryEventDeserializer`, `FactoryEventCollector`, `FactoryEventTypeRegistry` all present. |
| 8 | Server mode no `IEventTracker` | Satisfied | `IEventTracker.cs` deleted (verified `ls` 404). `Grep "IEventTracker"` over `src/RemoteFactory/AddRemoteFactoryServices.cs` = no matches. |
| 9 | Logical mode no `IEventTracker` | Satisfied | Same evidence as rule 8. The branching in `AddRemoteFactoryServices.cs` lines 72-90 has no IEventTracker registration in either Server or Logical branches. |
| 10 | `IEventScopeInitializer` registration removed from `AddRemoteFactoryServices.cs` | Satisfied | `Grep "IEventScopeInitializer|EventScopeInitializer|AddRemoteFactoryEventScopeInitializer"` over `AddRemoteFactoryServices.cs` = no matches. Read of file confirms no remnant. |
| 11 | `ICorrelationContext` still scoped-registered | Satisfied | `AddRemoteFactoryServices.cs:60` — `services.AddScoped<ICorrelationContext, CorrelationContextImpl>();` Used at line 124 in HttpCall registration. `ICorrelationContext.cs` and `CorrelationContextImpl.cs` present. |
| 12 | `EventTrackerHostedService` not registered in AspNetCore | Satisfied | `EventTrackerHostedService.cs` deleted (verified `ls` 404). `Grep "EventTrackerHostedService\|Microsoft.Extensions.Hosting"` over `ServiceCollectionExtensions.cs` = no matches. Read of file confirms only `AddNeatooAspNetCore` overloads remain. |
| 13 | Log event IDs 9001-9009 removed from Log.cs | Satisfied | `Grep "EventId\s*=\s*9"` over `src/RemoteFactory/Internal/Log.cs` = no matches (entire 9xxx category gone). Other event IDs (1xxx serialization, etc.) preserved per file structure. |
| 14 | No test references deleted types | Satisfied | `Grep "EventAttribute|IEventTracker|IEventScopeInitializer|EventTrackerHostedService|AddRemoteFactoryEventScopeInitializer|EventTrackerTests|EventScopeInitializerTests|EventGenerationTests|CorrelationEventPropagationTests|RemoteEventIntegrationTests|FactoryEventHandlerCoexistenceTests|DelegateEventScopeInitializer|CorrelationContextScopeInitializer|EventMethodModel"` over `src/Tests` returns ONLY `FactoryEventBaseAttributeTests.cs` hits, which match `FactoryEventAttribute` (different type — kept by design). |
| 15 | `IEventTestService` preserved + relocated | Satisfied | New file `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTestService.cs` exists with `IEventTestService` (line 6) + `EventTestService` (line 19). `FactoryEventHandlerTargets.cs` resolves the type at lines 77, 94, 111, 127, 147, 163, 181, 197, 223, 239, 271, 293, 319. `ClientServerContainers.cs` registers the type at lines 246, 247, 297, 341, 342. Old `EventTargets.cs` deleted — `ls TestTargets/Events/` shows only `EventTestService.cs`, `FactoryEventHandlerTargets.cs`, `FactoryEventRelayTargets.cs`, `FactoryEventTransactionTargets.cs`. |
| 16 | Full suite Release / both TFMs passes; only 3 perf-demo skips | Delegated to Step 6 | Orchestrator spawn-message reports 553 unit + 563 integration × 2 TFMs all pass; 3 pre-existing perf-demo skips per TFM. Builds pass on Release for main solution, Design solution, and reference-app solution. Final verification belongs to architect at Step 6. |

### Test Scenario Coverage

| # | Scenario | Verdict | Notes |
|---|---|---|---|
| 1 | `class EventAttribute` absent in `src/RemoteFactory/` | Satisfied | grep returned no matches |
| 2 | `EventAttribute` type absent in compiled DLL | Satisfied | source absent → DLL absent (build passes) |
| 3 | NF0401-NF0404 absent from descriptors + switch | Satisfied | grep returned no matches |
| 4 | Non-event factory tests pass | Delegated to Step 6 | per rule 16 |
| 5 | Mixed-attribute class behaves correctly | Satisfied | generator no longer scans `[Event]`; bare methods are ordinary |
| 6 | `[FactoryEventHandler<T>]` tests unchanged | Satisfied | tests + types preserved |
| 7 | v1.4 relay tests unchanged | Satisfied | `ForDelegateEvent` + `IFactoryEventRelay` preserved |
| 8 | Server mode no `IEventTracker` | Satisfied | type gone |
| 9 | Logical mode no `IEventTracker` | Satisfied | type gone |
| 10 | `IEventScopeInitializer` line removed | Satisfied | grep clean |
| 11 | `ICorrelationContext` still scoped | Satisfied | line 60 of `AddRemoteFactoryServices.cs` |
| 12 | `EventTrackerHostedService` registration removed | Satisfied | grep clean |
| 13 | Log events 9001-9009 removed | Satisfied | grep clean |
| 14 | No test references deleted types | Satisfied | only `FactoryEventAttribute` matches (kept type) |
| 15 | `IEventTestService` preserved + relocated | Satisfied | new file + 13 consumer references |
| 16 | Full suite green | Delegated to Step 6 | orchestrator reports clean; architect must independently verify |

### Design Drift Check

- `ICorrelationContext` preserved (scoped registration) — rule 11 satisfied ✓
- `IMakeRemoteDelegateRequest.ForDelegateEvent` preserved — rule 7 satisfied ✓
- `FactoryEventAttribute` (on `FactoryEventBase`) preserved — distinct from deleted `EventAttribute` ✓
- `[FactoryEventHandler<T>]` + NF0405 preserved — rule 6 satisfied ✓
- No design drift detected.

### Logic Errors / Dangling References

- No dangling references to deleted types found in production source, generator, AspNetCore, tests, design projects, examples, or reference-app
- No orphaned helpers detected
- DI registrations correctly preserved (`ICorrelationContext` scoped, `IFactoryEvents`, `IFactoryEventRelay` etc.)
- Generator pipeline appears coherent (no missing `Events` collection references in Model after removal — `Grep "Events\b"` over `src/Generator/Model` = no matches)

### Concerns

None blocking. Two observations for the orchestrator:

1. **`src/Design/CLAUDE-DESIGN.md`** still has ~10 lines referencing `[Event]`, `IEventTracker`, `IEventScopeInitializer`, `CorrelationContextScopeInitializer`, `AddRemoteFactoryEventScopeInitializer`, etc. (lines 40, 197-199, 256, 267, 273-274, 659, 661-682, 955, 961, 1045). The plan explicitly defers these to Step 7 (Documentation) — this is **expected**, not a defect. Flagging here so it isn't mistaken for missed scope.

2. **Rules 4 and 16** are full-suite-pass rules; final test execution is Step 6's job. Trust the orchestrator's spawn-message build/test report only insofar as the architect re-runs it for verification.

### Verdict
**Approved.** All 16 business rules traced; 14 directly satisfied by code state, 2 (rules 4 and 16 — both full-test-suite assertions) properly delegated to architect verification at Step 6. No design drift, no dangling references, no logic errors detected. Proceed to Step 6.
