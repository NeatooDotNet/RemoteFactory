# LazyLoad: Public Type and Protected Members for Inheritance

**Date:** 2026-03-28
**Related Todo:** [LazyLoad: Protected for Inheritance](../../todos/completed/lazyload-protected-for-inheritance.md)
**Status:** Complete
**Last Updated:** 2026-03-28

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Under Review (Developer) |
Concerns Raised | Ready for Implementation | In Progress | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Make `LazyLoad<T>` inheritable by Neatoo. Neatoo currently duplicates the entire `LazyLoad<T>` class to add domain-specific meta-property interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`). With RemoteFactory now shipping its own `LazyLoad<T>`, Neatoo should subclass it instead. This requires widening visibility on one type and three members.

---

## Business Requirements Context

[Architect populates during Step 3]

---

## Business Rules (Testable Assertions)

[Architect populates during Step 3]

### Test Scenarios

[Architect populates during Step 3]

---

## Approach

Visibility-only changes — no behavioral changes. Widen access modifiers so a subclass in Neatoo can:

1. Cast to `ILazyLoadDeserializable` for deserialization merging
2. Read the in-flight load task for `WaitForTasks()`
3. Clear the load error for `ClearAllMessages()` / `ClearSelfMessages()`
4. Fire and override `PropertyChanged` for additional meta-properties

---

## Domain Model Behavioral Design

N/A — This is a library infrastructure change, not a domain model feature. No computed properties, visibility flags, reactive rules, or validation rules are affected.

---

## Design

### 1. Make `ILazyLoadDeserializable` public

**File:** `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs`

Change `internal interface` to `public interface`. Keep in `Neatoo.RemoteFactory.Internal` namespace — "Internal" signals "use with care", not "inaccessible".

### 2. Expose `_loadTask` as protected read-only property

**File:** `src/RemoteFactory/LazyLoad.cs`

Add a protected property exposing the field:

```csharp
protected Task<T?>? LoadTask => _loadTask;
```

Neatoo's `WaitForTasks()` checks `_loadTask != null && !_loadTask.IsCompleted`. Read-only access is sufficient.

### 3. Add `ClearLoadError()` protected method

**File:** `src/RemoteFactory/LazyLoad.cs`

```csharp
protected void ClearLoadError()
{
    _loadError = null;
}
```

Neatoo's `ClearAllMessages()` and `ClearSelfMessages()` set `_loadError = null`. A method is cleaner than exposing the field — it constrains the subclass to clearing only.

### 4. Make `OnPropertyChanged` protected virtual

**File:** `src/RemoteFactory/LazyLoad.cs`

Change from:
```csharp
private void OnPropertyChanged(string propertyName)
```

To:
```csharp
protected virtual void OnPropertyChanged(string propertyName)
```

Standard INPC pattern. `protected` lets the subclass fire PropertyChanged for additional properties. `virtual` allows override for future extensibility (e.g., batching notifications).

---

## Implementation Steps

1. In `ILazyLoadDeserializable.cs`: Change `internal interface ILazyLoadDeserializable` to `public interface ILazyLoadDeserializable`
2. In `LazyLoad.cs`: Add `protected Task<T?>? LoadTask => _loadTask;`
3. In `LazyLoad.cs`: Add `protected void ClearLoadError() { _loadError = null; }`
4. In `LazyLoad.cs`: Change `private void OnPropertyChanged(string propertyName)` to `protected virtual void OnPropertyChanged(string propertyName)`
5. Build and run all tests — all should pass unchanged (visibility widening only)

---

## Acceptance Criteria

- [ ] `ILazyLoadDeserializable` is `public`
- [ ] `LazyLoad<T>` has `protected Task<T?>? LoadTask` property
- [ ] `LazyLoad<T>` has `protected void ClearLoadError()` method
- [ ] `LazyLoad<T>.OnPropertyChanged` is `protected virtual`
- [ ] All existing tests pass unchanged
- [ ] Solution builds clean (no warnings) on both net9.0 and net10.0

---

## Agent Phasing

[Architect populates during Step 3]

---

## Dependencies

- None. This is a leaf change with no downstream dependencies within RemoteFactory.

---

## Risks / Considerations

- **Binary breaking for NuGet consumers?** No — `ILazyLoadDeserializable` was `internal`, so no external consumer could reference it. Widening to `public` is additive.
- **Subclass misuse of `ClearLoadError()`** — Minimal risk. The method only nulls the error field. A subclass calling it at the wrong time just clears an error message, it doesn't corrupt state.
- **`virtual` on `OnPropertyChanged`** — An overriding subclass that forgets to call `base.OnPropertyChanged()` would break INPC notifications. This is a well-known INPC contract that any .NET developer would recognize.
