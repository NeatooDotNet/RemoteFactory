# Save Operation

The Save operation provides automatic routing to Insert, Update, or Delete based on entity state.

## IFactorySave Interface

Classes implementing `IFactorySaveMeta` get an `IFactorySave<T>` interface on their factory with a `Save()` method.

### Step 1: Implement IFactorySaveMeta

In the Domain layer, define an Employee entity that implements `IFactorySaveMeta` for state tracking:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
// IFactorySaveMeta requires: IsNew (true for new entities) and IsDeleted (true for deletion)
public bool IsNew { get; private set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L21-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IFactorySaveMeta` requires two properties:
- `IsNew`: true for new entities not yet persisted
- `IsDeleted`: true for entities marked for deletion

### Step 2: Implement Write Operations

In the Domain layer, add Insert, Update, and Delete operations to the Employee entity:

<!-- snippet: save-write-operations -->
<a id='snippet-save-write-operations'></a>
```cs
// Save routes to Insert/Update/Delete based on IsNew and IsDeleted flags
[Remote, Insert]
public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
{
    await repo.AddAsync(new EmployeeEntity { Id = Id, FirstName = FirstName }, ct);
    IsNew = false;
}

[Remote, Update]
public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
{
    var e = await repo.GetByIdAsync(Id, ct);
    if (e != null) { e.FirstName = FirstName; await repo.UpdateAsync(e, ct); }
}

[Remote, Delete]
public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
{
    await repo.DeleteAsync(Id, ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L85-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-write-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 3: Use Save Method

The Application layer uses the generated factory's Save method. The factory routes to the appropriate operation:

<!-- snippet: save-usage -->
<a id='snippet-save-usage'></a>
```cs
// Save routes based on state: Insert (IsNew=true), Update (IsNew=false), Delete (IsDeleted=true)
var saved = await _factory.Save(employee);       // Insert
saved = await _factory.Save((EmployeeCrud)saved!); // Update
((EmployeeCrud)saved!).IsDeleted = true;
await _factory.Save((EmployeeCrud)saved);        // Delete
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L29-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage' title='Start of snippet'>anchor</a></sup>
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
// Save routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete
// | IsNew | IsDeleted | Operation |
// |-------|-----------|-----------|
// | true  | false     | Insert    |
// | false | false     | Update    |
// | false | true      | Delete    |
// | true  | true      | no-op     |
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L113-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## State Management

Track state in your domain model:

### Constructor Sets IsNew

In the Application layer, demonstrate how Create initializes state:

<!-- snippet: save-state-new -->
<a id='snippet-save-state-new'></a>
```cs
// Create sets IsNew = true; Save(Insert) sets IsNew = false
var employee = _factory.Create();      // IsNew = true
employee.FirstName = "Alice";
var saved = await _factory.Save(employee);
var result = (EmployeeCrud)saved!;     // IsNew = false
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L57-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetch Clears IsNew

In the Application layer, demonstrate how Fetch sets state for existing entities:

<!-- snippet: save-state-fetch -->
<a id='snippet-save-state-fetch'></a>
```cs
// Fetch sets IsNew = false; subsequent Save routes to Update
var employee = await _factory.Fetch(existingEmployeeId);  // IsNew = false
employee!.FirstName = "Updated";
await _factory.Save(employee);  // Routes to Update (not Insert)
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L88-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### MarkDeleted Sets IsDeleted

In the Application layer, demonstrate deletion workflow:

<!-- snippet: save-state-delete -->
<a id='snippet-save-state-delete'></a>
```cs
// Set IsDeleted = true; Save routes to Delete
employee.IsDeleted = true;
await _factory.Save(employee);  // Routes to Delete
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveUsageSamples.cs#L118-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Complete Example

In the Domain layer, here's a complete Department entity with all CRUD operations:

<!-- snippet: save-complete-example -->
<a id='snippet-save-complete-example'></a>
```cs
// Full CRUD: [Factory] + IFactorySaveMeta + Create/Fetch/Insert/Update/Delete
[Factory]
public partial class DepartmentCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public DepartmentCrud() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository r, CancellationToken ct) { return true; }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository r, CancellationToken ct) { IsNew = false; }

    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository r, CancellationToken ct) { }

    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository r, CancellationToken ct) { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/DepartmentCrudSamples.cs#L10-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Usage:

<!-- snippet: save-complete-usage -->
<a id='snippet-save-complete-usage'></a>
```cs
// Complete CRUD: Create -> Insert -> Fetch -> Update -> Delete
var dept = _factory.Create();
dept.Name = "Engineering";
dept = (DepartmentCrud)(await _factory.Save(dept))!;  // Insert
dept = (await _factory.Fetch(dept.Id))!;              // Fetch
dept.Name = "Engineering v2";
await _factory.Save(dept);                            // Update
dept.IsDeleted = true;
await _factory.Save(dept);                            // Delete
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/DepartmentUsageSamples.cs#L24-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-usage' title='Start of snippet'>anchor</a></sup>
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
// Insert-only entity: no Update/Delete means records are immutable after creation
[Factory]
public partial class AuditLog : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Action { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public AuditLog() { Id = Guid.NewGuid(); }
    [Remote, Insert] public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }
    // No [Update] or [Delete] = immutable after insert
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/PartialSaveSamples.cs#L9-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-partial-methods' title='Start of snippet'>anchor</a></sup>
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
// Granular authorization: CanCreate for Create, CanWrite for Insert/Update/Delete
public interface IEmployeeWriteAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)] bool CanCreate();  // Any authenticated
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite();    // HR or Admin only
}

