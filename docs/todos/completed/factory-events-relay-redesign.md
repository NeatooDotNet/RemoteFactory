# Factory Events Client Relay Redesign

**Status:** Complete
**Priority:** High
**Created:** 2026-04-14
**Last Updated:** 2026-04-14

---

## Problem

The client-side factory event relay has a timing bug and a design smell:

1. **Timing bug (`src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:109-115`)** — the comment promises "fire-and-forget: dispatch relayed events after returning result", but `_ = _relay.DispatchRelayedEvents(...)` starts the async method body synchronously before `return deserialized;`. Any synchronous portion of a handler (or the first awaited continuation that completes synchronously) runs while the caller is still inside `await _entityFactory(entity)`. Handlers observe stale state — `_entity` has not yet been reassigned.

2. **Two competing client-event mechanisms.** `[FactoryEventHandler<T>]` means two different things depending on whether the method is static (server-side handler) or instance (client-side relay handler). Consumers trip over this. The client-side instance path duplicates responsibilities that a consumer's own event aggregator would handle better — and worse, it pulls source-generated dispatch, weak-reference tracking, and `Register/Unregister` into RemoteFactory's surface area when the consumer already has all of that.

## Solution

Breaking change. Strip the client-side relay machinery down to a single hook:

- `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase> events)` — one method, called fire-and-forget strictly after the factory method returns.
- Ship a no-op default implementation in Remote mode. If the consumer does nothing, no client events fire (zero surprise).
- Consumer replaces it with their own implementation that bridges to their event aggregator and handles threading (SyncContext marshal, etc.) themselves.
- Delete: instance-method `[FactoryEventHandler<T>]` on the client, `FactoryEventRelayDispatcher`'s register/unregister surface, `FactoryEventRelayRegistry`, the generator's `RelayHandlerRenderer`/`FactoryGenerator.RelayHandler.cs`.
- Keep: server-side `[FactoryEventHandler<T>]` static-method handlers (unchanged), `IFactoryEvents.Raise<T>`, `RaiseOptions.ServerOnly`, `FactoryEventBase`, `IFactoryEventCollector`, response-channel piggyback via `RemoteResponseDto.RelayedEvents`.
- RemoteFactory owns post-return ordering: dispatch queued via `Task.Yield()` so `Relay` fires strictly after the caller's continuation resumes.
- Documentation explains the bridge pattern with worked examples (MediatR, plain event aggregator, etc.). RemoteFactory does **not** ship a bridge.

