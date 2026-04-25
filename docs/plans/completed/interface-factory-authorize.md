# Interface Factory Auth Pedagogy + Anti-Pattern 2 Diagnostic

**Date:** 2026-04-23
**Related Todo:** [interface-factory-authorize](../../todos/completed/interface-factory-authorize.md)
**Status:** Complete
**Last Updated:** 2026-04-23

---

## Overview

Two narrow, independent deliverables:

1. **Design pedagogy** — add a `[Factory] [AuthorizeFactory<T>] public interface` example to `Design.Domain` demonstrating the shipped model (Execute/Read scope + parameter matching + plain impl), plus Design.Tests coverage and a Decision Table entry. The interface-factory-auth pattern ships, is tested in `InterfaceFactoryAuthTests.cs`, but is absent from the Design project — the reason a first-draft plan invented a wrong model.
2. **NF0xxx diagnostic** — enforce Anti-Pattern 2 (documented in prose at `CLAUDE-DESIGN.md:301` and `AllPatterns.cs:188`): a factory-operation attribute (`[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]`) on an interface method of a `[Factory]` interface emits an NF0xxx error with clear remediation text. Replaces cryptic CS0111/CS0738 duplicate-codegen failures.

No generator semantic changes beyond the new diagnostic. No changes to the `[AuthorizeFactory<T>]` model — the shipped behavior (Execute/Read scope, parameter-matched auth, `Can{Method}` generation) stays as-is.

---

## Current Behavior Map

### Interface-factory `[AuthorizeFactory<T>]` as shipped

- **Auth-class scope model.** Auth methods on the `[AuthorizeFactory<T>]` target type use `AuthorizeFactoryOperation.Execute` for "fires on any interface method call" and `AuthorizeFactoryOperation.Read` for read-style scopes. CRUD scopes (`Create`/`Fetch`/`Insert`/`Update`/`Delete`) are class-factory-specific — they don't match on interface factories because interface methods carry no operation.
- **Parameter matching.** Auth methods whose parameters match the interface-method signature by type are selected — e.g., `CanExecuteBoolFail(Guid id)` runs on any interface method whose parameters include a `Guid id`. Parameterless auth methods run on all methods.
- **Generated factory.** `IAuthorizedServiceFactory` (committed at `src/Tests/RemoteFactory.IntegrationTests/Generated/.../IAuthorizedServiceFactory.g.cs`) shows the shipped output: `Can{Method}` helpers per interface method, `Local{Method}` prepends scope+parameter-filtered auth guards, throws `NotAuthorizedException` on denial.
- **Impl class.** `AuthorizedService : IAuthorizedService` — plain class, no `[Factory]`, no op attributes. Server-side only.
- **Tests.** `src/Tests/RemoteFactory.IntegrationTests/FactoryGenerator/InterfaceFactory/InterfaceFactoryAuthTests.cs` — 25 tests covering pass/fail scenarios, bool/string auth returns, `Can{Method}` reflection, always-deny services.
- **Invariant:** this behavior shipped via completed todo GAP-004 (`docs/todos/completed/interface-factory-authorization-enforcement.md`). Release notes `v0.14.0.md`, `v0.29.0.md` reference it.

### Design project state

- `Design.Domain/Aggregates/AuthorizedOrder.cs` / `AuthorizedOrderAuth.cs` — class-factory + `[AuthorizeFactory<T>]` + CRUD scopes. Heavily commented.
- `Design.Domain/Aggregates/SecureOrder.cs` — class-factory + `[AspAuthorize]`.
- `Design.Domain/FactoryPatterns/AllPatterns.cs` — interface factory (`IExampleRepository`) without auth. Comments warn against op attrs on interface methods but give no auth example.
- **No example of `[Factory] [AuthorizeFactory<T>] public interface`** anywhere in Design.Domain.
- `CLAUDE-DESIGN.md:253` Decision Table row on authorization points only at `AuthorizedOrder.cs` and `SecureOrder.cs` — no interface reference.

### Anti-Pattern 2 current state

- `CLAUDE-DESIGN.md:301-322` documents the pattern as WRONG.
- `AllPatterns.cs:178-201` documents the pattern as "COMMON MISTAKE" and "DID NOT DO THIS."
- `docs/interface-factory.md:95-116` (published) documents it.
- **No generator diagnostic.** Probe: `[Fetch]` on interface method produces CS0111 duplicates and CS0738 return-type mismatches — 8 build errors, zero hint they're triggered by the interface-method attribute.

