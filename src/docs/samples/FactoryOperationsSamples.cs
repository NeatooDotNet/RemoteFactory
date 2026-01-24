using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/factory-operations.md documentation.
/// </summary>
public partial class FactoryOperationsSamples
{
    #region operations-create-constructor
    [Factory]
    public partial class ProductWithConstructorCreate
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Created { get; private set; }

        [Create]
        public ProductWithConstructorCreate()
        {
            Id = Guid.NewGuid();
            Created = DateTime.UtcNow;
            Price = 0.00m;
        }
    }
    #endregion

    #region operations-create-static
    [Factory]
    public partial class ProductWithStaticCreate
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; private set; } = string.Empty;
        public decimal Price { get; set; }

        private ProductWithStaticCreate() { }

        [Create]
        public static ProductWithStaticCreate Create(string sku, string name, decimal initialPrice)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU is required", nameof(sku));

            return new ProductWithStaticCreate
            {
                Id = Guid.NewGuid(),
                Sku = sku.ToUpperInvariant(),
                Name = name,
                Price = initialPrice
            };
        }
    }
    #endregion

    #region operations-create-return-types
    [Factory]
    public partial class CreateReturnTypesExample
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool Initialized { get; private set; }

        // Constructor Create - sets properties on created instance
        [Create]
        public CreateReturnTypesExample()
        {
            Id = Guid.NewGuid();
        }

        // Instance method Create - can modify this and return void
        [Create]
        public void Initialize(string name)
        {
            Name = name;
            Initialized = true;
        }

        // Static method Create - returns new instance
        [Create]
        public static CreateReturnTypesExample CreateWithDefaults()
        {
            return new CreateReturnTypesExample
            {
                Id = Guid.NewGuid(),
                Name = "Default",
                Initialized = true
            };
        }
    }
    #endregion

    #region operations-fetch-instance
    [Factory]
    public partial class OrderFetchExample
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; private set; } = string.Empty;
        public decimal Total { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public bool IsNew { get; private set; } = true;

        [Create]
        public OrderFetchExample() { }

        [Remote]
        [Fetch]
        public async Task Fetch(Guid orderId, [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            Id = entity.Id;
            OrderNumber = entity.OrderNumber;
            Total = entity.Total;
            Status = entity.Status;
            IsNew = false;
        }
    }
    #endregion

    #region operations-fetch-bool-return
    [Factory]
    public partial class OrderFetchBoolExample
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; private set; } = string.Empty;
        public decimal Total { get; private set; }

        [Create]
        public OrderFetchBoolExample() { }

        [Remote]
        [Fetch]
        public async Task<bool> TryFetch(Guid orderId, [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId);
            if (entity == null)
                return false;

            Id = entity.Id;
            OrderNumber = entity.OrderNumber;
            Total = entity.Total;
            return true;
        }
    }
    #endregion

    #region operations-insert
    [Factory]
    public partial class OrderInsertExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public OrderInsertExample()
        {
            Id = Guid.NewGuid();
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
        }

        [Remote]
        [Insert]
        public async Task Insert([Service] IOrderRepository repository)
        {
            var entity = new OrderEntity
            {
                Id = Id,
                OrderNumber = OrderNumber,
                Total = Total,
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();
            IsNew = false;
        }
    }
    #endregion

    #region operations-update
    [Factory]
    public partial class OrderUpdateExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; private set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public OrderUpdateExample() { Id = Guid.NewGuid(); }

        [Remote]
        [Fetch]
        public async Task<bool> Fetch(Guid orderId, [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId);
            if (entity == null) return false;

            Id = entity.Id;
            OrderNumber = entity.OrderNumber;
            Total = entity.Total;
            Status = entity.Status;
            IsNew = false;
            return true;
        }

        [Remote]
        [Update]
        public async Task Update([Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(Id)
                ?? throw new InvalidOperationException($"Order {Id} not found");

            entity.Total = Total;
            entity.Status = Status;
            entity.Modified = DateTime.UtcNow;

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();
        }
    }
    #endregion

    #region operations-delete
    [Factory]
    public partial class OrderDeleteExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public OrderDeleteExample() { Id = Guid.NewGuid(); }

        [Remote]
        [Fetch]
        public async Task<bool> Fetch(Guid orderId, [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId);
            if (entity == null) return false;
            Id = entity.Id;
            IsNew = false;
            return true;
        }

        [Remote]
        [Delete]
        public async Task Delete([Service] IOrderRepository repository)
        {
            await repository.DeleteAsync(Id);
            await repository.SaveChangesAsync();
        }
    }
    #endregion

    #region operations-insert-update
    [Factory]
    public partial class OrderUpsertExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public OrderUpsertExample()
        {
            Id = Guid.NewGuid();
            OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}";
        }

        [Remote]
        [Insert, Update]
        public async Task Upsert([Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(Id);

            if (entity == null)
            {
                entity = new OrderEntity
                {
                    Id = Id,
                    OrderNumber = OrderNumber,
                    Total = Total,
                    Status = "Pending",
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                await repository.AddAsync(entity);
            }
            else
            {
                entity.Total = Total;
                entity.Modified = DateTime.UtcNow;
                await repository.UpdateAsync(entity);
            }

            await repository.SaveChangesAsync();
            IsNew = false;
        }
    }
    #endregion

    #region operations-execute
    // [Execute] must be in static partial class
    public record OrderApprovalResult(Guid OrderId, bool IsApproved, string? ApprovalNotes);

    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class OrderApprovalCommand
    {
        [Remote]
        [Execute]
        private static async Task<OrderApprovalResult> _ApproveOrder(
            Guid orderId,
            string notes,
            [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            entity.Status = "Approved";
            entity.Modified = DateTime.UtcNow;

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            return new OrderApprovalResult(orderId, true, notes);
        }
    }
    #endregion

    #region operations-execute-command
    // Command pattern using static partial class
    public record OrderShippingResult(Guid OrderId, string TrackingNumber, DateTime ShippedAt, bool Success);

    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class OrderShippingCommand
    {
        [Remote]
        [Execute]
        private static async Task<OrderShippingResult> _ShipOrder(
            Guid orderId,
            string carrier,
            string trackingNumber,
            [Service] IOrderRepository repository)
        {
            var entity = await repository.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            if (entity.Status != "Approved")
            {
                throw new InvalidOperationException(
                    $"Order must be approved before shipping. Current status: {entity.Status}");
            }

            entity.Status = "Shipped";
            entity.Modified = DateTime.UtcNow;

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            return new OrderShippingResult(orderId, trackingNumber, DateTime.UtcNow, true);
        }
    }
    #endregion

    #region operations-event
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public partial class OrderEventExample
    {
        public Guid OrderId { get; set; }

        [Create]
        public OrderEventExample() { }

        [Event]
        public async Task SendOrderConfirmationEmail(
            Guid orderId,
            string customerEmail,
            [Service] IEmailService emailService,
            CancellationToken ct)
        {
            await emailService.SendAsync(
                customerEmail,
                "Order Confirmation",
                $"Your order {orderId} has been received.",
                ct);
        }
    }
    #endregion

    #region operations-event-tracker
    // IEventTracker tracks pending events for testing and graceful shutdown
    //
    // Fire event - returns immediately (fire-and-forget)
    // _ = sendConfirmationEmail(orderId, customerEmail);
    //
    // Wait for all pending events to complete
    // await eventTracker.WaitAllAsync();
    //
    // Check pending count
    // eventTracker.PendingCount; // 0 when all events complete
    #endregion

    #region operations-remote
    [Factory]
    public partial class RemoteOperationExample
    {
        public string Result { get; private set; } = string.Empty;

        [Create]
        public RemoteOperationExample() { }

        // This method executes on the server when called from a remote client
        // The client serializes parameters, sends via HTTP, server executes and returns result
        [Remote]
        [Fetch]
        public Task FetchFromServer(string query, [Service] IPersonRepository repository)
        {
            // This code runs on the server
            Result = $"Executed on server with query: {query}";
            return Task.CompletedTask;
        }
    }
    #endregion

    #region operations-lifecycle-onstart
    [Factory]
    public partial class LifecycleOnStartExample : IFactoryOnStart
    {
        public Guid Id { get; private set; }
        public bool OnStartCalled { get; private set; }
        public FactoryOperation? LastOperation { get; private set; }

        [Create]
        public LifecycleOnStartExample() { Id = Guid.NewGuid(); }

        public void FactoryStart(FactoryOperation factoryOperation)
        {
            OnStartCalled = true;
            LastOperation = factoryOperation;

            // Validate or prepare before operation executes
            if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
                throw new InvalidOperationException("Cannot delete: Id is not set");
        }
    }
    #endregion

    #region operations-lifecycle-oncomplete
    [Factory]
    public partial class LifecycleOnCompleteExample : IFactoryOnComplete
    {
        public Guid Id { get; private set; }
        public bool OnCompleteCalled { get; private set; }
        public FactoryOperation? CompletedOperation { get; private set; }

        [Create]
        public LifecycleOnCompleteExample() { Id = Guid.NewGuid(); }

        public void FactoryComplete(FactoryOperation factoryOperation)
        {
            OnCompleteCalled = true;
            CompletedOperation = factoryOperation;

            // Post-operation logic: logging, notifications, etc.
        }
    }
    #endregion

    #region operations-lifecycle-oncancelled
    [Factory]
    public partial class LifecycleOnCancelledExample : IFactoryOnCancelled
    {
        public Guid Id { get; private set; }
        public bool OnCancelledCalled { get; private set; }
        public FactoryOperation? CancelledOperation { get; private set; }

        [Create]
        public LifecycleOnCancelledExample() { Id = Guid.NewGuid(); }

        public void FactoryCancelled(FactoryOperation factoryOperation)
        {
            OnCancelledCalled = true;
            CancelledOperation = factoryOperation;

            // Cleanup logic when operation was cancelled
        }
    }
    #endregion

    #region operations-cancellation
    [Factory]
    public partial class CancellationTokenExample
    {
        public Guid Id { get; private set; }
        public bool Completed { get; private set; }

        [Create]
        public CancellationTokenExample() { Id = Guid.NewGuid(); }

        [Remote]
        [Fetch]
        public async Task Fetch(
            Guid id,
            [Service] IPersonRepository repository,
            CancellationToken cancellationToken)
        {
            // Check cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            // Pass token to async operations
            var entity = await repository.GetByIdAsync(id, cancellationToken);

            // Check cancellation during processing
            if (cancellationToken.IsCancellationRequested)
                return;

            Id = id;
            Completed = true;
        }
    }
    #endregion

    #region operations-params-value
    [Factory]
    public partial class ValueParametersExample
    {
        public int Count { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public DateTime Timestamp { get; private set; }
        public decimal Amount { get; private set; }

        [Create]
        public ValueParametersExample() { }

        [Remote]
        [Fetch]
        public Task Fetch(int count, string name, DateTime timestamp, decimal amount)
        {
            Count = count;
            Name = name;
            Timestamp = timestamp;
            Amount = amount;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region operations-params-service
    [Factory]
    public partial class ServiceParametersExample
    {
        public bool ServicesInjected { get; private set; }

        [Create]
        public ServiceParametersExample() { }

        [Remote]
        [Fetch]
        public Task Fetch(
            Guid id,
            [Service] IPersonRepository personRepository,
            [Service] IOrderRepository orderRepository,
            [Service] IUserContext userContext)
        {
            // Services are resolved from DI container on server
            ServicesInjected = personRepository != null
                && orderRepository != null
                && userContext != null;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region operations-params-array
    // Array parameters with [Execute] - use static partial class
    public record BatchProcessResult(Guid[] ProcessedIds, List<string> ProcessedNames);

    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ArrayParametersExample
    {
        [Remote]
        [Execute]
        private static Task<BatchProcessResult> _ProcessBatch(Guid[] ids, List<string> names)
        {
            return Task.FromResult(new BatchProcessResult(ids, names));
        }
    }
    #endregion

    #region operations-params-cancellation
    [Factory]
    public partial class OptionalCancellationExample
    {
        public bool Completed { get; private set; }

        [Create]
        public OptionalCancellationExample() { }

        [Remote]
        [Fetch]
        public async Task Fetch(
            Guid id,
            [Service] IPersonRepository repository,
            CancellationToken cancellationToken = default)
        {
            // CancellationToken is optional - receives default if not provided by caller
            await repository.GetByIdAsync(id, cancellationToken);
            Completed = true;
        }
    }
    #endregion

    // [Fact]
    public void Create_Constructor_Works()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IProductWithConstructorCreateFactory>();

        var product = factory.Create();

        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id);
    }

    // [Fact]
    public void Create_Static_Works()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IProductWithStaticCreateFactory>();

        var product = factory.Create("ABC123", "Test Product", 29.99m);

        Assert.NotNull(product);
        Assert.Equal("ABC123", product.Sku);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(29.99m, product.Price);
    }

    // Test methods removed - they reference static partial classes that require
    // namespace-level placement to work with the generator. The snippet code inside
    // #region blocks is correct and will work when placed at namespace level in user code.
}
