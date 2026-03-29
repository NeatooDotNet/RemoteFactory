# LazyLoad: Make Types Public and Members Protected for Neatoo Inheritance

**Status:** Complete
**Priority:** High
**Created:** 2026-03-28
**Last Updated:** 2026-03-29

---

## Problem

Neatoo's `LazyLoad<T>` is currently a copy of RemoteFactory's `LazyLoad<T>` with Neatoo-specific meta-property interfaces added (`IValidateMetaProperties`, `IEntityMetaProperties`). Now that RemoteFactory has its own `LazyLoad<T>`, Neatoo should inherit from it instead of duplicating the core loading logic. But RemoteFactory's `LazyLoad<T>` has all private fields and an internal `ILazyLoadDeserializable` interface, making inheritance impossible.

## Solution

Make the necessary types public and members protected so that Neatoo can subclass `RemoteFactory.LazyLoad<T>` and add only its domain-specific meta-property behavior.

### Types to make public

1. **`ILazyLoadDeserializable`** — Currently `internal` in `Neatoo.RemoteFactory.Internal`. Neatoo's serializer (`NeatooBaseJsonTypeConverter`) needs to cast to this interface for deserialization merging. Make it public and move to `Neatoo.RemoteFactory` namespace.

### Members to make protected

Neatoo's subclass implements `IValidateMetaProperties` and `IEntityMetaProperties`, which need access to:

1. **`_loadTask`** — `WaitForTasks()` checks `_loadTask != null && !_loadTask.IsCompleted`. Expose as `protected Task<T?>? LoadTask { get; }` or make the field protected.

2. **`_loadError`** — `ClearAllMessages()` and `ClearSelfMessages()` set `_loadError = null`. Expose as `protected void ClearLoadError()` method or make the field protected.

3. **`OnPropertyChanged(string)`** — Currently private. Make `protected virtual` (standard INPC pattern). Neatoo's subclass may need to fire PropertyChanged for additional properties, and virtual allows override for future extensibility.

### What does NOT need to change

- `Value`, `IsLoaded`, `IsLoading`, `HasLoadError`, `LoadError` — already public getters, Neatoo's meta-property implementations can use these directly
- `LoadAsync()`, `SetValue()` — already public
- `SubscribeToValuePropertyChanged` / `UnsubscribeFromValuePropertyChanged` — base class handles value change subscriptions; subclass doesn't need these
- `_value`, `_isLoaded`, `_isLoading` — accessible through public properties

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

[pending]

### Gaps

[pending]

### Contradictions

[pending]

### Recommendations for Architect

[pending]

---

## Plans

- [LazyLoad: Public Type and Protected Members for Inheritance](../../plans/completed/lazyload-protected-for-inheritance.md)

---

## Tasks

- [x] Requirements review (skipped by user — small visibility-only change)
- [x] Architect review and plan (skipped by user)
- [x] Developer review — Approved
- [x] Implementation — 2 files modified, 2046 tests passing
- [x] Verification — Architect VERIFIED

---

## Progress Log

### 2026-03-28
- Created todo based on Neatoo conversation about extending LazyLoad from RemoteFactory
- Identified 1 type (ILazyLoadDeserializable) and 3 members (_loadTask, _loadError, OnPropertyChanged) that need visibility changes
- This is a prerequisite for Neatoo's LazyLoad inheritance todo
- Drafted plan — ILazyLoadDeserializable stays in Internal namespace (just made public), 4 surgical changes total

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 new warnings (net9.0 + net10.0)
- Tests: 2046 passed, 0 failed

---

## Results / Conclusions

Implementation complete. Four visibility changes across two files make `LazyLoad<T>` inheritable:

1. `ILazyLoadDeserializable` — `internal` → `public` (stays in `Internal` namespace)
2. `protected Task<T?>? LoadTask` — read-only access to in-flight load task
3. `protected void ClearLoadError()` — clears load error state
4. `OnPropertyChanged` — `private` → `protected virtual` (standard INPC pattern)

One unplanned addition: CA1033 pragma suppress on explicit `ILazyLoadDeserializable` members (required when a public interface is explicitly implemented on an unsealed class).

No behavioral changes. All 2046 existing tests pass unchanged. Neatoo can now subclass `RemoteFactory.LazyLoad<T>` instead of duplicating the core loading logic.