---

## Out of Scope / Invariants

- **All 25 `InterfaceFactoryAuthTests.cs` tests continue to pass unchanged.**
- **Shipped generated output for `IAuthorizedServiceFactory` must not change semantically.** Whitespace/ordering changes fine if the refactor incidentally touches it; semantic content (guard methods, parameter matching, `Can{Method}` emission) is invariant.
- **No changes to the `[AuthorizeFactory]` attribute, scopes, or parameter-matching logic.** Design pedagogy describes shipped behavior; does not modify it.
- **No changes to class-factory auth.** `AuthorizedOrder` + `AuthorizedOrderAuth` stay as-is.
- **No impl-class attribute model.** Impl classes remain "plain service classes" per `docs/interface-factory.md`.
- **No strict naming-convention impl resolution.** `RegisterMatchingName` remains an opt-in DI helper, not a generator contract.
- **Class-factory op attributes unchanged.** `[Create]`/`[Fetch]`/etc. on class-factory methods continue to work identically.
- **`[Execute]` on static factories unchanged.**
- **Release-note-level compatibility.** v0.14.0 and v0.29.0 shipped guarantees hold.

---

## Approach

### Deliverable 1: Design pedagogy

- Add `src/Design/Design.Domain/Aggregates/AuthorizedRepository.cs` containing:
  - `IRepositoryAuth` — auth class with `[AuthorizeFactory(Execute)]` parameterless and parameterized auth methods demonstrating the model. Static flags or DI-injected state for test configurability (mirror `AuthorizedOrderAuth` style).
  - `[Factory] [AuthorizeFactory<IRepositoryAuth>] public interface IAuthorizedRepository` — bare interface, no method attributes.
  - `public class AuthorizedRepository : IAuthorizedRepository` — plain impl, no `[Factory]`, no op attributes. Server-side.
- Pedagogy comments cover, at minimum:
  - Why `Execute`/`Read` scopes (not `Create`/`Fetch`/`Delete`) apply to interface factories.
  - How parameter matching selects auth methods.
  - Why impl is plain (no `[Factory]`, no op attributes).
  - How `Can{Method}` is generated on the factory interface.
  - Explicit "DO NOT" callouts pointing at Anti-Pattern 2 / Critical Rule 4.
  - Cross-reference to `AuthorizedOrder.cs` for class-factory CRUD auth, contrasting the two patterns.
- Add `src/Design/Design.Tests/FactoryTests/InterfaceFactoryAuthorizationTests.cs` — tests mirroring the key scenarios from `InterfaceFactoryAuthTests.cs` but using the new Design types. Not a duplicate of the integration tests — these are pedagogical demonstrations that the comments in the Design file are accurate.
- Update `src/Design/CLAUDE-DESIGN.md`:
  - Decision Table row 253 ("Which authorization approach?"): add `AuthorizedRepository.cs` reference alongside `AuthorizedOrder.cs` / `SecureOrder.cs`.
  - Add a short subsection under Authorization explaining the interface-factory auth model (Execute/Read + parameter matching) with pointer to `AuthorizedRepository.cs`.

### Deliverable 2: NF0xxx diagnostic

- Identify next free NF0xxx number (probably NF0106 or NF03xx range — confirm in Step 3).
- Emit error when a factory-operation attribute (`[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]`) appears on a method of a `[Factory]` interface. Applies regardless of whether `[AuthorizeFactory<T>]` is present.
- Diagnostic message: clear remediation pointing at Anti-Pattern 2 and `AllPatterns.cs` (or `docs/interface-factory.md`). Example: *"Factory-operation attribute '[{X}]' on interface method '{Method}'. Interface factory methods must have no operation attributes — the interface IS the remote boundary. See Anti-Pattern 2 in CLAUDE-DESIGN.md."*
- Emit at the attribute's source location so the IDE surface-shows it at the right spot.
- Suppress downstream codegen for the offending interface method (or the whole interface) to prevent the CS0111/CS0738 cascade from obscuring NF0xxx. **Design call:** emit diagnostic only, still attempt interface-factory codegen for the method (stripping the bogus op attribute). Keeps remediation narrow and avoids cascading secondary errors.
- Update comments in `CLAUDE-DESIGN.md:301-322`, `AllPatterns.cs:178-201`, and `docs/interface-factory.md:95-116` to reference the new NF0xxx code.

