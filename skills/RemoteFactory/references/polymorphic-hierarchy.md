# Polymorphic Class Hierarchy with RemoteFactory

Multiple concrete types can share a generic base class, each with their own factory. This enables polymorphic domain models where the same data can be presented as different types based on runtime context.

## Pattern

```csharp
// Abstract generic base — owns shared properties, has [Factory] and CRTP constraint
[Factory]
internal abstract partial class PlanBase<T> : EntityBase<T>
    where T : PlanBase<T>
{
    public PlanBase(IEntityBaseServices<T> services) : base(services) { }

    // Shared properties — source generator creates implementations for each leaf
    public partial long Id { get; set; }
    public partial string Name { get; set; }
}

// Concrete type A — own [Factory], own [Create]/[Fetch]
[Factory]
internal partial class Consultation : PlanBase<Consultation>, IConsultation
{
    public Consultation(IEntityBaseServices<Consultation> services) : base(services) { }

    [Create]
    public void Create(string name) { Id = 1; Name = name; }

    [Fetch][Remote]
    internal void Fetch(long id, string name) { Id = id; Name = name; }
}

// Concrete type B — own [Factory], own [Create]/[Fetch]
[Factory]
internal partial class Maintenance : PlanBase<Maintenance>, IMaintenance
{
    public Maintenance(IEntityBaseServices<Maintenance> services) : base(services) { }

    public partial int MaintenanceNumber { get; set; }

    [Create]
    public void Create(string name, int num) { Id = 2; Name = name; MaintenanceNumber = num; }

    [Fetch][Remote]
    internal void Fetch(long id, string name, int num) { Id = id; Name = name; MaintenanceNumber = num; }
}
```

## Key Rules

1. **Base class must have `[Factory]`** — the source generator needs it to create property implementations for `partial` properties declared on the base.

2. **CRTP constraint is required** — `where T : PlanBase<T>`. Without this, the generated `InitializePropertyBackingFields` method cannot cast `this` to `T`. This matches the pattern used by `VisitOrchestratorBase<T>`.

3. **Base class should be `abstract`** — prevents direct instantiation; only leaf types are constructed.

4. **Each leaf class needs its own `[Create]`/`[Fetch]`** — these attributes are NOT inherited. `[Factory]` and `[Remote]` ARE inherited.

5. **Each leaf gets its own factory interface** — `IConsultationFactory`, `IMaintenanceFactory`, etc. There is no shared `IPlanBaseFactory`.

6. **Multi-level hierarchies work** — you can have `PlanBase<T>` → `AcuteBase<T> : PlanBase<T>` → `Consultation : AcuteBase<Consultation>` with each intermediate level adding properties.

## Serialization

RemoteFactory handles serialization automatically. When a `[Fetch][Remote]` method runs:
- Server creates the concrete type and populates properties
- Server serializes with full type information
- Client deserializes to the correct concrete type
- Factory returns the interface type (e.g., `IConsultation`)

The concrete type is preserved through the round-trip. A service returning `IPlan` (base interface) works correctly — the client receives the concrete type.

## Routing Pattern

A hand-coded routing service decides which factory to call based on data state:

```csharp
internal class PlanRoutingService
{
    private readonly IConsultationFactory _consultationFactory;
    private readonly IMaintenanceFactory _maintenanceFactory;

    public async Task<IPlan> GetOrCreatePlanAsync(long patientId)
    {
        var data = await _repository.GetPlanDataAsync(patientId);

        if (data.Type == "MAINTENANCE")
            return await _maintenanceFactory.Fetch(data.Id, ...);

        if (data.IsFirstVisit || !data.IsApproved)
            return await _consultationFactory.Fetch(data.Id, ...);

        return await _acuteFactory.Fetch(data.Id, ...);
    }
}
```

The routing decision runs once at the factory boundary. Downstream code works with the typed interface — no flag-checking needed.

## Tested In

Spike validated in `zTreatment.DomainModels.IntegrationTests/Tests/PolymorphicPlanSpikeTests.cs`:
- Generic base with `[Factory]` and CRTP constraint: compiles and generates correctly
- Two concrete types with own factories: both generate `IXxxFactory` interfaces
- Client-server JSON round-trip (`[Remote]` fetch): preserves concrete type
- Base interface access after deserialization: works
- Type-specific properties survive serialization: works
