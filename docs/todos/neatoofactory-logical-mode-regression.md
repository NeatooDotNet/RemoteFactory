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

- [ ] Investigate why `Logical` mode doesn't register `IFactorySave<T>`
- [ ] Investigate why `Logical` mode has parameter count mismatch with `[Remote]` methods
- [ ] Determine if `Logical` mode ever supported `[Remote]` methods or if this is by design
- [ ] If by design, document that `Logical` mode only works for entities without `[Remote]` attributes
- [ ] If regression, fix the generator to properly handle `[Remote]` methods in `Logical` mode

## Related Files

- Neatoo: `src/Examples/Person/Person.DomainModel/Person.cs`
- Neatoo: `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs`
- RemoteFactory: Factory generator code that produces `FactoryServiceRegistrar`
