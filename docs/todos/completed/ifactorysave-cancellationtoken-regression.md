# IFactorySave Not Generated When CancellationToken Used with [Remote]

## Problem

When an entity has `[Remote]` save methods (`[Insert]`, `[Update]`, `[Delete]`) with `CancellationToken` parameters, the generator produces a factory that:
- Extends `FactoryBase<T>` instead of `FactorySaveBase<T>`
- Does NOT implement `IFactorySave<T>`
- Does NOT register `IFactorySave<T>` in DI

This causes `EntityBase.Save()` to fail with:
```
SaveOperationException: No factory save method is configured.
```

## Root Cause

`EntityBase.Save()` relies on `IFactorySave<T>` being injected via `IEntityBaseServices<T>.Factory`. When the generator doesn't produce `IFactorySave<T>`, the `Factory` property is null.

## Reproduction

Entity with CancellationToken (fails):
```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db, CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```

Entity without CancellationToken (works):
```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db)
    {
        // ...
    }
}
```

## Expected Behavior

Generated factory should:
1. Extend `FactorySaveBase<T>` (not `FactoryBase<T>`)
2. Implement `IFactorySave<T>`
3. Include `services.AddScoped<IFactorySave<T>, TFactory>()` in `FactoryServiceRegistrar`

## Files to Investigate

- Generator logic that decides between `FactoryBase` vs `FactorySaveBase`
- Condition checking for `[Remote]` + `CancellationToken` combination

## Tasks

- [ ] Add failing test case reproducing the issue
- [ ] Fix generator to produce `IFactorySave<T>` when CancellationToken is present
- [ ] Verify fix works for all combinations: Insert/Update/Delete with/without CancellationToken
- [ ] Release patch version

## Version

First observed in RemoteFactory 10.7.0 (generator renamed from `Neatoo.RemoteFactory.FactoryGenerator` to `Neatoo.Generator`)
