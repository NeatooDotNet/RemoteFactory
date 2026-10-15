# Advanced RemoteFactory Patterns

## Custom Authorization with [AuthorizeFactory\<T\>]

> **Scope note:** this section covers `[AuthorizeFactory<T>]` on **class factories** — CRUD-scoped auth (`Create`/`Fetch`/`Insert`/`Update`/`Delete`) with generated `CanCreate`/`CanFetch`/`CanSave`/`CanDelete`. For `[AuthorizeFactory<T>]` on **interface factories** — which uses a different scope model (`Execute`/`Read` only) and per-method parameter matching — see `references/interface-factory.md`.

Define domain-specific authorization logic in a dedicated class:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
public interface IAuthorizedOrderAuth
{
    /// <summary>
    /// Broad-scope check covering all Read (Create, Fetch) and Write (Insert, Update, Delete) operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Fine-grained check for Create operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Fine-grained check for Fetch operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    /// <summary>
    /// Fine-grained check for Delete operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L68-L95' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
public bool HasAccess() => user.IsAuthenticated;

public bool CanCreate() => user.IsInRole(OrderRoles.Sales);

public bool CanFetch() => user.IsInRole(OrderRoles.Sales) || user.IsInRole(OrderRoles.Support);

public bool CanDelete() => user.IsInRole(OrderRoles.Manager);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L118-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICurrentUser` is the example's signed-in user (`IsAuthenticated`, `IsInRole`), injected by constructor and registered on both tiers, because the rules run wherever the operation runs. `OrderRoles` holds the role names.

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs#L97-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generated authorization methods:**
- `CanCreate()`, `CanFetch()`, `CanInsert()`, `CanUpdate()`, `CanDelete()` on factory interface
- Returns `Authorized<T>` result with `HasAccess` property

**Use `[AuthorizeFactory<T>]` when:**
- Authorization is domain-specific with complex rules
- You want testable auth without HTTP infrastructure
- Different operations have different authorization requirements

### Parameterized Authorization Methods

Auth methods can receive parameters matched by type from factory method parameters:

```csharp
public interface IOrderAuthorization
{
    // Guid param matched to Fetch(Guid orderId) by type
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetchOrder(Guid orderId);

    // Parameterless Read for Create
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();
}
```

The generator produces `CanFetch(Guid)` on the factory interface. Parameters are matched **by type**, not name.

Use specific scopes (e.g., `Fetch`) for parameterized methods — a `Read`-scoped method with a `Guid` param would try to match Create too, which may not have a `Guid` parameter.

### Target Parameter Authorization

Auth methods can receive the **target entity** on write operations for state-based authorization:

```csharp
public interface IOrderAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite(IOrder target);
}

public class OrderAuthorization : IOrderAuthorization
{
    public bool CanWrite(IOrder target)
    {
        return target.Status != "Locked"; // Deny writes to locked records
    }
}
```

**CanXxx generation with target parameters:** When Write auth has a target parameter, `CanInsert`/`CanUpdate`/`CanDelete` are **not generated** — the entity isn't available before the write operation. `CanSave` is the exception: two overloads are generated because the caller has the entity in hand when deciding whether to save. `CanSave()` (parameterless) runs only non-target Write auth methods (role checks). `CanSave(target)` runs ALL Write auth methods — both non-target and target-parameterized. `CanCreate`/`CanFetch` are still generated when Read auth is parameterless.

---

## Authorization with [AspAuthorize]

Apply ASP.NET Core policy-based authorization to factory operations:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
[Remote, Fetch]
[AspAuthorize("RequireAuthenticated")]
internal void Fetch(int id)
{
    Id = id;
    CustomerName = $"Customer_{id}";
    Total = 100.00m;
    IsNew = false;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L101-L111' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server configuration:**

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
services.AddAuthentication();   // add your scheme here: cookies, JWT bearer, ...
services.AddAuthorization(options =>
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser()));
services.AddHttpContextAccessor();
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L102-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The server also calls `app.UseAuthentication()` and `app.UseAuthorization()` before `app.UseNeatoo()`, so the policy sees the signed-in user.

**Note:** Multiple [AspAuthorize] attributes use AND logic (all must pass).

---

## Correlation Context

Track operations across the client/server boundary for distributed tracing:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Remote, Execute]
private static async Task<string> _ReportProblem(
    int orderId,
    string description,
    [Service] ICorrelationContext correlation,
    [Service] ISupportLog supportLog)
{
    // The same ID tags the server's log lines for this call
    var reference = correlation.CorrelationId ?? "unknown";
    await supportLog.RecordAsync(new SupportEntry(reference, orderId, description));
    return reference;
}
```
<sup><a href='/src/Design/Design.Domain/Services/SupportRequests.cs#L39-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICorrelationContext` is automatically registered by `AddNeatooRemoteFactory` and `AddNeatooAspNetCore`. No manual DI registration is required.

