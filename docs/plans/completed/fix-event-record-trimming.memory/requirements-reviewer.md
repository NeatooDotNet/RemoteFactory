# Requirements Reviewer — Fix Event Record Trimming

Last updated: 2026-04-13
Current step: Step 6B — Post-implementation requirements verification (FIRST run; Step 2 was skipped at user's direction).

## Key Context

- Plan: `docs/plans/fix-event-record-trimming.md` (Grade A, Architect-verified in Step 6A).
- Implementation adds `DtoConstructorRegistry.PreserveType<T>()`, annotates three generic methods with `[DynamicallyAccessedMembers(All)]`, and extends the generator to emit preservation calls rooted at `[FactoryEventHandler<T>]` event types, including recursive nested-DTO discovery.
- Architect verification (Step 6A) confirmed clean build (0 errors, 0 IL2xxx warnings), full test suite passing (1233 passed per TFM on net9.0/net10.0), and manual trimming verification on the Person example.
- All 18 business-rule-related tests (14 EventDtoDiscoveryTests + 2 DtoConstructorRegistryTests + 1 Design round-trip + integration suite) pass.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-04-13

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Factory Event execution model — shared scope, sequential, awaited | `docs/factory-events.md:24-26`, `docs/release-notes/v1.1.0.md:33-35`, `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:14-28` | Satisfied | `FactoryEventsDispatcher.DispatchToHandlers` unchanged: same `foreach (handler in handlers) await handler(...).ConfigureAwait(false)` loop, same `_sp` forwarding (caller scope). Nothing in this plan touches dispatch. |
| 2 | `Raise<T>` CancellationToken signature (v1.1.0 contract) | `docs/release-notes/v1.1.0.md:69-79`, `src/RemoteFactory/IFactoryEvents.cs:36` | Satisfied | Parameter list unchanged; only `[DynamicallyAccessedMembers(All)]` added to `T`. Additive — callers with concrete `T` are unaffected. |
| 3 | `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` signature (v1.1.0 contract) | `docs/release-notes/v1.1.0.md:151-164` | Satisfied | `(Type handlerClassType, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task>)` signature preserved; only `[DynamicallyAccessedMembers(All)]` added to `TEvent`. |
| 4 | DTO return type preservation via `DtoConstructorRegistry.Register<T>(() => new T())` | `docs/trimming.md:246-280`, published v0.27.0 release notes | Satisfied | Factory-return path unchanged. `DtoTypeWalker.WalkFactoryReturn` still enforces `IsDtoStructureCandidate && HasParameterlessCtor`, emits `Register<T>(() => new T())`. `NestedDtoDiscoveryTests` (all 13 Facts) still pass per architect report. |
| 5 | Nested DTO recursive discovery is cycle-safe with public-property walk | `docs/trimming.md:276-280` | Satisfied | `WalkEventRoot` shares the same `WalkProperties` primitive and `visited` set semantics; Scenario 6 (self-referencing) test passes. |
| 6 | Factory type preservation via `NeatooFactoryRegistrar` attribute | `docs/trimming.md:222-228` | Satisfied | `RelayHandlerRenderer.cs:31` still emits `[assembly: NeatooFactoryRegistrar(typeof(...))]` for every `[FactoryEventHandler<T>]` class, unchanged. |
| 7 | Prerequisite: direct `Neatoo.RemoteFactory` PackageReference in every project with factory types | `docs/trimming.md:62-76`, `skills/RemoteFactory/references/trimming.md:16-26` | Satisfied | Landed pre-todo and is consistent with this plan's behavior: the new preservation emission lives inside `FactoryServiceRegistrar`, so it only fires in projects where the generator runs — which is exactly the documented constraint. No contradiction. |
| 8 | `IsServerRuntime` guard wraps server-only registrations | `docs/trimming.md:38-41` (event registrations inside guard) | Satisfied | The per-entry server-side `FactoryEventHandlerRegistry.RegisterHandler` call remains inside the `if (NeatooRuntime.IsServerRuntime)` guard at `RelayHandlerRenderer.cs:105-116`. Only the new `PreserveType`/`Register` emissions precede the guard — correct per Rule 7 of the plan (client path also needs the metadata for deserializing server-raised events). |
| 9 | Design projects are the source of truth; new patterns get a Design example | `CLAUDE.md` Design Source of Truth section | Satisfied | `OrderShippedEvent` + `ShippingAddress` + `OrderShippedHandlers` added to `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:119-176` with heavy commentary. Matching round-trip test added to `Design.Tests/FactoryTests/FactoryEventHandlerTests.cs` (74 Design tests pass per architect). |
| 10 | `[FactoryEventHandler<T>]` is a class-level attribute with static-or-instance method matching | `docs/factory-events.md:262-275`, `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:86-116` | Satisfied | `TransformRelayHandler` unchanged in this respect. Matching rules, NF0501 (no matching method), NF0502 (multiple matching methods) unchanged. |
| 11 | NF0105: `[Remote] public` is a compile-time error | `docs/trimming.md:28-29` | Satisfied | Not touched by this plan. |
| 12 | Trimming-safe event-type resolution on client via `TypeFullName` string key | `docs/factory-events.md:258` | Satisfied | `RenderClientSideRelayHandler` still emits `typeof({eventTypeName}).FullName!` as the registry key (RelayHandlerRenderer.cs:128). The new preservation emission makes this more robust — the type metadata now survives trimming — without changing the lookup mechanism. |

### Unintended Side Effects (checked and cleared)

1. **`FactoryServiceRegistrar` now emitted for preservation-only classes.** Previously, a `[FactoryEventHandler<T>]` class that only produced NF0501/NF0502 (no valid handler method) would not produce a `RelayHandler.g.cs` file. Now it will, containing just the `PreserveType<T>()` call (the entries collection is empty but `eventRecordTypesList` is non-empty, so `TransformRelayHandler` returns a model — see `FactoryGenerator.RelayHandler.cs:183`). Reviewed: this is intended per Scenario 17 and is not in contradiction with any documented contract. NF0501 is still emitted; the preservation emission is a pure addition.
2. **`DtoConstructorRegistry` public API surface expands.** Added `PreserveType<T>()` static method. Additive — no documentation anywhere declares the API surface frozen. `TryCreate` still returns `false` after `PreserveType<T>()`, preserving existing callers' semantics (tested in `DtoConstructorRegistryTests.PreserveType_DoesNotRegisterConstructor`).
3. **Potential `IL2091` on user code that forwards `Raise<T>` through their own generic passthrough.** Acknowledged in the plan as a documentation item (release-note migration callout). Not a contract violation — `[DynamicallyAccessedMembers(All)]` is the standard .NET trimming-annotation flow, and the attribute on an implementation parameter is additive relative to the interface. User code that *called* `Raise<ConcreteType>(...)` directly (the intended usage) is unaffected. Only code that did `void Relay<T>(T evt) => _events.Raise(evt);` without matching annotation will see a new warning. Must be mentioned in Step 7B release notes.
4. **Design test in `Design.Tests/FactoryTests/FactoryEventHandlerTests.cs` was modified.** Inspected: the change adds a new `OrderShippedEvent` round-trip test; no existing test was gutted or had assertions removed. Existing 74 Design tests continue to pass per architect evidence.
5. **Pre-existing `IFactorySave.cs` modification in git status.** Verified — appears to be whitespace/XML-doc formatting only, unrelated to this plan. Out of scope for this review.

### Issues Found

None.

### Documentation Updates Needed in Step 7 (not performed here)

For the documentation step (Step 7B) the following files will require updates to describe the new behavior and API:

- `docs/trimming.md` — add a "Factory Event Type Preservation" subsection (parallel to the existing "DTO Return Type Preservation" section) describing automatic preservation for `[FactoryEventHandler<T>]` event types AND their recursively-walked nested DTOs/records, with the `Dictionary<K,V>` known-gap callout and the `PreserveType<T>` vs `Register<T>` distinction.
- `docs/factory-events.md` — add a short "IL Trimming" paragraph (it currently discusses trimming only for `RelayedFactoryEvent` at line 258) cross-linking to `docs/trimming.md` and noting that event-record types and nested records are preserved automatically.
- `skills/RemoteFactory/references/trimming.md` — mirror the trimming.md addition (skill is self-contained per `CLAUDE.md`).
- `skills/RemoteFactory/references/factory-events.md` — add the same cross-link note.
- `docs/release-notes/v1.2.0.md` (or next patch/minor) — new release notes entry. Must include:
  - "What's New": `DtoConstructorRegistry.PreserveType<T>()`; automatic preservation for event-record types and nested records.
  - "Bug Fixes": event records failing to round-trip in Blazor WASM Release builds with `PublishTrimmed=true`.
  - "Potential warning" callout: user code that forwards `Raise<T>` through its own generic passthrough may now see `IL2091`; add `[DynamicallyAccessedMembers(All)]` to the passthrough's `T` parameter to resolve.
  - Known limitation: `Dictionary<K,V>` value types are not recursively walked; document workaround (declare an additional `[FactoryEventHandler<Payload>]` or call `DtoConstructorRegistry.PreserveType<Payload>()` manually).

### Cross-Check Against Plan's Business Rules

All 9 business rules in `docs/plans/fix-event-record-trimming.md:24-34` are satisfied by the implementation. The code-level evidence is in the architect's memory file; from a requirements-contract standpoint, none of them contradict any documented behavior in `docs/`, the skill, or the Design projects. Rules 1, 4, 6, 7, 8 are additive generator behavior; Rules 2, 3, 5 extend documented patterns consistently; Rule 9 defines the semantics of a newly-introduced primitive (`PreserveType<T>`) that has no prior documented contract.

## Mistakes to Avoid

(First run — none yet.)

## User Corrections

(First run — none yet.)
