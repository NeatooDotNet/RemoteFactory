# PlainOptions/Options Split Breaks Downstream Converters

**Status:** Open
**Priority:** High
**Created:** 2026-03-20

---

## Problem

`NeatooJsonSerializer` in 0.21.3 introduced a `PlainOptions` code path (no `ReferenceHandler`) alongside the existing `Options` path (with `ReferenceHandler`). The `IsNeatooType()` method decides which path to use, but it only recognizes `IOrdinalSerializable` and interface/abstract types in `ServiceAssemblies`.

Both option sets share the same converter factories. So when a downstream consumer (like Neatoo) registers custom converters that need `ReferenceHandler`, those converters crash with `NullReferenceException` on the `PlainOptions` path — because `PlainOptions` has the converters but not the `ReferenceHandler` they depend on.

In 0.21.0, `Serialize` always used `Options` (with `ReferenceHandler`). This worked because `ReferenceHandler` is harmless for types that don't use `$id`/`$ref`.

### Root cause

The `PlainOptions` optimization assumes RemoteFactory knows which types need `ReferenceHandler`. It doesn't account for downstream converters that also need it. RemoteFactory shouldn't be making this decision — it's the converter's concern.

### The fix

Remove the `PlainOptions`/`Options` split. Always use `Options` (with `ReferenceHandler`) like 0.21.0 did. The overhead of an unused `ReferenceResolver` is negligible, and the split breaks extensibility.

Alternatively, if the optimization is worth keeping: set `ReferenceHandler` on both option sets and always initialize the resolver before serialization. That way downstream converters work regardless of which path is selected.

### Reproduction

In Neatoo repo: update RemoteFactory to 0.21.3, run `dotnet test src/Neatoo.sln` — 88 serialization tests fail with `NullReferenceException` at `NeatooBaseJsonTypeConverter.Write()` line 328:

```
options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists)
```

Stack: `NeatooJsonSerializer.Serialize` → selects `PlainOptions` → converter runs → `options.ReferenceHandler` is null.

---

## Tasks

- [ ] Fix PlainOptions to include ReferenceHandler (or remove the split)
- [ ] Verify Neatoo tests pass with updated package

---

## Progress Log

### 2026-03-20
- Discovered while upgrading Neatoo from RemoteFactory 0.21.0 to 0.21.3
- 88 Neatoo serialization tests fail with NullReferenceException
- Root cause: `PlainOptions` has converter factories but no `ReferenceHandler`
- Not a type-detection issue — the split itself is the problem
