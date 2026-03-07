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

<!-- snippet: skill-class-execute -->
<a id='snippet-skill-class-execute'></a>
```cs
public interface ISkillConsultation
{
    long PatientId { get; }
    string Status { get; }
}

[Factory]
public partial class SkillConsultation : ISkillConsultation
{
    public long PatientId { get; set; }
    public string Status { get; set; } = string.Empty;

    public SkillConsultation() { }

    [Remote, Create]
    public Task CreateAcute(long patientId, [Service] IEmployeeRepository repo)
    {
        PatientId = patientId;
        Status = "Acute";
        return Task.CompletedTask;
    }

    [Remote, Fetch]
    public Task<bool> FetchActive(long patientId, [Service] IEmployeeRepository repo)
    {
        PatientId = patientId;
        Status = "Active";
        return Task.FromResult(true);
    }

    // Execute on class factory: public static, returns the interface type
    [Remote, Execute]
    public static async Task<ISkillConsultation> StartForPatient(
        long patientId,
        [Service] ISkillConsultationFactory factory,
        [Service] IEmployeeRepository repo)
    {
        var existing = await factory.FetchActive(patientId);
        if (existing != null)
            return existing;

        return await factory.CreateAcute(patientId);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ClassFactoryExecuteSamples.cs#L6-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-class-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key differences from static factory `[Execute]`:
- Method is **`public static`** (no underscore prefix, no `private`)
- Must return the **containing type** (or its matching interface)
- Generates a **factory interface method**, not a delegate type

### Generated Code

When the class implements a matching `I{ClassName}` interface, all generated factory methods return the interface type. Classes without a matching interface return the concrete type instead.

```csharp
public interface ISkillConsultationFactory
{
    Task<ISkillConsultation> CreateAcute(long patientId, CancellationToken ct = default);
    Task<ISkillConsultation?> FetchActive(long patientId, CancellationToken ct = default);
    Task<ISkillConsultation> StartForPatient(long patientId, CancellationToken ct = default);
}
```

### Caller Usage

```csharp
// Inject the factory -- same as any other factory method
var factory = serviceProvider.GetRequiredService<IConsultationFactory>();
var consultation = await factory.StartForPatient(patientId);
```

---

## Internal Visibility for Child Entities

Child entity factory methods should be `internal` to signal that they are server-only. This produces an `internal` factory interface (invisible to the client) and enables IL trimming of the method bodies.

```csharp
// Internal class with public interface — recommended pattern
public interface IOrderLine
{
    int Id { get; set; }
    string ProductName { get; set; }
}

[Factory]
internal partial class OrderLine : IOrderLine
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";

    [Create]
    internal void Create(string productName, decimal price, int qty) { }

    [Fetch]
    internal void Fetch(int id, string productName, decimal price, int qty) { }
}
// Generated: internal interface IOrderLineFactory — client can't inject or see it
```

For aggregate roots, keep factory methods `public` (with `[Remote]` for operations that cross to the server). For child entities called only from server-side aggregate operations, use `internal`.

**CS0051 constraint:** When a generated factory interface is `internal`, it cannot be used as a `[Service]` parameter type in a `public` method on another class. This limits `internal` to leaf entities whose factory interfaces are not passed as `[Service]` parameters to public methods. If you hit CS0051, keep the method `public` — the feature is opt-in and all `public` methods work identically to before.

---

## Key Rules

1. **Classes must be `partial`** - Generator adds serialization code
2. **Properties need public setters** - Required for deserialization
3. **[Remote] marks client entry points** - Only on aggregate root operations
4. **Business logic belongs in the entity** - Not in the factory
5. **Execute methods must be `public static`** - No underscore prefix (unlike static factory Execute)
6. **Execute must return the containing type** (or concrete type if no matching interface) - Keeps the factory interface cohesive
7. **Use `internal` for child entity factory methods** - Server-only, trimmable, invisible to client

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
