# IFactoryEvents Mediator Pattern

**Status:** In Progress
**Priority:** High
**Created:** 2026-04-09
**Last Updated:** 2026-04-09

---

## Problem

The current `[Event]` system uses a one-delegate-per-handler pattern where callers inject a specific delegate type (e.g., `ExampleEvents.OnOrderPlacedEvent`) and invoke it directly. This has several limitations:

1. **Tight coupling** — The caller must know the specific delegate type for each handler
2. **Single handler per event** — No publish-subscribe (one delegate = one handler)
3. **Fire-and-forget only** — No option to await handler completion
4. **No event objects** — Events are parameter lists, not first-class types

## Solution

Introduce a **MediatR-style mediator pattern** using **source generation (no reflection)**:

- **`IFactoryEvents`** — Single injection point for publishing events
- **`Raise<T>(T event, RaiseOptions options)`** — Publisher creates an event object and calls Raise
- **Multiple handlers** per event type via `[EventHandler]` attribute
- **Caller-chosen semantics** via `RaiseOptions` flags enum:
  - Default: fire-and-forget (await server acknowledgment only)
  - `AwaitRemote`: await full handler completion including remote handlers
  - `ContinueOnFail`: continue executing remaining handlers if one fails (default: fail on first error)
- **Source-generated dispatch** — no reflection, compile-time handler discovery
- **Cross-assembly support** — each assembly registers its handlers via generated registrars
- **Coexists** with current `[Event]` pattern (no breaking change)

### Design Decisions (from conversation)

1. **Remote await** — Default fire-and-forget; optional `AwaitRemote` flag keeps HTTP connection open
2. **Handler type** — Static methods only (on `[Factory]` static classes)
3. **Execution** — Parallel (all handlers run concurrently)
4. **Error handling** — Default fail (first error propagates); optional `ContinueOnFail` flag
5. **Coexistence** — New pattern alongside existing `[Event]`, no breaking change
6. **Cross-assembly** — Yes, via DI registrar pattern (same as existing factory registrations)
7. **Polymorphic dispatch** — No. Strict type matching only.
8. **Options API** — `[Flags] enum RaiseOptions` for combining options

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-04-09
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Existing [Event] semantics** -- `AllPatterns.cs`, `CLAUDE-DESIGN.md`, `docs/events.md`: Fire-and-forget delegates with `Event` suffix, `CancellationToken` required (NF0404), isolated DI scopes, `IEventTracker` monitoring. Plan coexists without conflict (Business Rules 20-21).
2. **Static factory method signatures** -- `CLAUDE-DESIGN.md` Anti-Pattern 3: Private with underscore prefix. Plan's handler examples comply.
3. **[Remote] boundary rules** -- `CLAUDE-DESIGN.md` Critical Rule 1, Anti-Pattern 8: `[Remote]` requires `internal` on class factories but works with `private static` on static factories (existing pattern). Plan's `[Remote, EventHandler]` on `private static` is consistent.
4. **[Factory] attribute scope** -- `FactoryAttributes.cs`: Targets Class/Interface. Plan does not alter `[Factory]` semantics.
5. **DI registration pattern** -- `AddRemoteFactoryServices.cs`, `NeatooFactoryRegistrarAttribute`: Assembly attribute + `FactoryServiceRegistrar` method. Plan proposes parallel registrar mechanism (Gap -- needs architect clarification).
6. **Event delegate naming** -- `CLAUDE-DESIGN.md` Critical Rule 6: `Event` suffix on generated delegates. No collision with `[EventHandler]` attribute name.
7. **Serialization** -- `CLAUDE-DESIGN.md` Anti-Patterns 4, 5, 9: Event objects must serialize across boundary. Gap -- plan does not specify ordinal vs STJ serialization for `IFactoryEvent` types.
8. **Scope isolation and correlation** -- `docs/events.md`, `EventTargets.cs`: Existing pattern for isolated scopes with correlation propagation. Plan carries forward (Business Rules 16-17).
9. **Design Debt table** -- No entry for mediator/pub-sub events. Not a deferred feature.

### Gaps

1. **Event object serialization strategy** -- `IFactoryEvent` types lack `[Factory]`, so no ordinal serialization. Architect must specify STJ vs ordinal and constraints on event object types.
2. **Cross-assembly registration mechanism** -- Plan proposes `FactoryEventsRegistrar_{Assembly}` but does not specify integration with existing `NeatooFactoryRegistrarAttribute` pattern.
3. **IEventTracker integration** -- Plan does not specify whether fire-and-forget handlers use existing `IEventTracker`.
4. **AwaitRemote HTTP endpoint** -- Not specified whether new endpoint or reuse of `/api/neatoo`.
5. **Generator Collect() performance** -- Cross-type gather phase is new for this generator; caching impact not assessed.
6. **Multi-targeting** -- Plan does not mention net9.0/net10.0 compatibility.

### Contradictions

None that would warrant a VETO. Two low-severity concerns:

1. **`EventHandlerAttribute` naming** -- `System.EventHandler` is a well-known .NET type. Could cause `using` conflicts in user code. Consider `FactoryEventHandlerAttribute`.
2. **Asymmetry with [Event]** -- `[Event]` works on instance and static methods; `[EventHandler]` restricted to static only. Not a rule violation, but an API design inconsistency to document.

### Recommendations for Architect

1. Clarify event object serialization (ordinal vs STJ, constraints)
2. Clarify cross-assembly registration mechanism (reuse or parallel)
3. Consider `EventHandlerAttribute` naming to avoid `System.EventHandler` confusion
4. Document why `[EventHandler]` is static-only while `[Event]` supports both
5. Specify `IEventTracker` integration for fire-and-forget handlers
6. Assess generator `Collect()` caching performance
7. Verify multi-targeting compatibility

---

## Plans

- [IFactoryEvents Mediator Pattern Design](../plans/factory-events-mediator.md)

---

## Tasks

- [ ] Draft plan with full design (Step 1B)
- [ ] Business requirements review (Step 2)
- [ ] Architect validation (Step 3)
- [ ] Implementation (Step 4)
- [ ] Developer code review (Step 5)
- [ ] Verification (Step 6)
- [ ] Documentation (Step 7)
- [ ] Completion (Step 8)

---

## Progress Log

### 2026-04-09
- Created todo from design conversation
- User decided on all 7 design questions (see Solution section)
- User proposed `RaiseOptions` as a bitwise flags enum
- Architect feasibility analysis completed — rated High feasibility for most aspects
- Next: Draft complete plan

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

