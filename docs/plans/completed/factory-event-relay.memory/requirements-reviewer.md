# Requirements Reviewer — Factory Event Relay

Last updated: 2026-04-09
Current step: Step 6B post-implementation verification (Mode 2)

## Key Context

The Factory Event Relay plan adds server-to-client event forwarding, building on the existing IFactoryEvents mediator (commit 1750f52). Events raised on the server during factory operations are captured, serialized in RemoteResponseDto, and replayed on the client.

During implementation the design was refined from the plan:
- **Plan:** `IFactoryEventHandler<T>` interface on `[Factory]` classes; generator detects interface implementations.
- **Implementation:** `[FactoryEventHandler<T>]` class attribute (standalone generator pipeline, does not require `[Factory]`). Instance method → client relay. Static method → server-side handler (replaces the old method-level `[FactoryEventHandler]` attribute and `FactoryOperation.FactoryEventHandler` enum value, which have been removed).

## Pre-Design Review

**Verdict:** APPROVED
**Date:** 2026-04-09

(Pre-design review content archived — full table in git history of this file. Summary: no contradictions with documented patterns, anti-patterns, or design debt. Raised gaps around type resolution, test determinism, chained events, and diagnostics; all addressed during architect/implementation phases.)

## Requirements Verification (Step 6B)

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-04-09

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Design project must demonstrate new pattern (single source of truth) | `CLAUDE.md` project instructions | Satisfied | `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` (new) and `FactoryEventHandlerPattern.cs` (updated to class attribute) exist with "DID NOT DO THIS" comments explaining the interface-vs-attribute decision. |
| 2 | Design tests must verify new behavior | `CLAUDE.md` project instructions | Satisfied | `src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs` has 3 tests: relay dispatch, `RaiseOptions.ServerOnly` exclusion, `Unregister` stops delivery. Rules 1, 2, 8, 11, 12, 16, 17 verified. |
| 3 | CLAUDE-DESIGN.md documents pattern | `CLAUDE-DESIGN.md` as quick reference | Satisfied | Section "Pattern 4: Factory Event Handler (Mediator + Client Relay)" at line 134 shows both server-side static handler and client-side instance handler with full example and key points (NF0501/NF0502, ServerOnly, fire-and-forget semantics, exception swallowing). |
| 4 | CLAUDE-DESIGN.md Quick Decisions Table updated | `CLAUDE-DESIGN.md` quick reference | Satisfied | 5 new rows added (lines 229-233): client-side handler, server-side handler, `[Factory]` requirement, ServerOnly flag, multiple event types per class. |
| 5 | Anti-pattern for raising events outside factory methods | Pattern hygiene | Satisfied | Anti-Pattern 10 (line 503) documents "Raising Factory Events Outside a Factory Method" with WRONG/RIGHT code and explanation that the request-scoped collector only exists server-side inside factory operations. |
| 6 | Anti-pattern for decorating handler class with [Factory] | Pattern hygiene | Satisfied | Anti-Pattern 11 (line 530) documents "Decorating a [FactoryEventHandler<T>] Class with [Factory]" with WRONG/RIGHT and rationale about separate generator pipelines. |
| 7 | [Remote] requires `internal` (NF0105) — the server-side raiser `CheckoutOrder.Create` is `[Remote] internal` | `CLAUDE-DESIGN.md` Critical Rule 1 | Satisfied | `FactoryEventRelayPattern.cs:105` uses `[Remote, Create] internal async Task Create(...)`. |
| 8 | Properties need public setters for serialization | Anti-Pattern 4 | Satisfied | `CheckoutOrder.Id` and `Total` in `FactoryEventRelayPattern.cs:100-101` both use `public ... { get; set; }`. `RelayedFactoryEvent` DTO uses `public set` as well. |
| 9 | `partial` keyword required on factory classes | Anti-Pattern 6 / Quick Decisions | Satisfied | `CheckoutOrder` is `internal partial class`; `OrderCheckoutViewModel` is `public sealed partial class`; `OrderNotifyHandlers` / `OrderAuditHdlrs` are `public static partial class`. |
| 10 | `IsServerRuntime` guard discipline for server-only dispatch | Internal methods must be guarded | Satisfied | Server-side handler dispatch in `StaticFactoryRenderer`/`FactoryEventHandlerRegistry` retains `IsServerRuntime` guard. Relay dispatch is client-side; its registrar does not use a guard intentionally (documented non-blocking concern in plan; both client+server DI containers share the process in tests, and the trade-off — unused relay entries in server memory — is cheap). |
| 11 | NeatooTransportJsonContext must cover new transport types for trimming | `docs/trimming.md` / existing patterns | Satisfied | `src/RemoteFactory/Internal/NeatooTransportJsonContext.cs:16-17` adds `[JsonSerializable(typeof(RelayedFactoryEvent))]` and `[JsonSerializable(typeof(List<RelayedFactoryEvent>))]`. |
| 12 | No reflection rule (global instruction) | `~/.claude/CLAUDE.md` | Satisfied | Dispatch delegates are source-generated typed casts — no `System.Reflection`, `Type.GetType`, or `MethodInfo.Invoke` introduced. Registry is keyed by `string` (type full name), not `Type`, deliberately to avoid `Type.GetType` calls. |
| 13 | RaiseOptions is `[Flags]` enum with power-of-2 values | `RaiseOptions.cs` | Satisfied | `ServerOnly = 4` is the correct next power of two. |
| 14 | New diagnostics have ID + description | Generator conventions | Satisfied | `DiagnosticDescriptors.cs:249-264` — NF0501 (no matching handler method) and NF0502 (multiple matching handler methods). |
| 15 | Design Debt boundary | `CLAUDE-DESIGN.md` Design Debt table | Satisfied | None of the deferred items are implemented by this work; none of the new code contradicts the deferred items. |

