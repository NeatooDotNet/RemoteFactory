# Class Factory Pattern

Use for domain entities and aggregate roots that have lifecycle operations (Create, Fetch, Save).

## Complete Example

<!-- snippet: skill-class-factory-complete -->
<a id='snippet-skill-class-factory-complete'></a>
```cs
[Factory]
public partial class SkillEmployee : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Create]
    public void Create(string firstName, string lastName, [Service] IEmployeeRepository repo)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var data = await repo.GetByIdAsync(id, ct);
        if (data == null) return false;

        Id = data.Id;
        FirstName = data.FirstName;
        LastName = data.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}@example.com",
                FirstName.ToLowerInvariant(),
                LastName.ToLowerInvariant()),
            Position = "New Employee",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.FirstName = FirstName;
            entity.LastName = LastName;
            await repo.UpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);
        }
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ClassFactorySamples.cs#L7-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-class-factory-complete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**: `ISkillEmployeeFactory` with `Create()`, `Fetch()`, `Save()` methods.

---

## IFactorySaveMeta for Save Routing

Implement `IFactorySaveMeta` to enable automatic Save() routing:

```csharp
public partial class Employee : IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }
}
```

The factory's `Save()` method examines these properties:

| IsNew | IsDeleted | Operation Called |
|-------|-----------|------------------|
| true | false | Insert |
| false | false | Update |
| false | true | Delete |
| true | true | No operation |

---

## Lifecycle Hooks

Implement lifecycle interfaces for cross-cutting concerns:

<!-- snippet: skill-lifecycle-hooks -->
<a id='snippet-skill-lifecycle-hooks'></a>
```cs
[Factory]
public partial class SkillEmployeeWithLifecycle : IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SkillEmployeeWithLifecycle()
    {
        Id = Guid.NewGuid();
    }

    public Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        // Before operation - validation, logging
        return Task.CompletedTask;
    }

    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        // After operation - cleanup, state reset
        if (factoryOperation == FactoryOperation.Insert)
            IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}@example.com",
                FirstName.ToLowerInvariant(),
                LastName.ToLowerInvariant()),
            Position = "New Employee",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.FirstName = FirstName;
            entity.LastName = LastName;
            await repo.UpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/LifecycleHookSamples.cs#L7-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lifecycle-hooks' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available hooks:**
- `IFactoryOnStart` / `IFactoryOnStartAsync` - Before any operation
- `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - After successful operation
- `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - On operation failure

---

## Execute Methods on Class Factories

Use `[Execute]` on `public static` methods within a class factory to co-locate orchestration logic with the aggregate it operates on.

### When to Use

- The operation creates or returns an instance of the containing type
- The logic is tightly coupled to the aggregate (calls its factory methods, uses internal helpers)
- You want the operation on the same factory interface as Create/Fetch/Save

If the operation returns a different type, use a static factory `[Execute]` instead (see `references/static-factory.md`).

### Method Signature

```csharp
[Factory]
public partial class Consultation
{
    [Remote, Create]
    public Task CreateAcute(long patientId, [Service] IRepository repo) { /* ... */ }

    [Remote, Execute]
    public static async Task<Consultation> StartForPatient(
        long patientId,
        [Service] IConsultationFactory factory,
        [Service] IRepository repo)
    {
        var hasActive = await repo.HasActiveAsync(patientId);
        if (hasActive)
            return await factory.FetchActive(patientId);

        return await factory.CreateAcute(patientId);
    }
}
```

Key differences from static factory `[Execute]`:
- Method is **`public static`** (no underscore prefix, no `private`)
- Must return the **containing type** (or its service interface)
- Generates a **factory interface method**, not a delegate type

### Generated Code

```csharp
public interface IConsultationFactory
{
    Task<Consultation> CreateAcute(long patientId, CancellationToken ct = default);
    Task<Consultation> StartForPatient(long patientId, CancellationToken ct = default);
    // ... other Create/Fetch/Save methods
}
```

### Caller Usage

```csharp
// Inject the factory -- same as any other factory method
var factory = serviceProvider.GetRequiredService<IConsultationFactory>();
var consultation = await factory.StartForPatient(patientId);
```

---

## Key Rules

1. **Classes must be `partial`** - Generator adds serialization code
2. **Properties need public setters** - Required for deserialization
3. **[Remote] marks client entry points** - Only on aggregate root operations
4. **Business logic belongs in the entity** - Not in the factory
5. **Execute methods must be `public static`** - No underscore prefix (unlike static factory Execute)
6. **Execute must return the containing type** - Keeps the factory interface cohesive

---

## [SuppressFactory] for Derived Classes

Use `[SuppressFactory]` to prevent factory generation on derived classes that inherit from a `[Factory]` base:

<!-- snippet: attributes-suppressfactory -->
<a id='snippet-attributes-suppressfactory'></a>
```cs
[Factory]
public partial class BaseEntity { }

[SuppressFactory]  // Prevents factory generation on derived class
public partial class InternalEntity : BaseEntity { }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L19-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Use when:**
- Base class has `[Factory]` but derived class shouldn't have its own factory
- You want to manage polymorphic types through the base factory
