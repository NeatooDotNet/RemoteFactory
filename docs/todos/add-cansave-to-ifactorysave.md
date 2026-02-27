# Add CanSave to IFactorySave<T>

**Status:** In Progress
**Priority:** Normal
**Scope:** RemoteFactory only (Neatoo EditBase consumption tracked separately in Neatoo repo)
**Last Updated:** 2026-02-26

## Plans

- [Add CanSave to IFactorySave<T> - Implementation Plan](../plans/add-cansave-to-ifactorysave.md)

## Use Case

Neatoo's `EditBase` already resolves `IFactorySave<T>` from DI to call `Save()` without knowing the concrete factory type. The same pattern is needed for `CanSave()` — EditBase needs to ask "can this entity be saved?" through the same shared interface.

Currently `CanSave()` is only available on the concrete generated factory class and its generated interface (e.g., `ISecureOrderFactory`). EditBase can't access it because it only knows about `IFactorySave<T>`.

Once this is in place, Neatoo's EditBase will:
- Resolve `IFactorySave<T>` from DI
- Call `CanSave()` to check authorization before presenting save UI
- If no `IFactorySave<T>` is registered (no default Save), or if there's no authorization configured, `CanSave()` defaults to `Authorized(true)`

## What Changes

### 1. Add `CanSave` to `IFactorySave<T>` interface

**File:** `src/RemoteFactory/IFactorySave.cs`

Add `CanSave()` returning `Authorized`:

```csharp
public interface IFactorySave<T>
    where T : IFactorySaveMeta
{
    Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
    Task<Authorized> CanSave(CancellationToken cancellationToken = default);
}
```

### 2. Update `FactorySaveBase<T>` default implementation

**File:** `src/RemoteFactory/FactorySaveBase.cs`

Provide a default that returns `Authorized(true)` — when no authorization is configured, saving is allowed:

```csharp
Task<Authorized> IFactorySave<T>.CanSave(CancellationToken cancellationToken)
{
    return Task.FromResult(new Authorized(true));
}
```

### 3. Update Generator — explicit interface implementation

**File:** `src/Generator/Renderer/ClassFactoryRenderer.cs`

In the same area where `IFactorySave<T>.Save` is rendered (around line 816), add an explicit interface implementation for `IFactorySave<T>.CanSave` that delegates to the existing generated `CanSave()` method:

```csharp
async Task<Authorized> IFactorySave<T>.CanSave(CancellationToken cancellationToken)
{
    return await CanSave(cancellationToken);
}
```

This should only be rendered when:
- There is a default Save method (`model.HasDefaultSave`)
- The factory has a generated `CanSave()` method (authorization is configured)

When there's a default Save but no authorization, the base class default (`Authorized(true)`) is sufficient — no explicit implementation needed.

### 4. Update Generator — old generator path

**File:** `src/Generator/FactoryGenerator.Types.cs`

Same pattern as #3, applied to the legacy generator path (around line 1425 where `IFactorySave<T>.Save` is generated).

## Testing

- **Existing tests should still pass** — `CanSave()` is already generated on concrete factories; this just adds the explicit interface bridge
- **New unit test**: Resolve `IFactorySave<T>` and call `CanSave()`, verify it delegates to the authorization logic
- **New unit test**: When no authorization is configured, `IFactorySave<T>.CanSave()` returns `Authorized(true)` (base class default)
- **Integration test**: Verify `CanSave()` works through `IFactorySave<T>` in the client/server container pattern

## Breaking Change

This is a **breaking change** for anyone who directly implements `IFactorySave<T>` (rather than inheriting from `FactorySaveBase<T>`). They would need to add a `CanSave()` method. In practice this is unlikely since `IFactorySave<T>` implementations are generated.

## Not In Scope

- Neatoo EditBase changes to consume `IFactorySave<T>.CanSave()` — separate todo in Neatoo repo
- Adding `CanSave` to interface factory or static factory patterns — evaluate later if needed
