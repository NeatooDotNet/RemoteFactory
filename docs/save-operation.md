# Save Operation

The Save operation provides automatic routing to Insert, Update, or Delete based on entity state.

## IFactorySave Interface

Classes implementing `IFactorySaveMeta` get an `IFactorySave<T>` interface on their factory with a `Save()` method.

### Step 1: Implement IFactorySaveMeta

In the Domain layer, define an Employee entity that implements `IFactorySaveMeta` for state tracking:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
/// <summary>
/// Employee entity implementing IFactorySaveMeta for state tracking.
/// IsNew and IsDeleted determine which save operation to execute.
/// </summary>
[Factory]
public partial class EmployeeSaveState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// True for new entities not yet persisted.
    /// Set to true in constructor, false after Fetch or Insert.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// Set by application code before calling Save().
    /// </summary>
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeSaveState()
    {
        Id = Guid.NewGuid();
        // IsNew defaults to true for new entities
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false; // Fetched entities are not new
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false; // No longer new after insert
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L7-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IFactorySaveMeta` requires two properties:
- `IsNew`: true for new entities not yet persisted
- `IsDeleted`: true for entities marked for deletion

### Step 2: Implement Write Operations

In the Domain layer, add Insert, Update, and Delete operations to the Employee entity:

<!-- snippet: save-write-operations -->
<a id='snippet-save-write-operations'></a>
```cs
/// <summary>
/// Complete Insert, Update, and Delete operations with repository patterns.
/// </summary>
[Factory]
public partial class EmployeeWriteOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWriteOps() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Insert: Persists a new entity. Sets IsNew = false after success.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Update: Persists changes to an existing entity.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Delete: Removes the entity from persistence.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L89-L166' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-write-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 3: Use Save Method

The Application layer uses the generated factory's Save method. The factory routes to the appropriate operation:

<!-- snippet: save-usage -->
<a id='snippet-save-usage'></a>
```cs
/// <summary>
/// Demonstrates calling factory.Save() method.
/// Generated factory interface follows naming: I{ClassName}Factory.
/// </summary>
public static class SaveUsageSample
{
    /// <summary>
    /// Demonstrates Save workflow with generated factory.
    /// Save returns IFactorySaveMeta which must be cast to the concrete type.
    /// </summary>
    public static async Task SaveWorkflow(
        IEmployeeSaveStateSampleFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Create new employee - returns concrete EmployeeSaveStateSample type
        EmployeeSaveStateSample employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";

        // Save routes to Insert (IsNew = true)
        // Cast result back to concrete type
        var saved = (EmployeeSaveStateSample?)await factory.Save(employee);

        // Modify and save again (IsNew = false, routes to Update)
        saved!.FirstName = "Jane";
        saved = (EmployeeSaveStateSample?)await factory.Save(saved);

        // Mark for deletion and save (routes to Delete)
        // IsDeleted is settable on the concrete type
        saved!.IsDeleted = true;
        await factory.Save(saved);
    }
}

/// <summary>
/// Define factory interface to match the generated factory from EmployeeSaveStateSample.
/// This interface is automatically generated for [Factory] classes with IFactorySaveMeta.
/// </summary>
public interface IEmployeeSaveStateSampleFactory : IFactorySave<EmployeeSaveStateSample>
{
    EmployeeSaveStateSample Create();
}

[Factory]
public partial class EmployeeSaveStateSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeSaveStateSample() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }

    [Remote, Update]
    public Task Update(CancellationToken ct) { return Task.CompletedTask; }

    [Remote, Delete]
    public Task Delete(CancellationToken ct) { return Task.CompletedTask; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L263-L329' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory's `Save()` method examines `IsNew` and `IsDeleted` to determine which operation to call.

## Routing Logic

| IsNew | IsDeleted | Operation | Description |
|-------|-----------|-----------|-------------|
| true | false | **Insert** | New entity, persist to database |
| false | false | **Update** | Existing entity, persist changes |
| false | true | **Delete** | Existing entity, remove from database |
| true | true | **None** | New entity deleted before save, no-op |

Generated Save method:

The generator creates Save routing logic in the factory:

```csharp
// Source: Generated factory pattern from Generated/Neatoo.Generator/...
public virtual Task<ITestAggregate?> LocalSave(ITestAggregate target, CancellationToken ct = default)
{
    if (target.IsDeleted)
    {
        if (target.IsNew)
        {
            // New entity deleted before save - no-op
            return Task.FromResult(default(ITestAggregate));
        }
        // Existing entity marked for deletion
        return LocalDelete(target, ct)!;
    }
    else if (target.IsNew)
    {
        // New entity - insert
        return LocalInsert(target, ct)!;
    }
    else
    {
        // Existing entity - update
        return LocalUpdate(target, ct)!;
    }
}
```

*Source: Pattern from `Generated/Neatoo.Generator/Neatoo.Factory/` for types implementing `IFactorySaveMeta`*

## State Management

Track state in your domain model:

### Constructor Sets IsNew

In the Application layer, demonstrate how Create initializes state:

<!-- snippet: save-state-new -->
<a id='snippet-save-state-new'></a>
```cs
/// <summary>
/// Demonstrates Create initializing IsNew = true state.
/// </summary>
[Factory]
public partial class EmployeeNewState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Create sets IsNew = true for new entities.
    /// Workflow: Create() -> modify -> Save() -> Insert
    /// </summary>
    [Create]
    public EmployeeNewState()
    {
        Id = Guid.NewGuid();
        // IsNew is true by default - new entity
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L168-L206' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetch Clears IsNew

In the Application layer, demonstrate how Fetch sets state for existing entities:

<!-- snippet: save-state-fetch -->
<a id='snippet-save-state-fetch'></a>
```cs
/// <summary>
/// Demonstrates Fetch setting IsNew = false for existing entities.
/// </summary>
[Factory]
public partial class EmployeeFetchState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFetchState() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Fetch sets IsNew = false after loading.
    /// Workflow: Fetch() -> modify -> Save() -> Update
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false; // Existing entity - not new
        return true;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L208-L253' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### MarkDeleted Sets IsDeleted

In the Application layer, demonstrate deletion workflow:

<!-- snippet: save-state-delete -->
<a id='snippet-save-state-delete'></a>
```cs
/// <summary>
/// Demonstrates deletion workflow with IsDeleted = true.
/// </summary>
[Factory]
public partial class EmployeeDeleteState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeDeleteState() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Mark for deletion and save.
    /// Workflow: Fetch() -> entity.IsDeleted = true -> Save() -> Delete
    /// </summary>
    public void MarkDeleted()
    {
        IsDeleted = true;
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L255-L297' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Complete Example

In the Domain layer, here's a complete Department entity with all CRUD operations:

<!-- snippet: save-complete-example -->
<a id='snippet-save-complete-example'></a>
```cs
/// <summary>
/// Complete Department aggregate with all CRUD operations and IFactorySaveMeta.
/// </summary>
[Factory]
public partial class DepartmentSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public DepartmentSample()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = new DepartmentEntity { Id = Id, Name = Name, Code = Code, ManagerId = ManagerId };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = new DepartmentEntity { Id = Id, Name = Name, Code = Code, ManagerId = ManagerId };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L299-L357' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Usage:

<!-- snippet: save-complete-usage -->
<a id='snippet-save-complete-usage'></a>
```cs
/// <summary>
/// Complete Save workflow example.
/// </summary>
public class SaveCompleteUsageTests
{
    [Fact]
    public async Task CompleteSaveWorkflow()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create new employee
        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane.smith@example.com");
        employee.Position = "Designer";
        employee.Salary = new Money(70000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Save workflow: Create -> Insert -> Update -> Delete

        // 1. Insert (IsNew = true)
        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.False(employee.IsNew);
        var id = employee.Id;

        // 2. Fetch existing
        employee = await factory.Fetch(id);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // 3. Update (IsNew = false, IsDeleted = false)
        employee.Position = "Senior Designer";
        employee = await factory.Save(employee);
        Assert.Equal("Senior Designer", employee.Position);

        // 4. Delete (IsDeleted = true)
        employee.IsDeleted = true;
        await factory.Save(employee);

        // Verify deleted
        var deleted = await factory.Fetch(id);
        Assert.Null(deleted);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L266-L315' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Return Values

Save returns the entity or null:

```csharp
Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
```

**Returns null when:**
- Insert/Update/Delete operation returns false (not authorized or not found)
- IsNew = true and IsDeleted = true (new entity deleted before save)

**Returns entity when:**
- Operation succeeds (void or returns true)

## Partial Save Methods

You don't need to implement all three operations. Save routes based on what you've defined:

In the Domain layer, create an entity that only supports Insert (immutable after creation):

<!-- snippet: save-partial-methods -->
<a id='snippet-save-partial-methods'></a>
```cs
/// <summary>
/// Immutable audit log entry with only Insert (create-once pattern).
/// No Update or Delete operations defined.
/// </summary>
[Factory]
public partial class AuditLogEntry : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; } = "";
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string Details { get; private set; } = "";

    // IFactorySaveMeta - Insert only
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    // Note: Setting IsDeleted = true throws NotImplementedException
    // because Delete operation is not defined

    [Create]
    public static AuditLogEntry Create(string action, string entityType, Guid entityId, string details)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };
    }

    /// <summary>
    /// Only Insert is defined - audit logs are immutable.
    /// Save() routes new entities to Insert.
    /// Update and Delete are not supported.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(Action, EntityId, EntityType, Details, ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L359-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-partial-methods' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Common patterns:

| Operations Defined | Pattern | Save Behavior |
|-------------------|---------|---------------|
| Insert only | Create-once | Routes new entities to Insert, no-op for updates |
| Insert + Update | Full write | Routes to Insert or Update based on IsNew |
| Insert + Update + Delete | Full CRUD | Routes to all three based on state |
| Update + Delete | Modify/remove | No Insert allowed, can only modify or delete |

For Upsert (same method for Insert and Update), mark a single method with both `[Insert, Update]` attributes. See [Insert, Update, Delete Operations](factory-operations.md#insert-update-delete-operations) for details.

## Authorization with Save

Apply authorization to individual operations:

In the Domain layer, define authorization for Employee operations with granular control:

<!-- snippet: save-authorization -->
<a id='snippet-save-authorization'></a>
```cs
/// <summary>
/// Authorization interface with granular Insert, Update, Delete checks.
/// </summary>
public interface IEmployeeWriteAuth
{
    /// <summary>
    /// All managers can insert new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    bool CanInsert();

    /// <summary>
    /// Managers can update their direct reports.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate();

    /// <summary>
    /// Only HR can delete employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

public class EmployeeWriteAuthImpl : IEmployeeWriteAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    public bool CanInsert() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    public bool CanUpdate() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    public bool CanDelete() =>
        _userContext.IsAuthenticated && _userContext.IsInRole("HR");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L353-L401' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Or to Save as a whole:

In the Domain layer, use a single authorization check for all write operations:

<!-- snippet: save-authorization-combined -->
<a id='snippet-save-authorization-combined'></a>
```cs
/// <summary>
/// Single authorization check covering all write operations.
/// </summary>
public interface IEmployeeWriteCombinedAuth
{
    /// <summary>
    /// Single method authorizes all write operations (Insert, Update, Delete).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public class EmployeeWriteCombinedAuthImpl : IEmployeeWriteCombinedAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteCombinedAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L403-L430' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory checks authorization before routing.

## Validation Before Save

Validate state before saving:

In the Domain layer, use data annotations for validation:

<!-- snippet: save-validation -->
<a id='snippet-save-validation'></a>
```cs
/// <summary>
/// Employee with data annotation validation attributes.
/// </summary>
[Factory]
public partial class EmployeeValidated : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(0, 10000000, ErrorMessage = "Salary must be between 0 and 10,000,000")]
    public decimal Salary { get; set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeValidated() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L470-L514' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throw exceptions for validation failures:

In the Domain layer, perform server-side validation in the Insert method:

<!-- snippet: save-validation-throw -->
<a id='snippet-save-validation-throw'></a>
```cs
/// <summary>
/// Employee with server-side validation in Insert method.
/// </summary>
[Factory]
public partial class EmployeeServerValidated : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServerValidated() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Server-side validation before persisting.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        // Validate employee number format
        if (string.IsNullOrEmpty(EmployeeNumber) || !EmployeeNumber.StartsWith('E'))
        {
            throw new ArgumentException(
                "Employee number must start with 'E'",
                nameof(EmployeeNumber));
        }

        // Validate salary is reasonable
        if (Salary < 0)
        {
            throw new ArgumentException(
                "Salary cannot be negative",
                nameof(Salary));
        }

        if (Salary > 10_000_000)
        {
            throw new ArgumentException(
                "Salary exceeds maximum allowed value",
                nameof(Salary));
        }

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{EmployeeNumber.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L516-L574' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation-throw' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Optimistic Concurrency

Use version tokens or timestamps:

In the Domain layer, implement optimistic concurrency with row versioning:

<!-- snippet: save-optimistic-concurrency -->
<a id='snippet-save-optimistic-concurrency'></a>
```cs
/// <summary>
/// Demonstrates exception handling for optimistic concurrency.
/// RemoteFactory properly serializes exceptions across the client-server boundary.
/// </summary>
[Factory]
public partial class EmployeeWithConcurrency : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithConcurrency() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Update with concurrency handling.
    /// Exceptions are serialized and propagate to clients correctly.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        try
        {
            var entity = new EmployeeEntity
            {
                Id = Id, FirstName = Name, LastName = "",
                Email = $"{Name.ToLowerInvariant()}@example.com",
                DepartmentId = Guid.Empty, Position = "Updated",
                SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
            };
            await repo.UpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency"))
        {
            // Re-throw as a domain exception that clients understand
            throw new ConcurrencyException(
                "The employee was modified by another user. Please refresh and try again.",
                ex);
        }
    }
}

/// <summary>
/// Domain exception for concurrency conflicts.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L576-L641' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-optimistic-concurrency' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

EF Core DbUpdateConcurrencyException automatically becomes a 409 response when called remotely.

## Save Without Delete

If you don't implement Delete, IFactorySave still generates but throws `NotImplementedException` for deleted entities:

In the Domain layer, create an entity without Delete support:

<!-- snippet: save-no-delete -->
<a id='snippet-save-no-delete'></a>
```cs
/// <summary>
/// Entity with Insert and Update but no Delete support.
/// </summary>
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;

    // Note: If IsDeleted is set to true and Save() is called,
    // it throws NotImplementedException because Delete is not defined
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeNoDelete() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    // No [Delete] operation - Save() throws if IsDeleted = true
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/SaveOperationSamples.cs#L408-L468' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-no-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save throws `NotImplementedException` when `IsDeleted = true`.

## Alternative: Explicit Methods

Save is optional. You can always call Insert/Update/Delete directly:

In the Application layer, demonstrate explicit method calls vs Save:

<!-- snippet: save-explicit -->
<a id='snippet-save-explicit'></a>
```cs
/// <summary>
/// Save method routing based on IsNew and IsDeleted flags.
/// Note: Insert/Update/Delete are internal methods - use Save for routing.
/// </summary>
public class ExplicitMethodTests
{
    [Fact]
    public async Task SaveRoutesToInsertUpdateDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Explicit";
        employee.LastName = "Test";
        employee.Email = new EmailAddress("explicit.test@example.com");
        employee.Position = "Tester";
        employee.Salary = new Money(60000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert when IsNew = true
        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // Fetch and modify
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        fetched.Position = "Lead Tester";

        // Save routes to Update when IsNew = false, IsDeleted = false
        Assert.False(fetched.IsNew);
        Assert.False(fetched.IsDeleted);
        fetched = await factory.Save(fetched);
        Assert.NotNull(fetched);

        // Verify update
        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Lead Tester", updated?.Position);

        // Save routes to Delete when IsDeleted = true
        updated!.IsDeleted = true;
        await factory.Save(updated);

        // Verify deleted
        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L317-L369' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-explicit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use Save when:
- UI doesn't track entity state changes
- You want a single save button
- State-based routing simplifies client code

Use explicit methods when:
- Client knows the exact operation needed
- You want granular control
- Different UI actions map to different operations

## IFactorySaveMeta Extensions

Extend IFactorySave for batch operations:

In the Application layer, create utility methods for common save patterns:

<!-- snippet: save-extensions -->
<a id='snippet-save-extensions'></a>
```cs
/// <summary>
/// Save with validation and extensions pattern.
/// </summary>
[Factory]
public partial class EmployeeWithExtensions : IFactorySaveMeta, IFactoryOnStart
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithExtensions() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Pre-save validation via IFactoryOnStart lifecycle hook.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new System.ComponentModel.DataAnnotations.ValidationException("FirstName is required");
            if (string.IsNullOrWhiteSpace(LastName))
                throw new System.ComponentModel.DataAnnotations.ValidationException("LastName is required");
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L178-L261' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-extensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Track additional state without affecting Save routing.

## Testing Save Routing

Test routing logic:

In the Tests layer, verify Save routes to the correct operation:

<!-- snippet: save-testing -->
<a id='snippet-save-testing'></a>
```cs
/// <summary>
/// Testing Save operation state transitions.
/// </summary>
public class SaveOperationTests
{
    [Fact]
    public async Task Save_NewEmployee_CallsInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Insert";
        employee.Email = new EmailAddress("test.insert@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Assert initial state
        Assert.True(employee.IsNew);
        Assert.False(employee.IsDeleted);

        // Act - Save routes to Insert when IsNew = true
        var saved = await factory.Save(employee);

        // Assert - IsNew = false after insert
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_ExistingEmployee_CallsUpdate()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create and save initial employee
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Update";
        employee.Email = new EmailAddress("test.update@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);

        // Assert existing state
        Assert.False(employee.IsNew);
        Assert.False(employee.IsDeleted);

        // Act - Modify and save (routes to Update when IsNew = false)
        employee.Position = "Senior Developer";
        var updated = await factory.Save(employee);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Senior Developer", updated.Position);
    }

    [Fact]
    public async Task Save_DeletedEmployee_CallsDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create and save employee
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Delete";
        employee.Email = new EmailAddress("test.delete@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);
        var employeeId = employee.Id;

        // Act - Mark for deletion and save (routes to Delete when IsDeleted = true)
        employee.IsDeleted = true;
        await factory.Save(employee);

        // Assert - Employee no longer exists
        var deleted = await factory.Fetch(employeeId);
        Assert.Null(deleted);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L174-L264' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - Insert, Update, Delete details
- [Authorization](authorization.md) - Secure save operations
- [Serialization](serialization.md) - Entity state serialization
