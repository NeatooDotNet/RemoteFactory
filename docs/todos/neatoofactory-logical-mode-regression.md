# NeatooFactory.Logical Mode Regression with [Remote] Methods

## Problem

Integration tests that use `NeatooFactory.Logical` mode fail when the entity has `[Remote]` attributes on its `[Insert]`, `[Update]`, and `[Delete]` factory methods. This appears to be a regression - the tests were originally written with `Logical` mode and presumably worked at some point.

## Symptoms

### Error 1: Using `entity.Save()` with Logical mode

```
Neatoo.SaveOperationException : No factory save method is configured.
Ensure [Insert], [Update], and/or [Delete] methods with no non-service parameters are defined.
```

The `IFactorySave<T>` is not registered in DI when using `Logical` mode.

### Error 2: Using `factory.Save()` with Logical mode

```
System.Reflection.TargetParameterCountException : Parameter count mismatch.
```

The factory delegates are not properly registered for `[Remote]` methods in `Logical` mode.

## Reproduction

Entity with `[Remote]` on save methods:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    [Remote]
    [Insert]
    public async Task<PersonEntity?> Insert([Service] IPersonDbContext personContext, ...)
    { ... }

    [Remote]
    [Update]
    public async Task<PersonEntity?> Update([Service] IPersonDbContext personContext, ...)
    { ... }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonDbContext personContext, ...)
    { ... }
}
```

Test setup:

```csharp
// FAILS with both errors above
serviceCollection.AddNeatooServices(NeatooFactory.Logical, typeof(Person).Assembly);

// WORKS
serviceCollection.AddNeatooServices(NeatooFactory.Server, typeof(Person).Assembly);
```

## Analysis

Looking at the generated `FactoryServiceRegistrar` methods:

### With `NeatooFactory.Server` (works)

The generated factory:
- Inherits `FactorySaveBase<IPerson>`
- Implements `IFactorySave<Person>`
- Registers `IFactorySave<Person>` in DI
- Properly registers delegates for `[Remote]` methods

### With `NeatooFactory.Logical` (broken)

The factory appears to:
- Not register `IFactorySave<T>`
- Not properly set up delegates for `[Remote]` methods

## Expected Behavior

`NeatooFactory.Logical` mode should work for integration tests that:
1. Have direct database access (no actual remote calls needed)
2. Want to test the full save flow including `[Remote]` methods executing locally

This is a common integration testing pattern where you want to test the complete entity behavior without setting up a client/server split.

## Current Workaround

In Neatoo's `Person.DomainModel.Tests`, the workaround applied was:
1. Change `NeatooFactory.Logical` to `NeatooFactory.Server`
2. Change `await person.Save()` to `await factory.Save(person, CancellationToken.None)`

See commit: `0bec5a4` in Neatoo repository.

## Tasks

- [x] Investigate why `Logical` mode doesn't register `IFactorySave<T>`
- [x] Investigate why `Logical` mode has parameter count mismatch with `[Remote]` methods
- [x] Determine if `Logical` mode ever supported `[Remote]` methods or if this is by design
- [x] ~~If by design, document that `Logical` mode only works for entities without `[Remote]` attributes~~
- [x] If regression, fix the generator to properly handle `[Remote]` methods in `Logical` mode

## Resolution (2026-01-13)

**Fixed by RemoteFactory 10.8.0** - The regression was fixed as part of the "optional CancellationToken on all factory methods" feature (commit `eab7726`).

### Verification
- Upgraded Neatoo to RemoteFactory 10.9.0
- Added test `PersonSave_DirectSave_ShouldWork` that uses `person.Save()` directly in Logical mode
- Test **passes** with 10.9.0, **fails** with 10.7.0
- All 1,921 Neatoo tests pass

## Investigation Results (2026-01-13)

### Finding: RemoteFactory Logical mode works correctly

New tests added in `src/Tests/FactoryGeneratorTests/Factory/LogicalModeTests.cs` prove that:

1. **`IFactorySave<T>` is registered and resolvable** in Logical mode
2. **`IFactorySave<T>.Save()` works correctly** for Insert, Update, and Delete operations
3. **`factory.Save()` works correctly** with no parameter count mismatch
4. **`[Service]` parameters work correctly** in Logical mode
5. **Logical mode behaves identically to Server mode** for all save operations

All 13 new tests pass across net8.0, net9.0, and net10.0.

### Conclusion: Bug is NOT in RemoteFactory

The bug described in this todo is **not a RemoteFactory issue**. The issue must be in the **Neatoo project**:

1. **EntityBase implementation**: How `EntityBase.Save()` resolves or uses `IFactorySave<T>`
2. **Test project configuration**: Possibly using `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` when it should use `Full`
3. **Entity method signatures**: If the Neatoo entity has extra parameters beyond `target`, `[Service]`, and `CancellationToken`, the "default" save method won't be generated

### Next Steps (in Neatoo repository)

1. Check if the test project has `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` - if so, that's the issue
2. Verify the Person entity's `Insert`/`Update`/`Delete` method signatures match the pattern expected for `IFactorySave<T>`
3. Check how `EntityBase.Save()` resolves `IFactorySave<T>` - the error message comes from Neatoo, not RemoteFactory

## Related Files

- Neatoo: `src/Examples/Person/Person.DomainModel/Person.cs`
- Neatoo: `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs`
- RemoteFactory: Factory generator code that produces `FactoryServiceRegistrar`
- **NEW**: RemoteFactory: `src/Tests/FactoryGeneratorTests/Factory/LogicalModeTests.cs` - Tests proving Logical mode works
