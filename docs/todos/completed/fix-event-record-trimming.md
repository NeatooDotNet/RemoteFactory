# Fix Event Record Trimming in Blazor WASM Release Builds

**Status:** Complete
**Priority:** High
**Created:** 2026-04-13
**Last Updated:** 2026-04-13

---

## Problem

Factory Events (`[FactoryEventHandler<T>]` + `IFactoryEvents.Raise<T>`) shipped in v1.0/v1.1 work in Debug but fail in Blazor WASM **Release** builds where IL trimming is enabled. The event record types are stripped (or their constructors / properties are stripped) by the WASM trimmer because the generator never emits a preservation hint for them.

Secondary observation found during the same investigation: consuming projects must reference `Neatoo.RemoteFactory` **directly** — a transitive `PackageReference` (or a `ProjectReference` with `PrivateAssets=all`) does not flow the Roslyn source generator, so the consuming assembly never gets its generated `FactoryServiceRegistrar`, `DtoConstructorRegistry.Register` calls, or `[FactoryEventHandler<T>]` registrations. This is a documentation gap, not a generator bug.

### Root cause (trimming)

`RelayHandlerRenderer` emits `typeof(EventT)`, `(EventT)evt`, and `serializer.Deserialize<EventT>(json)` — these preserve the type symbol but carry no `[DynamicallyAccessedMembers(All)]` annotation, so the trimmer is free to strip the record's primary constructor and public properties that `NeatooJsonTypeInfoResolver` / `RecordBypassConverterFactory` need at runtime.

`DtoConstructorRegistry.Register<[DynamicallyAccessedMembers(All)] T>(Func<object>)` already exists and is the right preservation primitive, but (a) it's never emitted for `[FactoryEventHandler<T>]` event types, and (b) it assumes a public parameterless constructor, which event records (`public record Evt(int X) : FactoryEventBase`) don't have.

## Solution

Two-part fix:

1. **Trimming (source change).** Add a new API `DtoConstructorRegistry.PreserveType<[DynamicallyAccessedMembers(All)] T>()` that only registers the type for preservation (no factory lambda — records use `RecordBypassConverterFactory` to instantiate via the primary constructor). In `FactoryGenerator.RelayHandler.cs` / `RelayHandlerRenderer.cs`, for each `[FactoryEventHandler<T>]` emit `DtoConstructorRegistry.PreserveType<T>()` inside `FactoryServiceRegistrar`, then recursively walk `T`'s public properties (reusing the existing `DiscoverDtoTypesRecursive` logic), emitting `Register<Nested>` for parameterless-ctor DTOs and `PreserveType<NestedRecord>` for parameterized-ctor records. This mirrors the recursive nested-DTO discovery introduced in v0.27 (commit `e630387`), just rooted at the event-type argument rather than factory return types.

2. **Documentation (no source change).** Add a note to `docs/trimming.md` and the skill (`skills/RemoteFactory/references/`) explaining that `Neatoo.RemoteFactory` must be a direct `PackageReference` in every project that defines `[Factory]`, `[FactoryEventHandler<T>]`, or Execute/Save entry points — a transitive reference does not flow the source generator.

---

## Requirements Review

**Verdict:** Pending
**Reviewed:**
**Summary:**

---

## Plans

- [Fix Event Record Trimming](../plans/fix-event-record-trimming.md)

---

## Tasks

