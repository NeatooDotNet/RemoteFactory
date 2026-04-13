# Developer Code Review — Fix Event Record Trimming

Last updated: 2026-04-13
Current step: Step 5 — Developer Code Review (first pass)

## Summary

**Grade: A**
**Verdict: Approved**

All 9 business rules trace cleanly through the actual source. All 17 test scenarios are covered either by automated generator tests, the Design project round-trip, or (for Scenario 9) by compiler-enforced generic annotation propagation that was validated by the clean build. Code is tight, the walker split is clean (no duplication with `FactoryGenerator.Types.cs`), the `EquatableArray<string>` choice is correct, the ordering of preservation gathering vs. NF0501/NF0502 `continue` is correct, and the `FactoryGenerator.cs` early-return condition correctly emits source when entries are empty but preservation is non-empty.

No prioritized change list needed to reach Grade A.

---

## Assertion Trace

| # | Business Rule | Implementation Path | Verdict |
|---|---------------|---------------------|---------|
| 1 | `[FactoryEventHandler<TEvent>]` ⇒ exactly one `PreserveType<TEvent>()` regardless of ctor | `FactoryGenerator.RelayHandler.cs:75` calls `DtoTypeWalker.WalkEventRoot(eventType, …)` for every attribute; `DtoTypeWalker.cs:197` unconditionally adds the root to `parameterizedTypes`; `preservationVisited` guards against duplicate emits per class; `RelayHandlerRenderer.cs:56-59` renders `PreserveType<T>()`. | OK |
| 2 | Nested property types bucket-sort: parameterless-ctor → `Register<N>(() => new N())`, otherwise → `PreserveType<N>()` | `DtoTypeWalker.cs:201-231` `WalkNested` inner function: calls `IsDtoStructureCandidate` → if `HasParameterlessCtor` adds to `parameterlessCtorTypes`, else adds to `parameterizedTypes`. Rendered at `RelayHandlerRenderer.cs:52-59`. | OK |
| 3 | Cycle/dedupe inside a single class | Shared `preservationVisited: HashSet<string>` at `FactoryGenerator.RelayHandler.cs:31` threaded through each `WalkEventRoot` call. `DtoTypeWalker.cs:191-193` and `215-217` reject revisits via `visited.Add(fqn)`. | OK |
| 4 | Multi-attribute cross-event dedupe within a single `FactoryServiceRegistrar` | Same shared `preservationVisited` and shared `eventDtoTypesList` / `eventRecordTypesList` across the `foreach (var attr …)` loop at `FactoryGenerator.RelayHandler.cs:54-181`. Scenario 14 test (`EventDtoDiscoveryTests.cs:349-377`) confirms exactly-once emission. | OK |
| 5 | `IFactoryEvents.Raise<T>` and all implementations annotate `T` with `[DynamicallyAccessedMembers(All)]` | `IFactoryEvents.cs:36`, `FactoryEventsDispatcher.cs:29`, `RemoteFactoryEvents.cs:21` — all three carry the annotation. `IL2091` would fail the build if mismatched; build is clean (0 errors). | OK |
| 6 | `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` annotates `TEvent` | `FactoryEventHandlerRegistry.cs:43` — annotation present. Generator-emitted call sites (`RelayHandlerRenderer.cs:107`) use concrete `TEvent`, making each a preservation site. | OK |
| 7 | Preservation emission unconditional (no `IsServerRuntime` guard) | `RelayHandlerRenderer.cs:48-59` — preservation block emitted directly after `FactoryServiceRegistrar` opener (line 46) and before any `if (NeatooRuntime.IsServerRuntime)` guard (which only wraps `RenderServerSideHandler` at line 105). Scenario 15 test explicitly asserts unconditional placement for both static and instance handlers. | OK |
| 8 | Preservation still emitted when handler method signature is diagnostic-broken (NF0501/NF0502) | `FactoryGenerator.RelayHandler.cs:75` invokes `WalkEventRoot` BEFORE the `continue` branches at lines 127 and 143. `FactoryGenerator.cs:101-104` emits source when `EventDtoTypes.Count > 0 || EventRecordTypes.Count > 0`, even with zero entries. Scenario 17 test confirms NF0501 diagnostic co-occurs with `PreserveType<OrphanEvent>()`. | OK |
| 9 | `PreserveType<T>` is idempotent, no dictionary mutation, `TryCreate` still `false` | `DtoConstructorRegistry.cs:43-46` — method body is just `_ = typeof(T)`, no dictionary access. `DtoConstructorRegistryTests.cs:10-25` — two Facts verify `TryCreate` returns false after 1 and 3 calls. | OK |

---

## Scenario Coverage

