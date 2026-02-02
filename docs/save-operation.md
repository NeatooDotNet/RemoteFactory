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
/// Employee aggregate implementing IFactorySaveMeta for automatic Save routing.
/// </summary>
[Factory]
public partial class EmployeeForSave : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }

    /// <summary>
    /// True for new entities not yet persisted. Defaults to true.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new employee with a generated Id.
    /// IsNew defaults to true, so Save will route to Insert.
    /// </summary>
    [Create]
    public EmployeeForSave()
    {
        Id = Guid.NewGuid();
        // IsNew = true by default
    }

    /// <summary>
    /// Loads an existing employee from the repository.
    /// Sets IsNew = false so Save will route to Update.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false; // Fetched entities are not new
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L10-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with full Insert, Update, Delete operations.
/// </summary>
[Factory]
public partial class EmployeeCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCrud()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Loads an existing employee from the repository.
    /// Sets IsNew = false so Save will route to Update.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new employee into the database.
    /// Called by Save when IsNew = true.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId,
            HireDate = DateTime.UtcNow // Created timestamp
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false; // After insert, no longer new
    }

    /// <summary>
    /// Updates an existing employee in the database.
    /// Called by Save when IsNew = false and IsDeleted = false.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Employee {Id} not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.DepartmentId = DepartmentId;
        // Modified timestamp would be updated here

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the employee from the database.
    /// Called by Save when IsDeleted = true.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L67-L156' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-write-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 3: Use Save Method

The Application layer uses the generated factory's Save method. The factory routes to the appropriate operation:

<!-- snippet: save-usage -->
<a id='snippet-save-usage'></a>
```cs
/// <summary>
/// Demonstrates IFactorySave usage showing Insert, Update, and Delete routing.
/// </summary>
public class SaveUsageDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public SaveUsageDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Complete Save lifecycle: Insert, Update, Delete.
    /// </summary>
    public async Task DemonstrateSaveAsync()
    {
        // 1. Create new employee - IsNew = true by default
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert because IsNew = true
        var inserted = await _factory.Save(employee);

        // After Insert, IsNew = false
        var savedEmployee = (EmployeeCrud)inserted!;

        // 2. Modify and save - routes to Update because IsNew = false
        savedEmployee.FirstName = "Jane";
        await _factory.Save(savedEmployee);

        // 3. Mark for deletion and save - routes to Delete
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L10-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage' title='Start of snippet'>anchor</a></sup>
<a id='snippet-save-usage-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L267-L333' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage-1' title='Start of snippet'>anchor</a></sup>
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

<!-- snippet: save-generated -->
<a id='snippet-save-generated'></a>
```cs
/// <summary>
/// Conceptual illustration of the generated Save method routing logic.
/// The actual implementation is source-generated by RemoteFactory.
/// </summary>
public static class SaveRoutingLogic
{
    // The generated Save method follows this decision tree:
    //
    // async Task<T?> LocalSave(T entity, CancellationToken ct)
    // {
    //     if (entity.IsDeleted)
    //     {
    //         if (entity.IsNew)
    //             return default;  // New entity deleted before save = no-op
    //         else
    //             return await LocalDelete(ct);  // Existing entity = delete
    //     }
    //
    //     if (entity.IsNew)
    //         return await LocalInsert(ct);  // New entity = insert
    //
    //     return await LocalUpdate(ct);  // Existing entity = update
    // }
    //
    // Routing summary:
    // | IsNew  | IsDeleted | Result      |
    // |--------|-----------|-------------|
    // | true   | false     | LocalInsert |
    // | false  | false     | LocalUpdate |
    // | false  | true      | LocalDelete |
    // | true   | true      | null (no-op)|
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L162-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## State Management

Track state in your domain model:

### Constructor Sets IsNew

In the Application layer, demonstrate how Create initializes state:

<!-- snippet: save-state-new -->
<a id='snippet-save-state-new'></a>
```cs
/// <summary>
/// Demonstrates state after factory.Create().
/// </summary>
public class CreateStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public CreateStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateCreateStateAsync()
    {
        // Create returns a new entity
        var employee = _factory.Create();

        // New entities have IsNew = true, IsDeleted = false
        var isNewAfterCreate = employee.IsNew;      // true
        var isDeletedAfterCreate = employee.IsDeleted; // false

        // Set required properties
        employee.FirstName = "Alice";
        employee.LastName = "Smith";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert because IsNew = true
        var saved = await _factory.Save(employee);
        var savedEmployee = (EmployeeCrud)saved!;

        // After Insert, IsNew = false
        var isNewAfterSave = savedEmployee.IsNew; // false

        // Verify state transitions
        if (!isNewAfterCreate)
            throw new InvalidOperationException("IsNew should be true after Create");
        if (isNewAfterSave)
            throw new InvalidOperationException("IsNew should be false after Insert");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L55-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetch Clears IsNew

In the Application layer, demonstrate how Fetch sets state for existing entities:

<!-- snippet: save-state-fetch -->
<a id='snippet-save-state-fetch'></a>
```cs
/// <summary>
/// Demonstrates state after factory.Fetch().
/// </summary>
public class FetchStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public FetchStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateFetchStateAsync(Guid existingEmployeeId)
    {
        // Fetch returns an existing entity
        var employee = await _factory.Fetch(existingEmployeeId);

        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Fetched entities have IsNew = false
        var isNewAfterFetch = employee.IsNew; // false

