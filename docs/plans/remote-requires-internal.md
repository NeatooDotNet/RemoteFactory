# [Remote] Requires Internal Methods

**Date:** 2026-03-08
**Related Todo:** [Remote Requires Internal](../todos/remote-requires-internal.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-03-08 (Developer Review)

---

## Overview

Flip NF0105 so that `[Remote]` requires `internal` methods instead of `public`. This enables IL trimming of `[Remote]` method bodies on client assemblies. The generated factory interface method remains `public` (client-callable) regardless of the source method's access modifier. `[Remote] + public` becomes the compile-time error; `[Remote] + internal` becomes the correct pattern.

This is a **breaking change** -- any existing `[Remote] public` method will trigger NF0105 after the update.

---

## Business Requirements Context

**Source:** [Todo's Requirements Review](../todos/remote-requires-internal.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

1. **Critical Rule 10 / Anti-Pattern 8** (`src/Design/CLAUDE-DESIGN.md`): "[Remote] on internal methods is a contradiction -- emits diagnostic NF0105." This is the rule being **intentionally inverted** by the product owner. Currently documented in CLAUDE-DESIGN.md Critical Rules, Anti-Pattern 8, skill anti-patterns, and OrderLine.cs comments.

2. **Critical Rule 2** (`src/Design/CLAUDE-DESIGN.md`): "Factory Method Visibility Controls Guard Emission and Trimming." Current decision table has `[Remote] internal` as "N/A -- Diagnostic NF0105 contradiction." This row is being replaced with a working pattern.

3. **Factory Interface Visibility Rules** (`src/Design/CLAUDE-DESIGN.md`): Interface visibility derives from method accessibility. `AllMethodsInternal` => `internal interface`; any public method => `public interface`. The change introduces a new dimension: `[Remote]` on an `internal` method promotes that method's factory interface contribution to `public`.

4. **IL Trimming Documentation** (`docs/trimming.md`): Shows `[Remote] public` methods as guarded/trimmable. Must be updated to show `[Remote] internal` as the pattern.

5. **Client-Server Architecture** (`docs/client-server-architecture.md`): Shows `[Remote]` with `public` in code examples and visibility tables. Must be updated.

6. **Automatic [Remote] detection** (`src/Design/CLAUDE-DESIGN.md` Design Debt): "Must be explicit. Never." The change keeps `[Remote]` explicit -- no conflict.

7. **Static Factory Methods** (`src/Design/CLAUDE-DESIGN.md` Critical Rule 3): Static factory methods use `private static` with underscore prefix. They are processed in `BuildStaticFactory`, not `BuildClassFactory`, so NF0105 does not apply to them.

8. **Common Mistake 9** (`src/Design/CLAUDE-DESIGN.md`): "[Remote] on internal methods -- Contradictory." Must be replaced with "[Remote] on public methods."

9. **Attributes Reference** (`docs/attributes-reference.md`): Shows `[Remote]` with `public` methods via MarkdownSnippets. Source reference-app code must change.

#### Existing Tests

- `src/Tests/RemoteFactory.UnitTests/Diagnostics/NF0105Tests.cs` -- 4 tests verifying current NF0105 behavior:
  - `NF0105_RemoteInternal_ReportsDiagnostic` -- Expects error for `[Remote, Create] internal` -- must flip to no-error
  - `NF0105_RemoteInternalFetch_ReportsDiagnostic` -- Expects error for `[Remote, Fetch] internal` -- must flip to no-error
  - `NF0105_RemotePublic_NoDiagnostic` -- Expects no error for `[Remote, Create] public` -- must flip to error
  - `NF0105_InternalWithoutRemote_NoDiagnostic` -- Expects no error for `[Create] internal` -- unchanged (still no error)
  - `NF0105_RemoteInternalInsert_ReportsDiagnostic` -- Expects error for `[Remote, Insert] internal` -- must flip to no-error

### Gaps

**G1. Static factory [Remote] methods are unaffected.** Static factory methods (`private static` with `[Remote, Execute]` and `[Remote, Event]`) are processed in `BuildStaticFactory`, not `BuildClassFactory`. The NF0105 check exists only in `BuildClassFactory` (line 177). Static factories are completely unaffected. **No action needed.**

**G2. Constructor [Create] with [Remote].** Constructors with `[Create]` are included in `typeInfo.FactoryMethods` and pass through the NF0105 check. Under the new rule, `[Remote]` on a `public` constructor would trigger NF0105. However, `[Remote]` on constructors is unusual (PersonModel uses `[Create] public PersonModel()` without `[Remote]`). The NF0105 check applies uniformly to all non-static class factory methods, including constructors. **No special handling needed** -- the new condition (`IsRemote && !IsInternal`) applies correctly.

**G3. Entity duality with the new rule.** Under the new rule:
```csharp
[Remote, Fetch] internal Task<bool> Fetch(...)       // Aggregate root context -- promoted to public on interface
[Fetch]         internal void FetchAsChild(...)       // Child context -- stays internal on interface
```
Both methods are `internal` on the source class. The generator already tracks `IsRemote` separately from `IsInternal` on each `FactoryMethodModel`. The `needsInternalPrefix` logic in `ClassFactoryRenderer.RenderFactoryInterface` (line 119) must be updated to account for `[Remote]` promotion. **Requires implementation change.**

### Contradictions

None that warrant blocking. This is an intentional, product-owner-approved requirement change.

### Recommendations for Architect

1. The NF0105 condition flip is a single-line change in `FactoryModelBuilder.cs` line 178.
2. Interface visibility promotion (`[Remote]` makes an `internal` method contribute as `public` to the interface) requires changes to `AllMethodsInternal` and `needsInternalPrefix` logic.
3. The NF0105 diagnostic descriptor text must be updated.
4. All Design project `[Remote]` methods must change from `public` to `internal`.
5. Person example must change and trimming must be verified.
6. This is a breaking change requiring migration guidance.

---

## Business Rules (Testable Assertions)

### NF0105 Diagnostic Rules (Flipped)

1. WHEN a non-static class factory method has `[Remote]` AND `public` accessibility, THEN the generator EMITS diagnostic NF0105 (Error) and skips the method. -- Source: Inverts Requirement 1 (Critical Rule 10)

2. WHEN a non-static class factory method has `[Remote]` AND `internal` accessibility, THEN the generator DOES NOT emit NF0105 and generates factory code normally. -- Source: Inverts Requirement 1 (Critical Rule 10)

3. WHEN a non-static class factory method has no `[Remote]` AND `internal` accessibility, THEN the generator DOES NOT emit NF0105 (unchanged behavior). -- Source: Existing behavior, Requirement 2

4. WHEN a non-static class factory method has no `[Remote]` AND `public` accessibility, THEN the generator DOES NOT emit NF0105 (unchanged behavior). -- Source: Existing behavior, Requirement 2

### Guard Emission Rules (Unchanged Logic, New Input Domain)

5. WHEN a class factory method has `[Remote]` (which now requires `internal`), THEN the generated factory local method EMITS an `IsServerRuntime` guard. -- Source: Requirement 2 (Critical Rule 2), unchanged guard logic

6. WHEN a class factory method is `internal` without `[Remote]`, THEN the generated factory local method EMITS an `IsServerRuntime` guard. -- Source: Requirement 2 (Critical Rule 2), unchanged

7. WHEN a class factory method is `public` without `[Remote]`, THEN the generated factory local method DOES NOT emit an `IsServerRuntime` guard. -- Source: Requirement 2 (Critical Rule 2), unchanged

### Factory Interface Visibility Promotion Rules

8. WHEN a class factory method is `internal` AND has `[Remote]`, THEN that method's contribution to the factory interface is treated as `public` (no `internal` modifier on the interface member). -- Source: NEW (addresses Gap G3, confirmed in Clarification A1)

9. WHEN a class factory method is `internal` AND does NOT have `[Remote]`, THEN that method's contribution to the factory interface retains `internal` modifier (on a public interface) or no modifier (on an all-internal interface). -- Source: Requirement 3 (Factory Interface Visibility Rules), unchanged

10. WHEN all class factory methods are `internal` with no `[Remote]`, THEN the generated factory interface is `internal`. -- Source: Requirement 3, unchanged

11. WHEN any class factory method has `[Remote]` (which promotes to public), THEN the generated factory interface is `public`. -- Source: NEW (consequence of Rule 8 + Requirement 3)

12. WHEN a class factory has a mix of `[Remote] internal` and non-`[Remote] internal` methods, THEN the interface is `public`, `[Remote]` methods have no modifier, and non-`[Remote]` methods have `internal` modifier. -- Source: NEW (combination of Rules 8, 9, 11)

### Static Factory Isolation Rule

13. WHEN a static factory method has `[Remote, Execute]` or `[Remote, Event]` with `private static` accessibility, THEN NF0105 is NOT evaluated (static factories are processed in `BuildStaticFactory`, not `BuildClassFactory`). -- Source: Gap G1, confirmed by code analysis

### NF0105 Diagnostic Message

14. WHEN NF0105 is emitted, THEN the message reads: "Method '{0}' is marked [Remote] but has public accessibility. [Remote] methods must be internal so their bodies can be trimmed from client assemblies." -- Source: NEW (replaces current message)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Remote+public Create triggers NF0105 | `[Remote, Create] public void Create() {}` on a class factory | Rule 1 | NF0105 Error emitted, method name "Create" in message |
| 2 | Remote+internal Create passes | `[Remote, Create] internal void Create() {}` on a class factory | Rule 2 | No NF0105, factory code generated |
| 3 | Internal without Remote passes | `[Create] internal void Create() {}` on a class factory | Rule 3 | No NF0105, factory code generated |
| 4 | Public without Remote passes | `[Create] public void Create() {}` on a class factory | Rule 4 | No NF0105, factory code generated |
| 5 | Remote+public Fetch triggers NF0105 | `[Remote, Fetch] public void Fetch(int id) {}` on a class factory | Rule 1 | NF0105 Error emitted, method name "Fetch" in message |
| 6 | Remote+internal Fetch passes | `[Remote, Fetch] internal void Fetch(int id) {}` on a class factory | Rule 2 | No NF0105, factory code generated |
| 7 | Remote+public Insert triggers NF0105 | `[Remote, Insert] public Task Insert() {}` on IFactorySaveMeta class | Rule 1 | NF0105 Error emitted |
| 8 | Remote+internal Insert passes | `[Remote, Insert] internal Task Insert() {}` on IFactorySaveMeta class | Rule 2 | No NF0105 |
| 9 | Guard emitted for Remote internal | `[Remote, Create] internal void Create() {}` | Rule 5 | Generated factory local method contains `if (!NeatooRuntime.IsServerRuntime)` |
| 10 | Guard emitted for internal no Remote | `[Create] internal void Create() {}` | Rule 6 | Generated factory local method contains `if (!NeatooRuntime.IsServerRuntime)` |
| 11 | No guard for public no Remote | `[Create] public void Create() {}` | Rule 7 | Generated factory local method does NOT contain IsServerRuntime guard |
| 12 | Interface visibility promotion for Remote internal | `[Remote, Fetch] internal void Fetch()` + `[Fetch] internal void FetchChild()` on same class | Rules 8, 9, 11, 12 | Public interface with Fetch (no modifier) and FetchChild (internal modifier) |
| 13 | All-internal no-Remote stays internal interface | `[Create] internal void Create()` + `[Fetch] internal void Fetch()` on same class | Rule 10 | Internal factory interface |
| 14 | Static factory unaffected | `[Remote, Execute] private static Task<bool> _DoThing()` in static class | Rule 13 | No NF0105, static factory generated normally |
| 15 | NF0105 message text | `[Remote, Create] public void Create() {}` | Rule 14 | Message contains "public accessibility" and "must be internal" |
| 16 | Entity duality: both methods internal, only one Remote | `[Remote, Fetch] internal Task<bool> Fetch()` + `[Fetch] internal void FetchAsChild()` on same class | Rules 2, 8, 9, 12 | Both methods generated; Fetch promoted to public on interface; FetchAsChild has internal modifier |

---

## Approach

The change has five dimensions:

1. **Generator core** -- Flip the NF0105 condition and update the diagnostic message. Add `[Remote]` as a visibility promotion factor for factory interface generation.
2. **Design source of truth** -- Change all `[Remote]` methods from `public` to `internal` in the Design project.
3. **Person example** -- Same access modifier changes + trimming verification.
4. **Unit tests** -- Invert the NF0105 test expectations.
5. **Documentation** -- Update all docs and skill references.

The approach is to make the generator changes first (the NF0105 flip and interface promotion logic), then update the source code that consumes the generator (Design project, Person example), then update tests, and finally documentation.

---

## Design

### 1. Generator Changes

#### 1a. NF0105 Condition Flip

**File:** `src/Generator/Builder/FactoryModelBuilder.cs`, line 177-191

Change the condition from:
```
if (method.IsRemote && method.IsInternal)
```
to:
```
if (method.IsRemote && !method.IsInternal)
```

The `continue` (skip) behavior remains the same -- the method is skipped and not generated.

#### 1b. NF0105 Diagnostic Descriptor Update

**File:** `src/Generator/DiagnosticDescriptors.cs`, lines 52-59

Update:
- `title`: "[Remote] cannot be used with public methods"
- `messageFormat`: "Method '{0}' is marked [Remote] but has public accessibility. [Remote] methods must be internal so their bodies can be trimmed from client assemblies."
- `description`: "[Remote] marks a method as a client-to-server entry point, but the method body runs only on the server. Making [Remote] methods internal enables IL trimming of their bodies from client assemblies. The generated factory interface method remains public for client access. Change the method from public to internal."

#### 1c. Factory Interface Visibility Promotion

**File:** `src/Generator/Model/ClassFactoryModel.cs`

The `AllMethodsInternal` computed property currently checks:
```csharp
public bool AllMethodsInternal => Methods.Count > 0 && Methods.All(m => m.IsInternal);
```

With the new rule, a method that is `internal` + `[Remote]` should be treated as `public` for interface visibility purposes. The property needs to account for `[Remote]` promotion:
```csharp
public bool AllMethodsInternal => Methods.Count > 0 && Methods.All(m => m.IsInternal && !m.IsRemote);
```

Similarly, `HasPublicMethods`:
```csharp
public bool HasPublicMethods => Methods.Any(m => !m.IsInternal || m.IsRemote);
```

**File:** `src/Generator/Renderer/ClassFactoryRenderer.cs`, line 119

The `needsInternalPrefix` logic:
```csharp
bool needsInternalPrefix = !model.AllMethodsInternal && method.IsInternal;
```

Must account for `[Remote]` promotion:
```csharp
bool needsInternalPrefix = !model.AllMethodsInternal && method.IsInternal && !method.IsRemote;
```

A `[Remote] internal` method should NOT get the `internal` prefix on the interface -- it's promoted to public.

### 2. Design Project Changes

All `[Remote]` methods on class factories change from `public` to `internal`. Files affected:

- `src/Design/Design.Domain/Aggregates/Order.cs` -- Create, Fetch, Insert, Update, Delete (5 methods)
- `src/Design/Design.Domain/Aggregates/SecureOrder.cs` -- Create, Fetch, Insert, Update, Delete (5 methods)
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` -- Create, Fetch (2 methods on `ExampleClassFactory`)
- `src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs` -- Create (1 method; RunCommand is `public static` [Execute] -- different rules)
- `src/Design/Design.Domain/Services/CorrelationExample.cs` -- Create, Fetch on `AuditedOrder` (2 methods)
- `src/Design/Design.Domain/Entities/OrderLine.cs` -- Update comments referencing the old rule (no method changes needed -- OrderLine methods are already `internal` without `[Remote]`)

**Note on ClassFactoryWithExecute.cs:** The `RunCommand` method is `[Remote, Execute] public static`. Class factory `[Execute]` methods must be `public static` (they return the containing type). The NF0105 check fires before the Execute-specific handling (line 193), but `IsInternal` is `false` for `public static` methods so the flipped condition `method.IsRemote && !method.IsInternal` would trigger. However, `[Execute]` on a class factory is `public static`, and `IsInternal` is derived from `DeclaredAccessibility != Public`. Since `public static` has `DeclaredAccessibility = Public`, `IsInternal = false`, so `!method.IsInternal = true`. This means `[Remote, Execute] public static` WOULD trigger NF0105 under the new rule.

**This is a problem.** Class factory `[Execute]` methods are required to be `public static` by the generator (they return the containing type and the generator creates a matching public method). We need to exclude `[Execute]` methods from the NF0105 check. The condition should be:

```
if (method.IsRemote && !method.IsInternal && method.FactoryOperation != FactoryOperation.Execute)
```

Wait -- let me re-examine. Looking at `FactoryGenerator.Types.cs` line 472-473:
```csharp
this.IsStaticFactory = methodSymbol.IsStatic;
this.IsRemote = this.IsRemote || factoryOperation == FactoryOperation.Execute;
```

For class factory `[Execute]` methods, `IsRemote` is forced to `true` (line 473) AND `IsStaticFactory` is `true` (line 472). But `IsInternal` is determined by `DeclaredAccessibility` (line 674). For `public static` methods, `IsInternal = false`.

So the new NF0105 check `method.IsRemote && !method.IsInternal` would match. We need to also check `!method.IsStaticFactory` to exclude static methods on class factories:

```
if (method.IsRemote && !method.IsInternal && !method.IsStaticFactory)
```

This correctly excludes `[Execute] public static` on class factories while still catching `[Remote] public` on instance methods.

Actually, let me reconsider. Looking at the existing code structure, the NF0105 check is at line 177, and the Execute check with NF0102 is at line 193. The `continue` at line 190 means that if NF0105 fires, the method is skipped entirely and never reaches the Execute check. Under the current code, `[Remote, Execute] public static` methods have `IsRemote=true` and `IsInternal=false`. The current condition `method.IsRemote && method.IsInternal` is `true && false = false`, so NF0105 doesn't fire and the method proceeds to the Execute check at line 193. Good.

Under the new condition `method.IsRemote && !method.IsInternal`, this would be `true && true = true`, so NF0105 WOULD fire for `[Execute] public static` methods. This is wrong. The fix is to add `&& !method.IsStaticFactory` to the condition.

### 3. Person Example Changes

**File:** `src/Examples/Person/Person.DomainModel/PersonModel.cs`

Change three methods from `public` to `internal`:
- `[Remote] [Fetch] public async Task<bool> Fetch(...)` -> `internal`
- `[Remote] [Update] [Insert] public async Task Upsert(...)` -> `internal`
- `[Remote] [Delete] public async Task Delete(...)` -> `internal`

### 4. Updated Decision Table

The new decision table after the change:

| Source Method | `[Remote]` | Factory Interface | Guard | Client Trimmable |
|---|---|---|---|---|
| `public` | yes | **ERROR** (NF0105) | -- | -- |
| `public` | no | **public** | no | No |
| `internal` | yes | **public** (promoted) | yes | **Yes** |
| `internal` | no | **internal** | yes | Yes |

---

## Implementation Steps

### Phase 1: Generator Core Changes

1. Flip NF0105 condition in `FactoryModelBuilder.cs`:
   - Change `method.IsRemote && method.IsInternal` to `method.IsRemote && !method.IsInternal && !method.IsStaticFactory`
   - Update the comment above the condition

2. Update NF0105 diagnostic descriptor in `DiagnosticDescriptors.cs`:
   - Change title, messageFormat, and description to reference `public` instead of `internal`

3. Update factory interface visibility promotion in `ClassFactoryModel.cs`:
   - `AllMethodsInternal`: `Methods.All(m => m.IsInternal && !m.IsRemote)`
   - `HasPublicMethods`: `Methods.Any(m => !m.IsInternal || m.IsRemote)`

4. Update `needsInternalPrefix` in `ClassFactoryRenderer.cs`:
   - Add `&& !method.IsRemote` to the condition

5. Update NF0105 unit tests in `NF0105Tests.cs`:
   - `NF0105_RemoteInternal_ReportsDiagnostic` -> Flip to expect NO diagnostic
   - `NF0105_RemoteInternalFetch_ReportsDiagnostic` -> Flip to expect NO diagnostic
   - `NF0105_RemotePublic_NoDiagnostic` -> Flip to expect diagnostic
   - `NF0105_InternalWithoutRemote_NoDiagnostic` -> Unchanged
   - `NF0105_RemoteInternalInsert_ReportsDiagnostic` -> Flip to expect NO diagnostic
   - Add new test: `NF0105_RemotePublicExecute_NoDiagnostic` -- Verify `[Remote, Execute] public static` does NOT trigger NF0105
   - Add new test: Interface visibility promotion for `[Remote] internal` methods

6. Build and run all tests to verify Phase 1.

### Phase 2: Design Project + Example Updates

1. Change all `[Remote]` instance methods in Design.Domain from `public` to `internal`:
   - `Order.cs`: Create, Fetch, Insert, Update, Delete
   - `SecureOrder.cs`: Create, Fetch, Insert, Update, Delete
   - `AllPatterns.cs`: Create, Fetch on `ExampleClassFactory`
   - `ClassFactoryWithExecute.cs`: Create on `ClassExecuteDemo`
   - `CorrelationExample.cs`: Create, Fetch on `AuditedOrder`

2. Update comments in `OrderLine.cs` that reference the old rule.

3. Update comments and "DID NOT DO THIS" sections in Design.Domain files that reference the old `[Remote] public` pattern.

4. Change Person example methods from `public` to `internal`:
   - `PersonModel.cs`: Fetch, Upsert, Delete

5. Build and run all tests (Design + integration).

### Phase 3: Documentation Updates

1. Update `src/Design/CLAUDE-DESIGN.md`:
   - Critical Rule 2 decision table
   - Anti-Pattern 8 (flip the WRONG/RIGHT examples)
   - Common Mistake 9
   - Quick Reference code examples
   - Quick Decisions table
   - Factory Interface Visibility Rules section
   - IFactorySaveMeta code examples

2. Update `docs/trimming.md`:
   - Decision table
   - Code examples

3. Update `docs/client-server-architecture.md`:
   - Visibility table
   - Code examples

4. Update skill references:
   - `skills/RemoteFactory/references/anti-patterns.md`
   - `skills/RemoteFactory/references/trimming.md`
   - `skills/RemoteFactory/references/class-factory.md`
   - `skills/RemoteFactory/references/advanced-patterns.md`

5. Update `docs/attributes-reference.md`:
   - If using MarkdownSnippets, update reference-app source code first, then run `mdsnippets`
   - If hand-written, update code examples directly

6. Update `CLAUDE.md` project instructions:
   - Section "Understanding [Remote]" code examples

### Phase 4: Trimming Verification (Person Example)

1. Publish Person.Client with trimming enabled:
   ```bash
   dotnet publish src/Examples/Person/Person.Client/Person.Client.csproj -c Release
   ```
2. Verify that server-only types (EF Core references, `IPersonContext`) are trimmed from the output assembly.
3. Search for `PersonContext` or `DbContext` strings in the published Person.DomainModel.dll.
4. Document results.

---

## Acceptance Criteria

- [ ] NF0105 fires for `[Remote] public` non-static class factory methods
- [ ] NF0105 does NOT fire for `[Remote] internal` class factory methods
- [ ] NF0105 does NOT fire for `[Remote, Execute] public static` class factory methods
- [ ] NF0105 does NOT fire for static factory `[Remote, Execute]` / `[Remote, Event]` methods
- [ ] `[Remote] internal` methods produce `public` interface members (promotion)
- [ ] `internal` methods without `[Remote]` produce `internal` interface members (unchanged)
- [ ] Entity duality scenario works: `[Remote, Fetch] internal` + `[Fetch] internal` on same class produces correct interface
- [ ] Guard emission rules are unchanged (Remote or internal => guard; public no-Remote => no guard)
- [ ] All existing tests pass (with NF0105 tests updated)
- [ ] Design project compiles and all Design tests pass
- [ ] Person example compiles and runs
- [ ] Person example domain model trims correctly (verified with publish)
- [ ] NF0105 diagnostic message references `public` accessibility and `internal` requirement

---

## Dependencies

- None. This change is self-contained within the RemoteFactory repository.
- The `removeRemoteOnly` branch is the starting point (already removed `FactoryMode.RemoteOnly`).

---

## Risks / Considerations

1. **Breaking change**: All existing users with `[Remote] public` methods will get compile errors. Migration is mechanical (change `public` to `internal`) but affects every `[Remote]` method. Release notes must include a migration guide.

2. **Class factory [Execute] methods**: These are `public static` with implicit `IsRemote=true`. The NF0105 condition must exclude them via `!method.IsStaticFactory` to avoid false positives.

3. **ClassFactoryWithExecute.cs pattern**: The `Create` method changes to `internal` but `RunCommand` stays `public static`. The factory interface must correctly handle this mix (one promoted `internal` method + one `public static` method).

4. **Entity duality**: Both `Fetch` (aggregate root) and `FetchAsChild` (child context) become `internal`, distinguished only by `[Remote]`. The interface visibility logic must correctly promote only the `[Remote]` method. Verified by Test Scenario 16.

5. **SecureOrder**: All `[Remote]` methods change to `internal`. `SecureOrder` is a `public partial class` (not `internal`), so its `internal` methods are accessible within the assembly. The factory interface remains `public`. No issue.

6. **Reference-app MarkdownSnippets**: `docs/attributes-reference.md` pulls code via MarkdownSnippets from `src/docs/reference-app/`. The reference-app code also needs updating, then `mdsnippets` must be re-run.

---

## Architectural Verification

**Scope Table:**

| Pattern | Affected? | Change |
|---------|-----------|--------|
| Class Factory instance methods | Yes | `[Remote]` requires `internal` |
| Class Factory static Execute | No | `public static` exempt from NF0105 |
| Static Factory (Execute/Event) | No | Processed in `BuildStaticFactory` |
| Interface Factory | No | No method accessibility concept |
| Factory Interface Visibility | Yes | `[Remote]` promotes `internal` to `public` |
| Guard Emission | No | Logic unchanged; `IsInternal \|\| IsRemote` still correct |
| Ordinal Serialization | No | Not affected by method accessibility |
| DI Registration | No | Not affected by method accessibility |

**Verification Evidence:**

- NF0105 check location: `src/Generator/Builder/FactoryModelBuilder.cs` line 177-191 (only in `BuildClassFactory`)
- Static factory path: `src/Generator/Builder/FactoryModelBuilder.cs` lines 55-105 (`BuildStaticFactory` -- no NF0105 check)
- Interface visibility: `src/Generator/Model/ClassFactoryModel.cs` line 48 (`AllMethodsInternal`) and `src/Generator/Renderer/ClassFactoryRenderer.cs` line 110 + 119
- Guard emission: `src/Generator/Renderer/ClassFactoryRenderer.cs` lines 364, 778, 840, 1051, 1298 -- all check `method.IsInternal || method.IsRemote` (unchanged)
- `IsInternal` derivation: `src/Generator/FactoryGenerator.Types.cs` line 674 -- `methodSymbol.DeclaredAccessibility != Accessibility.Public`

**Breaking Changes:** Yes -- all `[Remote] public` methods become compile-time errors. Migration: change `public` to `internal` on `[Remote]` methods.

**Codebase Analysis:**
- `FactoryModelBuilder.cs`: Single condition flip + staticFactory exclusion at line 178
- `DiagnosticDescriptors.cs`: Text-only changes at line 52-59
- `ClassFactoryModel.cs`: Two computed property updates at lines 48, 52
- `ClassFactoryRenderer.cs`: One condition update at line 119
- Design project: 15 method access modifier changes across 5 files
- Person example: 3 method access modifier changes in 1 file
- NF0105 tests: 4 test expectation flips + 2 new tests
- Documentation: 10+ files need text updates

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Generator core + tests | developer | Yes | Clean context for generator changes, focused on 4 files + test file | None |
| Phase 2: Design project + Person example | developer | Yes | Different file set, different concern (source method modifiers, not generator logic) | Phase 1 (generator must accept `internal` before source methods change) |
| Phase 3: Documentation | developer | Yes | Large number of markdown files, no code logic; clean context avoids confusion with generator internals | Phase 2 (documentation must reflect final method signatures) |
| Phase 4: Trimming verification | developer | Yes | Publish + verify is a distinct task; needs clean context for build artifacts | Phase 2 (Person example must be updated) |

**Parallelizable phases:** Phases 3 and 4 can run in parallel (documentation and trimming verification are independent).

**Notes:**
- Phase 1 must complete and pass tests before Phase 2 begins (the generator must accept `[Remote] internal` before source methods change).
- Phase 2 must complete before Phase 3 (documentation must reference the correct final pattern).
- Phase 4 can start as soon as Phase 2 completes, independent of Phase 3.

---

## Developer Review

**Status:** Approved (with concerns addressed)
**Reviewed:** 2026-03-08

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `FactoryModelBuilder.BuildClassFactory` line 178: proposed condition `method.IsRemote && !method.IsInternal && !method.IsStaticFactory`. For `[Remote] public` non-static: `IsRemote=true`, `!IsInternal=true`, `!IsStaticFactory=true` => all true => diagnostic emitted, `continue` skips method. | NF0105 emitted, method skipped | Yes | Correct. The `!IsStaticFactory` guard is essential to avoid false positive on class `[Execute]` methods. |
| 2 | `FactoryModelBuilder.BuildClassFactory` line 178: proposed condition `method.IsRemote && !method.IsInternal && !method.IsStaticFactory`. For `[Remote] internal` non-static: `IsRemote=true`, `!IsInternal=false` => short-circuit false => no diagnostic, method proceeds to normal generation. | No NF0105, factory code generated | Yes | Correct. |
| 3 | `FactoryModelBuilder.BuildClassFactory` line 178: For `[Create] internal` without `[Remote]`: `IsRemote=false` => short-circuit false => no diagnostic. | No NF0105 | Yes | Unchanged behavior. |
| 4 | `FactoryModelBuilder.BuildClassFactory` line 178: For `[Create] public` without `[Remote]`: `IsRemote=false` => short-circuit false => no diagnostic. | No NF0105 | Yes | Unchanged behavior. |
| 5 | `ClassFactoryRenderer` lines 364, 778, 840, 1051, 1298: guard condition `method.IsInternal \|\| method.IsRemote`. For `[Remote] internal`: `IsInternal=true \|\| IsRemote=true` => `true` => guard emitted. | Guard emitted | Yes | Guard logic is unchanged. Both `IsInternal` and `IsRemote` are true, so the guard fires. |
| 6 | `ClassFactoryRenderer` lines 364, 778, 840, 1051, 1298: guard condition `method.IsInternal \|\| method.IsRemote`. For `internal` without `[Remote]`: `IsInternal=true \|\| IsRemote=false` => `true` => guard emitted. | Guard emitted | Yes | Unchanged behavior. |
| 7 | `ClassFactoryRenderer` lines 364, 778, 840, 1051, 1298: guard condition `method.IsInternal \|\| method.IsRemote`. For `public` without `[Remote]`: `IsInternal=false \|\| IsRemote=false` => `false` => no guard. | No guard | Yes | Unchanged behavior. |
| 8 | `ClassFactoryRenderer.RenderFactoryInterface` line 119: proposed condition `!model.AllMethodsInternal && method.IsInternal && !method.IsRemote`. For `[Remote] internal`: `!method.IsRemote = false` => `needsInternalPrefix = false` => no `internal` modifier on interface method (promoted to public). | No internal modifier on interface member | Yes | Correct promotion. |
| 9 | `ClassFactoryRenderer.RenderFactoryInterface` line 119: proposed condition `!model.AllMethodsInternal && method.IsInternal && !method.IsRemote`. For `internal` without `[Remote]`: `method.IsInternal=true`, `!method.IsRemote=true`. If `AllMethodsInternal=false` (mixed interface): `needsInternalPrefix=true` => `internal` modifier added. If `AllMethodsInternal=true`: `needsInternalPrefix=false` => no modifier (interface itself is internal). | `internal` modifier on public interface; no modifier on internal interface | Yes | Correct. Unchanged for non-`[Remote]` methods. |
| 10 | `ClassFactoryModel.AllMethodsInternal`: proposed `Methods.All(m => m.IsInternal && !m.IsRemote)`. For all `internal` no-`[Remote]`: every method has `IsInternal=true && !IsRemote=true` => `true` => internal interface. `ClassFactoryRenderer` line 110: `interfaceVisibility = "internal"`. | Internal factory interface | Yes | Correct. |
| 11 | `ClassFactoryModel.AllMethodsInternal`: proposed `Methods.All(m => m.IsInternal && !m.IsRemote)`. If any method has `[Remote]`: that method has `!IsRemote=false` => `All` returns `false` => `AllMethodsInternal=false`. `ClassFactoryRenderer` line 110: `interfaceVisibility = "public"`. | Public factory interface | Yes | Correct. |
| 12 | Combination of Rules 8, 9, 11. Mix of `[Remote] internal` + non-`[Remote] internal`. `AllMethodsInternal=false` (Rule 11). `[Remote]` method: `needsInternalPrefix=false` (Rule 8) => no modifier. Non-`[Remote]` method: `needsInternalPrefix=true` (Rule 9, mixed interface) => `internal` modifier. | Public interface; Remote method no modifier; non-Remote method has `internal` | Yes | Correct entity duality behavior. |
| 13 | Static factory methods are processed in `FactoryModelBuilder.BuildStaticFactory` (lines 42-105). This method has NO NF0105 check. Only `BuildClassFactory` has NF0105 (line 177). | No NF0105 evaluated | Yes | Confirmed by reading `BuildStaticFactory` -- no diagnostic check for NF0105. |
| 14 | `DiagnosticDescriptors.RemoteInternalContradiction`: proposed `messageFormat` = "Method '{0}' is marked [Remote] but has public accessibility. [Remote] methods must be internal so their bodies can be trimmed from client assemblies." | Message contains "public accessibility" and "must be internal" | Yes | Text-only change to descriptor. |

### Concerns

**Concern 1 (Critical -- Missing Scope): Integration test targets not in plan.**

The plan lists Design project files (5 files) and Person example (1 file) for access modifier changes in Phase 2, but does NOT mention the **15 integration test target files containing 95 `[Remote]` occurrences** in `src/Tests/RemoteFactory.IntegrationTests/TestTargets/`. These files use `[Remote]` on `public` instance methods and will all fail to compile after the NF0105 flip. Files affected:

- `TestTargets/FactoryRoundTrip/RoundTripTargets.cs` (10 occurrences)
- `TestTargets/Write/MixedWriteTargets.cs` (7 occurrences)
- `TestTargets/Parameters/RemoteComplexParameterTargets.cs` (16 occurrences)
- `TestTargets/Parameters/RemoteMultipleServiceTargets.cs` (10 occurrences)
- `TestTargets/TypeSerialization/InterfaceCollectionTargets.cs` (10 occurrences)
- `TestTargets/Execute/ClassExecuteTargets.cs` (8 occurrences -- mix of instance + static)
- `TestTargets/Execute/RemoteExecuteTargets.cs` (7 occurrences)
- `TestTargets/TypeSerialization/AggregateTargets.cs` (5 occurrences)
- `TestTargets/TypeSerialization/ValidationTargets.cs` (5 occurrences)
- `TestTargets/TypeSerialization/CoverageGapTargets.cs` (7 occurrences)
- `TestTargets/Parameters/RemoteCancellationTokenTargets.cs` (3 occurrences)
- `TestTargets/Parameters/RemoteParamsTargets.cs` (3 occurrences)
- `TestTargets/Parameters/RemoteNullableTargets.cs` (1 occurrence)
- `TestTargets/TypeSerialization/RecordTargets.cs` (2 occurrences)
- `TestTargets/Visibility/VisibilityIntegrationTargets.cs` (1 occurrence -- comment only, no code change)

These must be included in Phase 2. All `[Remote]` on `public` instance methods must change to `internal`. `[Remote, Execute] public static` methods must remain `public static` (exempt from NF0105).

**Concern 2 (Critical -- Missing Scope): Reference-app not in Phase 2.**

The plan mentions reference-app in Phase 3, item 5 as a documentation concern ("If using MarkdownSnippets, update reference-app source code first"). But the reference-app contains **47 files with 240 `[Remote]` occurrences** in `src/docs/reference-app/`. These are compilable `.cs` files that will fail to build after the NF0105 flip. They must be treated as source code changes in Phase 2, not documentation changes in Phase 3. The build will not pass at Phase 2's verification gate unless the reference-app is also updated.

**Resolution for Concerns 1 and 2:** These are scope omissions, not design flaws. The generator logic, interface promotion, and guard emission are all correct. Phase 2 must be expanded to include:
- All 15 integration test target files (change `[Remote] public` instance methods to `[Remote] internal`)
- All 47 reference-app files (same changes)
- Leave `[Remote, Execute] public static` methods unchanged in all files

**Concern 3 (Minor -- Clarification): `HasPublicMethods` property.**

`HasPublicMethods` is defined on `ClassFactoryModel` (line 52) but is not used anywhere in the renderer or builder. The plan proposes updating it for consistency, which is fine. However, the developer should be aware it is dead code and could be removed in a separate cleanup. No action needed for this plan.

**Concern 4 (Minor -- Naming): `RemoteInternalContradiction` field name.**

The plan updates the diagnostic descriptor text but does not rename the field `RemoteInternalContradiction` in `DiagnosticDescriptors.cs`. After the flip, this name is misleading -- the contradiction is now `[Remote] + public`, not `[Remote] + internal`. Consider renaming to `RemotePublicContradiction` or `RemoteRequiresInternal`. This is a non-breaking internal change (the field is `internal static`). The ID "NF0105" stays the same.

### Verdict

**Approved** -- with Concerns 1 and 2 incorporated into the Implementation Contract below. The generator design is sound, all assertion traces verify correctly, and the `!method.IsStaticFactory` exclusion for class `[Execute]` methods is correctly identified. The two missing scope items (integration test targets and reference-app) are mechanical access modifier changes, not design gaps.

---

## Implementation Contract

**Created:** 2026-03-08
**Approved by:** Developer Agent

### Verification Acceptance Criteria

All 14 business rule assertions traced and verified above. All 16 test scenarios from the plan must pass. Zero test failures across all target frameworks.

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `NF0105Tests.NF0105_RemotePublic_NoDiagnostic` (flipped to expect diagnostic) | Rename to `NF0105_RemotePublic_ReportsDiagnostic` |
| 2 | `NF0105Tests.NF0105_RemoteInternal_ReportsDiagnostic` (flipped to expect no diagnostic) | Rename to `NF0105_RemoteInternal_NoDiagnostic` |
| 3 | `NF0105Tests.NF0105_InternalWithoutRemote_NoDiagnostic` | Unchanged |
| 4 | Covered by scenario 1 and 3 implicitly (public without Remote is not a current test -- add one) | New test: `NF0105_PublicWithoutRemote_NoDiagnostic` |
| 5 | `NF0105Tests.NF0105_RemoteInternalFetch_ReportsDiagnostic` (flipped) | Rename to match new expectation |
| 6 | Covered by existing test flip (scenario 2 variant with Fetch) | |
| 7 | `NF0105Tests.NF0105_RemoteInternalInsert_ReportsDiagnostic` (flipped) | Rename and flip |
| 8 | Covered by flipped Insert test | |
| 9 | Verified via integration tests (round-trip tests exercise guard) | Existing integration tests |
| 10 | Verified via integration tests (`InternalCreateTarget`) | Existing integration test |
| 11 | Verified via integration tests (`PublicLocalCreateTarget`) | Existing integration test |
| 12 | New unit test: interface visibility promotion for `[Remote] internal` + non-`[Remote] internal` | Add to NF0105Tests or new test class |
| 13 | Verified via existing tests (no change to static factory handling) | |
| 14 | New test: `NF0105_RemotePublicExecute_NoDiagnostic` | Verify `[Remote, Execute] public static` exempt |
| 15 | Covered by scenario 1 (message assertion) | |
| 16 | Covered by scenario 12 (entity duality is a specific case of mixed interface) | |

### In Scope

**Phase 1: Generator Core Changes**
- [ ] `src/Generator/Builder/FactoryModelBuilder.cs` line 178: Change condition to `method.IsRemote && !method.IsInternal && !method.IsStaticFactory`
- [ ] `src/Generator/DiagnosticDescriptors.cs` lines 52-59: Update title, messageFormat, description text (optionally rename field to `RemotePublicContradiction`)
- [ ] `src/Generator/Model/ClassFactoryModel.cs` line 48: Change `AllMethodsInternal` to `Methods.All(m => m.IsInternal && !m.IsRemote)`
- [ ] `src/Generator/Model/ClassFactoryModel.cs` line 52: Change `HasPublicMethods` to `Methods.Any(m => !m.IsInternal || m.IsRemote)`
- [ ] `src/Generator/Renderer/ClassFactoryRenderer.cs` line 119: Change `needsInternalPrefix` to `!model.AllMethodsInternal && method.IsInternal && !method.IsRemote`
- [ ] `src/Tests/RemoteFactory.UnitTests/Diagnostics/NF0105Tests.cs`: Flip 4 tests, add 2+ new tests
- [ ] Checkpoint: `dotnet test src/Tests/RemoteFactory.UnitTests/RemoteFactory.UnitTests.csproj`

**Phase 2: Source Code Updates (Design + Examples + Tests + Reference-App)**
- [ ] `src/Design/Design.Domain/Aggregates/Order.cs`: Change 5 `[Remote]` instance methods from `public` to `internal`
- [ ] `src/Design/Design.Domain/Aggregates/SecureOrder.cs`: Change 5 `[Remote]` instance methods from `public` to `internal`
- [ ] `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs`: Change 2 `[Remote]` instance methods from `public` to `internal`
- [ ] `src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs`: Change 1 `[Remote, Create]` instance method from `public` to `internal` (leave `[Remote, Execute] public static` unchanged)
- [ ] `src/Design/Design.Domain/Services/CorrelationExample.cs`: Change 2 `[Remote]` instance methods from `public` to `internal`
- [ ] `src/Design/Design.Domain/Entities/OrderLine.cs`: Update comments referencing old rule
- [ ] `src/Examples/Person/Person.DomainModel/PersonModel.cs`: Change 3 `[Remote]` instance methods from `public` to `internal`
- [ ] All 15 integration test target files: Change `[Remote]` `public` instance methods to `internal` (leave `[Remote, Execute] public static` unchanged)
- [ ] All 47 reference-app files: Change `[Remote]` `public` instance methods to `internal` (leave `[Remote, Execute] public static` and static factory methods unchanged)
- [ ] Checkpoint: `dotnet build src/Neatoo.RemoteFactory.sln` and `dotnet test src/Neatoo.RemoteFactory.sln`

**Phase 3: Documentation Updates**
- [ ] `src/Design/CLAUDE-DESIGN.md`: Critical Rules, Anti-Patterns, Quick Decisions, Common Mistakes, code examples
- [ ] `docs/trimming.md`: Decision table and code examples
- [ ] `docs/client-server-architecture.md`: Visibility table and code examples
- [ ] `skills/RemoteFactory/references/anti-patterns.md`
- [ ] `skills/RemoteFactory/references/trimming.md`
- [ ] `skills/RemoteFactory/references/class-factory.md`
- [ ] `skills/RemoteFactory/references/advanced-patterns.md`
- [ ] `docs/attributes-reference.md`: Run `mdsnippets` after reference-app updated in Phase 2
- [ ] `CLAUDE.md`: Understanding [Remote] section

**Phase 4: Trimming Verification**
- [ ] Publish Person.Client with trimming, verify server-only types trimmed

### Out of Scope

- Renaming NF0105 diagnostic ID (stays "NF0105")
- Removing the unused `HasPublicMethods` property (separate cleanup)
- Changes to static factory processing (`BuildStaticFactory`)
- Changes to guard emission logic (`IsInternal || IsRemote` conditions)
- Changes to interface factory processing
- Changes to DI registration or serialization

### Verification Gates

1. After Phase 1: All generator unit tests pass (including flipped NF0105 tests and new tests)
2. After Phase 2: Full solution builds and all tests pass across all target frameworks (`dotnet test src/Neatoo.RemoteFactory.sln`)
3. After Phase 2 (reference-app): `dotnet build src/docs/reference-app/EmployeeManagement.sln`
4. After Phase 4: Person example trims correctly (verified with `dotnet publish`)
5. Final: All tests pass, all builds clean, no warnings from NF0105

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure that cannot be explained by the `[Remote]` access modifier change
- Architectural contradiction discovered (e.g., guard emission logic needs changing)
- Any `[Remote, Execute] public static` method triggering NF0105 (indicates the `!method.IsStaticFactory` exclusion is not working)
- Interface visibility regression (e.g., a `[Remote] internal` method NOT being promoted to public on the interface)

---

## Implementation Progress

**Started:** [date]
**Developer:** [agent name]

---

## Completion Evidence

**Reported:** [date]

- **Tests Passing:** [Output or summary]
- **Verification Resources Pass:** [Yes/No/N/A]
- **All Contract Items:** [Confirmed 100% complete]

---

## Documentation

**Agent:** [documentation agent name]
**Completed:** [date]

### Expected Deliverables

- [ ] `src/Design/CLAUDE-DESIGN.md` -- Critical Rules, Anti-Patterns, Quick Decisions, Common Mistakes, code examples
- [ ] `docs/trimming.md` -- Decision table and code examples
- [ ] `docs/client-server-architecture.md` -- Visibility table and code examples
- [ ] `skills/RemoteFactory/references/anti-patterns.md` -- Anti-Pattern 11
- [ ] `skills/RemoteFactory/references/trimming.md` -- Trimming table and recommendations
- [ ] `skills/RemoteFactory/references/class-factory.md` -- Code examples
- [ ] `skills/RemoteFactory/references/advanced-patterns.md` -- If applicable
- [ ] `docs/attributes-reference.md` -- Code examples (via reference-app + mdsnippets, or direct)
- [ ] `CLAUDE.md` -- Understanding [Remote] section
- [ ] Skill updates: Yes
- [ ] Sample updates: Yes (Person example, Design project -- covered in Phase 2)

### Files Updated

---

## Architect Verification

**Verified:** [date]
**Verdict:** [VERIFIED | SENT BACK]

**Independent test results:**
**Design match:**
**Issues found:**

---

## Requirements Verification

**Reviewer:** [agent name]
**Verified:** [date]
**Verdict:** [REQUIREMENTS SATISFIED | REQUIREMENTS VIOLATION]

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|

### Unintended Side Effects

### Issues Found
