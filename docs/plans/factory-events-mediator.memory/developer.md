# Developer -- Factory Events Mediator

Last updated: 2026-04-09
Current step: Code Review (Step 5)

## Key Context
- Reviewing implementation of IFactoryEvents mediator pattern against 26 business rules and 19 test scenarios
- All 2036 tests pass, 0 regressions
- Implementation spans core library, generator, design project, and integration tests

## Developer Review

**Status:** Concerns
**Date:** 2026-04-09

### Summary
The implementation delivers a working mediator pattern for factory events. Core dispatch, error handling, serialization, correlation propagation, coexistence, and cross-assembly registration all function correctly. However, there are gaps in diagnostic validation (missing private-static check, no unit tests for diagnostics) and the AwaitRemote option is deferred without documentation in the plan's acceptance criteria.

### Code Review Trace -- Business Rules

| # | Business Rule | Implementation Path | Verified? | Notes |
|---|--------------|---------------------|-----------|-------|
| 1 | FactoryEventBase is valid event type for Raise<T> | `FactoryEventBase.cs:8` abstract record; `IFactoryEvents.cs:17` constraint `where T : FactoryEventBase` | Yes | |
| 2 | Event objects must be serializable via STJ | `RemoteFactoryEvents.cs:24` passes event via `ForDelegateEvent`; integration tests in `FactoryEventHandlerSerializationTests.cs` verify round-trip | Yes | Records with primitives work; complex nested records tested too |
| 3 | [FactoryEventHandler] in [Factory] static class -> generator registers | `FactoryModelBuilder.cs:70-106` checks for `FactoryOperation.FactoryEventHandler`, calls `BuildFactoryEventHandlerMethod`; `StaticFactoryRenderer.cs:145-148` renders registration | Yes | |
| 4 | First non-[Service] parameter type determines event routing | `FactoryModelBuilder.cs:538` `eventParam = parameters.FirstOrDefault(p => !p.IsCancellationToken)`; `StaticFactoryRenderer.cs:347` `RegisterHandler<{eventTypeName}>` | Yes | |
| 5 | Multiple handlers across classes for same event -> all invoked | `FactoryEventHandlerRegistry.cs:21` appends to list per type; `FactoryEventsDispatcher.cs:34` iterates all handlers; Test: `FactoryEventHandlerLocalTests.Raise_MultipleHandlers_AllFire` | Yes | |
| 6 | Cross-assembly discovery via generated registrar | `StaticFactoryRenderer.cs:47` emits `[assembly: NeatooFactoryRegistrar]`; `AddRemoteFactoryServices.cs:149-156` invokes registrars from all assemblies | Yes | Registration happens during DI setup |
| 7 | Missing CancellationToken -> compile-time diagnostic | `FactoryModelBuilder.cs:72-85` emits NF0404 diagnostic | Yes | |
| 8 | Raise<T> -> all handlers execute in parallel | `FactoryEventsDispatcher.cs:33-37` creates List<Task>, adds all handler tasks, then `Task.WhenAll`; each handler uses `Task.Run` | Yes | |
| 9 | No handlers -> no-op | `FactoryEventsDispatcher.cs:30-31` returns `Task.CompletedTask` when handlers is null/empty; Test: `Raise_NoHandlers_CompletesWithoutError` | Yes | |
| 10 | Default (None) -> remote handlers fire-and-forget | `RemoteFactoryEvents.cs:21-24` sends via `ForDelegateEvent` (fire-and-forget pattern); client doesn't await handler completion | Partial | Client always sends the same way; AwaitRemote comment says "deferred to v2" |
| 11 | AwaitRemote -> caller awaits full handler completion | `RemoteFactoryEvents.cs:23` comment says "AwaitRemote is not yet supported -- deferred to v2" | NO | Not implemented for remote handlers |
| 12 | Non-[Remote] handlers execute locally regardless of RaiseOptions | `StaticFactoryRenderer.cs:345` registration is under `IsServerRuntime` guard; all current handlers run locally via `Task.Run` in generated code | Yes | No [Remote] handlers exist in tests |
| 13 | Handler throws, no ContinueOnFail -> exception propagates | `FactoryEventsDispatcher.cs:42` uses `Task.WhenAll` which propagates first exception; Test: `Raise_HandlerThrows_Default_ExceptionPropagates` | Yes | |
| 14 | Handler throws, ContinueOnFail -> remaining handlers continue | `FactoryEventsDispatcher.cs:39-40` uses `WhenAllContinueOnFail`; Test: `Raise_HandlerThrows_ContinueOnFail_OtherHandlersStillRun` | Yes | |
| 15 | Multiple failures + ContinueOnFail -> AggregateException | `FactoryEventsDispatcher.cs:62-63` throws `new AggregateException(exceptions)` when count > 1; single exception thrown directly (optimization) | Yes | Single-exception case is an optimization, not a violation |
| 16 | Handler runs in own DI scope | `StaticFactoryRenderer.cs:357` `using var scope = scopeFactory.CreateScope()` in generated code | Yes | |
| 17 | Correlation ID propagated to handler scope | `StaticFactoryRenderer.cs:352-362` captures parent correlation, sets in new scope; Test: `Raise_CorrelationId_PropagatedToHandlerScope` | Yes | |
| 18 | No polymorphic dispatch (Raise<TDerived> only matches TDerived) | `FactoryEventsDispatcher.cs:19` uses `typeof(T)` as key; `FactoryEventHandlerRegistry.cs:31` exact type match via dictionary lookup; Test: `Raise_DifferentEventTypes_StrictRouting` | Yes | |
| 19 | Dictionary<Type, Func<...>> dispatch, no reflection | `FactoryEventHandlerRegistry.cs:12` ConcurrentDictionary; `FactoryEventsDispatcher.cs:29` `GetHandlers(eventType)` | Yes | |
| 20 | [Event] and [FactoryEventHandler] coexist | `FactoryModelBuilder.cs:66-107` handles both in same loop; Test: `BothPatterns_WorkIndependently_InSameCompilation` | Yes | |
| 21 | [Event] methods continue with current delegate semantics | `FactoryModelBuilder.cs:66-68` builds EventMethod for [Event]; renderer generates delegates as before; Test: coexistence test invokes `OrderEventHandler.NotifyWarehouseEvent` | Yes | |
| 22 | [FactoryEventHandler] returning Task<T> -> diagnostic | `FactoryModelBuilder.cs:88-103` emits NF0401 when `IsTask && ReturnType != null && !ReturnType.Contains("Task")` | Yes | |
| 23 | [FactoryEventHandler] outside [Factory] class -> diagnostic | The generator only processes [Factory]-attributed types; methods outside [Factory] are never seen by the generator | Partial | NF0402 descriptor exists but is never emitted for [FactoryEventHandler] specifically -- only for [Event] methods. The generator silently ignores [FactoryEventHandler] on non-[Factory] classes. |
| 24 | [FactoryEventHandler] not private static -> diagnostic | No validation found | NO | No code checks method accessibility for [FactoryEventHandler]. A public or internal method would be accepted without diagnostic. |
| 25 | Fire-and-forget tasks tracked by IEventTracker | `StaticFactoryRenderer.cs:373` `tracker.Track(task)` in generated code; Test: `ServerRaise_FireAndForget_TaskTrackedByEventTracker` | Yes | |
| 26 | Generator extends FactoryServiceRegistrar for handler registration | `StaticFactoryRenderer.cs:109-152` renders `FactoryServiceRegistrar` method including event handler registrations; `StaticFactoryRenderer.cs:47` emits assembly attribute | Yes | |