| # | Scenario | Covered By | Verdict |
|---|----------|-----------|---------|
| 1 | Record event with parameterized ctor preserved | `EventDtoDiscoveryTests.Scenario1_RecordEventWithParameterizedCtor_IsPreservedUnconditionally` | OK |
| 2 | Parameterless-ctor event uses `PreserveType` (not `Register`) | `Scenario2_ParameterlessCtorEvent_UsesPreserveTypeNotRegister` | OK |
| 3 | Nested plain DTO uses `Register` | `Scenario3_NestedPlainDto_UsesRegister` | OK |
| 4 | Nested parameterized record uses `PreserveType` | `Scenario4_NestedParameterizedRecord_UsesPreserveType` | OK |
| 5 | Collection / nullable unwrapping | `Scenario5_CollectionAndNullableProperties_AreUnwrapped` | OK |
| 6 | Cycle suppression | `Scenario6_SelfReferencingEvent_EmitsExactlyOnce` | OK |
| 7 | Primitive/framework properties skipped | `Scenario7_PrimitiveAndFrameworkProperties_AreNotRegistered` | OK |
| 8 | `[Factory]`-annotated property types skipped | `Scenario8_FactoryAnnotatedPropertyType_IsSkipped` | OK |
| 9 | `Raise<T>` call-site preservation for producer-only projects | Inherent to generic annotation + compiler enforcement; no dedicated test. Clean build proves no missed `IL2091` warning. Architect's note (memory line 81) recommended `TreatWarningsAsErrors` which is already the repo default. Acceptable. | OK |
| 10 | End-to-end ClientServerContainers round-trip | `FactoryEventHandlerTests.Raise_EventWithNestedRecord_DispatchesSuccessfully` (Design.Tests) plus existing `TestComplexEvent` round-trip tests. | OK |
| 11 | Idempotent `PreserveType<T>` | `DtoConstructorRegistryTests.PreserveType_IsIdempotent` + `PreserveType_DoesNotRegisterConstructor` | OK |
| 12 | Negation (all-primitive event) | `Scenario12_AllPrimitiveEvent_EmitsExactlyOnePreserveTypeAndNoRegister` | OK |
| 13 | Abstract/interface property types skipped | `Scenario13_AbstractAndInterfaceProperties_AreSkipped` | OK |
| 14 | Multi-attribute cross-event dedupe | `Scenario14_SharedNestedRecord_EmittedOncePerClass` | OK |
| 15 | Static + instance handler both unconditional | `Scenario15_StaticAndInstanceHandlers_BothEmitPreservationUnconditionally` | OK |
| 16 | `Dictionary<K,V>` value type known gap | `Scenario16_DictionaryValueType_IsNotWalked` (regression guard) | OK |
| 17 | NF0501 + preservation co-occur | `Scenario17_MissingHandlerMethod_StillEmitsPreserveType` | OK |

---

## Independent Critique

### Walker split cleanliness
`DtoTypeWalker.cs` cleanly exposes `WalkFactoryReturn` and `WalkEventRoot` as two entry points sharing `IsDtoStructureCandidate`, `HasParameterlessCtor`, `UnwrapType`, and the private `WalkProperties` helper. `WalkEventRoot` uses a nested local function `WalkNested` for recursion that explicitly excludes the root-level "always-preserve-regardless-of-ctor" behavior from descendants. `FactoryGenerator.Types.cs:741-770` cleanly delegates to `WalkFactoryReturn` — all private helpers removed. No duplication.

### `EquatableArray<string>` usage
`RelayHandlerModel.cs:44,50` declares `EventDtoTypes` and `EventRecordTypes` as `EquatableArray<string>`. The surrounding model is an `internal sealed record` (line 9), so the compiler-synthesized `Equals` uses value equality on these fields. This is correct — incrementality cache hits/misses will be driven by actual string contents, not reference identity. The plan's callout about the latent `IReadOnlyList<string>` bug on other fields is explicit: it's NOT corrected here and is flagged as a future todo. Appropriate scope discipline.

### Dedupe correctness for Rule 4 / Scenario 14
The class-level dedupe uses a single `preservationVisited` HashSet shared across every `WalkEventRoot` invocation in the `foreach` loop. Inside `WalkEventRoot`, the root itself is added via `visited.Add(rootFqn)` (line 191). Nested recursion uses the same set. If `EventA` and `EventB` both contain `SharedNestedRecord`, the first walk adds `SharedNestedRecord` to `parameterizedTypes` and `visited`; the second walk sees it in `visited` and skips. Exactly-once emission — verified by Scenario 14 test using `Regex.Matches` count assertion.

### Ordering of preservation gathering vs. NF0501/NF0502 `continue`
`FactoryGenerator.RelayHandler.cs:75` is the `WalkEventRoot` call. The `continue` branches at lines 127 and 143 fire AFTER this line. Verified line-by-line. Even the degenerate case (attribute recognized but method match fails) still produces preservation — this is the ordering bug the plan called out as the critical fix, and it's correct.