- [x] Step 1: Draft plan with user
- [ ] Step 2: Business requirements review (deferred at user's direction; reviewer still runs in Step 6B for post-implementation verification)
- [x] Step 3: Architect validation — Grade A, Approved
- [x] Step 4: Implementation
- [x] Step 5: Developer code review — Grade A, Approved
- [x] Step 6A: Architect verification — VERIFIED (manual trimming verified against published Person.Server WASM)
- [x] Step 6B: Requirements verification — REQUIREMENTS SATISFIED
- [x] Step 7A: Requirements documentation — 9 files updated (docs/trimming.md, docs/factory-events.md, skill references, CLAUDE-DESIGN.md, v1.2.0 release notes, index, version bump)
- [x] Step 7B: General documentation — not needed (fully covered in 7A)
- [x] Step 8: Completion

---

## Progress Log

### 2026-04-13
- Created todo after investigating Factory Events trimming failure reported by user.
- Traced the trimming path: `RelayHandlerRenderer` → `Deserialize<EventT>` → `NeatooJsonTypeInfoResolver` → `RecordBypassConverterFactory`. No preservation call for event types.
- Located the existing pattern: `DtoConstructorRegistry.Register<T>` with `[DynamicallyAccessedMembers(All)]`. Gap: event records have no parameterless ctor.
- Pre-todo: added a "direct `Neatoo.RemoteFactory` PackageReference required in every project with factory types" note to `docs/trimming.md`, `skills/RemoteFactory/references/trimming.md`, and corrected `docs/getting-started.md` which had the opposite advice.
- Drafted plan `../plans/fix-event-record-trimming.md` with Business Rules 1-9, 11 test scenarios, and a two-pronged design.
- Step 2 deferred at user's direction; went straight to Step 3.
- Step 3 round 1 — architect graded plan **B+**, verdict **Concerns**. 10 items for Grade A: drop Rule 2 (always `PreserveType<T>` for event types), rewrite Rule 6 precision, remove Rule 9 (doc-only), add Scenarios 12-17, move preservation gathering before NF0501/NF0502 `continue`, use `EquatableArray<string>`, annotate `FactoryEventHandlerRegistry.RegisterHandler<TEvent>`, snapshot-test generated C#, add `OrderShippedEvent`/`ShippingAddress` to Design, IL2091 release-note callout.
- Applied all 10 items to the plan.
- Step 3 round 2 — architect re-graded **A**, verdict **Approved**. Plan status set to `Ready for Implementation`.
- Next: Step 4 (implementation). Recommended to start a fresh session per the workflow (plan + CLAUDE.md + RemoteFactory skill are the sole load).

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass (Debug and Release)
- [ ] All tests pass (net9.0 and net10.0)
- [ ] A new integration test confirms that a `[FactoryEventHandler<T>]` event round-trips through the client/server containers after simulated trimming (or equivalent: verifies the `FactoryServiceRegistrar` emits `PreserveType<T>` and `Register<Nested>` calls)

**Verification results:**
- Build: 0 errors, 0 new warnings on Debug + Release (net9.0 + net10.0)
- Tests: 1,233 passed / 0 failed / 3 intentionally skipped (577 unit × 2 TFMs + 582 integration × 2 TFMs + 74 Design × 2 TFMs)
- Trimming ground-truth: `Person.Server` published to Release retains event type names + property accessors in trimmed WASM

---

## Results / Conclusions

**Outcome:** Factory Events now round-trip correctly in Blazor WASM Release builds with IL trimming. The root cause (missing preservation for event record types) is fixed by three independent layers:

1. **Registry-based preservation** — generator emits `DtoConstructorRegistry.PreserveType<T>()` for every `[FactoryEventHandler<T>]` event type and `Register<N>()` / `PreserveType<N>()` for recursively reachable nested types. Emission is unconditional (outside `IsServerRuntime` guard) and happens BEFORE NF0501/NF0502 diagnostic branches.
2. **`Raise<T>` call-site preservation** — `[DynamicallyAccessedMembers(All)]` on `IFactoryEvents.Raise<T>` and both implementations (`FactoryEventsDispatcher`, `RemoteFactoryEvents`).
3. **`RegisterHandler<TEvent>` call-site preservation** — `[DynamicallyAccessedMembers(All)]` on the registration primitive for supplemental belt-and-suspenders coverage.

**New public API:** `DtoConstructorRegistry.PreserveType<T>()` — trimmer hint only, no state mutation.

**Known limitation:** `Dictionary<K,V>` value types are not walked by the existing property walker. Documented workaround in `docs/trimming.md` and the skill.

**User-visible consideration:** Potential `IL2091` cascade for user code that forwards `Raise<T>` through a generic passthrough. Migration note in v1.2.0 release notes.

**Version bump:** v1.1.0 → v1.2.0 (non-breaking minor — new public API + bug fix).

**Key files delivered:**
- `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` — `PreserveType<T>()`
- `src/Generator/DtoTypeWalker.cs` — shared walker
- `src/Generator/Renderer/RelayHandlerRenderer.cs` — unconditional preservation emission
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` — `OrderShippedEvent` + `ShippingAddress` nested-record example
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/EventTrimming/EventDtoDiscoveryTests.cs` — 14 new tests covering scenarios 1-8, 12-17
- `docs/release-notes/v1.2.0.md` — release notes

**Out-of-scope fixes made (user-approved):**
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` — threaded missing `CancellationToken` parameter on `ForDelegateEvent` (pre-existing gap from commit 387a300 that prevented the Design.Tests project from building)