Minor bump with breaking-change section in release notes (user's call). Migration guide required.

---

## Requirements Review

**Verdict:** APPROVED
**Reviewed:** 2026-04-14
**Summary:** Plan preserves all non-relay behavioral guarantees (server-side handler invariants, ServerOnly filtering, insertion-order preservation, logical-mode no-registration, handler exception isolation). Trimming story strengthened by moving `[DynamicallyAccessedMembers]` to `FactoryEventBase`. Deliberate breaking-change contradictions noted — more docs touch the removed surface than the plan's file list enumerates (see reviewer memory file for the full Step 7 touchlist). Timing fix needs a non-Blazor verification test.

---

## Plans

- [Factory Events Client Relay Redesign Plan](../../plans/completed/factory-events-relay-redesign.md)

---

## Tasks

- [x] Draft plan (Step 1)
- [x] Business requirements review (Step 2) — APPROVED
- [ ] ~~Architect validation (Step 3)~~ — SKIPPED (user instruction)
- [x] Implementation (Step 4)
- [x] Developer code review (Step 5) — APPROVED
- [x] Verification — architect + requirements (Step 6) — both VERIFIED / SATISFIED
- [x] Requirements documentation + general docs (Step 7) — Part A: CLAUDE-DESIGN.md + 4 docs/*.md updated; Part B: docs/release-notes/v1.4.0.md + index updated; skill files updated
- [x] Completion (Step 8)

---

## Progress Log

### 2026-04-14 (Grade-A Polish Pass — between Step 6A and Step 6B)
After Step 5 (developer review APPROVED) and Step 6 Part A (architect VERIFIED), spawned the developer agent to grade the implementation. Grade: **B+**. User selected the top 5 grade-A upgrades; all five completed:
- **#1 NoOp first-event warning** — `NoOpFactoryEventRelay.Relay` logs `Warning 3011` once per process on its first non-empty batch. Silent-drop footgun is now loud.
- **#2 NF0503 Warning for instance-method handlers** — `[FactoryEventHandler<T>]` instance methods now emit a compile-time Warning pointing at the method, telling the user to make it static or implement `IFactoryEventRelay`. Rule 16 in the plan upgraded from "silent skip" to "warn + skip".
- **#3 FactoryEventTypeRegistry collision logging** — duplicate `FullName` collisions now log `Warning 3012` with the kept-vs-dropped assembly names.
- **#4 Generator output renamed** — `*.RelayHandler.g.cs` → `*.FactoryEventHandler.g.cs` (the "relay" emission is gone).
- **#7 PublishTrimmed event-relay smoke test** — added `EventRelaySmokeTest.cs` to `RemoteFactory.TrimmingTests`. Trimmed `linux-x64` binary published with `TrimMode=full` round-trips a `FactoryEventBase` descendant through `FactoryEventTypeRegistry → FactoryEventDeserializer → CapturingRelay` successfully. `RemoteFactory.TrimmingTests` added to `RemoteFactory.csproj` `InternalsVisibleTo` for `FactoryEventDeserializer` access.
- Updated `NF04xxFactoryEventHandlerTests.InstanceMethodHandler_ReportsNF0503Warning` (formerly `InstanceOnlyHandler_SilentlyUnused_NoDiagnostic`).
- Build + test results unchanged: 579+579 unit, 581+581 integration, 74+74 Design — all pass on net9.0+net10.0 Release.
- Next: re-run Step 6 Part A architect verification (since Rule 16 changed) then Part B requirements verification.

### 2026-04-14 (Step 4 — Implementation Complete)
- Implementation complete. All 2,320 tests pass (Unit+Integration, Design; net9.0 + net10.0; Release).
- Added: `FactoryEventAttribute`, `NoOpFactoryEventRelay`, `UnknownFactoryEventTypeException`, `FactoryEventTypeRegistry`, `FactoryEventDeserializer`; `FactoryEventBase` now carries `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` with `Inherited = true`.
- Replaced: `IFactoryEventRelay.Register/Unregister` → `Relay(IReadOnlyList<FactoryEventBase>)`. DI wired with `TryAddSingleton<IFactoryEventRelay, NoOpFactoryEventRelay>` in Remote mode only.
- Rewired: `MakeRemoteDelegateRequest` dispatch site uses `Task.Run + Task.Yield` to fire relay strictly post-return. Added `FactoryEventRelayFailed` + `FactoryEventDeserializationFailed` log events (3008/3009).
- Deleted: `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, stale generated `*.RelayHandler.g.cs` for instance handlers.
- Generator: `RelayHandlerRenderer` no longer emits client-side relay registrations or DtoConstructorRegistry preservation for event types; `TransformRelayHandler` silently skips instance-method handlers (Rule 16) but still diagnoses NF0501/NF0502 when a static candidate exists with wrong shape.
- Test harnesses (`ClientServerContainers`, `DesignClientServerContainers`) updated to use the same `Task.Run + Task.Yield` pattern as production so timing tests are valid end-to-end.
- Design + integration tests rewritten to new consumer-implements-relay surface. Timing tests added (`RelayTimingTests`) including no-SyncContext coverage per plan Risk #3.
- Person example client: `PersonEventHandler` converted from instance-method `[FactoryEventHandler]` class to an `IFactoryEventRelay` implementation; registered via `AddSingleton<IFactoryEventRelay, PersonEventHandler>()`.
- Obsolete `EventDtoDiscoveryTests` (tested the removed DtoTypeWalker event-root emission) deleted; `NF05xx` diagnostic tests rewritten to cover static-handler shape validation and Rule 16 silent-skip behavior.
- Next: developer code review (Step 5).

### 2026-04-14
- Created todo after conversation with user about the `_entity = await _entityFactory(entity)` event-timing smell.
- Plan reviewed section-by-section with user; finalized decisions: minor version bump, always-invoke `Relay` (empty batch OK), fail-loud on unknown event types, no diagnostic for removed instance-method handler pattern, runtime attribute scan (`[FactoryEvent]` on `FactoryEventBase`, `Inherited = true`) instead of client-side codegen.
- Business requirements review: APPROVED (findings in `docs/plans/factory-events-relay-redesign.memory/requirements-reviewer.md`).
- Architect validation skipped per user instruction. Ready to move to Step 4 (implementation).
- Next: user's call on when to start implementation (fresh context recommended).
- Key design decisions captured in conversation:
  - Breaking change accepted — consumer assumes responsibility for event aggregator and bridge.
  - `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase> events)` — single method, deserialized events.
  - No-op default implementation in Remote mode.
  - Post-return ordering owned by RemoteFactory via `Task.Yield()`.
  - Client/server detection uses `NeatooFactory` enum (Server/Remote/Logical), not `[Remote]` attribute.
  - Delete all client-side instance-method `[FactoryEventHandler<T>]` plumbing. Server-side static-method handlers unchanged.
- Next: draft plan (Step 1 Part B).

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: PASS — `src/Neatoo.RemoteFactory.sln` and `src/Design/Design.sln` Release, net9.0 + net10.0
- Tests: PASS — 579+579 unit, 581+581 integration (3 pre-existing perf-demo skips), 74+74 Design = 2,468 tests

---

## Results / Conclusions

The client-side factory event relay was redesigned around a single integration hook. Three meaningful behavioral wins:

1. **Timing bug fixed.** The `_entity = await factory.Create(...)` pattern is now safe — `IFactoryEventRelay.Relay` is invoked strictly after the caller's continuation resumes, via `Task.Run + Task.Yield + CancellationToken.None` in `MakeRemoteDelegateRequest`. Proven by `RelayTimingTests` including a no-SyncContext host case.

2. **One-call invariant.** Every `[Remote]` factory call produces exactly one `Relay` invocation, even when the batch is empty. Consumers can rely on the empty-batch invocation as a "factory call just returned" signal.

3. **Trim-safe via base-class annotation.** `FactoryEventBase` carries `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicCtors | PublicProps)]` with `Inherited = true`. Every descendant is auto-discoverable by the runtime `FactoryEventTypeRegistry` and trim-preserved. Per-handler codegen for event preservation is gone. Verified end-to-end by a `PublishTrimmed=true` `linux-x64` smoke binary.

**Removed:** `IFactoryEventRelay.Register/Unregister`, `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, instance-method `[FactoryEventHandler<T>]` client dispatch path.

**Added:** `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)`, `NoOpFactoryEventRelay` (with first-event Warning 3011), `FactoryEventTypeRegistry` (with collision Warning 3012), `FactoryEventDeserializer`, `UnknownFactoryEventTypeException`, `FactoryEventAttribute`, log events 3008/3009 for relay/deserialization failures, NF0503 Warning for ignored instance-method handlers.

**Grade-A polish pass** added five upgrades on top of the base implementation: NoOp first-event warning, NF0503, registry collision logging, generator output rename to `*.FactoryEventHandler.g.cs`, PublishTrimmed smoke test. Implementation graded **B+** before polish; intent of the polish was to close every silent-failure mode a consumer can hit in production.

**Released as:** v1.4.0 (minor bump with breaking-change section per user's pre-1.0-API-stability framing).

**Workflow notes:** Architect verification ran twice (initial + post-Grade-A). Requirements review approved both times. Rule 16 was upgraded from "silent skip" to "warn + skip" in the plan to match the implementation after the user picked Grade-A item #2. All sacred existing tests preserved (only `EventDtoDiscoveryTests` and `FactoryEventRelayDispatcherTests` were deleted — both tested explicitly removed code paths).