### Test Scenario Mapping

| # | Scenario | Test Method | File | Covered? |
|---|----------|-------------|------|----------|
| 1 | Single handler, default options | `Raise_SingleHandlerType_HandlerFires` | `FactoryEventHandlerLocalTests.cs:21` | Yes |
| 2 | Multiple handlers, same event | `Raise_MultipleHandlers_AllFire` | `FactoryEventHandlerLocalTests.cs:37` | Yes |
| 3 | No handlers registered | `Raise_NoHandlers_CompletesWithoutError` | `FactoryEventHandlerLocalTests.cs:54` | Yes |
| 4 | Cross-assembly handler | N/A | N/A | Partial -- cross-assembly is tested implicitly via ClientServerContainers (client and server are same assembly). No explicit multi-assembly test. |
| 5 | Handler throws, default options | `Raise_HandlerThrows_Default_ExceptionPropagates` | `FactoryEventHandlerErrorTests.cs:21` | Yes |
| 6 | Handler throws, ContinueOnFail | `Raise_HandlerThrows_ContinueOnFail_OtherHandlersStillRun` + `Raise_HandlerThrows_ContinueOnFail_ExceptionStillThrown` | `FactoryEventHandlerErrorTests.cs:34,79` | Yes |
| 7 | Remote handler, default (fire-and-forget) | `ClientRaise_FireAndForget_ServerStillExecutes` | `FactoryEventHandlerClientServerTests.cs:73` | Partial -- tests client-to-server round-trip but no [Remote] attribute on handlers |
| 8 | Remote handler, AwaitRemote | None | N/A | NO -- AwaitRemote deferred to v2, no test |
| 9 | Mixed local + remote handlers | None | N/A | NO -- no [Remote, FactoryEventHandler] handlers exist in test targets |
| 10 | Derived event type | `Raise_DifferentEventTypes_StrictRouting` | `FactoryEventHandlerLocalTests.cs:64` | Partial -- tests different event types, but not derived/base relationship |
| 11 | Missing CancellationToken diagnostic | None | N/A | NO -- no unit test for NF0404 diagnostic |
| 12 | Coexistence | `BothPatterns_WorkIndependently_InSameCompilation` | `FactoryEventHandlerCoexistenceTests.cs:21` | Yes |
| 13 | Event serialization round-trip | `ClientRaise_MultipleProperties_SurviveSerialization` + `ClientRaise_NestedRecordEvent_SurvivesSerialization` | `FactoryEventHandlerClientServerTests.cs:37` + `FactoryEventHandlerSerializationTests.cs:26` | Yes |
| 14 | Correlation ID propagation | `Raise_CorrelationId_PropagatedToHandlerScope` + `Raise_MultipleHandlers_AllGetSameCorrelationId` | `FactoryEventHandlerCorrelationTests.cs:21,44` | Yes |
| 15 | Handler returns Task<T> diagnostic | None | N/A | NO -- no unit test for NF0401 diagnostic |
| 16 | Attribute outside [Factory] diagnostic | None | N/A | NO -- no unit test for NF0402 diagnostic, and generator silently ignores rather than emitting diagnostic |
| 17 | Handler not private static diagnostic | None | N/A | NO -- not implemented (no validation code exists) |
| 18 | Fire-and-forget tracking | `ServerRaise_FireAndForget_TaskTrackedByEventTracker` | `FactoryEventHandlerSerializationTests.cs:210` | Yes |
| 19 | Multiple event types in sequence | `ClientRaise_MultipleDifferentEventTypes_AllDispatchCorrectly` + `ServerRaise_MultipleDifferentEventTypes_AllDispatchCorrectly` | `FactoryEventHandlerSerializationTests.cs:77,107` | Yes |

