# Interface Factory Authorization: Design Pedagogy + Anti-Pattern 2 Diagnostic

**Type:** Enhancement
**Status:** Complete
**Priority:** Medium
**Created:** 2026-04-23
**Last Updated:** 2026-04-23

---

## Problem

Two distinct gaps, both surfaced during an investigation into `[AuthorizeFactory<T>]` on interface factories:

**Gap 1 — Design pedagogy missing.** `[AuthorizeFactory<T>]` on interface factories is shipped and tested (25 integration tests in `InterfaceFactoryAuthTests.cs`, GAP-004 from a prior completed todo). But `src/Design/Design.Domain/` contained **zero examples** of this pattern — `AuthorizedOrder.cs` and `SecureOrder.cs` are class-factory examples, the `CLAUDE-DESIGN.md` Decision Table row on authorization pointed only at class-factory files. The result: anyone (human or agent) reasoning from the Design project first reached for class-factory semantics (`Create`/`Fetch`/`Delete` scopes) and concluded the interface-factory support was broken.

**Gap 2 — Anti-Pattern 2 was prose, not enforcement.** `CLAUDE-DESIGN.md:301` (Anti-Pattern 2) and `AllPatterns.cs:188` documented "factory-op attribute on interface method → duplicate generation" as a COMMON MISTAKE. Empirically verified via probe: `[Fetch]`/`[Create]`/`[Delete]` on an interface method produced 8 build errors (CS0111, CS0738, `Task<Task>` return types) with no diagnostic pointing at the real cause.

## Solution

Two narrow, coordinated deliverables:

1. **Design project additions** teaching the shipped interface-factory-auth pattern (Execute/Read scope + parameter matching + plain impl). Plus `CLAUDE-DESIGN.md` updates.
2. **NF0106 diagnostic** enforcing Anti-Pattern 2 — any factory-operation attribute on a `[Factory]` interface method → compile-time error with clear remediation. Applies regardless of `[AuthorizeFactory<T>]` presence.

---

## Skipped Steps

- Step 2.5 (Plan Review) — skipped; scope narrowed after VETO already clarified plan-vs-code contradictions.

---

## Plans

- [Interface Factory Auth Pedagogy + Anti-Pattern 2 Diagnostic](../plans/completed/interface-factory-authorize.md)

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-04-23
**Verdict:** VETOED (first draft), then resolved by plan rewrite + Design pedagogy updates.

### Summary

First-draft plan invented a model (op attrs on impl class + strict naming-convention impl resolution) that contradicted `CLAUDE-DESIGN.md` Critical Rule 4, Anti-Pattern 2, the Quick Decisions Table row 247, the "DID NOT DO THIS" comment in `AllPatterns.cs:178-201`, published `docs/interface-factory.md:95-116`, and the already-shipped GAP-004 implementation. Reviewer's core finding: `[AuthorizeFactory<T>]` on interfaces is not broken — it ships and works with `Execute`/`Read` scope + parameter-matched auth methods. My probe used the wrong auth-class (`IAuthorizedOrderAuth`, which scopes for class-factory CRUD) and mis-read the empty scope match as "broken."

### Resolution

1. **Dropped** the invented model: no op attrs on impl, no strict naming, no `Can{Method}` changes, no scope-mapping rewrite.
2. **Kept** only Rule 7 (diagnostic for factory-op attrs on interface methods — Anti-Pattern 2 enforcement, the sole valid item).
3. **Added** what was really missing: Design pedagogy for the shipped interface-factory-auth pattern (the reason the first draft went astray).

Full veto details preserved in the original entry below.

---

### 2026-04-23 — VETOED (original entry)

**Summary:** The plan's core premise (interface-factory `[AuthorizeFactory<T>]` is broken / silently ignores scope-specific auth methods) contradicts the shipped, tested, committed-generated behavior in `InterfaceFactoryAuthTests.cs` and `IAuthorizedServiceFactory.g.cs`. Plan's proposed scoping model (op attributes on the impl class) violates Critical Rule 4, Anti-Pattern 2, the Quick Decisions Table row for interface attributes, and the deliberate "DID NOT DO THIS" decision in `AllPatterns.cs:178-201`.

