using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/attributes-reference.md documentation.
/// </summary>
public partial class AttributesReferenceSamples
{
    #region attributes-factory
    // [Factory] marks a class for factory generation
    [Factory]
    public partial class BasicEntity
    {
        public Guid Id { get; private set; }

        [Create]
        public BasicEntity() { Id = Guid.NewGuid(); }
    }

    // [Factory] on an interface requires an implementing class
    public interface IFactoryTarget
    {
        Guid Id { get; }
    }

    [Factory]
    public partial class FactoryTargetImpl : IFactoryTarget
    {
        public Guid Id { get; private set; }

        [Create]
        public FactoryTargetImpl() { Id = Guid.NewGuid(); }
    }
    #endregion

    #region attributes-suppressfactory
    [Factory]
    public partial class BaseEntity
    {
        public Guid Id { get; protected set; }

        [Create]
        public BaseEntity() { Id = Guid.NewGuid(); }
    }

    // Prevent factory generation for derived class
    [SuppressFactory]
    public partial class DerivedEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        // No factory will be generated for DerivedEntity
        // Use BaseEntityFactory to create instances
    }
    #endregion

    #region attributes-create
    [Factory]
    public partial class CreateAttributeExample
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool Initialized { get; private set; }

        // [Create] on constructor
        [Create]
        public CreateAttributeExample()
        {
            Id = Guid.NewGuid();
        }

        // [Create] on instance method
        [Create]
        public void Initialize(string name)
        {
            Name = name;
            Initialized = true;
        }

        // [Create] on static method
        [Create]
        public static CreateAttributeExample CreateWithName(string name)
        {
            return new CreateAttributeExample { Name = name, Initialized = true };
        }
    }
    #endregion

    #region attributes-fetch
    [Factory]
    public partial class FetchAttributeExample
    {
        public Guid Id { get; private set; }
        public string Data { get; private set; } = string.Empty;

        [Create]
        public FetchAttributeExample() { Id = Guid.NewGuid(); }

        // [Fetch] generates Fetch method on factory
        [Fetch]
        public Task Fetch(Guid id)
        {
            Id = id;
            Data = "Fetched";
            return Task.CompletedTask;
        }

        // Multiple Fetch overloads with different parameters
        [Fetch]
        public Task FetchByName(string name)
        {
            Data = $"Fetched: {name}";
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-insert
    [Factory]
    public partial class InsertAttributeExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public InsertAttributeExample() { Id = Guid.NewGuid(); }

        // [Insert] generates Insert method, called by Save when IsNew = true
        [Insert]
        public Task Insert([Service] IPersonRepository repository)
        {
            IsNew = false;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-update
    [Factory]
    public partial class UpdateAttributeExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Data { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public UpdateAttributeExample() { Id = Guid.NewGuid(); }

        // [Update] generates Update method, called by Save when IsNew = false
        [Update]
        public Task Update([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-delete
    [Factory]
    public partial class DeleteAttributeExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public DeleteAttributeExample() { Id = Guid.NewGuid(); }

        // [Delete] generates Delete method, called by Save when IsDeleted = true
        [Delete]
        public Task Delete([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-execute
    // [Execute] must be in a static partial class
    // Used for command/query operations that don't require instance state
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ExecuteAttributeExample
    {
        // [Execute] generates a delegate for command operations
        [Execute]
        private static Task<string> _ProcessCommand(string input, [Service] IPersonRepository repository)
        {
            return Task.FromResult($"Processed: {input}");
        }

        // [Remote] [Execute] executes on server
        [Remote, Execute]
        private static Task<string> _ProcessCommandRemote(string input, [Service] IPersonRepository repository)
        {
            return Task.FromResult($"Remote processed: {input}");
        }
    }
    #endregion

    #region attributes-event
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public partial class EventAttributeExample
    {
        [Create]
        public EventAttributeExample() { }

        // [Event] generates fire-and-forget event delegate
        // Must have CancellationToken as final parameter
        [Event]
        public Task SendNotification(
            Guid entityId,
            string message,
            [Service] IEmailService emailService,
            CancellationToken ct)
        {
            return emailService.SendAsync("notify@example.com", "Notification", message, ct);
        }
    }
    #endregion

    #region attributes-remote
    [Factory]
    public partial class RemoteAttributeExample
    {
        public Guid Id { get; private set; }
        public string ServerData { get; private set; } = string.Empty;

        [Create]
        public RemoteAttributeExample() { Id = Guid.NewGuid(); }

        // [Remote] marks method for server-side execution
        // When called from client, parameters are serialized and sent via HTTP
        [Remote]
        [Fetch]
        public Task FetchFromServer(Guid id, [Service] IPersonRepository repository)
        {
            Id = id;
            ServerData = "Loaded from server";
            return Task.CompletedTask;
        }

        // Without [Remote], method executes locally
        [Fetch]
        public Task FetchLocal(Guid id)
        {
            Id = id;
            ServerData = "Local only";
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-service
    [Factory]
    public partial class ServiceAttributeExample
    {
        public bool Injected { get; private set; }

        [Create]
        public ServiceAttributeExample() { }

        // [Service] marks parameters for DI injection
        [Fetch]
        public Task Fetch(
            Guid id,                                    // Value parameter - passed by caller
            [Service] IPersonRepository repository,    // Service - injected from DI
            [Service] IUserContext userContext)        // Service - injected from DI
        {
            Injected = repository != null && userContext != null;
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-authorizefactory-generic
    public interface IResourceAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead();

        [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
        bool CanWrite();
    }

    public partial class ResourceAuth : IResourceAuth
    {
        private readonly IUserContext _userContext;
        public ResourceAuth(IUserContext userContext) { _userContext = userContext; }
        public bool CanRead() => _userContext.IsAuthenticated;
        public bool CanWrite() => _userContext.IsInRole("Writer");
    }

    // [AuthorizeFactory<T>] on class applies authorization
    [Factory]
    [AuthorizeFactory<IResourceAuth>]
    public partial class ProtectedResource
    {
        public Guid Id { get; private set; }

        [Create]
        public ProtectedResource() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id) { Id = id; return Task.FromResult(true); }
    }
    #endregion

    #region attributes-authorizefactory-interface
    // Authorization interface with operation-specific methods
    public interface IEntityAuth
    {
        // [AuthorizeFactory] on interface methods defines what operations they authorize
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool CanCreate();

        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanFetch(Guid entityId);

        [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
        bool CanUpdate(Guid entityId);

        [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
        bool CanDelete(Guid entityId);
    }

    public partial class EntityAuth : IEntityAuth
    {
        private readonly IUserContext _userContext;
        public EntityAuth(IUserContext userContext) { _userContext = userContext; }

        public bool CanCreate() => _userContext.IsAuthenticated;
        public bool CanFetch(Guid entityId) => _userContext.IsAuthenticated;
        public bool CanUpdate(Guid entityId) => _userContext.IsInRole("Editor");
        public bool CanDelete(Guid entityId) => _userContext.IsInRole("Admin");
    }
    #endregion

    #region attributes-authorizefactory-method
    [Factory]
    [AuthorizeFactory<IResourceAuth>]
    public partial class MethodLevelAuth
    {
        public Guid Id { get; private set; }

        [Create]
        public MethodLevelAuth() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id) { Id = id; return Task.FromResult(true); }

        // Method-level [AspAuthorize] can override class-level auth on Fetch/Insert/Update/Delete
        [Remote, Fetch]
        [AspAuthorize(Roles = "Admin")]
        public Task<bool> FetchAdminOnly(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }
    #endregion

    #region attributes-aspauthorize
    [Factory]
    public partial class AspAuthorizeExample
    {
        public Guid Id { get; private set; }

        [Create]
        public AspAuthorizeExample() { Id = Guid.NewGuid(); }

        // [AspAuthorize] with policy
        [Remote, Fetch]
        [AspAuthorize("RequireAuthenticated")]
        public Task<bool> Fetch(Guid id) { Id = id; return Task.FromResult(true); }

        // [AspAuthorize] with roles on Fetch
        [Remote, Fetch]
        [AspAuthorize(Roles = "Admin,Manager")]
        public Task<bool> FetchForManagers(Guid id) { Id = id; return Task.FromResult(true); }

        // [AspAuthorize] with authentication schemes
        [Remote, Fetch]
        [AspAuthorize(AuthenticationSchemes = "Bearer")]
        public Task<bool> FetchWithBearer(Guid id) { Id = id; return Task.FromResult(true); }
    }

    // For Execute with [AspAuthorize], use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class AspAuthorizeExecuteExample
    {
        [Remote, Execute]
        [AspAuthorize(Roles = "Admin,Manager")]
        private static Task<string> _ManagerOperation(string command)
        {
            return Task.FromResult($"Executed: {command}");
        }
    }
    #endregion

    #region attributes-factorymode
    // Assembly-level attribute to control factory generation mode
    // [assembly: FactoryMode(FactoryMode.RemoteOnly)]

    // FactoryMode.Full (default) - generates both local and remote execution paths
    // FactoryMode.RemoteOnly - generates HTTP stubs only, for client assemblies
    #endregion

    #region attributes-factoryhintnamelength
    // Assembly-level attribute to limit generated file name length
    // Useful when generated file paths exceed OS limits
    // [assembly: FactoryHintNameLength(100)]
    #endregion

    #region attributes-multiple-operations
    [Factory]
    public partial class MultipleOperationsExample : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public MultipleOperationsExample() { Id = Guid.NewGuid(); }

        // Single method handles both Insert and Update
        [Insert, Update]
        public Task Upsert([Service] IPersonRepository repository)
        {
            IsNew = false;
            return Task.CompletedTask;
        }

        [Delete]
        public Task Delete([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-remote-operation
    [Factory]
    public partial class RemoteOperationExampleForAttrs
    {
        public Guid Id { get; private set; }

        [Create]
        public RemoteOperationExampleForAttrs() { Id = Guid.NewGuid(); }

        // Combine [Remote] with operation attributes
        [Remote, Fetch]
        public Task<bool> FetchRemote(Guid id, [Service] IPersonRepository repository)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }

    // For [Remote, Execute], use static partial class
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class RemoteExecuteOperations
    {
        [Remote, Execute]
        private static Task<string> _ExecuteRemote(string command, [Service] IPersonRepository repository)
        {
            return Task.FromResult($"Executed: {command}");
        }
    }
    #endregion

    #region attributes-authorization-operation
    public interface IOperationAuth
    {
        // Combined flags for multiple operations
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        bool CanReadWrite();

        // Individual operation
        [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
        bool CanDelete();
    }

    public partial class OperationAuth : IOperationAuth
    {
        private readonly IUserContext _userContext;
        public OperationAuth(IUserContext userContext) { _userContext = userContext; }
        public bool CanReadWrite() => _userContext.IsAuthenticated;
        public bool CanDelete() => _userContext.IsInRole("Admin");
    }
    #endregion

    #region attributes-inheritance
    [Factory]
    public partial class BaseEntityWithFactory
    {
        public Guid Id { get; protected set; }
        public string Name { get; set; } = string.Empty;

        [Create]
        public BaseEntityWithFactory() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public virtual Task<bool> Fetch(Guid id)
        {
            Id = id;
            return Task.FromResult(true);
        }
    }

    // Derived class inherits [Factory] but can override methods
    public partial class DerivedWithInheritedFactory : BaseEntityWithFactory
    {
        public string ExtraData { get; set; } = string.Empty;

        [Remote, Fetch]
        public override Task<bool> Fetch(Guid id)
        {
            Id = id;
            ExtraData = "Derived fetch";
            return Task.FromResult(true);
        }
    }

    // Suppress factory for specific derived class
    [SuppressFactory]
    public partial class DerivedWithoutFactory : BaseEntityWithFactory
    {
        // No factory generated for this class
    }
    #endregion

    #region attributes-pattern-crud
    // Complete CRUD entity pattern
    [Factory]
    public partial class CrudEntity : IFactorySaveMeta
    {
        public Guid Id { get; private set; }
        public string Name { get; set; } = string.Empty;
        public bool IsNew { get; private set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public CrudEntity() { Id = Guid.NewGuid(); }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
        {
            Id = id;
            IsNew = false;
            return Task.FromResult(true);
        }

        [Remote, Insert]
        public Task Insert([Service] IPersonRepository repository)
        {
            IsNew = false;
            return Task.CompletedTask;
        }

        [Remote, Update]
        public Task Update([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }

        [Remote, Delete]
        public Task Delete([Service] IPersonRepository repository)
        {
            return Task.CompletedTask;
        }
    }
    #endregion

    #region attributes-pattern-readonly
    // Read-only entity pattern (Create and Fetch only)
    [Factory]
    public partial class ReadOnlyEntity
    {
        public Guid Id { get; private set; }
        public string Data { get; private set; } = string.Empty;
        public DateTime Created { get; private set; }

        [Create]
        public ReadOnlyEntity()
        {
            Id = Guid.NewGuid();
            Created = DateTime.UtcNow;
        }

        [Remote, Fetch]
        public Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
        {
            Id = id;
            Data = "Fetched";
            return Task.FromResult(true);
        }

        // No Insert, Update, or Delete - entity is read-only after creation
    }
    #endregion

    #region attributes-pattern-command
    // Command pattern using static partial class with [Execute]
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class ApproveOrderCommand
    {
        // Command returns result record
        public record ApproveResult(Guid OrderId, bool Success, string Message);

        [Remote, Execute]
        private static Task<ApproveResult> _Execute(
            Guid orderId,
            string approverNotes,
            [Service] IOrderRepository repository)
        {
            return Task.FromResult(new ApproveResult(
                orderId,
                true,
                $"Order {orderId} approved: {approverNotes}"));
        }
    }
    #endregion

    #region attributes-pattern-event
    // Event handler pattern (Events only) - must be static partial
    [SuppressFactory] // Nested in wrapper class - pattern demonstration only
    public static partial class OrderEventHandlers
    {
        [Event]
        private static Task _OnOrderCreated(
            Guid orderId,
            [Service] IEmailService emailService,
            CancellationToken ct)
        {
            return emailService.SendAsync("notify@example.com", "Order Created", $"Order {orderId} created", ct);
        }

        [Event]
        private static Task _OnOrderShipped(
            Guid orderId,
            string trackingNumber,
            [Service] IEmailService emailService,
            CancellationToken ct)
        {
            var message = $"Order {orderId} shipped: {trackingNumber}";
            return emailService.SendAsync("notify@example.com", "Order Shipped", message, ct);
        }
    }
    #endregion

    // [Fact]
    public void BasicEntity_CanBeCreated()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IBasicEntityFactory>();

        var entity = factory.Create();
        Assert.NotNull(entity);
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    // [Fact]
    public async Task CrudEntity_FullWorkflow()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ICrudEntityFactory>();

        // Create
        var entity = factory.Create();
        entity.Name = "Test";
        Assert.True(entity.IsNew);

        // Save (Insert)
        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Update
        saved.Name = "Updated";
        var updated = await factory.Save(saved);
        Assert.NotNull(updated);

        // Delete
        updated.IsDeleted = true;
        await factory.Save(updated);
    }

    // Test methods removed - static partial classes with [Execute] are demonstration-only
    // when nested in wrapper classes. Use at namespace level in production.
}
