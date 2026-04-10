# Requirements Reviewer -- IFactoryEvents Mediator Pattern

Last updated: 2026-04-09
Current step: Post-implementation verification complete (Mode 2)

## Key Context

Verified the IFactoryEvents mediator pattern implementation against all 26 business rules (2 deferred). Traced through the actual source code: API types, generator pipeline, runtime dispatcher, diagnostics, Design project examples, integration tests, and unit tests.

Pre-design review gaps were all resolved during implementation:
- Naming: adopted `FactoryEventHandlerAttribute` (addresses System.EventHandler confusion)
- Serialization: STJ for v1 (ordinal deferred to v2)
- Cross-assembly registration: reuses existing `FactoryServiceRegistrar` + `NeatooFactoryRegistrarAttribute` pattern
- IEventTracker: integrated for fire-and-forget handlers
- Multi-targeting: no framework-specific code

## Mistakes to Avoid

None so far.

## User Corrections

None so far.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-04-09

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Classes/records inheriting FactoryEventBase are valid event types | Plan BR-1 | Satisfied | `FactoryEventBase.cs`: abstract record. All event types inherit from it. `Raise<T>` constrains `where T : FactoryEventBase`. |
| 2 | Event objects serializable via STJ | Plan BR-2 | Satisfied | `RemoteFactoryEvents.cs` serializes via `ForDelegateEvent`. `FactoryEventHandlerSerializationTests.ClientRaise_NestedRecordEvent_SurvivesSerialization` verifies complex nested records cross the wire. |
| 3 | [FactoryEventHandler] in [Factory] static class registered by generator | Plan BR-3 | Satisfied | `FactoryModelBuilder.cs:70-122`: detects `FactoryOperation.FactoryEventHandler`. `StaticFactoryRenderer.cs:331-377`: generates `FactoryEventHandlerRegistry.RegisterHandler<TEvent>()`. |
| 4 | First non-[Service] parameter type determines event routing | Plan BR-4 | Satisfied | `FactoryModelBuilder.cs:BuildFactoryEventHandlerMethod`: extracts first non-service, non-CT parameter as `EventTypeName`. |
| 5 | Multiple handlers across classes all invoked | Plan BR-5 | Satisfied | `FactoryEventHandlerLocalTests.Raise_MultipleHandlers_AllFire`: HandlerA + HandlerB both fire. |
| 6 | Cross-assembly handler discovery via generated registrar | Plan BR-6 | Satisfied | `StaticFactoryRenderer.cs:47`: emits `[assembly: NeatooFactoryRegistrar(typeof(...))]`. Same pattern as existing factories. |
| 7 | Missing CancellationToken emits NF0404 | Plan BR-7 | Satisfied | `DiagnosticDescriptors.cs:226-231`. `NF04xxFactoryEventHandlerTests.NF0404_*` verifies both positive and negative cases. |
| 8 | Raise dispatches all handlers in parallel | Plan BR-8 | Satisfied | `FactoryEventsDispatcher.cs:33-43`: builds `List<Task>`, calls `Task.WhenAll`. Each handler via `Task.Run`. |
| 9 | No handlers = no-op | Plan BR-9 | Satisfied | `FactoryEventsDispatcher.cs:29-30`: returns `Task.CompletedTask`. Test `Raise_NoHandlers_CompletesWithoutError` verifies. |
| 10 | Default fire-and-forget for remote handlers | Plan BR-10 | Satisfied | `RemoteFactoryEvents.cs:24`: sends via `ForDelegateEvent`. Server handlers run in background via `Task.Run`. |
| 11 | AwaitRemote | Plan BR-11 | DEFERRED | Explicitly deferred to v2. `RemoteFactoryEvents.cs:23` comment. Follow-up todo exists. |
| 12 | Non-[Remote] handlers execute locally | Plan BR-12 | Satisfied | All test handlers are non-[Remote]. `FactoryEventHandlerLocalTests` confirms local execution. |
| 13 | Handler throws, default: exception propagates | Plan BR-13 | Satisfied | `FactoryEventsDispatcher.cs:42`: `Task.WhenAll` propagates. `Raise_HandlerThrows_Default_ExceptionPropagates` verifies. |
| 14 | ContinueOnFail: remaining handlers continue | Plan BR-14 | Satisfied | `WhenAllContinueOnFail` awaits all tasks individually. `Raise_HandlerThrows_ContinueOnFail_OtherHandlersStillRun` verifies. |
| 15 | Multiple failures: AggregateException | Plan BR-15 | Satisfied | `FactoryEventsDispatcher.cs:62-63`: `AggregateException` for count>1. Single exception unwrapped (better UX, matches convention). |
| 16 | Handlers run in isolated DI scope | Plan BR-16 | Satisfied | Generated code: `scope = scopeFactory.CreateScope()`, services from `scope.ServiceProvider`. Same as existing [Event]. |
| 17 | Correlation ID propagated to handler scope | Plan BR-17 | Satisfied | Generated code captures `parentCorrelation?.CorrelationId`, sets on new scope. `Raise_CorrelationId_PropagatedToHandlerScope` verifies. |
| 18 | No polymorphic dispatch (strict type matching) | Plan BR-18 | Satisfied | `FactoryEventsDispatcher.cs:19`: `typeof(T)` exact match. `Raise_DifferentEventTypes_StrictRouting` verifies. |
| 19 | Dictionary dispatch, no reflection | Plan BR-19 | Satisfied | `FactoryEventHandlerRegistry.cs:12`: `ConcurrentDictionary<Type, List<Func<...>>>`. No reflection at dispatch. |
| 20 | [Event] and [FactoryEventHandler] coexist | Plan BR-20 | Satisfied | `BothPatterns_WorkIndependently_InSameCompilation`: both delegate and mediator patterns work. |
| 21 | [Event] continues unchanged | Plan BR-21 | Satisfied | Event delegate generation untouched. Existing test files (`RemoteEventIntegrationTests.cs`, `CorrelationEventPropagationTests.cs`) remain. |
| 22 | Task<T> return emits NF0401 | Plan BR-22 | Satisfied | `NF04xxFactoryEventHandlerTests.NF0401_FactoryEventHandler_ReturnsTaskT_ReportsDiagnostic` verifies. |
| 23 | [FactoryEventHandler] outside [Factory] | Plan BR-23 | DEFERRED | Generator only processes methods inside [Factory] classes. Silently ignored. |
| 24 | Not private static emits NF0405 | Plan BR-24 | Satisfied | `NF04xxFactoryEventHandlerTests.NF0405_FactoryEventHandler_PublicMethod_ReportsDiagnostic` verifies. |
| 25 | Fire-and-forget tasks tracked by IEventTracker | Plan BR-25 | Satisfied | Generated code: `tracker.Track(task)`. `ServerRaise_FireAndForget_TaskTrackedByEventTracker` verifies pending/complete. |
| 26 | Generator extends existing FactoryServiceRegistrar | Plan BR-26 | Satisfied | `StaticFactoryRenderer.cs:109-152`: handler registration in `FactoryServiceRegistrar`. Uses existing `NeatooFactoryRegistrar` assembly attribute. |

