using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/save-operation.md documentation.
/// </summary>
public partial class SaveOperationSamples
{
    #region save-ifactorysavemeta
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
    #endregion

    #region save-write-operations
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
    #endregion

    #region save-usage
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
    #endregion

    #region save-generated
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
    #endregion

    #region save-state-new
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
    #endregion

    #region save-state-fetch
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
    #endregion

    #region save-state-delete
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
    #endregion

    #region save-complete-example
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
    #endregion

    #region save-complete-usage
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
    #endregion

    #region save-partial-methods
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
    #endregion

    #region save-authorization
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
    #endregion

    #region save-authorization-combined
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
    #endregion

    #region save-validation
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
    #endregion

    #region save-validation-throw
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
    #endregion

    #region save-optimistic-concurrency
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
    #endregion

    #region save-no-delete
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
    #endregion

    #region save-explicit
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
    #endregion

    #region save-extensions
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
    #endregion

    #region save-testing
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
    #endregion

    // [Fact]
    public void Customer_ImplementsIFactorySaveMeta()
    {
        var customer = new Customer();
        Assert.True(customer.IsNew);
        Assert.False(customer.IsDeleted);
    }

    // [Fact]
    public async Task Invoice_CompleteWorkflow()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IInvoiceFactory>();

        var invoice = factory.Create();
        invoice.CustomerId = Guid.NewGuid();
        invoice.Total = 100.00m;

        var saved = await factory.Save(invoice);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }
}
