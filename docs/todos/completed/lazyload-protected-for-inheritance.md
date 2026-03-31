# LazyLoad: Make Types Public and Members Protected for Neatoo Inheritance

**Status:** Complete
**Priority:** High
**Created:** 2026-03-28
**Last Updated:** 2026-03-29

---

## Problem

Neatoo's `LazyLoad<T>` is currently a copy of RemoteFactory's `LazyLoad<T>` with Neatoo-specific meta-property interfaces added (`IValidateMetaProperties`, `IEntityMetaProperties`). Now that RemoteFactory has its own `LazyLoad<T>`, Neatoo should inherit from it instead of duplicating the core loading logic. But RemoteFactory's `LazyLoad<T>` had all private fields and an internal `ILazyLoadDeserializable` interface, making inheritance impossible.

## Solution

Made the necessary types public and members protected so that Neatoo can subclass `RemoteFactory.LazyLoad<T>`.

### Changes made

1. **`ILazyLoadDeserializable`** — Made `public` (was `internal`) in `Neatoo.RemoteFactory.Internal`
2. **`LoadTask`** — Added `protected Task<T?>? LoadTask` property exposing `_loadTask` for subclass `WaitForTasks()`
3. **`ClearLoadError()`** — Added `protected void ClearLoadError()` method for subclass `ClearAllMessages()`/`ClearSelfMessages()`
4. **`OnPropertyChanged(string)`** — Made `protected virtual` (was `private`)

Published as RemoteFactory 0.26.0.

---

## Results / Conclusions

All changes published in Neatoo.RemoteFactory 0.26.0 on NuGet. Ready for Neatoo to consume and implement LazyLoad inheritance.