**Relevant Requirements Found:**
1. `CLAUDE-DESIGN.md:247` Quick Decisions Table — "Should interface methods have attributes? No — Interface IS the boundary."
2. `CLAUDE-DESIGN.md:783-799` Critical Rule 4 — Interface Factory Methods Need NO Attributes.
3. `CLAUDE-DESIGN.md:301-322` Anti-Pattern 2 — Attributes on interface factory methods cause duplicate generation.
4. `AllPatterns.cs:178-201` — Deliberate "DID NOT DO THIS: Require [Fetch], [Execute] on interface methods."
5. `AllPatterns.cs:232-239` — "DID NOT DO THIS: Require [Remote] on interface methods."
6. `docs/interface-factory.md:95-116` — Published "Critical Rule: No Attributes on Interface Methods."
7. `docs/interface-factory.md:118-137` — Anti-pattern: `[Factory]` on implementation class.
8. `docs/todos/completed/interface-factory-authorization-enforcement.md` (GAP-004) — prior completed todo.
9. `InterfaceFactoryAuthTests.cs` + `IAuthorizedServiceFactory.g.cs` — 25 passing tests proving the shipped pattern works.
10. Release notes `v0.14.0.md`, `v0.29.0.md` — shipped guarantees about interface-factory auth.

**Recommendations (all applied in rewrite):**
1. Keep: diagnostic for op attributes on `[Factory]` interface methods.
2. Drop: the "op attribute on impl class" scoping model and strict naming-convention impl resolution.

---

## Plan Review

Skipped — scope narrowed sufficiently after the VETO that a second reviewer pass wasn't warranted.

---

## Graded Review

Not run as a separate formal review step — the work was split into three independently-verified phases:
1. **Design pedagogy:** all 82 Design.Tests pass on net9.0 and net10.0 (was 72 before, +10 new tests in `InterfaceFactoryAuthorizationTests.cs`).
2. **NF0106 diagnostic:** 8 new diagnostic tests in `NF0106Tests.cs` pass on net9.0 and net10.0. Covers positive cases (Fetch/Create/Execute/Delete on interface method → NF0106), negative cases (clean interface factory and class factory → no NF0106), interaction with `[AuthorizeFactory<T>]`, and regression check for no cascade from duplicate codegen.
3. **Full regression:** 553 UnitTests, 563 IntegrationTests, 82 Design.Tests — all pass on both TFMs.

User acknowledgment: accepted on 2026-04-23 (proceeding to completion).

---

## Documentation

**Completed:** 2026-04-23
**Files updated:**