        // Modify and save - routes to Update (not Insert)
        employee.FirstName = "Updated Name";
        await _factory.Save(employee);

        // Verify Update was called (IsNew remains false)
        if (isNewAfterFetch)
            throw new InvalidOperationException("IsNew should be false after Fetch");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L102-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### MarkDeleted Sets IsDeleted

In the Application layer, demonstrate deletion workflow:

<!-- snippet: save-state-delete -->
<a id='snippet-save-state-delete'></a>
```cs
/// <summary>
/// Demonstrates deletion workflow via Save.
/// </summary>
public class DeleteStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public DeleteStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateDeleteStateAsync(Guid existingEmployeeId)
    {
        // Start with existing employee
        var employee = await _factory.Fetch(existingEmployeeId);

        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Verify initial state: IsNew = false, IsDeleted = false
        var isNewBefore = employee.IsNew;       // false
        var isDeletedBefore = employee.IsDeleted; // false

        // Mark for deletion
        employee.IsDeleted = true;

        // Verify state: IsNew = false, IsDeleted = true
        var isNewAfterMark = employee.IsNew;       // false
        var isDeletedAfterMark = employee.IsDeleted; // true

        // Save routes to Delete because IsDeleted = true
        var result = await _factory.Save(employee);

        // Save returns the deleted entity
        var deletedEmployee = result;

        // Verify state transitions
        if (isNewBefore)
            throw new InvalidOperationException("Fetched entity should not be new");
        if (!isDeletedAfterMark)
            throw new InvalidOperationException("IsDeleted should be true after marking");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L141-L186' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Complete Example

In the Domain layer, here's a complete Department entity with all CRUD operations:

<!-- snippet: save-complete-example -->
<a id='snippet-save-complete-example'></a>
```cs
/// <summary>
/// Complete Department aggregate with full CRUD operations via IFactorySaveMeta.
/// </summary>
[Factory]
public partial class DepartmentCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public decimal Budget { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new department with generated Id and default active status.
    /// </summary>
    [Create]
    public DepartmentCrud()
    {
        Id = Guid.NewGuid();
        IsActive = true;
    }