### `global::` FQN rendering consistency
`DtoTypeWalker.cs:150,189,213` all use `SymbolDisplayFormat.FullyQualifiedFormat` which emits `global::`. `RelayHandlerRenderer.cs:54,58` renders `DtoConstructorRegistry.Register<{dtoType}>` and `DtoConstructorRegistry.PreserveType<{recordType}>` using those `global::`-prefixed strings directly. Cross-checked against `ClassFactoryRenderer.cs:1578`, `StaticFactoryRenderer.cs:158`, `InterfaceFactoryRenderer.cs:486` — all use the same pattern with `global::`-prefixed input. `global::` is valid C# syntax in generic type arguments. Consistent. Removing the `Unglobal` stripping mid-implementation was correct.

### `DiagnosticTestHelper` `LanguageVersion.Latest` change
Line 73 switched from whatever was default to `LanguageVersion.Latest`. Required because the new tests use generic attribute syntax `[FactoryEventHandler<OrderPlaced>]` which requires C# 11+. Pre-existing tests all still pass (577/577), indicating no test depended on parsing a construct that newer language versions reject. `Latest` is slightly volatile but acceptable for a test helper. No concern.

### `FactoryGenerator.cs` early-return condition
Line 103: `if (model.Entries.Count == 0 && model.EventDtoTypes.Count == 0 && model.EventRecordTypes.Count == 0) return;`. This correctly emits source when preservation is non-empty even if entries are empty (Scenario 17). The diagnostic loop at lines 96-99 runs UNCONDITIONALLY before this early-return, so diagnostics fire even if nothing is emitted. Correct ordering.

### Dead code / accidental behavior changes
Verified `FactoryGenerator.Types.cs` is reduced from prior line count — private `IsDtoCandidate`, `UnwrapType`, `DiscoverDtoTypesRecursive` are fully gone, replaced by `DtoTypeWalker.*` calls. No orphan helpers. `NestedDtoDiscoveryTests.cs` 13 Facts all still pass per the reported 577-test count (indicating the refactor preserved factory-return semantics).

### Minor polish observations (not blockers, not grade-affecting)

- `DtoTypeWalker.cs:159` calls `WalkProperties(namedType, WalkFactoryReturnNested)` where `WalkFactoryReturnNested` is a local function that recurses into `WalkFactoryReturn`. This works but creates a closure per top-level call. For a source generator running during compilation, cost is negligible. Not worth changing.
- `RelayHandlerRenderer.cs:52,56` — the loop variable names `dtoType`/`recordType` were noted by the architect as potentially shadowing model property names. Benign in practice; both are local scope and the properties are accessed via `model.` prefix.
- `DtoConstructorRegistryTests.cs` uses a single test record `ParameterizedRecord`. If someone adds a second test later that calls `PreserveType<ParameterizedRecord>()`, it still won't affect `TryCreate` behavior (because `PreserveType` doesn't touch the dictionary). No test-order dependency. Fine.
- `FactoryGenerator.RelayHandler.cs:183` — null-return condition is `entries.Count == 0 && diagnostics.Count == 0 && eventDtoTypesList.Count == 0 && eventRecordTypesList.Count == 0`. Note this includes `diagnostics.Count == 0`. That's slightly broader than the `FactoryGenerator.cs:103` emission condition (which excludes diagnostics). Not a bug — the `null` return at line 183 means "nothing to track at all, don't even create the model"; the emission condition at line 103 means "have a model but no source to emit". A class with an NF0101 diagnostic but zero `[FactoryEventHandler<T>]` attributes would produce a non-null model with diagnostics-only, reporting the diagnostic but not emitting source. Correct behavior.

### Scenario 9 (Raise<T> call-site preservation) verification

The architect's memory note acknowledges Scenario 9 cannot be unit-tested in the generator test set because preservation flows from the annotation on the `T` parameter at the call site — a trimmer/compiler-enforced behavior, not generator emission. Verification comes from:

1. **Compiler-enforced parity**: `IL2091` warning/error would fire if `IFactoryEvents.Raise<T>` annotates `T` but any implementation doesn't. The repo builds clean (0 errors, 2 unrelated warnings), proving all three sites (`IFactoryEvents`, `FactoryEventsDispatcher`, `RemoteFactoryEvents`) have matching annotations.
2. **Runtime trimming verification** is deferred to Step 6A (manual `dotnet publish -c Release` + ILSpy inspection). The plan explicitly defers this to the architect.

Acceptable coverage for a compile-time guarantee.

---

## Final Verdict

**Approved — Grade A.**

All rules and scenarios verified. No concrete changes required to reach or maintain Grade A. The implementation is ready for Step 6 (verification).

Recommendation to orchestrator: proceed to Step 6A (architect verification — builds, tests, manual trimming check via `dotnet publish`).
