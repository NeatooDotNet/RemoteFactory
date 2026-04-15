# Requirements Reviewer — Factory Events Client Relay Redesign

Last updated: 2026-04-14
Current step: Step 2 — Pre-Design Review

## Key Context

This plan is an intentional breaking change to the client-side relay surface:

- Removes: instance-method `[FactoryEventHandler<T>]` plumbing, `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, `IFactoryEventRelay.Register/Unregister`, generator `RelayHandlerRenderer` / `RelayHandlerModel` / `FactoryGenerator.RelayHandler.cs`.
- Adds: single-method `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)`, `NoOpFactoryEventRelay`, `[FactoryEventAttribute]` + `[DynamicallyAccessedMembers]` on `FactoryEventBase` (both `Inherited = true`), runtime `FactoryEventTypeRegistry`, `FactoryEventDeserializer`, `UnknownFactoryEventTypeException`.
- Fixes timing bug in `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:109-115` using `Task.Run + Task.Yield`.
- Keeps server-side static-method `[FactoryEventHandler<T>]` byte-for-byte unchanged.

## Verdict: APPROVED

Date: 2026-04-14

The plan is consistent with every non-relay behavioral guarantee in the Design projects and docs. The contradictions it introduces against the current documented client-relay behavior are the *point* of the plan (explicit breaking-change intent, acknowledged in the plan itself and in CLAUDE.md's note that pre-1.0 minor bumps may break). No design-debt entry conflicts with this work.

## Compliance With Existing Requirements

| # | Requirement | Source | Status | Notes |
|---|-------------|--------|--------|-------|
| 1 | Server-side `[FactoryEventHandler<T>]` three invariants (shared scope, sequential, awaited) | `src/Design/CLAUDE-DESIGN.md:180-190`, `Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` | Preserved | Plan explicitly scopes changes to client-relay path; server-side dispatch via `FactoryEventHandlerRegistry` + `FactoryEventsDispatcher` untouched. Plan Acceptance Criterion: "byte-for-byte unchanged". |
| 2 | `RaiseOptions.ServerOnly` excludes event from client relay (server handlers still run) | `src/RemoteFactory/FactoryEventsDispatcher.cs:39-45`, `CLAUDE-DESIGN.md:227`, `FactoryEventRelayPattern.cs:129-137` | Preserved | Filtering lives in server-side `FactoryEventsDispatcher.DispatchToHandlers` before `_collector.Collect`. Plan does not modify this class. Rule 11 in plan reconfirms. |
| 3 | Events piggyback on `RemoteResponseDto.RelayedEvents` (no SignalR) | `CLAUDE-DESIGN.md:228`, existing `MakeRemoteDelegateRequest` | Preserved | Wire format unchanged; only the client-side consumption of `result.RelayedEvents` is rewritten. |
| 4 | Insertion-order preservation for batches | `src/RemoteFactory/Internal/FactoryEventCollector.cs:15-18` (`List<FactoryEventBase>`) | Preserved | Plan rule 10 depends on this; flagged as Risk #4 ("flag if that ever changes"). |
| 5 | Handler exceptions do NOT propagate to factory caller | `CLAUDE-DESIGN.md:231` | Preserved | Plan rule 9 explicitly reasserts and implements via try/catch inside the discarded `Task.Run`. |
| 6 | Factory result returned to caller before relay dispatch | `CLAUDE-DESIGN.md:230` | Strengthened | Plan rule 6 upgrades this from a fire-and-forget best-effort (currently racy) to a hard ordering guarantee. This is a **fix**, not a contradiction. |
| 7 | Logical mode registers neither collector nor relay | `CLAUDE-DESIGN.md:233` | Preserved | Plan rule 3 reconfirms. |
| 8 | Server mode does not register `IFactoryEventRelay` | `AddRemoteFactoryServices.cs:117-132` | Preserved | Plan rule 2 reconfirms. |
| 9 | `IFactoryEvents.Raise<T>` DynamicallyAccessedMembers annotation for trimming | `docs/trimming.md:325-329` | Preserved | Plan does not touch `IFactoryEvents.Raise` signature. |
| 10 | Event records survive IL trimming | Commit `84ba1a8`, `docs/trimming.md:282-307` | Strengthened | Plan supersedes the per-handler `PreserveType<T>` emission path with a single `[DynamicallyAccessedMembers(Inherited=true)]` on `FactoryEventBase`. Net: stronger guarantee (covers descendants even when no `[FactoryEventHandler<T>]` exists), less generated code. |
| 11 | `NeatooJsonSerializer` reflection-based deserialization continues to work | `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` | Preserved | New `FactoryEventDeserializer` delegates to `INeatooJsonSerializer` after type resolution. |
| 12 | `[FactoryEventHandler<T>]` class-level attribute stays a separate generator pipeline from `[Factory]` | `CLAUDE-DESIGN.md:568-589` (Anti-Pattern 11), `FactoryEventHandlerPattern.cs:42` | Preserved | Server-side pipeline unchanged. |
| 13 | Design-debt boundaries | `CLAUDE-DESIGN.md:967-974` | No conflict | None of the five design-debt entries address client-relay surface. |

## Contradictions (intended — breaking change)

These are places the plan deliberately contradicts currently-documented behavior. The plan acknowledges them as the scope of the breaking change, and CLAUDE.md permits minor-bump breaking changes pre-1.0 (user's call already captured in the todo).

1. **Client-side instance-method `[FactoryEventHandler<T>]`** — documented as a supported pattern in:
   - `src/Design/CLAUDE-DESIGN.md:200-234` (shows `OrderViewModel` + `_relay.Register(this)` / `Unregister`)
   - `src/Design/CLAUDE-DESIGN.md:256` (Decision Table: "How do I handle a factory event on the client?")
   - `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` (entire file)
   - `src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs` (all three tests call `Register` / `Unregister`)
   - `docs/factory-events.md:159-197`, `docs/interfaces-reference.md:668-680`, `docs/attributes-reference.md:210, 236-252`, `docs/trimming.md:74`, `docs/events.md:5`
   Plan rule 16 acknowledges: instance-method handlers compile but are silently unused. File Changes section covers rewriting `FactoryEventRelayPattern.cs`, `FactoryEventRelayTests.cs`, and the Design test harness. Step 7 implementation updates skill/docs/release notes.
   **Required follow-through (noted to architect):** every file listed above must be updated by Step 7, not just the three Design files the plan enumerates. In particular `CLAUDE-DESIGN.md` lines 200-234 / 256-258 / 957 / 1008 / 1012 contain explicit examples and Decision Table entries that will become wrong after this change.

2. **`IFactoryEventRelay.Register/Unregister` public surface + `WeakReference` handler tracking** — documented in `CLAUDE-DESIGN.md:232`, `docs/factory-events.md:191-197`, `docs/interfaces-reference.md:668-680`. Plan rule 18 removes both methods from the interface; `WeakReference` tracking goes with the deleted `FactoryEventRelayDispatcher`.

3. **"`RelayedEvents` is `null` (not empty list) when zero events are captured"** — `CLAUDE-DESIGN.md:229`. The wire format is unchanged (server still sends null for empty); plan rule 12 normalizes `null` to `Array.Empty<FactoryEventBase>()` at the client boundary before invoking `Relay`. This is a consumer-facing behavior change, not a wire contract change. Acceptable, but worth highlighting so CLAUDE-DESIGN.md line 229 is updated: the bullet about "preserves backward-compatible JSON payloads" still applies on the wire.

4. **Handler exceptions are "swallowed" (no logging specified)** — `CLAUDE-DESIGN.md:231`. Plan adds structured logging for `Relay` throws and `UnknownFactoryEventTypeException`. Strict improvement; the "never propagate" guarantee is preserved. Update CLAUDE-DESIGN.md bullet to "Handler exceptions are isolated from the factory caller; relay/deserialization failures are logged via `ILogger`" during Step 7.

## Implicit Dependencies to Verify in Design

1. **Test harnesses — `DesignClientServerContainers` and integration `ClientServerContainers`.** Both simulate HTTP via a delegate-based round-trip and both currently call `DispatchRelayedEvents`. The plan calls out both (`src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` and `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs`). Architect should verify these harnesses themselves implement the new `Task.Run + Task.Yield` ordering so the timing test is valid in integration tests, not just production.
2. **`FactoryEventAttribute` conflict potential.** The proposed public type name is new to the codebase (confirmed via repo search — only the plan and todo mention `FactoryEventAttribute`). No collision. Applying `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` to `FactoryEventBase` with `Inherited = true` is safe: no existing type references either attribute, and the `Inherited` semantic is what `GetCustomAttribute<T>(inherit: true)` will use.
3. **Runtime assembly-scan scope (Risk #2).** The plan documents the dynamically-loaded-assembly hazard and mitigates with a re-scan on miss. No existing business rule is violated — the previous codegen-based approach had no equivalent dynamic-assembly support either.
4. **Authorization patterns (`AuthorizeFactoryPattern.cs`, `SecureOrder.cs`) and `SavePattern`.** None of these patterns reference `IFactoryEventRelay.Register/Unregister`, instance-method event handlers, or the relay dispatch timing — confirmed by search. The save/execute lifecycle interacts with `IFactoryEvents.Raise` on the server only.
5. **`NeatooFactory` enum** — plan uses this for mode-based DI registration. Matches existing `AddRemoteFactoryServices.cs` switch pattern. No contradiction.

## Gaps for Architect

1. **Post-return ordering across non-Blazor hosts (plan Risk #3).** The plan proposes `Task.Run + Task.Yield` but flags that it needs verification outside Blazor (console apps, Xamarin, server-render scenarios). Architect should confirm the approach holds with a test that reproduces the `_entity = await factory(...)` pattern in a no-sync-context host and a sync-context host. Rule 6 must hold in both.
2. **Logger availability in `MakeRemoteDelegateRequest` for the fire-and-forget block.** Plan Risk #6 notes this needs to be checked. `MakeRemoteDelegateRequest.cs:90` already references `logger`, so availability is confirmed; architect need only verify the new log event names (`FactoryEventRelayFailed`, `FactoryEventDeserializationFailed`) don't collide in `NeatooLoggerCategories.cs`.
3. **Migration story for reference application.** `src/docs/reference-app/` may contain instance-method handlers exercised by mdsnippets regions. Architect should grep `[FactoryEventHandler` under `src/docs/reference-app/` during Step 3 to confirm scope of Step 7 doc changes.
4. **Skill updates.** Per CLAUDE.md, `skills/RemoteFactory/` must be completely self-contained. Any references to `Register/Unregister` or instance-method client handlers in `skills/RemoteFactory/references/` must be rewritten in Step 7 (plan's "Documentation" section mentions this but only generically). Flag for docs-writer.

## Recommendations to Architect

1. The timing fix (`Task.Run + Task.Yield`) is the structurally critical part — the `RelayTimingTests` test that fails against current code and passes against the new dispatch is mandatory. Make it a hard gate.
2. The breaking-surface deletions (`Register/Unregister`, `FactoryEventRelayDispatcher`, `FactoryEventRelayRegistry`, generator relay-handler emission) are extensive; plan the deletion phase AFTER the new surface compiles and tests pass, not in a single commit.
3. `FactoryEventAttribute` applied to `FactoryEventBase` with `Inherited = true` + `[DynamicallyAccessedMembers]` is the cleanest IL-trimming story the library has had — single annotation, no generator involvement for event preservation. Architect should confirm the existing generator emission of `DtoConstructorRegistry.PreserveType<T>()` for events in the `[FactoryEventHandler<T>]` pipeline can be fully removed (plan says yes; verify no other pipeline depends on it).
4. Update `src/Design/CLAUDE-DESIGN.md` quick-reference rows during Step 7 (not Step 4): lines 200-234, 256, 957, 1008, 1012, plus the "Execution model" bullets at 229, 231. Several of these are behavioral guarantees, not just docs — they need careful rewriting.
5. Published docs touched by this change (confirmed present today): `docs/factory-events.md`, `docs/events.md`, `docs/interfaces-reference.md`, `docs/attributes-reference.md`, `docs/trimming.md`. All must be updated in Step 7.

## Mistakes to Avoid

None yet (first run).

## User Corrections

None yet.

---

# Step 6B Requirements Verification

**Verdict: REQUIREMENTS SATISFIED**

Date: 2026-04-14
Current step: Step 6B — Requirements Verification (post-Grade-A)

## Re-trace of Step 2 Invariants vs Current Implementation

| # | Step 2 Invariant | Current Implementation Reference | Status |
|---|------------------|----------------------------------|--------|
| 1 | Server-side static-method `[FactoryEventHandler<T>]` byte-for-byte unchanged | `src/Generator/FactoryGenerator.RelayHandler.cs:60-114` (static-only filter at 106-113), `src/RemoteFactory/FactoryEventHandlerRegistry.cs`, `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs`, `src/Design/Design.Tests/FactoryTests/FactoryEventHandlerTests.cs` (all passing, unchanged) | Preserved |
| 2 | `RaiseOptions.ServerOnly` filtering | `src/RemoteFactory/FactoryEventsDispatcher.cs:42-45` — unchanged, filter still lives in `DispatchToHandlers` before `_collector.Collect` | Preserved |
| 3 | Wire format `RemoteResponseDto.RelayedEvents` unchanged | `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:116` still reads `result.RelayedEvents`; only client consumption path rewritten | Preserved |
| 4 | Insertion-order preservation of event batches | `FactoryEventCollector` unchanged; `FactoryEventDeserializer.Deserialize` iterates input list preserving order (test `Deserialize_MultipleEvents_PreservesOrder` passing) | Preserved |
| 5 | Handler/relay exception isolation | `MakeRemoteDelegateRequest.cs:138-145` — explicit try/catch around `relay.Relay(...)` with `FactoryEventRelayFailed` log (EventId 3008, Level Error). Factory caller `await` never sees the exception. | Preserved + Strengthened (now logged at EventId 3008) |
| 6 | Post-return ordering | `MakeRemoteDelegateRequest.cs:122-146` — `Task.Run(async () => { await Task.Yield(); ... }, CancellationToken.None)` pattern. Two passing tests: `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` + `Relay_FiresAfterCallerContinuation_InNoSyncContextHost` | Upgraded from best-effort to hard guarantee |
| 7 | Logical mode registers no relay | `AddRemoteFactoryServices.cs` — registration gated on `NeatooFactory.Remote`; test `LogicalMode_IFactoryEventRelay_NotRegistered` passes | Preserved |
| 8 | Server mode registers no relay | Same file, same gate; test `ServerMode_IFactoryEventRelay_NotRegistered` passes | Preserved |
| 9 | Event records survive IL trimming | `src/RemoteFactory/FactoryEventBase.cs:15-17` carries `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` with `Inherited = true`; `FactoryEventAttribute.cs:17` has `Inherited = true`; end-to-end proof via `src/Tests/RemoteFactory.TrimmingTests/EventRelaySmokeTest.cs` and verified by published-trimmed-binary smoke output line `Event relay smoke PASSED`. | Preserved + Strengthened (now end-to-end proved) |
| 10 | One `[Remote]` call = exactly one `Relay` invocation | `MakeRemoteDelegateRequest.cs:114-148` — single `_ = Task.Run(...)` dispatch per call; empty batch normalization at 128-130 via `Array.Empty<FactoryEventBase>()`; deserialization-failure path at 132-136 returns early before calling Relay (the only legitimate case of zero Relay invocations). Tests: `NoEvents_RelayInvokedOnceWithEmptyBatch`, `MultipleEventsRelay_ArriveInServerRaiseOrder`. | New invariant, correctly implemented |

No Step 2 invariant is violated. Every guarantee is preserved; several are strengthened (5, 6, 9).

## Grade-A Side-Effect Analysis

### 1. NF0503 Warning on instance-method `[FactoryEventHandler<T>]` handlers

Descriptor: `src/Generator/DiagnosticDescriptors.cs:278-285`. Emission: `src/Generator/FactoryGenerator.RelayHandler.cs:73, 116-133`.

**Mis-fire risk analysis:** The NF0503 emission filter requires (a) the containing class has `[FactoryEventHandler<T>]` attribute (checked at the enclosing foreach over `foreach (var attr in symbol.GetAttributes())`), (b) the method is non-static `MethodKind.Ordinary`, (c) the method returns `Task`, (d) the first non-`[Service]`/non-`CancellationToken` parameter's fully-qualified type matches the event type `T` declared in the attribute. So for NF0503 to fire on an unrelated method, the class must still be annotated with `[FactoryEventHandler<T>]` — which is the exact signal that this method is intended as a handler. There is no scenario where a class without `[FactoryEventHandler<T>]` gets NF0503, and no scenario where an instance method that "happens to take" a `FactoryEventBase` descendant gets NF0503 unless (i) the class is `[FactoryEventHandler<T>]`-annotated and (ii) the param type exactly matches `T`. Both signals establish clear migration intent. Severity `Warning` is correct: the consumer's code compiles and runs, but silently drops the handler — a warning surfaces the footgun without breaking the build. **No spurious mis-fire risk.**

### 2. Collision Warning (EventId 3012) in `FactoryEventTypeRegistry`

Log method: `src/RemoteFactory/Internal/Log.cs:172-180`. Fire site: `FactoryEventTypeRegistry.cs:143-155`.

**Spurious-fire risk:** The collision branch is guarded by `!ReferenceEquals(existing, type)`. Identity-same entries — exactly the case when the same `Type` is re-discovered during a rescan, or from two assemblies in the same load context surfacing the same `RuntimeType` — are silently skipped via `continue` (line 154) without logging. A true collision (two distinct `Type` instances with the same `FullName`) is pathological and merits the warning. Hot-reload re-registration: the registry's `Scan` rebuilds `map` from scratch each call, so within one `Scan` invocation a given `Type` appears only once per `(FullName)` key. Multiple `Scan` calls via `Rescan` replace `_cache` wholesale, so no cross-scan accumulation of duplicates. **No spurious fire risk on legitimate dev/test reloads.**

### 3. NoOp First-Event Warning (EventId 3011)

Fire site: `NoOpFactoryEventRelay.cs:28-35`. Log method: `Log.cs:164-170`.

**Spurious-fire risk:** The warning is emitted only by `NoOpFactoryEventRelay`. When a consumer has registered their own `IFactoryEventRelay`, the DI container resolves *their* implementation instead of `NoOpFactoryEventRelay` (standard DI override semantics). The no-op is only resolved when no custom relay was registered; this is confirmed by the `RemoteMode_ConsumerRegistersBeforeAdd_TryAddKeepsConsumerRegistration` and `RemoteMode_ConsumerRegistersAfterAdd_OverridesNoOp` tests. Additionally, the warning is gated by `events is { Count: > 0 }` (line 30), so an empty batch does not fire the warning even on the no-op path — consumers who intentionally want the no-op and don't care about dropped events only see the warning when a non-empty batch is dropped, which is exactly the "you might have forgotten to register" signal. `Interlocked.Exchange` gate (line 31) guarantees one-time emission per process. **No spurious fire risk on consumer-registered relays.**

All three Grade-A additions are well-scoped and do not introduce unintended side effects against the business contracts.

## Pending Step 7 (Documentation Updates)

The implementation intentionally does NOT touch `docs/` or `skills/` markdown — those are Step 7 work for the documenter agent. Every item below must be addressed before the todo can move to Complete.

### Design — `src/Design/CLAUDE-DESIGN.md` (authoritative quick-reference)

1. **Lines 200-234** — `FactoryEventRelayPattern` quick-reference section currently shows instance-method `[FactoryEventHandler<T>]` on a client-side class with `_relay.Register(this)` / `Unregister(this)`. Rewrite to show consumer implementing `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)` (single method). Drop `Register/Unregister` entirely.
2. **Line 211, 220** — references to `_relay.Register(this)` / `_relay.Unregister(this)` (flagged by architect in drift observations). Remove.
3. **Lines 227-234 (Execution model bullets)** —
   - Line 229 "RelayedEvents is null (not empty list) when zero events are captured" — still wire-accurate, but add note that client normalizes null to `Array.Empty<FactoryEventBase>()` so `Relay` is still invoked exactly once with an empty batch.
   - Line 230 "Factory result returned to caller before relay dispatch" — upgrade wording to "hard guarantee: `Task.Run + Task.Yield` ensures `Relay` runs strictly after the caller's continuation resumes".
   - Line 231 "Handler exceptions do NOT propagate" — reword to "Relay exceptions and deserialization failures are isolated via try/catch; both are logged via `ILogger` (EventIds 3008, 3009)".
   - Line 232 "Register/Unregister" — delete entire bullet.
4. **Line 256-258** — Decision Table entry "How do I handle a factory event on the client?" — rewrite answer to "Implement `IFactoryEventRelay` and register it in DI".
5. **Line 957, 1008, 1012** — any remaining mentions of `Register/Unregister` or instance-method client handlers (re-grep to find residual).
6. **Add new entries** for NF0503 in the diagnostics quick-reference section (sibling of NF0501/NF0502).

### Design — `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs`

Already rewritten during implementation (verified by architect Step 6A scenario cross-check). No further action.

### Design — `src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs`

Already rewritten during implementation. No further action.

### Published docs (Jekyll, `docs/`)

7. **`docs/factory-events.md:159-197`** — instance-method handler example + `Register/Unregister`. Rewrite to `IFactoryEventRelay.Relay` single-method pattern. Add NF0503 callout.
8. **`docs/events.md:5`** — any reference to `Register/Unregister`; rewrite.
9. **`docs/interfaces-reference.md:668-680`** — `IFactoryEventRelay` interface description. Rewrite to show only `Relay(IReadOnlyList<FactoryEventBase>)`.
10. **`docs/attributes-reference.md:210, 236-252`** — `[FactoryEventHandler<T>]` section. Add: "Instance-method handlers are no longer supported; see NF0503." Document new `[FactoryEvent]` attribute as inherited-from-`FactoryEventBase` (consumers do not apply it directly).
11. **`docs/trimming.md:74, 282-307, 325-329`** — update "how events are preserved" explanation: was per-handler generator-emitted `PreserveType<T>`, now a single `[DynamicallyAccessedMembers]` on `FactoryEventBase` with `Inherited = true`. Reference the `EventRelaySmokeTest.cs` trimmed-publish verification.
12. **New page or section: `docs/diagnostics/NF0503.md`** — document the new Warning, its cause, and migration guidance (make method `static` for server handling, or implement `IFactoryEventRelay` for client reception).

### Skill — `skills/RemoteFactory/`

13. Per `CLAUDE.md`, skill must be self-contained. Any skill `references/*.md` mentioning `Register/Unregister` or instance-method client handlers must be rewritten to the `IFactoryEventRelay.Relay` pattern. If skill code samples are MarkdownSnippets-extracted from `src/docs/reference-app/`, update the source regions first, then re-run `mdsnippets`.

### Release notes — `docs/release-notes/`

14. New release notes file documenting the breaking change:
   - Breaking: `IFactoryEventRelay` interface reshaped — `Register/Unregister` removed, `Relay(IReadOnlyList<FactoryEventBase>)` added.
   - Breaking: client-side instance-method `[FactoryEventHandler<T>]` handlers are no longer dispatched (NF0503 Warning emitted).
   - Breaking: `FactoryEventRelayRegistry` and `FactoryEventRelayDispatcher` public types removed.
   - New: `NoOpFactoryEventRelay` registered by default in Remote mode; logs Warning 3011 on first non-empty batch drop.
   - New: `[FactoryEvent]` attribute inherited from `FactoryEventBase` — drives the new `FactoryEventTypeRegistry`.
   - Fix: post-return ordering is now a hard guarantee (was previously racy).

## New Requirements to Document (beyond the list above)

These are behavioral contracts introduced by the Grade-A polish pass that need explicit documentation entries somewhere authoritative (CLAUDE-DESIGN.md and/or docs/):

A. **NF0503 Warning** — title, message format, severity (Warning), migration guidance (static → server; `IFactoryEventRelay` → client).
B. **Log Event 3008** `FactoryEventRelayFailed` — Error, fired when consumer's `Relay` throws; exception NOT propagated.
C. **Log Event 3009** `FactoryEventDeserializationFailed` — Error, fired when wire-format event deserialization fails (e.g., `UnknownFactoryEventTypeException`); Relay NOT invoked for that call.
D. **Log Event 3011** `NoOpFactoryEventRelayFirstEvent` — Warning, fired once per process on first non-empty batch dropped by `NoOpFactoryEventRelay`; signals consumer forgot to register a custom relay.
E. **Log Event 3012** `FactoryEventTypeRegistryCollision` — Warning, fired during assembly scan when two distinct `Type`s share the same `FullName`; documents kept/dropped assembly.
F. **One-Relay-per-call invariant** — every `[Remote]` factory call produces exactly one `Relay` invocation (including empty batch), UNLESS deserialization of the batch fails, in which case `Relay` is not invoked and 3009 is logged. Consumers can rely on this for batch-end bookkeeping.
G. **Post-return ordering hard guarantee** — `Task.Run + Task.Yield + CancellationToken.None` dispatch pattern ensures `Relay` runs strictly after the caller's continuation resumes, across both sync-context and no-sync-context hosts. Was previously best-effort.
H. **`[FactoryEvent]` attribute inheritance** — applied once to `FactoryEventBase` with `Inherited = true`, `AllowMultiple = false`. Consumers never apply it directly; inheriting `FactoryEventBase` is sufficient.
I. **`FactoryEventTypeRegistry` runtime-populated, no generator involvement** — lazy first-use scan, thread-safe, rescans on miss to pick up dynamically-loaded assemblies.
J. **`UnknownFactoryEventTypeException`** — thrown by `FactoryEventDeserializer` when the wire-format `TypeFullName` does not resolve via the registry; caught at the dispatch isolation boundary, logged as 3009, does not propagate to the factory caller.

## Issues for Orchestrator

None. Implementation satisfies all business requirements per Step 2 review. Verdict: **REQUIREMENTS SATISFIED**. Proceed to Step 7 (Documentation) with the 14+10 items above as the concrete work list for the documenter agent.
