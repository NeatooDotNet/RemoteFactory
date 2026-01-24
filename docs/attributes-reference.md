# Attributes Reference

Complete reference of all RemoteFactory attributes.

## Factory Discovery Attributes

### [Factory]

Marks a class or interface for factory generation.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-factory -->
<a id='snippet-attributes-factory'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L11-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generates:
- `I{TypeName}Factory` interface
- `{TypeName}Factory` implementation class with static `FactoryServiceRegistrar` method for DI registration

### [SuppressFactory]

Prevents factory generation for a class or interface.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-suppressfactory -->
<a id='snippet-attributes-suppressfactory'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L38-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use when:
- Base class has `[Factory]` but derived class shouldn't
- Type should not have a factory despite matching generation criteria

## Operation Attributes

### [Create]

Marks constructors or methods that create new instances.

**Target:** Constructor, Method, Class
**Inherited:** No

<!-- snippet: attributes-create -->
<a id='snippet-attributes-create'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L59-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Create | Read`

### [Fetch]

Marks methods that load data into existing instances.

**Target:** Method, Constructor
**Inherited:** No

<!-- snippet: attributes-fetch -->
<a id='snippet-attributes-fetch'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L91-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Fetch | Read`

### [Insert]

Marks methods that persist new entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-insert -->
<a id='snippet-attributes-insert'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L120-L139' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Insert | Write`

### [Update]

Marks methods that persist changes to existing entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-update -->
<a id='snippet-attributes-update'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L141-L160' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Update | Write`

### [Delete]

Marks methods that remove entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-delete -->
<a id='snippet-attributes-delete'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L162-L180' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Delete | Write`

### [Execute]

Marks methods for business operations.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-execute -->
<a id='snippet-attributes-execute'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L182-L202' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Execute | Read`

### [Event]

Marks methods for fire-and-forget domain events.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-event -->
<a id='snippet-attributes-event'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L204-L223' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Requirements:
- Must have `CancellationToken` as last parameter
- Returns `void` or `Task`

Operation flags: `AuthorizeFactoryOperation.Event`

## Execution Control Attributes

### [Remote]

Marks methods that execute on the server.

**Target:** Method
**Inherited:** Yes

<!-- snippet: attributes-remote -->
<a id='snippet-attributes-remote'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L225-L255' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without `[Remote]`, methods execute locally (no serialization, no HTTP).

### [Service]

Marks parameters for dependency injection.

**Target:** Parameter
**Inherited:** No

<!-- snippet: attributes-service -->
<a id='snippet-attributes-service'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L257-L277' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Service parameters:
- Resolved from DI container
- Not serialized
- Must be registered in the appropriate container (server for remote methods)

## Authorization Attributes

### [AuthorizeFactory\<T\>]

Applies custom authorization class to the factory.

**Target:** Class, Interface
**Inherited:** No

<!-- snippet: attributes-authorizefactory-generic -->
<a id='snippet-attributes-authorizefactory-generic'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L279-L310' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The type parameter must be an interface with authorization methods decorated with `[AuthorizeFactory]`.

### [AuthorizeFactory]

Marks methods in authorization interfaces or applies to specific factory methods.

**Target:** Method
**Inherited:** No

**On authorization interface:**

<!-- snippet: attributes-authorizefactory-interface -->
<a id='snippet-attributes-authorizefactory-interface'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L312-L340' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**On factory method (additional check):**

<!-- snippet: attributes-authorizefactory-method -->
<a id='snippet-attributes-authorizefactory-method'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L342-L364' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `operation` (AuthorizeFactoryOperation): Flags indicating which operations require this authorization

### [AspAuthorize]

Applies ASP.NET Core authorization policies to endpoints.

**Target:** Method
**Inherited:** No
**Multiple:** Yes

<!-- snippet: attributes-aspauthorize -->
<a id='snippet-attributes-aspauthorize'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L366-L402' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Properties:**
- `Policy` (string?): Authorization policy name
- `Roles` (string?): Comma-delimited role list
- `AuthenticationSchemes` (string?): Comma-delimited authentication schemes

Applied to the generated `/api/neatoo` endpoint for the method.

## Assembly-Level Attributes

### [assembly: FactoryMode]

Specifies factory generation mode for the assembly.

**Target:** Assembly
**Inherited:** No

<!-- snippet: attributes-factorymode -->
<a id='snippet-attributes-factorymode'></a>
```cs
// Assembly-level attribute to control factory generation mode
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]

// FactoryMode.Full (default) - generates both local and remote execution paths
// FactoryMode.RemoteOnly - generates HTTP stubs only, for client assemblies
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L404-L410' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `mode` (FactoryMode): Full or RemoteOnly

Modes:
- **Full**: Generate local and remote code (default)
- **RemoteOnly**: Generate HTTP stubs only (client assemblies)

### [assembly: FactoryHintNameLength]

Limits generated file hint name length for long paths.

**Target:** Assembly
**Inherited:** No

<!-- snippet: attributes-factoryhintnamelength -->
<a id='snippet-attributes-factoryhintnamelength'></a>
```cs
// Assembly-level attribute to limit generated file name length
// Useful when generated file paths exceed OS limits
// [assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L412-L416' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `maxHintNameLength` (int): Maximum hint name length

Use when hitting Windows path length limits (260 characters).

## Attribute Combinations

### Multiple Operations on One Method

<!-- snippet: attributes-multiple-operations -->
<a id='snippet-attributes-multiple-operations'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L418-L443' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory methods:
```csharp
Task Insert(IPerson person);
Task Update(IPerson person);
```

Both route to the same method.

### Remote + Operation

<!-- snippet: attributes-remote-operation -->
<a id='snippet-attributes-remote-operation'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L445-L473' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Executes on server (serialized call).

### Authorization + Operation

<!-- snippet: attributes-authorization-operation -->
<a id='snippet-attributes-authorization-operation'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L475-L494' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Authorization checked before execution.

## Attribute Inheritance

| Attribute | Inherited |
|-----------|-----------|
| `[Factory]` | Yes |
| `[SuppressFactory]` | Yes |
| `[Create]`, `[Fetch]`, etc. | No |
| `[Remote]` | Yes |
| `[Service]` | No |
| `[AuthorizeFactory<T>]` | No |
| `[AuthorizeFactory]` | No |
| `[AspAuthorize]` | No |

Example:

<!-- snippet: attributes-inheritance -->
<a id='snippet-attributes-inheritance'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L496-L534' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`DerivedWithInheritedFactory` inherits:
- `[Factory]` from `BaseEntityWithFactory`
- `[Remote]` from `BaseEntityWithFactory.Fetch`

`DerivedWithInheritedFactory` does NOT inherit:
- `[Create]` from `BaseEntityWithFactory` constructor
- `[AuthorizeFactory<T>]` (if it were applied to `BaseEntityWithFactory`)

## Common Patterns

### CRUD Entity

<!-- snippet: attributes-pattern-crud -->
<a id='snippet-attributes-pattern-crud'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L536-L576' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Read-Only Entity

<!-- snippet: attributes-pattern-readonly -->
<a id='snippet-attributes-pattern-readonly'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L578-L604' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-readonly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Command Handler

<!-- snippet: attributes-pattern-command -->
<a id='snippet-attributes-pattern-command'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L606-L626' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Event Publisher

<!-- snippet: attributes-pattern-event -->
<a id='snippet-attributes-pattern-event'></a>
```cs
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
```
<sup><a href='/src/docs/samples/AttributesReferenceSamples.cs#L628-L653' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) - All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) - Operation details
- [Authorization](authorization.md) - Authorization attribute usage
