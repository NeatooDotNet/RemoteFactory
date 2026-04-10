# Delete Dead Generator Code

**Status:** Complete
**Priority:** Low
**Created:** 2026-04-06
**Last Updated:** 2026-04-06


---

## Problem

The generator contains a dead code path from the old rendering approach that is no longer wired up. The `GenerateExecute` method in `FactoryGenerator.cs` (line 84) and the entire class hierarchy it uses (`FactoryMethod`, `ReadFactoryMethod`, `WriteFactoryMethod`, `SaveFactoryMethod`, `CanFactoryMethod` in `FactoryGenerator.Types.cs`) are never called. The active code path uses `FactoryModelBuilder.Build` + `FactoryRenderer.Render` (dispatching to `ClassFactoryRenderer`, `InterfaceFactoryRenderer`, `StaticFactoryRenderer`).

This dead code includes its own authorization rendering (`MakeAuthCall` at line 642 of `FactoryGenerator.Types.cs`) which duplicates the active `RenderAuthMethodCall` in the renderers. Keeping it creates confusion about which code path is live and makes maintenance harder.

## Solution

Delete the dead code:

- `FactoryGenerator.cs`: Remove `GenerateExecute` method and any helper methods only called by it
- `FactoryGenerator.Types.cs`: Remove the `FactoryMethod` class hierarchy (`FactoryMethod`, `ReadFactoryMethod`, `WriteFactoryMethod`, `SaveFactoryMethod`, `CanFactoryMethod`, `FactoryText`) and `MakeAuthCall` — anything not referenced by the active `TypeInfo`/`TypeFactoryMethodInfo`/`TypeAuthMethodInfo`/`MethodInfo`/`MethodParameterInfo` types that the builder and transform still use
- Verify no compile errors after removal
- Run full test suite to confirm nothing depended on the dead code

---

## Requirements Review

**Verdict:** Pending
**Reviewed:** 
**Summary:** 

---

## Plans

None yet.

---

## Tasks

- [x] Identify all dead code (trace from `GenerateExecute` and `FactoryMethod` hierarchy)
- [x] Delete dead code
- [x] Build and test

---

## Progress Log

### 2026-04-06
- Created todo. Dead code identified during auth-target-param-support work when investigating whether both old and new renderers needed fixes. Only the new renderer (`ClassFactoryRenderer`) is active.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 2 warnings (pre-existing Blazor WASM warnings)
- Tests: 2,148 passed (532 unit x2 TFMs + 539 integration x2 TFMs), 6 skipped (performance), 0 failures

---

## Results / Conclusions

Deleted ~1,040 lines of dead generator code across two files:

**`FactoryGenerator.cs`** (from 895 to 192 lines):
- `GenerateExecute` — old rendering code path, never called from `Initialize`
- `EventMethodResult` struct — only used by dead event methods
- `GenerateEventMethodForNonStatic` — never called
- `GenerateEventMethod` — only called from dead `GenerateExecute`
- `GenerateInterfaceFactory` — old interface factory rendering, never called from `Initialize`
- `WithStringBuilder` — utility only used by dead methods
- Removed unused `using` statements (`System.Text`, `System.Reflection`, `System.Text.RegularExpressions`)

**`FactoryGenerator.Types.cs`** (from 2,088 to 1,043 lines):
- `MakeAuthCall` method on `TypeAuthMethodInfo` — only called from dead `FactoryMethod` subclasses
- `FactoryMethod` abstract class and all subclasses (`ReadFactoryMethod`, `WriteFactoryMethod`, `SaveFactoryMethod`, `InterfaceFactoryMethod`, `CanFactoryMethod`)
- `FactoryText` class — only used by dead `GenerateInterfaceFactory`
- Removed unused `using System.Text`

The active code path (`FactoryModelBuilder.Build` + `FactoryRenderer.Render`) was unaffected.