### Unintended Side Effects

1. **FactoryEventHandlerRegistry is static** -- Handler registrations persist across test runs in the same process. `Clear()` method exists. Since each test creates fresh containers and registration happens during DI setup, this is safe for normal test execution but could cause test pollution in edge cases. The `Clear()` method is available as a safety net.

2. **FactoryOperation enum extended** -- `FactoryEventHandler = AuthorizeFactoryOperation.FactoryEventHandler` (1024) added. Additive to `[Flags]` enum with no collision on existing values (Event=512). No existing behavior affected.

3. **WhenAllContinueOnFail single-exception behavior** -- When exactly one handler fails, throws original exception directly rather than wrapping in `AggregateException`. Better UX, matches `Task.WhenAll` convention. Not a violation.

4. **IFactoryEvents has RaiseUntyped method** -- Not in plan's API surface. Added for server-side dispatch when concrete type is only known at runtime (deserialized from client). Internal implementation detail.

5. **FactoryEventBase is abstract record, not abstract class** -- Plan specified `class`, implementation uses `record`. Records are better for events (immutable, structural equality). Improvement, not violation.

6. **No changes to existing [Event] code paths** -- Verified `StaticFactoryRenderer.cs` event delegate generation (lines 100-107, 260-329) remains identical. Existing event test files untouched.

### Issues Found

None. All 24 non-deferred business rules are satisfied. The 2 deferred rules (11: AwaitRemote, 23: diagnostic for outside [Factory]) are explicitly marked as deferred in the plan with documented follow-up.