---

## Design

### Generator changes

**Where the diagnostic fires.** Inside the shared method-processing path (likely `TypeFactoryMethods` in `FactoryGenerator.Transform.cs`, or wherever factory-op attributes are first recognized per-method), add a pre-check: if the declaring type is a `TypeKind.Interface`, emit NF0xxx and skip op-attribute processing for that method.

**Codegen downstream of diagnostic.** The interface-factory path should proceed as if the method had no op attribute (the existing correct behavior for bare interface methods). This is the "suppress cascading errors" outcome.

**Testing.** Add a generator diagnostic test at `src/Tests/RemoteFactory.UnitTests/GeneratorDiagnostics/` (or wherever diagnostic tests live — confirm in Step 3). Two cases:
- `[Fetch]` on interface method of `[Factory]` interface (no auth) → NF0xxx.
- `[Fetch]` on interface method of `[Factory] [AuthorizeFactory<T>]` interface → NF0xxx.

Regression case: interface factories without any op attribute on methods → no NF0xxx, codegen unchanged.

### Design project file structure

```
src/Design/Design.Domain/Aggregates/
├── AuthorizedOrder.cs              (existing — class factory + CRUD auth)
├── AuthorizedOrderAuth.cs          (existing)
├── AuthorizedRepository.cs         (NEW — interface factory + Execute/Read auth)
│   ├── IRepositoryAuth (auth class)
│   ├── IAuthorizedRepository (interface factory)
│   └── AuthorizedRepository (plain impl)
├── SecureOrder.cs                  (existing — class factory + AspAuthorize)
```

A single-file approach for the new example parallels how `InterfaceFactoryAuthTests.cs` co-locates its types, and keeps the pedagogy in one place.

### CLAUDE-DESIGN.md update

- Decision Table row 253: value becomes `` `[AuthorizeFactory<T>]` for domain-specific rules (class: `AuthorizedOrder.cs`; interface: `AuthorizedRepository.cs`); `[AspAuthorize]` for ASP.NET Core policies ``.
- Add a new subsection under the Authorization section: "Authorization on Interface Factories" — 10–20 lines explaining Execute/Read + parameter matching and pointing at `AuthorizedRepository.cs`.
- Anti-Pattern 2: add "Enforced by NF0xxx" line.

---

## Business Rules (Testable Assertions)

