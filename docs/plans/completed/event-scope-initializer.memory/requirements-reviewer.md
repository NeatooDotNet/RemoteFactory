# Requirements Reviewer -- Event Scope Initializer

Last updated: 2026-04-03
Current step: Mode 2 Post-Implementation Verification complete

## Key Context

- Pre-design review (Mode 1) completed 2026-04-03: APPROVED with one behavioral concern (correlation timing)
- Post-implementation verification (Mode 2) completed 2026-04-03: REQUIREMENTS SATISFIED
- The plan was implemented as designed — all business rules respected
- The timing behavioral change (BR-ESI-014) was accepted by the user and the test was updated accordingly
- The `[GENERATOR BEHAVIOR]` comment in `CorrelationExample.cs` still describes the old mechanism — flagged as a documentation step item (not a requirements violation)

## Mistakes to Avoid

- None yet

## User Corrections

- User accepted the correlation ID capture timing change (from synchronous value capture before Task.Run to reading from parent scope inside Task.Run)

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-04-03

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Event scope isolation is deliberate | domain-events-feature.md Section 3, 6b | Satisfied | IEventScopeInitializer generalizes the existing ICorrelationContext exception into an extensible mechanism. Scope isolation is preserved — initializers explicitly copy values, not share scope access. |
| 2 | IsServerRuntime guard on event registrations | CLAUDE-DESIGN.md lines 485-487 | Satisfied | Initializer resolution and invocation lives inside the existing `if (NeatooRuntime.IsServerRuntime)` block in both renderers. No new guard needed. |
| 3 | Conditional registration pattern (Remote mode exclusion) | AddRemoteFactoryServices.cs:73-76 | Satisfied | `CorrelationContextScopeInitializer` is registered inside the `if (remoteLocal != NeatooFactory.Remote)` block (line 78). Test `BuiltInInitializer_RegisteredInServerAndLogical_NotInRemote` verifies this. |
| 4 | Generated code changes affect all consumers | Implicit dependency | Satisfied | Both `StaticFactoryRenderer.cs` and `ClassFactoryRenderer.cs` updated with identical initializer patterns. Minor version bump. |
| 5 | Event delegates get Event suffix | CLAUDE-DESIGN.md line 148 | Unaffected | Delegate naming unchanged. |
| 6 | CancellationToken required on [Event] methods | CLAUDE-DESIGN.md line 148; docs/events.md lines 100-104 | Unaffected | Method signature requirements unchanged. |
| 7 | ICorrelationContext is scoped | AddRemoteFactoryServices.cs:60-61 | Satisfied | CorrelationContextScopeInitializer correctly reads from parent scope instance and writes to child scope instance. Both are scoped. |
| 8 | Correlation ID propagation behavior tested | CorrelationEventPropagationTests.cs | Satisfied | All 7 correlation tests present. Timing test renamed to `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` to reflect new semantics. Asserts either original or changed value is valid. |
| 9 | Generator targets netstandard2.0 | CLAUDE.md Architecture Notes | Satisfied | Renderer changes are string manipulation only. `IEventScopeInitializer` and `GetServices<T>()` are in generated code (runs on net9.0/net10.0), not in the generator itself. |
| 10 | Design Debt table | CLAUDE-DESIGN.md lines 759-769 | Unaffected | No deliberately deferred feature is being implemented. |
| 11 | Anti-Patterns 1-9 | CLAUDE-DESIGN.md lines 162-424 | Unaffected | No anti-pattern introduced or violated. |
| 12 | [Remote] is only for aggregate root entry points | CLAUDE-DESIGN.md Critical Rule 1 | Unaffected | No visibility or [Remote] changes. |
| 13 | Event error handling semantics | BR-ESI-009 | Satisfied | Generated code wraps each individual initializer call in try/catch (not the entire loop). Exceptions are logged and swallowed. Event handler still executes. Test `FailingInitializer_DoesNotPreventEventHandler` verifies this. |
| 14 | Multiple initializers accumulate | BR-ESI-004, BR-ESI-008 | Satisfied | `AddTransient` (not `TryAdd`) allows multiple registrations. Test `MultipleRegistrations_AccumulateWithBuiltIn` verifies 1 built-in + 3 custom = 4. Test `MultipleInitializers_AllRun_InRegistrationOrder` verifies all execute. |

### Unintended Side Effects

1. **`[GENERATOR BEHAVIOR]` comment in CorrelationExample.cs is now inaccurate** — Lines 122-126 still say "the generator captures the correlation ID before Task.Run." The generator now resolves `IEventScopeInitializer` instances and invokes them inside `Task.Run`. This was identified in the pre-design review (Gap 5) and the plan marks it as a documentation step deliverable. Not a requirements violation, but should be updated in the documentation step.

2. **No other generated code patterns affected** — The change is contained to `RenderLocalEventRegistration` in both renderers. Remote event registration (`RenderRemoteEventRegistration`) is unchanged. Non-event factory operations are unchanged.

3. **No serialization contract changes** — The new interface (`IEventScopeInitializer`) is runtime-only (not serialized). No changes to what crosses the client/server wire.

4. **No factory interface changes** — No generated interface signatures are affected.

### Issues Found

None. All documented requirements are respected.