### Gaps and Concerns

#### Critical (Must Address Before Verification)

1. **Business Rule 24 not implemented**: No diagnostic is emitted when `[FactoryEventHandler]` is placed on a non-`private static` method. The plan explicitly requires this validation. No code in FactoryModelBuilder or FactoryGenerator checks method accessibility for FactoryEventHandler methods.

2. **Business Rule 11 / Test Scenario 8 (AwaitRemote) not implemented**: `RemoteFactoryEvents.cs:23` explicitly states "AwaitRemote is not yet supported -- deferred to v2." The plan's business rule 11 and acceptance criteria say "RaiseOptions.AwaitRemote awaits full remote handler completion." This is a gap between plan and implementation. If this is intentionally deferred, the plan should be updated.

3. **Business Rule 23 / Test Scenario 16 (outside [Factory] diagnostic)**: The NF0402 descriptor exists but is only used for [Event] methods. [FactoryEventHandler] on a non-[Factory] class is silently ignored (the generator never processes non-[Factory] types). The plan requires a compile-time diagnostic.

4. **No unit tests for diagnostics**: Test scenarios 11, 15, 16, 17 require compile-time diagnostic tests (NF0401, NF0402, NF0404 for [FactoryEventHandler]). No unit tests exist in `RemoteFactory.UnitTests/Diagnostics/` for any NF04xx diagnostic, despite the existing pattern of NF01xx/NF02xx diagnostic tests.