---

## Entity Duality

An entity can be an aggregate root in one context and a child in another:

<!-- snippet: skill-entity-duality -->
<a id='snippet-skill-entity-duality'></a>
```cs
[Factory]
public partial class SkillDepartment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Aggregate root context - client entry point
    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var data = await repo.GetByIdAsync(id, ct);
        if (data == null) return false;

        Id = data.Id;
        Name = data.Name;
        Code = data.Code;
        return true;
    }

    // Child context - called from Employee.Fetch on server
    [Fetch]  // No [Remote] - server-side only
    public void FetchAsChild(Guid id, string name, string code)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/EntityDualitySamples.cs#L6-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-duality' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Key insight:** [Remote] is about *how the method is called*, not *what the type is*.

For entity duality, use `internal` on all factory methods. The aggregate root entry point (`Fetch` above) uses `[Remote] internal` -- `[Remote]` promotes it to `public` on the factory interface so clients can call it, while `internal` enables IL trimming of the method body. The child context method (`FetchAsChild`) is `internal` without `[Remote]` -- it gets an `IsServerRuntime` guard, becomes trimmable, and appears on the public factory interface with the `internal` modifier (accessible to same-assembly callers only).

---

## Value Objects

Value objects serialize correctly when they have public setters:

<!-- snippet: skill-value-object-factory -->
<a id='snippet-skill-value-object-factory'></a>
```cs
[Factory]
public partial record SkillMoney
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    [Create]
    public void Create(decimal amount, string currency = "USD")
    {
        Amount = amount;
        Currency = currency;
    }

    public static SkillMoney Zero => new() { Amount = 0, Currency = "USD" };

    public SkillMoney Add(SkillMoney other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new SkillMoney { Amount = Amount + other.Amount, Currency = Currency };
    }

    public static SkillMoney operator +(SkillMoney a, SkillMoney b)
    {
        return a.Add(b);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ValueObjectSamples.cs#L5-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Even record types need public setters for serialization.

---

## Complex Aggregate with Child Collections

<!-- snippet: skill-complex-aggregate -->
<a id='snippet-skill-complex-aggregate'></a>
```cs
[Factory]
public partial class SkillEmployeeWithAssignments : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public SkillAssignmentList Assignments { get; set; } = null!;
    public decimal TotalHours => Assignments?.CalculateTotalHours() ?? 0;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Create]
    internal void Create(
        string firstName,
        string lastName,
        [Service] ISkillAssignmentListFactory assignmentListFactory)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Assignments = assignmentListFactory.Create();
        IsNew = true;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(
        Guid id,
        [Service] ISkillAssignmentListFactory assignmentListFactory,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;

        // Fetch child collection with data
        var assignmentData = new List<(int, string, decimal, DateTime)>
        {
            (1, "Project Alpha", 40, DateTime.Today),
            (2, "Project Beta", 20, DateTime.Today.AddDays(7))
        };
#pragma warning disable CA2016 // Factory method does not accept CancellationToken
        Assignments = assignmentListFactory.Fetch(assignmentData);
#pragma warning restore CA2016
        IsNew = false;
        return true;
    }

    // Domain method - business logic in entity
    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        Assignments.AddAssignment(projectName, hoursAllocated, startDate);
    }
}

[Factory]
public partial class SkillAssignmentList : List<Assignment>
{
    private readonly IAssignmentFactory _assignmentFactory;

    [Create]
    public SkillAssignmentList([Service] IAssignmentFactory assignmentFactory)
    {
        _assignmentFactory = assignmentFactory;
    }

    [Fetch]
    public void Fetch(
        List<(int Id, string ProjectName, decimal Hours, DateTime StartDate)> data,
        [Service] IAssignmentFactory assignmentFactory)
    {
        foreach (var item in data)
        {
            var assignment = assignmentFactory.Fetch(
                item.Id, item.ProjectName, item.Hours, item.StartDate);
            Add(assignment);
        }
    }

    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        var assignment = _assignmentFactory.Create(projectName, hoursAllocated, startDate);
        Add(assignment);
    }

    public decimal CalculateTotalHours()
    {
        return this.Sum(a => a.HoursAllocated);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ComplexAggregateSamples.cs#L6-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-complex-aggregate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Collection uses constructor injection (`[Service]` on constructor) so factory is available after round-trip.

---

## Child Collection Factory

<!-- snippet: collection-factory-basic -->
<a id='snippet-collection-factory-basic'></a>
```cs
/// <summary>
/// Creates an empty list. Public and local, so a root created on the client
/// can create its list there (R-C).
/// </summary>
[Create]
public void Create()
{
}

/// <summary>
/// Loads the lines from the order row's line rows, handing each line its row.
/// </summary>
[Fetch]
internal void Fetch(IEnumerable<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
{
    foreach (var row in rows)
    {
        Add((OrderLine)lineFactory.Fetch(row));
    }
}

/// <summary>
/// The cascade: makes, finds or removes each line's row and hands each live
/// line its row through the line factory's Save.
/// </summary>
/// <remarks>
/// Called by Order.Insert and Order.Update through IOrderLineListFactory.Save,
/// with the order row's line collection. Neither the list nor the root maps a
/// line's row; the line does.
/// </remarks>
[Update]
internal void Update(ICollection<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
{
    foreach (var line in this)
    {
        if (line.IsDeleted)
        {
            rows.Remove(rows.Single(r => r.Id == line.Id));
            continue;
        }

        OrderLineRow row;
        if (line.IsNew)
        {
            row = new OrderLineRow();
            rows.Add(row);
        }
        else
        {
            row = rows.Single(r => r.Id == line.Id);
        }

        lineFactory.Save(line, row);
    }

    // The removed lines' rows are gone; the list the save returns holds live lines only.
    RemoveAll(l => l.IsDeleted);
}

// -------------------------------------------------------------------------
// Domain Methods
// -------------------------------------------------------------------------

public void AddLine(IOrderLine line)
{
    Add((OrderLine)line);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L283-L351' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`OrderLineRow` is the example's stored row for one line, held in the order row's line collection. The list holds no factory: it takes `IOrderLineFactory` as a method parameter in each operation, so nothing has to survive the round trip.

---

## Testing with ClientServerContainers

Validate serialization round-trips using the two DI container pattern:

<!-- snippet: clientserver-container-usage -->
<a id='snippet-clientserver-container-usage'></a>
```cs
[Fact]
public void Local_Create_WorksWithoutSerialization()
{
    var (server, client, local) = ClientServerContainers.Scopes();
    var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();
    var employee = factory.Create();  // Runs locally (Logical mode)
    Assert.NotNull(employee);
    server.Dispose(); client.Dispose(); local.Dispose();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/ClientServerContainerSamples.cs#L63-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Container types:**
- `client` - Simulates remote client (serializes through HTTP simulation)
- `server` - Direct server execution (no serialization)
- `local` - Single-tier mode (factory runs locally)

---

## NuGet Package Structure

When consuming RemoteFactory via NuGet:

```xml
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="x.y.z" />
```

**Neatoo.RemoteFactory**: Core library + embedded source generator
**Neatoo.RemoteFactory.AspNetCore**: Server-side ASP.NET Core integration

---

## Framework Support

RemoteFactory supports:
- .NET 9.0 (STS)
- .NET 10.0 (LTS)

Both frameworks are included in the NuGet packages.

---

## Generated Code Location

Generated code appears in:
```
obj/Debug/{tfm}/generated/Neatoo.Generator/Neatoo.Factory/
```

Files generated:
- `{Namespace}.{TypeName}Factory.g.cs` - Factory interface and implementation
- `{Namespace}.{TypeName}.Ordinal.g.cs` - Serialization implementation

---

## Assembly-Level Attributes

### [assembly: FactoryHintNameLength]

Limits generated file hint name length for Windows path limits:

<!-- snippet: attributes-factoryhintnamelength -->
<a id='snippet-attributes-factoryhintnamelength'></a>
```cs
// Increase hint name length to accommodate long namespace/type names
// Use when hitting Windows path length limits (260 characters)
[assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/AssemblyAttributes.cs#L3-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength' title='Start of snippet'>anchor</a></sup>
<a id='snippet-attributes-factoryhintnamelength-1'></a>
```cs
// Limits generated file name length for Windows path limits
// [assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L5-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use when hitting Windows 260-character path limits with deeply nested namespaces or generic types.

---

## Attribute Inheritance Rules

| Attribute | Inherited | Notes |
|-----------|-----------|-------|
| `[Factory]` | Yes | Derived classes get factories |
| `[SuppressFactory]` | Yes | Stops inheritance chain |
| `[Remote]` | Yes | Derived methods are also remote |
| `[Create]`, `[Fetch]`, etc. | No | Must apply to each method |
| `[Service]` | No | Must apply to each parameter |
| `[AuthorizeFactory<T>]` | No | Must apply per class |
| `[AspAuthorize]` | No | Must apply per method |

**Example:**

<!-- snippet: attributes-inheritance -->
<a id='snippet-attributes-inheritance'></a>
```cs
[Factory]   // Inherited: Yes
public partial class BaseWithFactory
{
    [Create]    // Inherited: No
    public BaseWithFactory() { }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    internal Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);
}

public partial class DerivedEntity : BaseWithFactory
{
    // Inherits [Factory] and [Remote] from base
    // Does NOT inherit [Create] - must redeclare
    [Create]
    public DerivedEntity() : base() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L222-L240' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Troubleshooting

### "Type not registered" at runtime

The factory method wasn't registered. Check:
1. Is the assembly passed to `AddNeatooRemoteFactory()`?
2. Does the class have `[Factory]` attribute?
3. Does the method have appropriate operation attribute (`[Create]`, `[Fetch]`, etc.)?

### Serialization returns null properties

Properties need public setters. Change:
```csharp
public int Id { get; private set; }  // Won't serialize
```
To:
```csharp
public int Id { get; set; }  // Will serialize
```

### CS0260 compilation error

Missing `partial` keyword:
```csharp
[Factory]
public partial class MyEntity { }  // Add 'partial'
```

### Method-injected service is null

Services injected via method parameters are NOT serialized:
```csharp
// This field will be null after round-trip
private IService _service;

[Remote, Create]
internal void Create([Service] IService service)
{
    _service = service;  // WRONG - will be null on client
}
```

Use constructor injection if you need the service after serialization:
```csharp
public MyEntity([Service] IService service)
{
    _service = service;  // Available on both sides
}
```

### N+1 remote calls performance issue

Child entities should NOT have `[Remote]`:
```csharp
// WRONG - each line causes a remote call
[Factory]
internal partial class Assignment : IAssignment
{
    [Remote, Create]  // Remove [Remote]
    internal void Create() { }
}
```
