# Save Operation

The Save operation provides automatic routing to Insert, Update, or Delete based on entity state.

## IFactorySave Interface

Classes implementing `IFactorySaveMeta` get an `IFactorySave<T>` interface on their factory with a `Save()` method.

### Step 1: Implement IFactorySaveMeta

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
[Factory]
public partial class Customer : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }

    // IFactorySaveMeta implementation
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Customer()
    {
        Id = Guid.NewGuid();
        // IsNew defaults to true for new instances
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        Email = entity.Email;
        IsNew = false; // Mark as existing after fetch
        return true;
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L12-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IFactorySaveMeta` requires two properties:
- `IsNew`: true for new entities not yet persisted
- `IsDeleted`: true for entities marked for deletion

### Step 2: Implement Write Operations

<!-- snippet: save-write-operations -->
<a id='snippet-save-write-operations'></a>
```cs
[Factory]
public partial class CustomerWithWriteOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public CustomerWithWriteOps() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public async Task Insert([Service] IPersonRepository repository)
    {
        var entity = new PersonEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = string.Empty,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };
        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Customer {Id} not found");

        entity.FirstName = Name;
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    [Remote, Delete]
    public async Task Delete([Service] IPersonRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L46-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-write-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 3: Use Save Method

<!-- snippet: save-usage -->
<a id='snippet-save-usage'></a>
```cs
public partial class SaveUsageExample
{
    // [Fact]
    public async Task UsingSaveMethod()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<ICustomerWithWriteOpsFactory>();

        // Create new customer
        var customer = factory.Create();
        customer.Name = "Acme Corp";

        // Save routes to Insert (IsNew = true)
        var saved = await factory.Save(customer);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Modify and save again - routes to Update (IsNew = false)
        saved.Name = "Acme Corporation";
        var updated = await factory.Save(saved);

        // Mark for deletion and save - routes to Delete
        updated!.IsDeleted = true;
        await factory.Save(updated);
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L96-L123' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage' title='Start of snippet'>anchor</a></sup>
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
// Generated Save method routing logic:
//
// public Task<T?> LocalSave(T entity)
// {
//     if (entity.IsDeleted)
//     {
//         if (entity.IsNew)
//             return Task.FromResult(default(T)); // New item deleted before save - no operation
//
//         return LocalDelete(entity); // Route to Delete, returns the entity
//     }
//     else if (entity.IsNew)
//     {
//         return LocalInsert(entity); // Route to Insert
//     }
//     else
//     {
//         return LocalUpdate(entity); // Route to Update
//     }
// }
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L125-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## State Management

Track state in your domain model:

### Constructor Sets IsNew

<!-- snippet: save-state-new -->
<a id='snippet-save-state-new'></a>
```cs
public partial class SaveStateNewExample
{
    // [Fact]
    public async Task NewEntity_RoutesToInsert()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        var customer = factory.Create();
        Assert.True(customer.IsNew); // New entity
        Assert.False(customer.IsDeleted);

        customer.Name = "New Customer";
        var saved = await factory.Save(customer);

        // After save, IsNew should be false
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L148-L169' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetch Clears IsNew

<!-- snippet: save-state-fetch -->
<a id='snippet-save-state-fetch'></a>
```cs
public partial class SaveStateFetchExample
{
    // [Fact]
    public async Task FetchedEntity_RoutesToUpdate()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        // First create and save a customer
        var customer = factory.Create();
        customer.Name = "Original Name";
        var saved = await factory.Save(customer);

        // After save, IsNew = false, so next save routes to Update
        Assert.False(saved!.IsNew);

        // Modify and save - routes to Update
        saved.Name = "Updated Name";
        var updated = await factory.Save(saved);

        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L171-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### MarkDeleted Sets IsDeleted

<!-- snippet: save-state-delete -->
<a id='snippet-save-state-delete'></a>
```cs
public partial class SaveStateDeleteExample
{
    // [Fact]
    public async Task DeletedEntity_RoutesToDelete()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        // Create and save
        var customer = factory.Create();
        customer.Name = "To Be Deleted";
        var saved = await factory.Save(customer);

        // Mark for deletion
        saved!.IsDeleted = true;
        Assert.False(saved.IsNew);
        Assert.True(saved.IsDeleted);

        // Save routes to Delete
        var result = await factory.Save(saved);
        Assert.NotNull(result); // Delete returns the deleted entity
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L198-L222' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-state-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Complete Example

<!-- snippet: save-complete-example -->
<a id='snippet-save-complete-example'></a>
```cs
[Factory]
public partial class Invoice : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; private set; } = "Draft";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Invoice()
    {
        Id = Guid.NewGuid();
        InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        InvoiceNumber = entity.OrderNumber;
        CustomerId = entity.CustomerId;
        Total = entity.Total;
        Status = entity.Status;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IOrderRepository repository)
    {
        var entity = new OrderEntity
        {
            Id = Id,
            OrderNumber = InvoiceNumber,
            CustomerId = CustomerId,
            Total = Total,
            Status = Status,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Invoice {Id} not found");

        entity.CustomerId = CustomerId;
        entity.Total = Total;
        entity.Status = Status;
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    [Remote, Delete]
    public async Task Delete([Service] IOrderRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L224-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Usage:

<!-- snippet: save-complete-usage -->
<a id='snippet-save-complete-usage'></a>
```cs
public partial class SaveCompleteUsageExample
{
    // [Fact]
    public async Task FullCrudWorkflow()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IInvoiceFactory>();

        // CREATE
        var invoice = factory.Create();
        invoice.CustomerId = Guid.NewGuid();
        invoice.Total = 1500.00m;
        invoice.DueDate = DateTime.UtcNow.AddDays(30);

        var created = await factory.Save(invoice);
        Assert.NotNull(created);
        var invoiceId = created.Id;

        // READ
        var fetched = await factory.Fetch(invoiceId);
        Assert.NotNull(fetched);
        Assert.Equal(1500.00m, fetched.Total);

        // UPDATE
        fetched.Total = 1750.00m;
        var updated = await factory.Save(fetched);
        Assert.NotNull(updated);
        Assert.Equal(1750.00m, updated.Total);

        // DELETE
        updated.IsDeleted = true;
        var deleted = await factory.Save(updated);
        Assert.NotNull(deleted); // Returns the deleted entity
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L302-L338' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-complete-usage' title='Start of snippet'>anchor</a></sup>
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

<!-- snippet: save-partial-methods -->
<a id='snippet-save-partial-methods'></a>
```cs
[Factory]
public partial class ReadOnlyAfterCreate : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ReadOnlyAfterCreate() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IPersonRepository repository)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    // No Update method - entity becomes read-only after creation
    // No Delete method - entity cannot be deleted

    // Save will:
    // - Call Insert when IsNew = true
    // - Do nothing when IsNew = false (no Update defined)
    // - Do nothing when IsDeleted = true (no Delete defined)
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L340-L367' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-partial-methods' title='Start of snippet'>anchor</a></sup>
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

<!-- snippet: save-authorization -->
<a id='snippet-save-authorization'></a>
```cs
public interface IInvoiceAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public partial class InvoiceAuth : IInvoiceAuth
{
    private readonly IUserContext _userContext;
    public InvoiceAuth(IUserContext userContext) { _userContext = userContext; }

    public bool CanCreate() => _userContext.IsAuthenticated;
    public bool CanWrite() => _userContext.IsInRole("Accountant") || _userContext.IsInRole("Admin");
}

[Factory]
[AuthorizeFactory<IInvoiceAuth>]
public partial class AuthorizedInvoice : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public decimal Total { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedInvoice() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IOrderRepository repository)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update([Service] IOrderRepository repository)
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete([Service] IOrderRepository repository)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L369-L419' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Or to Save as a whole:

<!-- snippet: save-authorization-combined -->
<a id='snippet-save-authorization-combined'></a>
```cs
public interface ICombinedWriteAuth
{
    // Single method authorizes all write operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public partial class CombinedWriteAuth : ICombinedWriteAuth
{
    private readonly IUserContext _userContext;
    public CombinedWriteAuth(IUserContext userContext) { _userContext = userContext; }

    // Write = Insert | Update | Delete
    public bool CanWrite() => _userContext.IsInRole("Writer") || _userContext.IsInRole("Admin");
}

[Factory]
[AuthorizeFactory<ICombinedWriteAuth>]
public partial class WriteAuthorizedEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Data { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public WriteAuthorizedEntity() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert() { IsNew = false; return Task.CompletedTask; }

    [Remote, Update]
    public Task Update() { return Task.CompletedTask; }

    [Remote, Delete]
    public Task Delete() { return Task.CompletedTask; }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L421-L459' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-authorization-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory checks authorization before routing.

## Validation Before Save

Validate state before saving:

<!-- snippet: save-validation -->
<a id='snippet-save-validation'></a>
```cs
[Factory]
public partial class ValidatedInvoice : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "Customer is required")]
    public Guid CustomerId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Total must be greater than zero")]
    public decimal Total { get; set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ValidatedInvoice() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IOrderRepository repository)
    {
        // Validation happens before save
        IsNew = false;
        return Task.CompletedTask;
    }
}

public partial class ValidationBeforeSave
{
    public static async Task<ValidatedInvoice?> SaveWithValidation(
        IValidatedInvoiceFactory factory,
        ValidatedInvoice invoice)
    {
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            invoice,
            new ValidationContext(invoice),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            // Handle validation errors
            return null;
        }

        return await factory.Save(invoice);
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L461-L510' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Throw exceptions for validation failures:

<!-- snippet: save-validation-throw -->
<a id='snippet-save-validation-throw'></a>
```cs
[Factory]
public partial class StrictValidatedEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public StrictValidatedEntity() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert()
    {
        // Validate on server - throw if invalid
        if (string.IsNullOrWhiteSpace(Name))
            throw new ValidationException("Name is required");

        IsNew = false;
        return Task.CompletedTask;
    }
}

public partial class ValidationExceptionHandling
{
    // [Fact]
    public async Task HandleValidationException()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IStrictValidatedEntityFactory>();

        var entity = factory.Create();
        entity.Name = string.Empty; // Invalid

        try
        {
            await factory.Save(entity);
            Assert.Fail("Should have thrown ValidationException");
        }
        catch (ValidationException ex)
        {
            Assert.Equal("Name is required", ex.Message);
        }
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L512-L558' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-validation-throw' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Optimistic Concurrency

Use version tokens or timestamps:

<!-- snippet: save-optimistic-concurrency -->
<a id='snippet-save-optimistic-concurrency'></a>
```cs
[Factory]
public partial class ConcurrentEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Data { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ConcurrentEntity() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        Data = entity.FirstName;
        RowVersion = entity.RowVersion;
        IsNew = false;
        return true;
    }

    [Remote, Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Entity {Id} not found");

        // Check row version for optimistic concurrency
        if (RowVersion != null && entity.RowVersion != null &&
            !RowVersion.SequenceEqual(entity.RowVersion))
        {
            throw new InvalidOperationException(
                "The entity was modified by another user. Please refresh and try again.");
        }

        entity.FirstName = Data;
        entity.Modified = DateTime.UtcNow;
        // RowVersion updated by database

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L560-L608' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-optimistic-concurrency' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

EF Core DbUpdateConcurrencyException automatically becomes a 409 response when called remotely.

## Save Without Delete

If you don't implement Delete, IFactorySave still generates but throws `NotImplementedException` for deleted entities:

<!-- snippet: save-no-delete -->
<a id='snippet-save-no-delete'></a>
```cs
[Factory]
public partial class NoDeleteEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public NoDeleteEntity() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert() { IsNew = false; return Task.CompletedTask; }

    [Remote, Update]
    public Task Update() { return Task.CompletedTask; }

    // No Delete method - setting IsDeleted = true and calling Save throws NotImplementedException
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L610-L630' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-no-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save throws `NotImplementedException` when `IsDeleted = true`.

## Alternative: Explicit Methods

Save is optional. You can always call Insert/Update/Delete directly:

<!-- snippet: save-explicit -->
<a id='snippet-save-explicit'></a>
```cs
// When individual operation methods are needed, use Save with appropriate IsNew/IsDeleted flags.
// The Save method routes to Insert, Update, or Delete based on the entity state:
//
// public partial class ExplicitOperationCalls
// {
//     public async Task CallOperationsDirectly()
//     {
//         var factory = services.GetRequiredService<ICustomerWithWriteOpsFactory>();
//
//         // Create and Insert
//         var customer = factory.Create();
//         customer.Name = "New Customer";
//         var inserted = await factory.Save(customer); // Routes to Insert (IsNew = true)
//
//         // Update
//         inserted!.Name = "Updated Name";
//         var updated = await factory.Save(inserted); // Routes to Update (IsNew = false)
//
//         // Delete
//         updated!.IsDeleted = true;
//         await factory.Save(updated); // Routes to Delete (IsDeleted = true)
//     }
// }
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L632-L656' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-explicit' title='Start of snippet'>anchor</a></sup>
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

Extend IFactorySaveMeta for your domain:

<!-- snippet: save-extensions -->
<a id='snippet-save-extensions'></a>
```cs
// Extension methods for IFactorySave<T> (define in a top-level static class)
// Example usage pattern:
//
// public static class SaveExtensions
// {
//     public static async Task<T?> SaveAsync<T>(this IFactorySave<T> factory, T entity, CancellationToken ct = default)
//         where T : class, IFactorySaveMeta
//     {
//         ct.ThrowIfCancellationRequested();
//         return await factory.Save(entity);
//     }
// }

// Utility class demonstrating batch save operations
public partial class SaveUtilities
{
    public static async Task<T?> SaveWithCancellation<T>(
        IFactorySave<T> factory,
        T entity,
        CancellationToken ct = default)
        where T : class, IFactorySaveMeta
    {
        ct.ThrowIfCancellationRequested();
        var result = await factory.Save(entity);
        return (T?)result;
    }

    public static async Task<List<T>> SaveBatch<T>(
        IFactorySave<T> factory,
        IEnumerable<T> entities,
        CancellationToken ct = default)
        where T : class, IFactorySaveMeta
    {
        var results = new List<T>();
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            var saved = await factory.Save(entity);
            if (saved != null)
                results.Add((T)saved);
        }
        return results;
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L658-L703' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-extensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Track additional state without affecting Save routing.

## Testing Save Routing

Test routing logic:

<!-- snippet: save-testing -->
<a id='snippet-save-testing'></a>
```cs
public partial class SaveRoutingTests
{
    // [Fact]
    public async Task NewEntity_RoutesToInsert()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        var customer = factory.Create();
        Assert.True(customer.IsNew);

        customer.Name = "Test";
        var saved = await factory.Save(customer);

        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }

    // [Fact]
    public async Task ExistingEntity_RoutesToUpdate()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        // Create and save first
        var customer = factory.Create();
        customer.Name = "Original";
        var saved = await factory.Save(customer);

        // Modify and save again
        saved!.Name = "Modified";
        var updated = await factory.Save(saved);

        Assert.NotNull(updated);
        Assert.Equal("Modified", updated.Name);
    }

    // [Fact]
    public async Task DeletedEntity_RoutesToDelete()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICustomerWithWriteOpsFactory>();

        var customer = factory.Create();
        customer.Name = "ToDelete";
        var saved = await factory.Save(customer);

        saved!.IsDeleted = true;
        var result = await factory.Save(saved);

        Assert.NotNull(result); // Delete returns the entity
    }
}
```
<sup><a href='/src/docs/samples/SaveOperationSamples.cs#L705-L759' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - Insert, Update, Delete details
- [Authorization](authorization.md) - Secure save operations
- [Serialization](serialization.md) - Entity state serialization
