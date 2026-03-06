# Explore IL Trimming Feature Switches for Remote-Only Code Separation

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-03-03
**Last Updated:** 2026-03-03 (plan created)

---

## Problem

Blazor WASM publish produces bloated output when server-only code (EF Core, repositories, database access) is referenced from shared assemblies. The current workaround is splitting assemblies (e.g., `Person.Dal` separate from `Person.Ef`), which adds project complexity.

RemoteFactory's generated factory code directly calls `[Remote]` methods (e.g., `DataPortal_Fetch(id, dbContext)`), which makes those methods — and their server-only dependencies — reachable to the IL trimmer even in WASM builds.

---

## Solution

Explore using .NET 9+ `[FeatureSwitchDefinition]` to have RemoteFactory's source generator emit feature-switch-guarded code. The trimmer would treat guarded branches as dead code in WASM builds, removing server-only methods and their transitive dependencies (EF Core, etc.) without requiring assembly splits.

Key questions to answer:
1. Can the generator emit `if (NeatooRuntime.IsServerRuntime)` guards around server-only method calls?
2. Does the trimmer reliably remove guarded code AND its transitive dependencies?
3. Does anything in Neatoo's base classes (virtual methods, interface dispatch) defeat member-level trimming?
4. What is the minimum .NET version requirement?

---

## Plans

- [Exploration: IL Trimming Feature Switches](../plans/explore-trimming-remote-only.md)

---

## Tasks

- [ ] Architect creates exploration plan with testable assertions
- [ ] Developer reviews plan
- [ ] Implementation and prototyping
- [ ] Architect verification

---

## Progress Log

### 2026-03-03
- Rewrote todo to follow updated project-todos skill methodology
- Prior research (2026-03-02) captured key finding: guards must be in RemoteFactory's generated code, not application code

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass
- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

_(pending exploration)_
