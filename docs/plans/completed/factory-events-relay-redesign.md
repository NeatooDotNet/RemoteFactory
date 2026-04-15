# Factory Events Client Relay Redesign

**Date:** 2026-04-14
**Related Todo:** [Factory Events Client Relay Redesign](../../todos/completed/factory-events-relay-redesign.md)
**Status:** Complete
**Last Updated:** 2026-04-14

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Replace the current client-side factory event relay (source-generated instance-method `[FactoryEventHandler<T>]` + `FactoryEventRelayRegistry` + `Register/Unregister` weak-ref tracking) with a single-method integration hook `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase> events)`. Ship a no-op default in Remote mode. RemoteFactory guarantees `Relay` is invoked **fire-and-forget, strictly after the factory method returns to the caller**. Consumers implement `IFactoryEventRelay` to bridge events to their own event aggregator; RemoteFactory does not provide a bridge.

Also fixes the timing bug where `_ = _relay.DispatchRelayedEvents(...)` executed the async method's synchronous prologue (and potentially whole handlers) before `return deserialized;` ran — causing handlers to observe pre-assignment caller state.

This is a **breaking change**, released as a **minor bump** (user's call — pre-1.0 product, breaking changes are permitted in minor versions). Server-side static-method `[FactoryEventHandler<T>]` handlers are unaffected.

---

## Skills

- `skills/knockoff/SKILL.md` — For stubbing `IFactoryEventRelay` / `IFactoryEvents` in unit tests

Sources of truth for this work: `src/Design/CLAUDE-DESIGN.md`, `src/Design/Design.Domain/`, and the source code.

---

## Business Rules (Testable Assertions)

### IFactoryEventRelay Contract

1. WHEN RemoteFactory is configured with `NeatooFactory.Remote` AND no `IFactoryEventRelay` implementation is registered by the consumer, THEN a built-in `NoOpFactoryEventRelay` is registered as singleton and `Relay(events)` is a no-op (no handlers fire, no exceptions) — NEW

2. WHEN RemoteFactory is configured with `NeatooFactory.Server`, THEN `IFactoryEventRelay` is NOT registered (server never relays to itself) — NEW

3. WHEN RemoteFactory is configured with `NeatooFactory.Logical`, THEN `IFactoryEventRelay` is NOT registered (no serialization boundary, events dispatch via `FactoryEventsDispatcher` directly in the caller's scope) — NEW

4. WHEN a consumer registers their own `IFactoryEventRelay` implementation in DI BEFORE calling `AddNeatooRemoteFactory`, THEN the no-op default is NOT registered (TryAdd semantics) — NEW

5. WHEN a consumer registers their own `IFactoryEventRelay` implementation AFTER `AddNeatooRemoteFactory`, THEN their registration replaces the default via standard DI override — NEW

### Post-Return Ordering Guarantee

6. WHEN a client-side factory method returns a result to its caller (e.g., `_entity = await factory.Create(...)`), THEN `IFactoryEventRelay.Relay` has NOT yet been invoked at the point the caller's continuation begins executing — NEW

7. WHEN the caller's continuation after `await factory.Create(...)` completes (or yields back to the scheduler), THEN `IFactoryEventRelay.Relay` is invoked with all events captured during the factory call — NEW

8. WHEN a `[Remote]` factory call completes AND deserialization of the relayed batch succeeds, THEN `IFactoryEventRelay.Relay` is invoked exactly once (regardless of event count, including empty). WHEN deserialization fails (e.g., unknown event type — see rule 15), THEN `Relay` is NOT invoked for that call and the failure is logged — NEW

9. WHEN `IFactoryEventRelay.Relay` throws an exception, THEN the exception does NOT propagate to the original factory caller (fire-and-forget isolation) — NEW

### Event Batch Contents

10. WHEN events are captured during a `[Remote]` factory call, THEN events in the batch are ordered by the server-side order in which `Raise<T>` was called — NEW

11. WHEN an event is raised with `RaiseOptions.ServerOnly`, THEN it is NOT included in the batch passed to `IFactoryEventRelay.Relay` on the client — NEW (existing behavior, reconfirmed)

12. WHEN a `[Remote]` factory call raises zero events, THEN `IFactoryEventRelay.Relay` IS invoked with an empty `IReadOnlyList<FactoryEventBase>` (every `[Remote]` call produces exactly one `Relay` invocation — consumers can rely on this for batch-end bookkeeping) — NEW

13. WHEN events are passed to `IFactoryEventRelay.Relay`, THEN each event is a fully deserialized `FactoryEventBase` subclass instance (not a raw JSON payload) — NEW

### Serialization / Type Resolution

14. WHEN an event type inherits `FactoryEventBase` (which carries `[FactoryEvent]` and `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` with `Inherited = true`), THEN its constructors and properties are preserved through IL trimming and it can be serialized/deserialized across the wire — NEW (extends commit 84ba1a8 preservation, now enforced by the base class itself rather than by client-side codegen)

15. WHEN a concrete event type used on the wire is not known to the client at deserialization time, THEN RemoteFactory throws (the batch fails fast; the exception is caught by the relay dispatch isolation and logged, so it does not propagate to the factory caller) — NEW

### Removal of Prior Client-Side Machinery

16. WHEN a consumer declares `[FactoryEventHandler<T>]` on a class with an instance-method handler (the former client-relay shape), THEN the generator emits NO code for that method AND emits diagnostic NF0503 (Warning) pointing at the instance method, telling the user to make the method `static` (server handler) or implement `IFactoryEventRelay` (client receiver) — NEW (upgraded from "silent skip" per developer-grade Grade-A item #2; warning makes the migration footgun loud at compile time without breaking the build)

17. WHEN a consumer declares `[FactoryEventHandler<T>]` on a **static** method (server-side), THEN behavior is unchanged (dispatched via `FactoryEventHandlerRegistry` during server-side factory execution) — existing

18. WHEN the public surface is reviewed post-change, THEN `IFactoryEventRelay.Register/Unregister`, `FactoryEventRelayRegistry`, and `FactoryEventRelayDispatcher` are no longer present — NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Default no-op in Remote mode | `AddNeatooRemoteFactory(NeatooFactory.Remote)`, no consumer relay | 1 | `sp.GetRequiredService<IFactoryEventRelay>()` returns `NoOpFactoryEventRelay` |
| 2 | Server mode skips relay | `AddNeatooRemoteFactory(NeatooFactory.Server)` | 2 | `sp.GetService<IFactoryEventRelay>()` returns `null` |
| 3 | Logical mode skips relay | `AddNeatooRemoteFactory(NeatooFactory.Logical)` | 3 | `sp.GetService<IFactoryEventRelay>()` returns `null` |
| 4 | Consumer registers custom relay | `services.AddSingleton<IFactoryEventRelay, MyRelay>(); AddNeatooRemoteFactory(Remote);` | 4 | `MyRelay` is resolved, not `NoOpFactoryEventRelay` |
| 5 | Consumer overrides after Add | `AddNeatooRemoteFactory(Remote); services.AddSingleton<IFactoryEventRelay, MyRelay>();` | 5 | `MyRelay` is resolved |
| 6 | Post-return assignment sees new value | `_entity = await factory.Create(...)` where `Create` raises event; relay observes `_entity` reference | 6, 7 | Relay's handler, invoked post-return, sees the newly-assigned `_entity` (proven via test that sets a field in the continuation and asserts the relay sees it) |
| 7 | Single invocation per call | One `[Remote]` call raising 3 events | 8 | `Relay` called exactly once with 3-event batch |
| 7b | Single invocation — zero events | One `[Remote]` call raising 0 events | 8, 12 | `Relay` called exactly once with empty batch |
| 8 | Relay exception isolation | Relay implementation throws in `Relay(events)` | 9 | Factory caller observes successful return; exception is logged, not propagated |
| 9 | Event ordering preserved | Factory raises EventA, EventB, EventC in that order | 10 | Batch arrives to client-side `Relay` as `[EventA, EventB, EventC]` |
| 10 | ServerOnly excluded | Factory raises EventX (default) and EventY (`ServerOnly`) | 11 | Client `Relay` receives `[EventX]` only |
| 11 | Empty batch delivered | `[Remote]` call raises no events | 12 | `Relay` invoked once with `events.Count == 0` |
| 12 | Deserialized instances | Server raises `OrderCheckoutCompleted(42, 19.99m)` | 13 | Client `Relay` receives `IReadOnlyList<FactoryEventBase>` where `events[0] is OrderCheckoutCompleted && ((OrderCheckoutCompleted)events[0]).OrderId == 42` |
| 13 | Trimming preservation | Blazor WASM published with `PublishTrimmed=true`; factory raises `OrderCheckoutCompleted` | 14 | Event deserializes correctly on the trimmed client |
| 14 | Unknown event type aborts batch | Client receives 3-event batch; middle event's `TypeFullName` is not known to the client | 8, 15 | `FactoryEventDeserializer.Deserialize` throws `UnknownFactoryEventTypeException`; caught by relay dispatch isolation; logged with all three type names; `Relay` is NOT invoked for this call; factory caller's `await` unaffected |
| 15 | Instance-method ignored with NF0503 warning | Project declares `[FactoryEventHandler<X>] public class Foo { public Task Handle(X evt) => ... }` (instance method only) | 16 | Compilation succeeds; generator emits nothing for `Foo`; NF0503 Warning is reported on the instance method; handler never invoked at runtime |
| 16 | Static-method handler unchanged | Server project declares `[FactoryEventHandler<X>] public static class Bar { public static Task HandleAsync(X evt) => ... }` | 17 | Behavior unchanged — dispatched via `FactoryEventHandlerRegistry` |
| 17 | Removed surface | Compile consumer code referencing `IFactoryEventRelay.Register(handler)` | 18 | Compilation fails (method no longer exists) |

---

## Approach

**Phase 1 — Add the new model alongside the old.**
1. Add `FactoryEventAttribute` (`AttributeTargets.Class, Inherited = true`) and apply it to `FactoryEventBase`. Apply `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` with `Inherited = true` to `FactoryEventBase` so every descendant's constructors and properties survive IL trimming.
2. Introduce `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase> events)` on the existing interface.
3. Introduce `NoOpFactoryEventRelay : IFactoryEventRelay` as a sealed internal default implementation.
4. Introduce `FactoryEventTypeRegistry` — internal, runtime-populated (**no generator involvement**). Lazily scans `AppDomain.CurrentDomain.GetAssemblies()` on first use for non-abstract types whose `GetCustomAttribute<FactoryEventAttribute>(inherit: true)` is non-null (i.e., every `FactoryEventBase` descendant). Caches the `TypeFullName → Type` map. Thread-safe.
5. Introduce `FactoryEventDeserializer` — uses `FactoryEventTypeRegistry` to resolve the `Type` from the wire payload's `TypeFullName`, then deserializes via `INeatooJsonSerializer` (reflection-based STJ, works with trimming given the preservation on `FactoryEventBase`). Throws `UnknownFactoryEventTypeException` when no match.
6. Rewire `MakeRemoteDelegateRequest.ForDelegate` — schedule `relay.Relay(events)` via `Task.Run(async () => { await Task.Yield(); try { ... } catch ... })`. Discarded task with `Task.Yield()` ensures `Relay` runs strictly after the caller's continuation resumes. Deserialization happens inside the task so `UnknownFactoryEventTypeException` is caught by the same isolation block.

**Phase 2 — Remove the old model.**
7. Delete `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, generator's `RelayHandlerRenderer` / `RelayHandlerModel` / `FactoryGenerator.RelayHandler.cs`, `Register/Unregister` on `IFactoryEventRelay`.
8. Update `DesignClientServerContainers` + test `ClientServerContainers` to invoke `relay.Relay(events)` instead of `DispatchRelayedEvents`.
9. Rewrite `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` and the corresponding Design test to demonstrate the new pattern.

**Phase 3 — Documentation.**
9. Update `skills/RemoteFactory/references/factory-events.md` and the skill's main SKILL.md references.
10. Update `docs/` Jekyll pages and add a migration guide to release notes for the major version bump.
11. Add a worked bridge example (simple in-memory aggregator) to the reference app (`src/docs/reference-app/`) and embed via mdsnippets into the docs.

### Event Type Discovery — Decided

**Runtime attribute scan, no client codegen.** `[FactoryEventAttribute]` is applied once to `FactoryEventBase` with `Inherited = true`. At runtime, `FactoryEventTypeRegistry` lazily scans loaded assemblies for non-abstract types where `GetCustomAttribute<FactoryEventAttribute>(inherit: true) != null` — every descendant of `FactoryEventBase` matches via inherited-attribute semantics. Trimming preservation via `[DynamicallyAccessedMembers]` on `FactoryEventBase` (also inherited) keeps descendant metadata alive.

Consequences:
- No client-side code generation for events (the generator's event-type emission path is deleted in full, not replaced).
- No consumer opt-in per event type — inheriting `FactoryEventBase` is sufficient.
- Attribute scan cost is paid once at first use, cached.
- Works under `PublishTrimmed=true` because the base class carries the preservation annotation.

---

## Domain Model Behavioral Design

N/A — this is framework infrastructure, not a domain model. No computed properties, visibility flags, reactive rules, or validation rules apply.

---

## Design

### New / Changed Public Surface

```csharp
namespace Neatoo.RemoteFactory;

public interface IFactoryEventRelay
{
    // Replaces Register/Unregister. Consumer implements this to receive relayed events.
    // Called fire-and-forget by RemoteFactory strictly after the factory method returns.
    // Exceptions are caught and logged; they do not propagate to the factory caller.
    // The consumer owns any threading / SyncContext marshaling inside this method.
    Task Relay(IReadOnlyList<FactoryEventBase> events);
}

// Internal default registered when NeatooFactory.Remote is selected and the consumer
// has not registered their own implementation.
internal sealed class NoOpFactoryEventRelay : IFactoryEventRelay
{
    public Task Relay(IReadOnlyList<FactoryEventBase> events) => Task.CompletedTask;
}
```

### Removed Public Surface

- `IFactoryEventRelay.Register(object handler)` / `Unregister(object handler)` — gone.
- `FactoryEventRelayRegistry` (public static class) — replaced by internal `FactoryEventTypeRegistry`.
- Instance-method `[FactoryEventHandler<T>]` — compile-time error NF0503.

### Dispatch Site (the timing fix)

File: `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs`

```csharp
// OLD (BUG — runs synchronously before return):
if (_relay != null && result.RelayedEvents != null)
{
    _ = _relay.DispatchRelayedEvents(result.RelayedEvents, this.NeatooJsonSerializer);
}
return deserialized;

// NEW:
if (_relay != null)
{
    // One [Remote] call = one Relay call (may be empty batch) — unless deserialization
    // fails, in which case the batch is aborted and the failure is logged.
    // Discarded Task with Task.Yield() ensures Relay runs strictly after the caller's
    // continuation resumes. Exceptions are isolated from the factory caller.
    var rawEvents = result.RelayedEvents;
    _ = Task.Run(async () =>
    {
        await Task.Yield();
        try
        {
            IReadOnlyList<FactoryEventBase> events = rawEvents is { Count: > 0 }
                ? FactoryEventDeserializer.Deserialize(rawEvents, this.NeatooJsonSerializer)  // throws on unknown type — fail loud
                : Array.Empty<FactoryEventBase>();

            await _relay.Relay(events).ConfigureAwait(false);
        }
        catch (UnknownFactoryEventTypeException ex)
        {
            logger.FactoryEventDeserializationFailed(correlationId, ex);
        }
        catch (Exception ex)
        {
            logger.FactoryEventRelayFailed(correlationId, ex);
        }
    });
}

return deserialized;
```

Rationale for `Task.Run + Task.Yield`: `Task.Run` moves execution off the calling thread (the `return` completes immediately); `Task.Yield` inside that task forces the remaining work to queue behind whatever the sync context is currently running. On Blazor, the caller's continuation after `await` runs on the UI thread's current message pump pass; our `Relay` call is posted as a separate pass.

### Data Flow (unchanged shape, new invocation timing)

1. Client calls `[Remote]` factory method → `IMakeRemoteDelegateRequest.ForDelegate`.
2. HTTP POST to server.
3. Server executes factory method; `FactoryEventsDispatcher` captures events via `IFactoryEventCollector` unless `ServerOnly`.
4. Server returns `RemoteResponseDto { Result, RelayedEvents }`.
5. Client deserializes `Result` and returns it to caller.
6. *(NEW ordering)* Client schedules `relay.Relay(deserializedEvents)` on a separate continuation.
7. Caller's `_entity = await factory.Create(...)` resumes with the new `_entity`.
8. Sync context processes the scheduled `Relay` call.

### File Changes

**Modified:**
- `src/RemoteFactory/IFactoryEventRelay.cs` — replace surface
- `src/RemoteFactory/FactoryEventBase.cs` — add `[FactoryEvent]` attribute application + `[DynamicallyAccessedMembers]` (both `Inherited = true`)
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs` — new dispatch timing
- `src/RemoteFactory/AddRemoteFactoryServices.cs` — register `NoOpFactoryEventRelay` for Remote mode only
- `src/Generator/FactoryGenerator.cs` — remove all event-handler client-side emission paths (server-side static-method handler emission unchanged)
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` — rewrite to new pattern
- `src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs` — rewrite
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` — use `Relay(events)`
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` — use `Relay(events)`
- `src/RemoteFactory/NeatooLoggerCategories.cs` (and logger) — add `FactoryEventRelayFailed` + `FactoryEventDeserializationFailed` log events

**Added:**
- `src/RemoteFactory/FactoryEventAttribute.cs` (public; `AttributeTargets.Class, Inherited = true`)
- `src/RemoteFactory/NoOpFactoryEventRelay.cs`
- `src/RemoteFactory/UnknownFactoryEventTypeException.cs` (public — consumers may inspect it via log context)
- `src/RemoteFactory/Internal/FactoryEventDeserializer.cs`
- `src/RemoteFactory/Internal/FactoryEventTypeRegistry.cs` (runtime reflection scan, not generator output)
- `src/Tests/RemoteFactory.UnitTests/Internal/FactoryEventTypeRegistryTests.cs` — scan correctness (discovers descendants, skips abstracts, caches)
- `src/Tests/RemoteFactory.UnitTests/Internal/FactoryEventDeserializerTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventRelay/RelayTimingTests.cs` — the critical `_entity = await factory(...)` ordering test

**Deleted:**
- `src/RemoteFactory/FactoryEventRelayDispatcher.cs`
- `src/RemoteFactory/FactoryEventRelayRegistry.cs`
- `src/Generator/FactoryGenerator.RelayHandler.cs`
- `src/Generator/Model/RelayHandlerModel.cs`
- `src/Generator/Renderer/RelayHandlerRenderer.cs`
- `src/Tests/RemoteFactory.UnitTests/Internal/FactoryEventRelayDispatcherTests.cs`

---

## Implementation Steps

1. **Add `FactoryEventAttribute`** (public, `AttributeTargets.Class, Inherited = true`). Apply to `FactoryEventBase`. Also apply `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` to `FactoryEventBase` (inherited). Unit test that a descendant reports the attribute via `GetCustomAttribute<FactoryEventAttribute>(inherit: true)`.
2. **Add `FactoryEventTypeRegistry`** — internal, runtime, lazy-initialized via `AppDomain.CurrentDomain.GetAssemblies()` scan. Thread-safe caching of `TypeFullName → Type`. Unit tests: discovers descendants, skips abstracts, handles dynamically-loaded assemblies (re-scan on miss), caches results.
3. **Add `FactoryEventDeserializer` + `UnknownFactoryEventTypeException`.** Deserialize via `INeatooJsonSerializer.Deserialize(json, type)`. Throw on unknown type (fail loud per rule 15). Unit tests.
4. **Replace `IFactoryEventRelay`** surface with `Relay(IReadOnlyList<FactoryEventBase>)`. Add `NoOpFactoryEventRelay`. Register conditionally in `AddRemoteFactoryServices` based on `NeatooFactory` mode (Remote → register default; Server/Logical → skip).
5. **Rewire `MakeRemoteDelegateRequest.ForDelegate`** with the post-return `Task.Run + Task.Yield` pattern. Deserialization inside the task so `UnknownFactoryEventTypeException` is caught by the same isolation. Add structured logging for relay failures and deserialization failures.
6. **Write the timing test** (`RelayTimingTests`) that proves `_entity = await factory(...)` assignment happens before `Relay` fires. This test must fail against the current code and pass against the new code.
7. **Delete** `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, generator relay-handler emission (`RelayHandlerModel`, `RelayHandlerRenderer`, `FactoryGenerator.RelayHandler.cs`), old unit tests. Confirm no other generator code paths reference the removed models.
8. **Rewrite** `DesignClientServerContainers` and `ClientServerContainers` (integration test harness) to invoke `relay.Relay(events)` with the deserialized batch.
9. **Rewrite** `Design.Domain.FactoryPatterns.FactoryEventRelayPattern` and its Design test to show the new consumer-implements-relay pattern (include an inline simple aggregator as the demo bridge).
10. **Update** existing integration tests in `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventRelay/` to the new surface.
11. **Build + test** across net9.0 and net10.0 — confirm zero regressions outside the deprecated surface. Verify Blazor WASM publish-trimmed scenario using the Design.Client.Blazor project (or add a smoke test). **Also write a non-Blazor timing test** (console / plain ThreadPool context, no SyncContext) to confirm `Task.Run + Task.Yield` ordering holds when no sync context is present — flagged by requirements review.

Documentation (Step 7, not implementation):
- Update `skills/RemoteFactory/references/factory-events.md`.
- Update `src/Design/CLAUDE-DESIGN.md` — specifically sections around lines 200-234, 256-258, 957, 1008, 1012 (per reviewer findings; full list in reviewer's memory file).
- Update `docs/factory-events.md`, `docs/events.md`, `docs/interfaces-reference.md`, `docs/attributes-reference.md`, `docs/trimming.md` (per reviewer findings).
- Add migration guide in `docs/release-notes/vX.Y.Z.md` (minor bump with breaking-change section).
- Add worked bridge example in reference app; embed via mdsnippets.

---

## Acceptance Criteria

- [ ] All 17 test scenarios in the Business Rules table have corresponding passing test methods.
- [ ] `RelayTimingTests` proves post-return ordering: fails against current code, passes against new code.
- [ ] `NoOpFactoryEventRelay` is resolved in Remote mode when no consumer registration exists.
- [ ] `IFactoryEventRelay` is NOT resolved in Server or Logical mode.
- [ ] Instance-method `[FactoryEventHandler<T>]` compiles without error and produces no generated output (confirmed by a test that reflects generated sources).
- [ ] Server-side static-method `[FactoryEventHandler<T>]` behavior is byte-for-byte unchanged (existing tests pass without modification).
- [ ] Blazor WASM trimming preservation verified (event records survive `PublishTrimmed=true`).
- [ ] `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, `IFactoryEventRelay.Register/Unregister` no longer exist in the public or internal surface.
- [ ] Design project pattern (`FactoryEventRelayPattern.cs`) demonstrates the new bridge pattern and its test passes.
- [ ] All builds pass on net9.0 and net10.0 in Release configuration.

---

## Dependencies

- Prior plan: `factory-events-mediator.md` (IFactoryEvents mediator pattern) — **already Complete**. Server-side handler machinery stays; this plan modifies only the client-relay path.
- Commit `84ba1a8` (event record trimming preservation) — this plan supersedes that preservation path. The `[DynamicallyAccessedMembers]` annotation moves from the `[FactoryEventHandler<T>]` generator path to `FactoryEventBase` itself (inherited to all descendants). Net: stronger guarantee, less generated code.
- Release notes system + version bump procedure in CLAUDE.md (minor bump — user's call).

---

## Risks / Considerations

1. **Breaking change magnitude.** Every consumer currently using client-side instance-method `[FactoryEventHandler<T>]` will find their handlers silently inert after upgrade (no compile error, no runtime error — just no events delivered). Mitigation: clear migration guide + release notes call-out. The lack of a warning is an intentional trade-off (user's call) — no new diagnostic to maintain.
2. **Runtime assembly scan scope.** `FactoryEventTypeRegistry` scans `AppDomain.CurrentDomain.GetAssemblies()` on first use. Assemblies loaded LATER (plugin scenarios, dynamic `Assembly.Load`) won't be in the initial scan. Mitigation: on `TypeFullName` miss, rescan once before throwing `UnknownFactoryEventTypeException`. Document that the domain model library must be loaded before the first `[Remote]` factory call that returns events — in practice, Blazor WASM loads everything eagerly so this is not a problem there.
3. **`Task.Run + Task.Yield` behavior in non-Blazor hosts.** In a console app without a sync context, `Task.Yield` falls back to the thread pool. `Relay` still runs after the caller's `await` resumes because the caller's continuation runs inline or on the pool, and our discarded task was posted via `Task.Run`. Needs verification test outside Blazor.
4. **Event batch ordering.** Relies on the server-side `FactoryEventCollector` preserving insertion order (currently uses `List<>` — fine). Flag if that ever changes.
5. **Empty batch policy** (rule 12). Decision: always invoke `Relay` — one `[Remote]` call = one `Relay` call. Consumers can rely on this for batch-end bookkeeping / UI signals ("a factory call just returned"). Minor cost: one extra no-op invocation on calls that raise no events.
6. **Logger availability.** The dispatch site needs an `ILogger` to log `Relay` exceptions. `MakeRemoteDelegateRequest` already has `logger` in scope — check.
7. **Consumer threading mistakes.** If the consumer's `Relay` implementation does blocking work (synchronous `.Wait()`, blocks on UI thread), they'll feel it. Document clearly in the bridge pattern example: "post to your aggregator, don't do the handler work inline."
8. **Test harness parity.** Both `DesignClientServerContainers` and `ClientServerContainers` simulate the client/server boundary without HTTP. They must be updated together so test-layer timing matches production behavior.
