# RemoteFactory: Support [Execute] Static Methods on Non-Static [Factory] Classes

## Summary

Allow `[Execute]` on `private static` methods within non-static `[Factory]` classes. Currently, diagnostic `NF0103` rejects this combination, requiring a separate static class. The feature would generate delegate types on the existing class factory interface, keeping orchestration logic co-located with the aggregate it operates on.

## Use Case

In zTreatment, the `Consultation` class is a `[Factory]` aggregate root with standard `[Create]`, `[Fetch]`, `[Insert]`, `[Update]` methods. It also has an orchestration method `StartForPatient` that:

1. Checks if the patient has an active consultation (via repository)
2. If yes, fetches the existing consultation via `IConsultationFactory.FetchActiveForPatient()`
3. If no, determines ACUTE vs MAINTENANCE from patient history, creates via `IConsultationFactory.CreateMaintenance()` or `CreateAcute()`, then saves via `IConsultationFactory.Save()`

This method is called from two other aggregates (`ConsultationWorkflow` and `VisitHub`) in their own `[Create][Remote]` methods.

### Current Code (workaround)

Because `[Execute]` isn't supported on non-static classes, the method is a plain `public static` with no factory attributes. Callers reference the concrete class directly:

```csharp
[Factory]
public partial class Consultation : NeatooBase
{
    // Standard factory methods...
    [Remote, Create]
    public async Task CreateAcute(long patientId, [Service] IConsultationRepository repository, ...) { }

    [Remote, Fetch]
    public async Task FetchActiveForPatient(long patientId, [Service] IConsultationRepository repository, ...) { }

    // Orchestration method -- NO factory attributes, plain static
    public static async Task<IConsultation> StartForPatient(
        long patientId,
        IConsultationFactory factory,          // passed explicitly
        IConsultationRepository repository)    // passed explicitly
    {
        var hasActive = await repository.HasActiveConsultationAsync(patientId);
        if (hasActive)
            return await factory.FetchActiveForPatient(patientId);

        var isInitialComplete = await IsInitialCompleteAsync(patientId, repository);
        var consultation = isInitialComplete
            ? await factory.CreateMaintenance(patientId)
            : await factory.CreateAcute(patientId);

        return await factory.Save(consultation);
    }
}
```

Callers must reference the concrete type:

```csharp
// In ConsultationWorkflow.cs and VisitHub.cs -- breaks the factory pattern
ConsultationEntity = await Consultation.StartForPatient(
    patientId, ConsultationEntityFactory, repository);
```

### Desired Code

```csharp
[Factory]
public partial class Consultation : NeatooBase
{
    // Standard factory methods...

    [Remote, Execute]
    private static async Task<IConsultation> _StartForPatient(
        long patientId,
        [Service] IConsultationFactory factory,
        [Service] IConsultationRepository repository)
    {
        // Same orchestration logic, but services are injected
    }
}
```

Callers use the generated factory:

```csharp
// Generated onto IConsultationFactory
ConsultationEntity = await ConsultationEntityFactory.StartForPatient(patientId);
```

## Why Not a Separate Static Class?

The orchestration is tightly coupled to the `Consultation` aggregate -- it calls `Consultation`-specific factory methods and uses `Consultation`-internal helpers like `IsInitialCompleteAsync`. Splitting it into a separate `ConsultationCommands` static class:

- Scatters related logic across two files/classes
- Requires making internal helpers public or duplicating them
- Loses the natural discoverability of "what can I do with a Consultation?"

## Generator Changes

### Transform Phase (`FactoryGenerator.Transform.cs`)

Remove or relax the `NF0103` diagnostic. Currently at lines ~179-203:

```csharp
else if (factoryOperation == FactoryOperation.Execute
            && serviceSymbol.TypeKind != TypeKind.Interface)
{
    if (!methodSymbol.IsStatic || !serviceSymbol.IsStatic)
    {
        // NF0103: Execute method must be in a static class
        diagnostics.Add(...);
        continue;
    }
}
```

Change to: allow `[Execute]` when the method is static, even if the class is not static. The method still needs to be `private static` with underscore prefix (same convention as static factory classes).

### Builder Phase (`FactoryModelBuilder.cs`)

In `BuildClassFactory`, add handling for `FactoryOperation.Execute` alongside the existing Event handling:

```csharp
foreach (var method in typeInfo.FactoryMethods)
{
    if (method.FactoryOperation == FactoryOperation.Event)
    {
        events.Add(BuildEventMethod(method, ...));
        continue;
    }
    if (method.FactoryOperation == FactoryOperation.Execute)
    {
        // Build as ExecuteDelegateModel, add to class factory
        delegates.Add(BuildExecuteDelegate(method));
        continue;
    }
    // existing Create/Fetch/Insert/Update/Delete handling...
}
```

### Renderer Phase

The class factory renderer needs to emit the `[Execute]` delegate on `IConsultationFactory` alongside the existing Create/Fetch/Save methods.

### Test Coverage

Add test targets in `RemoteFactory.UnitTests/TestTargets/Execute/` for:

- `[Execute]` static method on a non-static `[Factory]` class
- With `[Service]` parameters
- With `[Remote]` for client-server execution
- Returning a type different from the containing class (e.g., returning `IConsultation` from `Consultation`)