#### Non-Critical (Would Improve Quality)

5. **Test Scenario 10 (Derived event type) partially covered**: The test verifies different types route independently, but does not test the specific base/derived relationship described in business rule 18 (where handlers for `BaseEvent` should NOT fire when `Raise<DerivedEvent>` is called).

6. **Test Scenarios 7, 9 (Remote handlers with [Remote, FactoryEventHandler])**: No test targets use `[Remote, FactoryEventHandler]` combination. The client-server tests only test the `RemoteFactoryEvents` path (client sends to server), not actual `[Remote]` handlers.

7. **Static registry thread safety**: `FactoryEventHandlerRegistry` uses `ConcurrentDictionary` + `lock(list)` for registration, but `GetHandlers` returns a snapshot via `ToArray()`. During test execution with shared static state, the `Clear()` method could cause issues between tests. Integration tests don't appear to call `Clear()`, which means handler registrations accumulate across tests. This works because each test uses unique GUIDs, but is fragile.

8. **WhenAllContinueOnFail single-exception optimization**: When exactly one handler fails with ContinueOnFail, the dispatcher throws the raw exception (line 61) rather than AggregateException. This is reasonable but differs from what a user might expect reading rule 15. The behavior is only specified for "multiple" failures so this is not a violation.

### Design Drift Assessment

The implementation generally follows the plan's approach. Key differences:

1. **Dispatcher is NOT generated**: The plan describes a generated `FactoryEventsDispatcher` class (Design section). The actual implementation uses a **runtime** `FactoryEventsDispatcher` class in `src/RemoteFactory/` that reads from a static `FactoryEventHandlerRegistry`. The generated code registers handler lambdas into this registry. This is a simpler and arguably better approach than generating the entire dispatcher.

2. **No separate Generator/EventHandler/ directory**: The plan expected `src/Generator/EventHandler/` as a new directory. Instead, the handler logic is integrated into the existing `FactoryModelBuilder` and `StaticFactoryRenderer`. This is fine -- it follows existing patterns.

3. **`RaiseUntyped` method added**: Not in the plan but needed for server-side dispatch of remote event requests where the concrete type is only known at runtime.

4. **`RemoteFactoryEvents` class added**: Not explicitly in the plan but necessary for client-side Remote mode behavior.

5. **AwaitRemote deferred**: The plan includes AwaitRemote as a v1 feature, but implementation explicitly defers it to v2.

### Verdict

**Concerns** -- The implementation works well for the core mediator pattern, but has missing validation (rule 24: private static check), deferred functionality documented as in-scope (rule 11: AwaitRemote), missing diagnostic for [FactoryEventHandler] outside [Factory] (rule 23), and no unit tests for compile-time diagnostics (scenarios 11, 15, 16, 17). These should be addressed or the plan should be updated to reflect the actual scope before proceeding to verification.

## Mistakes to Avoid
- None yet (first review)

## User Corrections
- None yet
