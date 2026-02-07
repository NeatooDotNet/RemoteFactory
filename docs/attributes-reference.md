# Attributes Reference

Complete reference of all RemoteFactory attributes.

## Quick Lookup

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[Factory]` | Class, Interface | Enable factory generation |
| `[SuppressFactory]` | Class, Interface | Disable factory generation |
| `[Create]` | Constructor, Method | Instance creation |
| `[Fetch]` | Method | Load existing data |
| `[Insert]` | Method | Persist new entity |
| `[Update]` | Method | Persist changes |
| `[Delete]` | Method | Remove entity |
| `[Execute]` | Method | Business operations |
| `[Event]` | Method | Fire-and-forget events |
| `[Remote]` | Method | Execute on server |
| `[Service]` | Parameter | Inject from DI |
| `[AuthorizeFactory<T>]` | Class, Interface | Custom authorization |
| `[AuthorizeFactory]` | Method | Authorization check |
| `[AspAuthorize]` | Method | ASP.NET Core policies |
| `[assembly: FactoryMode]` | Assembly | Generation mode (Full/RemoteOnly) |
| `[assembly: FactoryHintNameLength]` | Assembly | Limit generated file names |

---

## Factory Discovery Attributes

### [Factory]

Marks a class or interface for factory generation.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-factory -->
<a id='snippet-attributes-factory'></a>
```cs
[Factory]  // Enables factory generation
public partial class MinimalEmployee
{
    [Create]
    public MinimalEmployee() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L10-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
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
public partial class BaseEntity { }

[SuppressFactory]  // Prevents factory generation on derived class
public partial class InternalEntity : BaseEntity { }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L19-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeCreate
{
    [Create]  // Constructor-based creation
    public EmployeeCreate(string name) { Name = name; }

    [Create]  // Static factory method - different signature
    public static EmployeeCreate Create(string name, decimal salary) => new(name) { Salary = salary };

    public string Name { get; }
    public decimal Salary { get; private set; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L27-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-create' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeFetch
{
    [Remote, Fetch]  // Returns bool: false = not found (factory returns null)
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L42-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeInsert : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Insert]  // Persists new entity
    public Task Insert([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L52-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeUpdate : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Update]  // Persists changes to existing entity
    public Task Update([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L64-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-update' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeDelete : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Delete]  // Removes entity from persistence
    public Task Delete([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L76-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Delete | Write`

### [Execute]

Marks methods for business operations.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-execute -->
<a id='snippet-attributes-execute'></a>
```cs
[Factory]
public static partial class PromoteCommand
{
    [Remote, Execute]  // Business operation - underscore prefix removed in delegate name
    private static Task<bool> _Execute(Guid employeeId, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L88-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Execute | Read`

### [Event]

Marks methods for fire-and-forget domain events.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-event -->
<a id='snippet-attributes-event'></a>
```cs
[Factory]
public partial class EmployeeEventsMinimal
{
    [Event]  // Fire-and-forget - CancellationToken must be last parameter
    public Task NotifyManager(Guid employeeId, [Service] IEmailService email, CancellationToken ct)
        => email.SendAsync("mgr@co.com", "Update", $"Employee {employeeId}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L98-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-event' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeRemote
{
    [Create]  // No [Remote] - executes locally without network call
    public EmployeeRemote() { }

    [Remote, Fetch]  // [Remote] - serializes request, sends via HTTP, deserializes response
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct) => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L108-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
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
public partial class EmployeeWithService
{
    [Remote, Fetch]
    public Task<bool> Fetch(
        Guid employeeId,                          // Value parameter: serialized to server
        [Service] IEmployeeRepository repository, // [Service]: resolved from DI container
        CancellationToken ct) => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L120-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
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
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level authorization
public partial class AuthEmployee { }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L132-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
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
public interface IMinimalDocAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]   // Maps to Fetch operations
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]  // Maps to Insert, Update, Delete
    bool CanWrite();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L138-L147' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**On factory method (additional check):**

<!-- snippet: attributes-authorizefactory-method -->
<a id='snippet-attributes-authorizefactory-method'></a>
```cs
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level: checked first
public partial class MethodAuthEmployee : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Delete]
    [AspAuthorize(Roles = "HRManager")]  // Method-level: additional check after class-level
    public Task Delete([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L149-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-method' title='Start of snippet'>anchor</a></sup>
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
public partial class PolicyEmployee : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Fetch]
    [AspAuthorize("RequireEmployee")]  // Policy-based authorization
    public Task<bool> FetchWithPolicy(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);

    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]  // Role-based authorization
    public Task InsertWithRoles([Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L163-L180' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
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
// Full mode (default): generates local and remote code
// [assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: generates HTTP stubs only (use in Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L5-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L13-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength-1' title='Start of snippet'>anchor</a></sup>
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
public partial class UpsertSetting : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Insert, Update]  // Both operations point to same method
    public Task Upsert(CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L182-L192' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
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
public partial class RemoteOps : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Fetch]   // Server-side data loading
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);

    [Remote, Insert]  // Server-side persistence
    public Task Insert([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L194-L207' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Executes on server (serialized call).

### Authorization + Operation

<!-- snippet: attributes-authorization-operation -->
<a id='snippet-attributes-authorization-operation'></a>
```cs
public interface IOpAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]  // Combined flags
    bool CanCreateAndRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L209-L218' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
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
[Factory]   // Inherited: Yes
public partial class BaseWithFactory
{
    [Create]    // Inherited: No
    public BaseWithFactory() { }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);
}

public partial class DerivedEntity : BaseWithFactory
{
    // Inherits [Factory] and [Remote] from base
    // Does NOT inherit [Create] - must redeclare
    [Create]
    public DerivedEntity() : base() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L220-L238' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
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
[Factory]
public partial class CrudEntity : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public CrudEntity() { }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct)
        => Task.FromResult(true);

    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;

    [Remote, Update]
    public Task Update([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;

    [Remote, Delete]
    public Task Delete([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L240-L263' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Read-Only Entity

<!-- snippet: attributes-pattern-readonly -->
<a id='snippet-attributes-pattern-readonly'></a>
```cs
[Factory]
public partial class ReadOnlyEntity
{
    [Create]
    public ReadOnlyEntity() { }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct)
        => Task.FromResult(true);
    // No Insert, Update, Delete - read-only projection
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L265-L277' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-readonly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Command Handler

<!-- snippet: attributes-pattern-command -->
<a id='snippet-attributes-pattern-command'></a>
```cs
[Factory]
public static partial class TransferCommand
{
    [Remote, Execute]  // Static class with [Execute] for command pattern
    private static Task<TransferCommandResult> _Execute(
        Guid employeeId, Guid newDeptId, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(new TransferCommandResult(true, "Transferred"));
}
public record TransferCommandResult(bool Success, string Message);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L279-L289' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Event Publisher

<!-- snippet: attributes-pattern-event -->
<a id='snippet-attributes-pattern-event'></a>
```cs
[Factory]
public partial class LifecycleEvents
{
    [Event]  // Fire-and-forget domain events
    public Task OnEmployeeHired(Guid id, string email, [Service] IEmailService svc, CancellationToken ct)
        => svc.SendAsync(email, "Welcome!", $"ID: {id}", ct);

    [Event]
    public Task OnEmployeePromoted(Guid id, string title, [Service] IEmailService svc, CancellationToken ct)
        => svc.SendAsync("hr@co.com", "Promotion", $"{id} to {title}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L291-L303' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) - All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) - Operation details
- [Authorization](authorization.md) - Authorization attribute usage