[Factory]
[AuthorizeFactory<IEmployeeWriteAuth>]  // Factory checks auth before routing
public partial class AuthorizedEmployeeWrite : IFactorySaveMeta { /* ... */ }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveAuthorizationSamples.cs#L10-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Or to Save as a whole:

In the Domain layer, use a single authorization check for all write operations:

<!-- snippet: save-authorization-combined -->
<a id='snippet-save-authorization-combined'></a>
```cs
// Single auth check for all writes: Write = Insert | Update | Delete
public interface ICombinedWriteAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite();  // Covers all writes
}

[Factory]
[AuthorizeFactory<ICombinedWriteAuth>]  // Single check for Insert, Update, Delete
public partial class AuthorizedDepartmentWrite : IFactorySaveMeta { /* ... */ }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveAuthorizationSamples.cs#L61-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory checks authorization before routing.

## Validation Before Save

Validate state before saving:

In the Domain layer, use data annotations for validation:

<!-- snippet: save-validation -->
<a id='snippet-save-validation'></a>
```cs
// Use DataAnnotations for validation; call Validator.TryValidateObject before Save
[Factory]
public partial class SaveValidatedEmployee : IFactorySaveMeta
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [EmailAddress] public string? Email { get; set; }
    [Range(0, double.MaxValue)] public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    /* ... */
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveValidationSamples.cs#L11-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throw exceptions for validation failures:

In the Domain layer, perform server-side validation in the Insert method:

<!-- snippet: save-validation-throw -->
<a id='snippet-save-validation-throw'></a>
```cs
// Throw ValidationException in Insert/Update for server-side validation
[Remote, Insert]
public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(FirstName)) throw new ValidationException("First name required");
    if (string.IsNullOrWhiteSpace(LastName)) throw new ValidationException("Last name required");
    IsNew = false;
    return Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveValidationSamples.cs#L74-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation-throw' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Optimistic Concurrency

Use version tokens or timestamps:

In the Domain layer, implement optimistic concurrency with row versioning:

<!-- snippet: save-optimistic-concurrency -->
<a id='snippet-save-optimistic-concurrency'></a>
```cs
// Add RowVersion property; EF Core throws DbUpdateConcurrencyException on conflict -> 409 response
[Factory]
public partial class ConcurrentEmployee : IFactorySaveMeta
{
    public byte[]? RowVersion { get; private set; }  // Concurrency token, auto-updated by database
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    /* Fetch loads RowVersion, Update includes it for conflict detection */
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/SaveConcurrencySamples.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-optimistic-concurrency' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

EF Core DbUpdateConcurrencyException automatically becomes a 409 response when called remotely.

## Save Without Delete

If you don't implement Delete, IFactorySave still generates but throws `NotImplementedException` for deleted entities:

In the Domain layer, create an entity without Delete support:

<!-- snippet: save-no-delete -->
<a id='snippet-save-no-delete'></a>
```cs
// Insert + Update only: setting IsDeleted=true and calling Save throws NotImplementedException
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public EmployeeNoDelete() { Id = Guid.NewGuid(); }
    [Remote, Insert] public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }
    [Remote, Update] public Task Update(CancellationToken ct) { return Task.CompletedTask; }
    // No [Delete] = cannot delete, use for soft-delete patterns
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/PartialSaveSamples.cs#L29-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-no-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save throws `NotImplementedException` when `IsDeleted = true`.

## Alternative: Explicit Methods

Save is optional. You can always call Insert/Update/Delete directly:

In the Application layer, demonstrate explicit method calls vs Save:

<!-- snippet: save-explicit -->
<a id='snippet-save-explicit'></a>
```cs
// Save: state-based routing (single "Save" button in UI)
await _factory.Save(employee);           // Routes to Insert (IsNew=true)
employee.FirstName = "Jane";
await _factory.Save(employee);           // Routes to Update (IsNew=false)
employee.IsDeleted = true;
await _factory.Save(employee);           // Routes to Delete (IsDeleted=true)
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveExplicitSamples.cs#L29-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-explicit' title='Start of snippet'>anchor</a></sup>
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
// Batch save utility: saves multiple entities with cancellation support
public static async Task<List<T>> SaveBatch<T>(IFactorySave<T> factory, IEnumerable<T> entities, CancellationToken ct)
    where T : class, IFactorySaveMeta
{
    var saved = new List<T>();
    foreach (var entity in entities)
    {
        ct.ThrowIfCancellationRequested();
        if (await factory.Save(entity, ct) is T result) saved.Add(result);
    }
    return saved;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/SaveOperation/SaveExtensionsSamples.cs#L11-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-extensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Track additional state without affecting Save routing.

## Testing Save Routing

Test routing logic:

In the Tests layer, verify Save routes to the correct operation:

<!-- snippet: save-testing -->
<a id='snippet-save-testing'></a>
```cs
// Test Save routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete
[Fact]
public async Task Save_WhenIsNewTrue_RoutesToInsert()
{
    var scopes = TestClientServerContainers.CreateScopes();
    var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();
    var employee = factory.Create();
    Assert.True(employee.IsNew);
    var saved = (EmployeeCrud)(await factory.Save(employee))!;
    Assert.False(saved.IsNew);  // Insert sets IsNew = false
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/SaveOperation/SaveRoutingTestSample.cs#L16-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - Insert, Update, Delete details
- [Authorization](authorization.md) - Secure save operations
- [Serialization](serialization.md) - Entity state serialization
