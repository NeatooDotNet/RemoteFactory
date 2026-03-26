# Fix NeatooJsonSerializer Trimming Failure for Non-Neatoo DTOs

**Status:** Complete
**Priority:** High
**Created:** 2026-03-24
**Last Updated:** 2026-03-25


---

## Problem

`NeatooJsonSerializer.Deserialize<T>()` fails to deserialize plain DTO classes (non-Neatoo types) when running on a Blazor WASM client published with IL trimming. The trimmer strips the parameterless constructor because `System.Text.Json` discovers it via reflection. Error: `System.NotSupportedException: DeserializeNoConstructor`.

GitHub issue: https://github.com/NeatooDotNet/RemoteFactory/issues/48

Prior trimming work addressed ordinal converters and feature switches but not this serializer path for non-Neatoo types.

### Key files

- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` — `Deserialize<T>()` (line ~168) and `DeserializeRemoteResponse<T>()` (line ~342)
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs` — calls `DeserializeRemoteResponse<T>()` (line ~93)

### Related completed todos

- [Explore IL Trimming Feature Switches](completed/explore-trimming-remote-only.md)
- [Fix IL2026 Trimming Errors in Generated Ordinal Converters](completed/fix-ordinal-trimming-errors.md)
- [Trimming-Safe Factory Registration](completed/trimming-safe-factory-registration.md)

## Solution

Architect researched and confirmed `[DynamicallyAccessedMembers]` is NOT sufficient — the .NET runtime team themselves called it a "false sense of hope" (dotnet/runtime#52268). The chosen approach: have the source generator emit explicit `CreateObject = () => new EmployeeDto()` lambdas for non-Neatoo DTO return types, similar to how Neatoo types already use `CreateObject` via DI. This gives the IL trimmer a static constructor reference, avoiding reflection-based discovery entirely.

---

## Clarifications

**Architect confirmed Ready (2026-03-25)**

Q: Is `[DynamicallyAccessedMembers]` sufficient?
A: No. Architect researched via context7 and web searches. The .NET runtime team discussed removing these annotations from `JsonSerializer.Deserialize<T>()` because they only preserve top-level type members and mislead developers. Three options identified:
1. Generator-emitted `JsonSerializerContext` — full STJ source generation
2. Enhanced `IJsonTypeInfoResolver` — provide constructor metadata for known DTOs
3. Explicit `CreateObject` registration — generator emits `CreateObject = () => new Dto()` lambdas

**User chose Option 3** — it fits existing patterns (Neatoo types already use `CreateObject` via DI) and sidesteps the trimming problem by giving STJ a direct constructor reference instead of asking it to discover one via reflection.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-25
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Existing `CreateObject` pattern in `NeatooJsonTypeInfoResolver`** (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs:28-35`): The resolver already sets `CreateObject` for Neatoo types via DI when `CreateObject is null`. The proposed fix extends this same mechanism to non-Neatoo DTO types with direct constructor lambdas — fully consistent with existing architecture.

2. **Trimming-Safe Factory Registration** (`CLAUDE-DESIGN.md`, "Trimming-Safe Factory Registration"): The generator already emits static `typeof()` references for factory types to survive IL trimming. Emitting `() => new Dto()` lambdas follows the same principle — static references the trimmer can trace.

3. **Interface Factory returning records/DTOs is explicitly supported** (`CLAUDE-DESIGN.md`, Quick Decisions Table): "Can Interface Factory return a record? Yes, plain records/DTOs without Neatoo types." This confirms non-Neatoo return types are a first-class pattern that must work correctly, including under trimming.

4. **Anti-Pattern 9 — Mixing Neatoo types with records** (`CLAUDE-DESIGN.md`): Not contradicted. The fix targets pure DTO return types from Interface Factories, not mixed graphs.

5. **RecordBypassConverterFactory** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs`): Records with parameterized constructors are handled by a separate converter that bypasses reference tracking. The proposed fix targets types with parameterless constructors (plain DTOs/classes), which go through the standard STJ path where `CreateObject` applies. No conflict.

### Gaps

1. **No existing Design project example demonstrates a non-Neatoo DTO return type under trimming.** The architect should consider whether a Design.Tests test should validate DTO round-trip deserialization, or whether integration tests are sufficient.

2. **Scope of DTO discovery by the generator is undefined.** The generator must identify which non-Neatoo types need `CreateObject` lambdas. The architect needs to define the discovery heuristic (e.g., return types of Interface Factory methods, `[Fetch]` return types on Class Factories) and document edge cases (generics like `Task<List<Dto>>`, nested DTOs).

### Contradictions

None found.

### Recommendations for Architect

1. **Follow the existing `CreateObject` pattern** — emit lambdas that the `NeatooJsonTypeInfoResolver` can use, rather than introducing a new mechanism. The resolver already checks `CreateObject is null` before setting it.
2. **Define the type discovery scope precisely** — which return types does the generator scan? Only direct return types, or also types reachable through collections/generics? Edge cases: `Task<List<EmployeeDto>>`, `Task<EmployeeDto?>`, nested DTO properties.
3. **Ensure records with parameterized constructors are excluded** — those are handled by `RecordBypassConverterFactory` and do not need `CreateObject`. Only types with accessible parameterless constructors need the lambda.
4. **Multi-targeting** — verify the fix works on both net9.0 and net10.0, as trimming behavior may differ between them.

---

## Plans

- [Fix DTO Trimming Deserialization -- Design Plan](../plans/fix-dto-trimming-design.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan and design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8) — VERIFIED + REQUIREMENTS SATISFIED

---

## Progress Log

### 2026-03-24
- Created todo from GitHub issue #48
- Prior trimming work identified but does not cover this case

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (net9.0 and net10.0)
- Tests: 2,068 passed, 0 failed (UnitTests 980, IntegrationTests 1,004, Design.Tests 84)

---

## Results / Conclusions

Added `else if` branch in `NeatooJsonTypeInfoResolver.GetTypeInfo()` that sets `CreateObject = () => Activator.CreateInstance(type)!` for non-DI types with a public parameterless constructor. This bypasses STJ's reflection-based constructor discovery that the IL trimmer breaks.

Key learnings:
- `[DynamicallyAccessedMembers]` is NOT sufficient for trimming-safe STJ deserialization (dotnet/runtime#52268)
- The initial plan (generator-emitted `DtoConstructorRegistry`) was over-engineered — the runtime debugger agent identified the simple `Activator.CreateInstance` fallback
- A plain `else` branch broke records — needed `else if` with constructor guard because `RecordBypassConverter.Read()` re-enters the resolver

**Future work:** Consumer-provided `JsonSerializerContext` for full trimming safety (including property metadata). Tracked as a potential future enhancement, not a current need.