1. WHEN a `[Factory]` interface method has any factory-operation attribute (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`), THEN the generator emits diagnostic NF0xxx with a message referencing Anti-Pattern 2. — Source: NEW (enforces `CLAUDE-DESIGN.md:301` Anti-Pattern 2).
2. WHEN NF0xxx is emitted, THEN the generator does NOT produce duplicate method/delegate definitions for that interface. The only compile-time failure surfaced is NF0xxx. — Source: NEW (regression guard for the 8-error probe cascade).
3. WHEN a `[Factory]` interface has no op attributes on any method, THEN no NF0xxx is emitted and generated output is identical to pre-change output. — Source: invariant.
4. WHEN a `[Factory] [AuthorizeFactory<T>]` interface has no op attributes on any method, THEN generated output is semantically identical to pre-change output — scope-filtered auth via `Execute`/`Read`, parameter matching, `Can{Method}` generation all unchanged. — Source: invariant (shipped behavior).
5. WHEN the `AuthorizedRepository.cs` Design file is compiled, THEN it produces a working `IAuthorizedRepositoryFactory` with auth guards and `Can{Method}` helpers, demonstrating the shipped pattern. — Source: NEW (pedagogy).
6. WHEN `InterfaceFactoryAuthorizationTests.cs` (Design.Tests) runs, THEN all scenarios pass, validating that the Design file's comments accurately describe the generator's behavior. — Source: NEW (pedagogy).

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Op attr on interface method (no auth) | `[Factory] public interface IFoo { [Fetch] Task<int> GetAsync(); }` | 1, 2 | NF0xxx emitted; no CS0111/CS0738 |
| 2 | Op attr on interface method (with auth) | Same, plus `[AuthorizeFactory<SomeAuth>]` on interface | 1, 2 | NF0xxx emitted; no duplicate codegen |
| 3 | Multiple op attrs across methods | Two interface methods, each with `[Fetch]` | 1, 2 | Two NF0xxx diagnostics (one per method) |
| 4 | Clean interface factory (no auth) | `[Factory] public interface IExampleRepository { ... }` (existing) | 3 | No NF0xxx; output semantically unchanged |
| 5 | Clean interface factory (with auth, shipped pattern) | `IAuthorizedService` (existing, in Integration tests) | 4 | No NF0xxx; 25 tests continue to pass |
| 6 | Design example compiles and auth works | `AuthorizedRepository` auth-allowed state | 5 | Method calls succeed, return expected data |
| 7 | Design example auth deny (bool) | `AuthorizedRepository` with bool auth denying | 5, 6 | `NotAuthorizedException` thrown |
| 8 | Design example auth deny (string) | `AuthorizedRepository` with string auth denying | 5, 6 | `NotAuthorizedException` with message |
| 9 | Design example `Can{Method}` reflects auth state | `AuthorizedRepository` with varying auth flags | 5, 6 | `factory.Can{Method}(id).HasAccess` matches |
| 10 | Design example parameter matching | Auth method `CanExec(Guid id)` applied only to methods with `Guid id` | 5, 6 | Denial only for methods whose signature matches auth method |

---

## Domain Model Behavioral Design

N/A — no domain model changes. Generator + Design pedagogy only.

---

## Design Decisions

### 2026-04-23
- **Decision:** Rewrite scope to pedagogy + single diagnostic only. Drop all invented model (op attrs on impl, strict naming, `Can{Method}` changes).
- **Alternative considered:** Extend the `[AuthorizeFactory<T>]` model with new per-method scoping.
- **Reason:** Business-requirements-reviewer veto established that the shipped model is correct and tested. The root problem was Design-project pedagogy debt (no interface-factory-auth example), which led me to invent an incorrect model. Fix the pedagogy; don't extend a model that already works.

### 2026-04-23
- **Decision:** Single-file Design example (`AuthorizedRepository.cs` containing auth class + interface + impl).
- **Alternative considered:** Three separate files (`AuthorizedRepository.cs`, `AuthorizedRepositoryAuth.cs`, `AuthorizedRepositoryImpl.cs`).
- **Reason:** The pedagogical story is one contiguous teaching; splitting across files fragments the narrative. `InterfaceFactoryAuthTests.cs` co-locates types the same way (auth class + interface + impl in one file); Design file mirrors that.

### 2026-04-23
- **Decision:** NF0xxx diagnostic applies regardless of `[AuthorizeFactory<T>]` presence.
- **Alternative considered:** Scope diagnostic only to auth'd interfaces.
- **Reason:** Anti-Pattern 2 is a property of interface factories in general, not auth'd interfaces. The cryptic CS0111 failure mode happens today on any interface factory with an op attribute. Universal diagnostic is cleaner and converts the existing prose documentation into enforcement.

### 2026-04-23
- **Decision:** Diagnostic emits but interface-factory codegen proceeds (stripping the bogus op attribute).
- **Alternative considered:** Skip codegen entirely for the affected interface to force the user to remediate.
- **Reason:** Stripping the attribute and proceeding keeps the error surface narrow — user sees one NF0xxx, fixes it, moves on. Skipping codegen would cascade "type not found" errors in consumers of the interface, obscuring the real cause.

### 2026-04-23
- **Decision:** Reuse style/conventions from `AuthorizedOrder.cs` for the pedagogy file.
- **Alternative considered:** New pedagogical style.
- **Reason:** Consistency across Design files helps readers navigate.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — authoritative factory-pattern reference; needed to ensure the pedagogy comments align with skill-file conventions.

---

## Implementation Steps

1. **Read `InterfaceFactoryAuthTests.cs` and `IAuthorizedServiceFactory.g.cs` end-to-end.** Confirm the shipped model's exact semantics for the pedagogy write-up.
2. **Locate next free NF0xxx number.** Inspect existing diagnostics in `src/Generator/Diagnostics/` (or equivalent); pick the next slot.
3. **Add NF0xxx diagnostic.** In the generator's method-processing path, detect op attributes on interface methods and emit NF0xxx. Strip the attribute so downstream interface-factory codegen proceeds.
4. **Generator unit tests.** Scenarios 1, 2, 3, 4, 5.
5. **Write `AuthorizedRepository.cs` Design file.** Auth class (`IRepositoryAuth`), interface (`IAuthorizedRepository`), impl (`AuthorizedRepository`). Heavy pedagogy comments.
6. **Write `InterfaceFactoryAuthorizationTests.cs` Design test.** Scenarios 6, 7, 8, 9, 10.
7. **Update `CLAUDE-DESIGN.md`.** Decision Table row 253, Anti-Pattern 2 (reference NF0xxx), new "Authorization on Interface Factories" subsection.
8. **Update comment references.** `AllPatterns.cs:178-201` "COMMON MISTAKE" → reference NF0xxx. `docs/interface-factory.md:95-116` → reference NF0xxx.
9. **Full test pass** on net9.0 and net10.0. Confirm: all 25 `InterfaceFactoryAuthTests.cs` tests pass, new Design tests pass, new generator tests pass, no regression in class-factory auth tests.

---

## Acceptance Criteria

- [ ] NF0xxx diagnostic emits for every factory-op attribute on an interface method of a `[Factory]` interface (Rule 1).
- [ ] No CS0111/CS0738 cascade when NF0xxx fires (Rule 2).
- [ ] Interface factories without op attributes emit no NF0xxx and produce identical output (Rules 3, 4).
- [ ] `InterfaceFactoryAuthTests.cs` — all 25 tests still pass.
- [ ] `AuthorizedRepository.cs` exists with heavy pedagogy comments teaching Execute/Read + parameter matching + plain impl model.
- [ ] `InterfaceFactoryAuthorizationTests.cs` (Design.Tests) exists with scenarios 6–10 and all pass.
- [ ] `CLAUDE-DESIGN.md` Decision Table row 253 references `AuthorizedRepository.cs`.
- [ ] `CLAUDE-DESIGN.md` has a new "Authorization on Interface Factories" subsection.
- [ ] Anti-Pattern 2 references NF0xxx in both `CLAUDE-DESIGN.md` and `AllPatterns.cs` comments.
- [ ] `docs/interface-factory.md` Critical Rule 4 references NF0xxx.
- [ ] `dotnet test` passes on net9.0 and net10.0 across all projects.
- [ ] Class-factory auth tests unchanged and passing.

---

## Deferred Scope

- Any change to the `[AuthorizeFactory<T>]` scope model.
- Per-impl-method scoping.
- Strict naming-convention impl resolution.
- `IFactorySaveMeta` on interface factories.
- `Authorized<T>` return wrapping for interface factories.
- Publishing `docs/interface-factory.md` updates beyond NF0xxx reference — deeper doc work is out of scope here; if the Design change reveals the published docs need more, that's a follow-up.

---

## Dependencies

- None external. Changes confined to `src/Generator/`, `src/Design/`, test projects, and `CLAUDE-DESIGN.md` + minor doc references.

---

## Risks / Considerations

- **Diagnostic number collisions.** Verify the chosen NF0xxx number isn't used in any existing test or doc.
- **Attribute stripping robustness.** Stripping the bogus op attribute and proceeding requires the generator's method-processing path to behave correctly when the attribute list is mutated. If the path assumes immutability, Step 3 might need a different approach (e.g., a flag on the per-method model that codegen honors).
- **Test integration.** `InterfaceFactoryAuthTests.cs` uses `ClientServerContainers.Scopes()`; Design.Tests uses a different infrastructure (based on `AuthorizationTests.cs` pattern). Align the new Design test with Design.Tests conventions, not Integration conventions.
- **Whitespace / ordering in generated output.** The generator change should not disturb the existing `IAuthorizedServiceFactory.g.cs` output. Pre-change snapshot + post-change diff at Step 9.
- **Published `docs/interface-factory.md`** is Jekyll-based; updating it needs the right front-matter and snippet-markers. The NF0xxx reference is a one-line addition; larger revisions deferred.
- **RemoteFactory skill file** — if the skill teaches `[AuthorizeFactory]` for interfaces, check that its example matches the new Design pedagogy. This is a read-check, not a planned change.
