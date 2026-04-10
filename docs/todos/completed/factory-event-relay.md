# Factory Event Relay (Server-to-Client)

**Status:** Complete
**Priority:** High
**Created:** 2026-04-09
**Last Updated:** 2026-04-09

---

## Problem

Factory events raised on the server during factory operations (Create, Fetch, Save/Update) have no mechanism to notify the client. A treatment completion event raised in a server-side Update method has no way to tell a client-side header viewmodel to display "Therapy Complete."

The current `IFactoryEvents` mediator dispatches to server-side `[FactoryEventHandler]` methods only. The client can raise events TO the server (via `RemoteFactoryEvents`), but the server cannot relay events BACK to the client.

## Solution

**Factory Event Relay** — Events raised during server-side factory operations are captured in a request-scoped buffer, serialized alongside the operation response, and replayed on the client after the operation completes.

Key design decisions:
1. Events hitchhike on the existing HTTP response — no SignalR or separate push channel needed
2. Events are NOT raised on the client until the factory operation fully completes
3. Server operations NEVER wait for client-side event handling
4. `RaiseOptions.ServerOnly` flag opts events out of relay (default: events ARE relayed)
5. Client-side handlers implement `IFactoryEventHandler<T>` and register with `IFactoryEventRelay`
6. Source-generated dispatch table maps event types to typed delegates (no reflection)
7. ALL events raised during the entire server-side operation are captured (including from nested operations)

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-04-09
**Verdict:** APPROVED
**Summary:** No contradictions with documented patterns, anti-patterns, or design debt. The plan correctly mirrors existing patterns (FactoryEventHandlerRegistry for relay registry, IsServerRuntime guards, NeatooTransportJsonContext for trimming, NeatooFactory mode branching for DI). Five gaps identified for the architect to address: (1) type resolution for RelayedFactoryEvent.TypeFullName on client, (2) test determinism for fire-and-forget relay dispatch, (3) chained event capture behavior, (4) whether IFactoryEventHandler<T> requires [Factory] decoration, (5) both test standins need updating.

---

## Plans

- [Factory Event Relay Design](../plans/factory-event-relay.md)

---

## Tasks

- [x] Draft plan with full design (Step 1B)
- [x] Business requirements review (Step 2) — APPROVED
- [x] Architect validation (Step 3) — APPROVED WITH CONCERNS (all addressed)
- [x] Implementation (Step 4)
- [x] Developer code review (Step 5) — Concerns raised, documented as non-blocking follow-ups
- [x] Verification (Step 6) — VERIFIED (6A) + REQUIREMENTS SATISFIED (6B)
- [x] Documentation (Step 7) — CLAUDE-DESIGN.md + published docs + skill updated
- [x] Completion (Step 8)

---

## Progress Log

### 2026-04-09
- Created todo from design conversation
- User described the use case: treatment completion event raised on server, client header VM shows "Therapy Complete"
- Explored existing IFactoryEvents mediator, serialization pipeline, and client/server boundary
- User decisions: IFactoryEventHandler<T> interface on client, DI enforcement OK, source-generated dispatch, ALL events captured, ServerOnly opt-out, default relays
- Next: Draft complete plan

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (2 warnings unrelated to this feature)
- Tests: 506 UnitTests + 538 IntegrationTests + 47 Design.Tests, all passing, per TFM (net9.0, net10.0)

---

## Results / Conclusions

### What was delivered

1. **Server-side event capture** — `IFactoryEventCollector` buffers events raised via `IFactoryEvents.Raise()` during factory operations. `RaiseOptions.ServerOnly` excludes events from the relay.

2. **Transport** — `RemoteResponseDto.RelayedEvents` carries serialized events from server to client. `NeatooTransportJsonContext` updated for trimming safety.

3. **Client-side relay** — `IFactoryEventRelay` singleton dispatches relayed events to registered handlers using weak references. Fire-and-forget (factory result returned first).

4. **`[FactoryEventHandler<T>]` class attribute** — unified server-side mediator pattern (static methods) and client-side relay (instance methods) into one generator pipeline. Replaces the old method-level `[FactoryEventHandler]` attribute entirely. NF0501/NF0502 diagnostics for missing/ambiguous method matching.

5. **Person example** — demonstrates the feature end-to-end with MudBlazor snackbars showing "Inserted Person [1]", "Updated Person [1]", "Deleted Person [1]".

### Design evolution

The plan originally called for an `IFactoryEventHandler<T>` interface. During implementation, this was refined to a `[FactoryEventHandler<T>]` class attribute because:
- Attribute-on-class is cheap to find in Roslyn (vs. scanning all interface implementations)
- Unifying with the existing mediator pattern removes API surface duplication
- Static vs instance method differentiates server-side vs client-side handlers cleanly

### Non-blocking follow-ups (see plan's "Follow-up Items" section)

1. RelayHandlerRenderer lacks `IsServerRuntime` guard (intentional test trade-off)
2. Rule 18 (weak reference) test is weak — add stronger assertion
3. No explicit tests for Rules 4 (nested ops) and 7 (Logical mode)
4. `MakeRemoteDelegateRequest` concrete cast to `FactoryEventRelayDispatcher`
5. Rule 9 (null, not empty) only implicitly tested
