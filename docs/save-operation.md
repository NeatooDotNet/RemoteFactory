# Save Operation

Save is the persistence routing heart of RemoteFactory. Instead of the caller deciding whether to insert, update, or delete, the entity tracks its own lifecycle state and Save routes to the right operation. This matters most with object graphs — a parent aggregate calls Save once, and each entity in the graph handles its own persistence based on its own state, cascading the save to its children. One call at the root handles inserts, updates, and deletes across the entire graph, regardless of what the user did in the UI.

This pattern comes from CSLA's DataPortal — the data mapper principle that each entity knows how to save itself.

## IFactorySaveMeta

To opt into Save routing, implement `IFactorySaveMeta`. It requires two properties:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
// IFactorySaveMeta requires: IsNew (true for new entities) and IsDeleted (true for deletion)
public bool IsNew { get; private set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/SaveOperation/EmployeeSaveMetaSamples.cs#L21-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Why two booleans instead of an enum? These are regular properties that participate in data binding. `IsDeleted` is user-settable — the UI can bind a delete button to it. `IsNew` is set by your Create and Fetch methods. Both properties naturally flow from UI state into persistence routing without any framework-managed state machine.

When a class implements `IFactorySaveMeta`, RemoteFactory generates an `IFactorySave<T>` interface on its factory with a `Save()` method.

## Routing Logic

Save inspects `IsNew` and `IsDeleted` to decide which operation to call:

| IsNew | IsDeleted | Operation | Why |
|-------|-----------|-----------|-----|
| true | false | **Insert** | New entity, not yet persisted |
| false | false | **Update** | Existing entity with changes |
| false | true | **Delete** | Existing entity marked for removal |
| true | true | **No-op** | Created and deleted before saving — nothing to persist |

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

## Write Operations

Each write operation is a method on your domain class. You write the persistence logic; Save routes to the right one:

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

The caller just calls Save — the routing is invisible to them:

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

## Cascading Saves

Save's real power shows in object graphs. When a parent aggregate saves, it can cascade the save to its children. Each child entity knows its own state (IsNew, IsDeleted) and handles its own persistence, then continues the cascade to *its* children. The result: one `Save()` call at the root persists the entire graph — new children are inserted, modified children are updated, removed children are deleted — all in one operation.

You implement the cascade in your Insert/Update methods by calling Save on child collections. Each child entity's `IsNew`/`IsDeleted` state drives its own routing independently.

## Partial Operations

You don't need all three write operations. Save routes based on what you've defined:

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

| Operations Defined | Pattern | Example |
|-------------------|---------|---------|
| Insert only | Immutable records | Audit logs, event sourcing entries |
| Insert + Update | Full write, no delete | Soft-delete systems |
| Insert + Update + Delete | Full CRUD | Standard entities |

For upsert (same method for Insert and Update), mark a single method with both `[Insert, Update]`. See [Factory Operations](factory-operations.md#combining-operations).

## Return Values

```csharp
Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
```

Returns the entity on success. Returns null when:
- The write operation returns `false` (not authorized or not found)
- `IsNew = true` and `IsDeleted = true` (no-op)

## Save vs Explicit Methods

Save is optional. You can always call Insert, Update, or Delete directly through the factory.

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

Use **Save** when the UI has a single save button and the entity tracks its own state. Use **explicit methods** when the client knows the exact operation (e.g., separate "Create" and "Edit" pages).

## Optimistic Concurrency

Use version tokens or timestamps for conflict detection:

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

EF Core's `DbUpdateConcurrencyException` automatically becomes a 409 response when called remotely.

## Authorization and Validation

Save respects authorization rules — you can authorize at the operation level (separate permissions for Insert vs Update vs Delete) or at the Write level (single permission for all writes). See [Authorization](authorization.md) for details.

Validate before Save however you prefer — DataAnnotations, FluentValidation, or manual checks in your write methods. RemoteFactory doesn't prescribe a validation approach.

## Testing Save Routing

Verify that Save routes to the correct operation based on entity state:

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

- [Factory Operations](factory-operations.md) — Insert, Update, Delete details
- [Authorization](authorization.md) — Secure save operations
- [Serialization](serialization.md) — Entity state serialization