    /// <summary>
    /// Loads an existing department from the repository.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
        // Budget and IsActive would be loaded from extended entity
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new department into the database.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = new DepartmentEntity
        {
            Id = Id,
            Name = Name,
            Code = Code,
            ManagerId = ManagerId
            // Budget, IsActive, Created/Modified timestamps would be set in extended entity
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing department in the database.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Department {Id} not found");

        entity.Name = Name;
        entity.Code = Code;
        entity.ManagerId = ManagerId;
        // Budget, IsActive, Modified timestamp would be updated in extended entity

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the department from the database.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/DepartmentCrudSamples.cs#L10-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Usage:

<!-- snippet: save-complete-usage -->
<a id='snippet-save-complete-usage'></a>
```cs
/// <summary>
/// Demonstrates complete CRUD workflow with Department using Save.
/// </summary>
public class DepartmentCrudWorkflow
{
    private readonly IDepartmentCrudFactory _factory;

    public DepartmentCrudWorkflow(IDepartmentCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task<Guid> DemonstrateCrudAsync()
    {
        // CREATE: Create new department
        var department = _factory.Create();
        department.Name = "Engineering";
        department.Code = "ENG";
        department.Budget = 1_000_000m;
        department.IsActive = true;

        // Save routes to Insert (IsNew = true)
        var created = await _factory.Save(department);
        var savedDept = (DepartmentCrud)created!;
        var departmentId = savedDept.Id;

        // READ: Fetch the department
        var fetched = await _factory.Fetch(departmentId);

        if (fetched == null)
            throw new InvalidOperationException("Department not found");

        // UPDATE: Modify budget
        fetched.Budget = 1_500_000m;

        // Save routes to Update (IsNew = false)
        await _factory.Save(fetched);

        // DELETE: Mark for deletion
        fetched.IsDeleted = true;

        // Save routes to Delete (IsDeleted = true)
        await _factory.Save(fetched);

        return departmentId;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/DepartmentUsageSamples.cs#L10-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-usage' title='Start of snippet'>anchor</a></sup>
<a id='snippet-save-complete-usage-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L266-L315' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-usage-1' title='Start of snippet'>anchor</a></sup>
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
/// AuditLog entity that only supports Insert - records are immutable after creation.
/// Use this pattern for audit logs, event sourcing, or compliance records.
/// </summary>
[Factory]
public partial class AuditLog : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; private set; }
    public Guid UserId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new audit log entry with auto-generated Id and timestamp.
    /// </summary>
    [Create]
    public AuditLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Inserts the audit log record.
    /// This is the ONLY write operation - audit logs are immutable.
    /// </summary>
    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        // Persist to audit store
        IsNew = false;
        return Task.CompletedTask;
    }

    // No Update method = entity becomes read-only after creation
    // No Delete method = audit records cannot be deleted

    // Save behavior:
    // - If IsNew: routes to Insert
    // - If not IsNew: no-op (no Update defined)
    // - If IsDeleted: throws NotImplementedException (no Delete defined)
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/PartialSaveSamples.cs#L9-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-partial-methods' title='Start of snippet'>anchor</a></sup>
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
/// Authorization interface with granular control over Employee operations.
/// </summary>
public interface IEmployeeWriteAuth
{
    /// <summary>
    /// Controls Create operation authorization.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Controls Insert, Update, and Delete operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Authorization implementation with role-based rules.
/// </summary>
public class EmployeeWriteAuth : IEmployeeWriteAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Any authenticated user can create employees.
    /// </summary>
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Only HR or Admin can modify employee records.
    /// </summary>
    public bool CanWrite()
    {
        return _userContext.IsInRole("HR") || _userContext.IsInRole("Admin");
    }
}

/// <summary>
/// Employee aggregate with granular authorization on write operations.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeWriteAuth>]
public partial class AuthorizedEmployeeWrite : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployeeWrite()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert requires CanWrite() = true.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Update requires CanWrite() = true.
    /// </summary>
    [Remote, Update]
    public Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Delete requires CanWrite() = true.
    /// </summary>
    [Remote, Delete]
    public Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveAuthorizationSamples.cs#L10-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Or to Save as a whole:

In the Domain layer, use a single authorization check for all write operations:

<!-- snippet: save-authorization-combined -->
<a id='snippet-save-authorization-combined'></a>
```cs
/// <summary>
/// Authorization interface with single check for all write operations.
/// Write = Insert | Update | Delete
/// </summary>
public interface ICombinedWriteAuth
{
    /// <summary>
    /// Single authorization check covering Insert, Update, and Delete.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Combined authorization implementation.
/// </summary>
public class CombinedWriteAuth : ICombinedWriteAuth
{
    private readonly IUserContext _userContext;

    public CombinedWriteAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Only Editor or Admin can perform any write operation.
    /// </summary>
    public bool CanWrite()
    {
        return _userContext.IsInRole("Editor") || _userContext.IsInRole("Admin");
    }
}

/// <summary>
/// Department aggregate with combined authorization for all write operations.
/// </summary>
[Factory]
[AuthorizeFactory<ICombinedWriteAuth>]
public partial class AuthorizedDepartmentWrite : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedDepartmentWrite()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveAuthorizationSamples.cs#L111-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory checks authorization before routing.

## Validation Before Save

Validate state before saving:

In the Domain layer, use data annotations for validation:

<!-- snippet: save-validation -->
<a id='snippet-save-validation'></a>
```cs
/// <summary>
/// Employee aggregate with validation attributes for Save operations.
/// </summary>
[Factory]
public partial class SaveValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be non-negative")]
    public decimal Salary { get; set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SaveValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert persists the employee after validation passes.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Validation happens before this method via SaveWithValidation helper
        IsNew = false;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Helper class for validating entities before save.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates an entity and saves if valid.
    /// Returns null with validation errors if invalid.
    /// </summary>
    public static async Task<(T? Result, ICollection<ValidationResult>? Errors)> SaveWithValidation<T>(
        IFactorySave<T> factory,
        T entity)
        where T : class, IFactorySaveMeta
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(entity);

        if (!Validator.TryValidateObject(entity, validationContext, validationResults, validateAllProperties: true))
        {
            return (null, validationResults);
        }

        var result = await factory.Save(entity);
        return (result as T, null);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveValidationSamples.cs#L11-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throw exceptions for validation failures:

In the Domain layer, perform server-side validation in the Insert method:

<!-- snippet: save-validation-throw -->
<a id='snippet-save-validation-throw'></a>
```cs
/// <summary>
/// Employee aggregate with server-side validation in Insert method.
/// </summary>
[Factory]
public partial class SaveServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SaveServerValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert with server-side validation.
    /// Throws ValidationException if validation fails.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Validate FirstName
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            throw new ValidationException("First name is required");
        }

        // Validate LastName
        if (string.IsNullOrWhiteSpace(LastName))
        {
            throw new ValidationException("Last name is required");
        }

        // Validate Email format if provided
        if (!string.IsNullOrEmpty(Email) && !Email.Contains('@'))
        {
            throw new ValidationException("Invalid email format");
        }

        // All validations passed - persist entity
        IsNew = false;
        return Task.CompletedTask;
    }
}

// Usage pattern:
// try
// {
//     await factory.Save(employee);
// }
// catch (ValidationException ex)
// {
//     // Handle validation failure
//     Console.WriteLine($"Validation failed: {ex.Message}");
// }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveValidationSamples.cs#L85-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation-throw' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Optimistic Concurrency

Use version tokens or timestamps:

In the Domain layer, implement optimistic concurrency with row versioning:

<!-- snippet: save-optimistic-concurrency -->
<a id='snippet-save-optimistic-concurrency'></a>
```cs
/// <summary>
/// Employee aggregate with optimistic concurrency control using row versioning.
/// </summary>
[Factory]
public partial class ConcurrentEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// Updated by database on each save.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ConcurrentEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches employee including RowVersion for concurrency checking.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        // RowVersion would be loaded from the entity in a real implementation
        // RowVersion = entity.RowVersion;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Update with concurrency check.
    /// Throws if another user modified the record.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var current = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Employee {Id} not found");

        // In a real EF Core implementation, the RowVersion would be compared:
        // if (current.RowVersion != null && RowVersion != null &&
        //     !current.RowVersion.SequenceEqual(RowVersion))
        // {
        //     throw new InvalidOperationException(
        //         "The record has been modified by another user. Please refresh and try again.");
        // }

        current.FirstName = FirstName;
        current.LastName = LastName;
        // RowVersion is updated automatically by database

        await repository.UpdateAsync(current, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveConcurrencySamples.cs#L10-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-optimistic-concurrency' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

EF Core DbUpdateConcurrencyException automatically becomes a 409 response when called remotely.

## Save Without Delete

If you don't implement Delete, IFactorySave still generates but throws `NotImplementedException` for deleted entities:

In the Domain layer, create an entity without Delete support:

<!-- snippet: save-no-delete -->
<a id='snippet-save-no-delete'></a>
```cs
/// <summary>
/// Employee entity that supports Insert and Update but not Delete.
/// Use for soft-delete patterns where actual deletion is not allowed.
/// </summary>
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeNoDelete()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    // NO Delete method defined
    // Setting IsDeleted = true and calling Save throws NotImplementedException
    // Use case: soft-delete pattern where records are deactivated, not removed
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/PartialSaveSamples.cs#L62-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-no-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save throws `NotImplementedException` when `IsDeleted = true`.

## Alternative: Explicit Methods

Save is optional. You can always call Insert/Update/Delete directly:

In the Application layer, demonstrate explicit method calls vs Save:

<!-- snippet: save-explicit -->
<a id='snippet-save-explicit'></a>
```cs
/// <summary>
/// Demonstrates Save method vs explicit Insert/Update/Delete calls.
/// </summary>
public class SaveVsExplicitDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public SaveVsExplicitDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Using Save with automatic routing based on state flags.
    /// Save examines IsNew and IsDeleted to determine which operation to call.
    /// </summary>
    public async Task UsingSaveAsync()
    {
        // Create new employee
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert (IsNew = true)
        var saved = await _factory.Save(employee);
        var savedEmployee = (EmployeeCrud)saved!;

        // Modify and Save routes to Update (IsNew = false)
        savedEmployee.FirstName = "Jane";
        await _factory.Save(savedEmployee);

        // Mark deleted and Save routes to Delete (IsDeleted = true)
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }

    // When to use Save:
    // - UI doesn't track state (single "Save" button)
    // - State-based routing simplifies client code
    // - Form-based applications where user edits then saves
    //
    // When to use explicit methods:
    // - Client knows the exact operation needed
    // - Different UI actions map to different operations
    // - You want granular control over each operation
    // - API endpoints that map directly to CRUD operations
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveExplicitSamples.cs#L10-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-explicit' title='Start of snippet'>anchor</a></sup>
<a id='snippet-save-explicit-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L317-L369' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-explicit-1' title='Start of snippet'>anchor</a></sup>
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
/// Utility methods for common save patterns.
/// </summary>
public static class SaveUtilities
{
    /// <summary>
    /// Saves with explicit cancellation check before save.
    /// </summary>
    public static async Task<T?> SaveWithCancellation<T>(
        IFactorySave<T> factory,
        T entity,
        CancellationToken cancellationToken)
        where T : class, IFactorySaveMeta
    {
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await factory.Save(entity, cancellationToken);
        return result as T;
    }

    /// <summary>
    /// Saves a batch of entities, checking cancellation between each save.
    /// Returns list of successfully saved entities.
    /// </summary>
    public static async Task<List<T>> SaveBatch<T>(
        IFactorySave<T> factory,
        IEnumerable<T> entities,
        CancellationToken cancellationToken)
        where T : class, IFactorySaveMeta
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(entities);

        var saved = new List<T>();

        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await factory.Save(entity, cancellationToken);
            if (result is T typedResult)
            {
                saved.Add(typedResult);
            }
        }

        return saved;
    }

    // Extension method pattern (alternative syntax):
    // public static async Task<T?> SaveWithCancel<T>(
    //     this IFactorySave<T> factory,
    //     T entity,
    //     CancellationToken ct)
    //     where T : class, IFactorySaveMeta
    // {
    //     ct.ThrowIfCancellationRequested();
    //     return await factory.Save(entity, ct) as T;
    // }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveExtensionsSamples.cs#L9-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-extensions' title='Start of snippet'>anchor</a></sup>
<a id='snippet-save-extensions-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L182-L265' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-extensions-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Track additional state without affecting Save routing.

## Testing Save Routing

Test routing logic:

In the Tests layer, verify Save routes to the correct operation:

<!-- snippet: save-testing -->
<a id='snippet-save-testing'></a>
```cs
/// <summary>
/// Unit tests verifying Save routes to the correct operation.
/// </summary>
public class SaveRoutingTests
{
    [Fact]
    public async Task Save_WhenIsNewTrue_RoutesToInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Assert initial state
        Assert.True(employee.IsNew);

        // Act - Save routes to Insert
        var result = await factory.Save(employee);
        var saved = (EmployeeCrud)result!;

        // Assert - Insert was called (IsNew becomes false)
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_AfterInsert_RoutesToUpdate()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Insert first
        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        // Act - Modify and save (should route to Update)
        savedEmployee.FirstName = "Jane";
        var updated = await factory.Save(savedEmployee);
        var updatedEmployee = (EmployeeCrud)updated!;

        // Assert - Update was called (verify modification persisted)
        Assert.Equal("Jane", updatedEmployee.FirstName);
        Assert.False(updatedEmployee.IsNew);
    }

    [Fact]
    public async Task Save_WhenIsDeletedTrue_RoutesToDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Insert first
        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        // Act - Mark deleted and save
        savedEmployee.IsDeleted = true;
        var result = await factory.Save(savedEmployee);

        // Assert - Delete was called (result is returned)
        Assert.NotNull(result);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/SaveOperation/SaveRoutingTestSample.cs#L11-L90' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing' title='Start of snippet'>anchor</a></sup>
<a id='snippet-save-testing-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L174-L264' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - Insert, Update, Delete details
- [Authorization](authorization.md) - Secure save operations
- [Serialization](serialization.md) - Entity state serialization