- `src/Design/CLAUDE-DESIGN.md` — Decision Table row 253 updated to split auth guidance by factory type; new row on scopes-for-interface-factories; new "Pattern 2a: Interface Factory with [AuthorizeFactory<T>]" subsection; Anti-Pattern 2 and Critical Rule 4 now reference NF0106 enforcement; Design Files table points at new file + new test.
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` — "COMMON MISTAKE" comment references NF0106.
- `src/Design/Design.Domain/Aggregates/AuthorizedRepository.cs` — new heavily-commented Design file, references NF0106.

Published docs (`docs/interface-factory.md`, release notes): deferred to a follow-up docs pass. Low-risk: existing published docs are still correct; NF0106 is an enforcement of already-documented prose.

**Developer deliverables:** none beyond the source changes already listed.

---

## Progress Log

### 2026-04-23
- Initial probe on Design project: interface factory + `[AuthorizeFactory<IAuthorizedOrderAuth>]`. With op attrs on interface: 8 build errors. Without op attrs: compiles, `HasAccess()` wired.
- **Misinterpreted** the probe result: concluded scoped auth was broken and designed a model with op attrs on impl class and strict naming-convention resolution.
- First draft of todo + plan created with that invented model.
- **Business-requirements-reviewer VETOED.** Key finding: interface-factory `[AuthorizeFactory<T>]` is already shipped and tested; I'd used the wrong auth class (class-factory CRUD scopes instead of Execute/Read) and misread empty-match as broken.
- Verified reviewer's claims: `InterfaceFactoryAuthTests.cs` exists with 25 passing tests.
- Rewrote todo + plan to the narrow scope: Design pedagogy + NF0106 diagnostic.
- Created `AuthorizedRepository.cs` in Design.Domain — heavily-commented pedagogy file teaching the shipped pattern.
- Created `InterfaceFactoryAuthorizationTests.cs` in Design.Tests — 10 tests covering auth-allowed/denied/parameter-matching/Can-methods/client-server round-trip.
- Updated `CLAUDE-DESIGN.md` — Decision Table, Pattern 2a subsection, Anti-Pattern 2 + Critical Rule 4 NF0106 references, Design Files table.
- Implemented NF0106 diagnostic in `src/Generator/DiagnosticDescriptors.cs` and `FactoryGenerator.Transform.cs`. Key subtleties: (a) only fire on real user-source attributes, not the synthetic `Execute` default the generator adds to every interface method; (b) use `methodSymbol.ContainingType.TypeKind` (not `serviceSymbol`) because for class factories with matching I-prefixed interfaces, `serviceSymbol` is reassigned to the interface and would misfire.
- Added `NF0106Tests.cs` — 8 diagnostic tests including regression coverage for clean interfaces and class factories.
- Surfaced two pre-existing generator bugs uncovered by the Design example (heterogeneous parameter signatures crash the auth-method forwarder; bare `Task` return type produces `Task<Task>`). Noted as KNOWN LIMITATION in the Design file; deferred as separate bug todos.
- Full test pass: 553 UnitTests + 563 IntegrationTests + 82 Design.Tests on net9.0 and net10.0. All green.

---

## Results / Conclusions

### What shipped

- Design project now teaches `[AuthorizeFactory<T>]` on interface factories with `AuthorizedRepository.cs` + `InterfaceFactoryAuthorizationTests.cs` + `CLAUDE-DESIGN.md` updates.
- NF0106 converts the Anti-Pattern 2 prose warning into a compile-time error with a clear remediation message. Fires for any `[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]` on a `[Factory]` interface method.
- No behavioral change to existing interface-factory `[AuthorizeFactory<T>]` semantics — the shipped Execute/Read + parameter-matching model was already correct.

### Lessons learned

- **Design pedagogy debt is expensive.** The missing `AuthorizedRepository.cs` led to a vetoed plan and a day of misdirection. When a pattern ships but isn't demonstrated in the Design project, the next human or agent reasoning about it will reach for an analogous pattern that IS demonstrated — and design against the wrong model. Moral: the Design project is the source of truth for requirements reviews; patterns only-tested-in-integration-tests are effectively invisible.
- **Probes are valuable even when "wrong."** The initial misinterpretation of the probe output (treating class-factory CRUD scopes as the pattern to use with interface factories) was a real failure mode a user could hit. Flagging it led directly to the Design pedagogy addition and the NF0106 diagnostic.
- **Know the synthetic attribute list before emitting diagnostics.** The generator silently adds `Execute` to every interface method's effective attribute list; diagnostics that iterate the attribute list must distinguish real source-placed attributes from these synthetic defaults, or fire false positives on every clean interface method.

### Deferred bug todos (to file separately)

1. **Heterogeneous parameter signatures** — when a parameterized `[AuthorizeFactory]` auth method's parameter type is absent from some interface methods, the generator emits `auth.Method( /* Missing Guid id */)` literally, producing CS7036. Should skip the auth method for those interface methods.
2. **Bare `Task` on interface method** — return type `Task` (void) produces `Task<Task>` in the factory (CS0738). Should preserve `Task` return type in delegate/factory emission.