### Implicit Dependency Check

**Removed the method-level `[FactoryEventHandler]` attribute and `FactoryOperation.FactoryEventHandler` enum value.** Grep confirms:

- No production code references `FactoryOperation.FactoryEventHandler` or bare `[FactoryEventHandler]` (non-generic method attribute form). The only references to the old form live in plan/todo/memory markdown files (factory-events-mediator plan, factory-event-relay memory files), which is expected historical documentation.
- The existing mediator behavior (server-side dispatch in isolated scope with `[Service]` injection and `CancellationToken`) is preserved under the new pipeline: static methods inside `[FactoryEventHandler<T>]` classes register into `FactoryEventHandlerRegistry` the same way. `FactoryEventHandlerPattern.cs` has been rewritten to show the class-attribute form while preserving the same behavioral contract.
- The Person example is referenced in the Quick Decisions table ("Can I handle multiple event types in one class?" → `PersonEventHandler.cs`). Not verified in this review because it's outside the Design project scope, but the statement is consistent with the class-attribute allowing `AllowMultiple = true`.

**Class-attribute approach conflict check:**

- The new `[FactoryEventHandler<T>]` runs in a separate generator pipeline from `[Factory]`. Anti-Pattern 11 explicitly forbids stacking them. No existing pattern requires `[FactoryEventHandler<T>]` and `[Factory]` on the same class.
- The attribute uses `AttributeTargets.Class` + `AllowMultiple = true` — consistent with existing RemoteFactory attributes that allow multiple declarations (e.g., `[AspAuthorize]`).
- No conflict with aggregate root, child entity, interface factory, or static factory patterns. The handler class is neither a factory nor a domain entity — it is a standalone subscriber.

### Published Docs (`docs/*.md`)

Grep across `docs/*.md` (excluding `docs/plans/` and `docs/todos/`) for `FactoryEvent`, `FactoryEventHandler`, `FactoryEventRelay`, `IFactoryEvents`, `RaiseOptions` returns **zero hits**. The Jekyll-published docs do not currently document the factory events mediator or relay at all. This means:

1. **No stale references to remove** — there is no documentation describing the old method-level `[FactoryEventHandler]` attribute that needs updating.
2. **Documentation gap (pre-existing)** — the factory-events-mediator feature (commit 1750f52) was shipped without updating `docs/events.md`, `docs/attributes-reference.md`, or adding a dedicated `docs/factory-events.md`. The relay feature inherits this gap.
3. **Recommendation for Step 7B (Documentation):** Add a new doc page (e.g., `docs/factory-events.md` or extend `docs/events.md`) that covers:
   - `IFactoryEvents` injection and `Raise()`
   - `[FactoryEventHandler<T>]` class attribute (static → server, instance → client relay)
   - `RaiseOptions.ServerOnly`
   - NF0501 / NF0502 diagnostics
   - `IFactoryEventRelay.Register` / `Unregister` lifecycle for client VMs
   - Update `docs/attributes-reference.md` with the `[FactoryEventHandler<T>]` attribute row
   - Update `docs/trimming.md` if the `RelayedFactoryEvent` transport type is worth mentioning

**Note:** These are Step 7B concerns (Requirements Documented / Documentation Complete), not Step 6B. Not a violation — the Design project is the single source of truth and the documentation gap is pre-existing (inherited from the mediator feature).

### Unintended Side Effects

None detected. Specifically checked:

- **Existing factory patterns unchanged:** Class Factory, Interface Factory, Static Factory (with `[Execute]`/`[Event]`) are untouched. `AllPatterns.cs`, `Order.cs`, `OrderLine.cs`, `SecureOrder.cs` are not impacted.
- **Serialization contract:** `RemoteResponseDto` adds a nullable `RelayedEvents` with `[JsonConstructor]` + `private set` following the existing pattern for transport DTOs. Default `null` preserves backward compatibility with existing clients that don't expect the property.
- **DI registration modes:** Collector is scoped in Server mode only; relay is singleton in Remote mode only; Logical mode has neither. Matches existing mode branching in `AddRemoteFactoryServices`.
- **Design project tests:** 47 Design.Tests per TFM pass, including the 3 new relay tests. No regressions to the 26 existing factory pattern tests.
- **No reflection introduced** — verified by inspection of the plan's registry design (string-keyed, source-generated typed dispatch delegates) and the global no-reflection rule.

### Issues Found

None blocking. Minor non-blocking observations (already documented as plan follow-ups):

1. Relay handler registrar lacks `!IsServerRuntime` guard (intentional for test determinism; server memory trade-off acknowledged).
2. Rule 18 weak-reference cleanup test is soft (asserts no crash, not explicit list pruning).
3. Rules 4 (nested ops) and 7 (Logical mode no capture) are DI-construction-verified but not explicitly tested in integration tests.
4. `MakeRemoteDelegateRequest` casts `IFactoryEventRelay as FactoryEventRelayDispatcher` — minor design smell.
5. Rule 9 (null, not empty list) is implicitly tested via `NoEvents_NoRelayedEvents` rather than directly asserted.

None of these violate documented requirements — they are enhancement opportunities logged in the plan's "Follow-up Items" section.

## Mistakes to Avoid

(none yet)

## User Corrections

(none yet)
