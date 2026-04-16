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
| `[FactoryEventHandler<T>]` | Class | Mediator + client relay handler for `FactoryEventBase` events |
| `[Remote]` | Method | Client-to-server entry point |
| `[Service]` | Parameter | Inject from DI |
| `[AuthorizeFactory<T>]` | Class, Interface | Custom authorization |
| `[AuthorizeFactory]` | Method | Authorization check |
| `[AspAuthorize]` | Method | ASP.NET Core policies |
| `[assembly: FactoryHintNameLength]` | Assembly | Limit generated file names |

---

## Factory Discovery

### [Factory]

Marks a class or interface for factory generation. Generates `I{TypeName}Factory` interface and `{TypeName}Factory` implementation.

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

On an interface, `[Factory]` generates a remote proxy. All interface methods become remote entry points — no operation attributes needed. The server provides the implementation class (without `[Factory]`). See [Interface Factory](interface-factory.md) for the full pattern.

```csharp
[Factory]  // Generates proxy — all methods are remote
public interface IOrderQueryService
{
    Task<IReadOnlyList<OrderSummary>> GetAllAsync();
    Task<OrderSummary?> GetByIdAsync(int id);
}

// Server implementation — no [Factory] here
public class OrderQueryService : IOrderQueryService { ... }
```

### [SuppressFactory]

Prevents factory generation for a class or interface. Use when a base class has `[Factory]` but a derived class shouldn't have its own factory.

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

## Operation Attributes

### [Create]

Marks constructors or methods that create new instances. Supports multiple overloads with different signatures.

**Inherited:** No | **Auth flags:** `Create | Read`

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

### [Fetch]

Marks methods that load data into existing instances. Returns `bool` or `Task<bool>` — `false` means not found (factory returns `null`).

**Inherited:** No | **Auth flags:** `Fetch | Read`

<!-- snippet: attributes-fetch -->
<a id='snippet-attributes-fetch'></a>
```cs
[Factory]
public partial class EmployeeFetch
{
    [Remote, Fetch]  // Returns bool: false = not found (factory returns null)
    internal Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L42-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [Insert], [Update], [Delete]

Write operations routed by `Save()` based on `IsNew` and `IsDeleted` flags. Require `IFactorySaveMeta`. Can be combined on a single method (e.g., `[Insert, Update]` for upsert).

**Inherited:** No | **Auth flags:** `Insert|Write`, `Update|Write`, `Delete|Write`

<!-- snippet: attributes-insert -->
<a id='snippet-attributes-insert'></a>
```cs
[Factory]
public partial class EmployeeInsert : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Insert]  // Persists new entity
    internal Task Insert([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L52-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Combining operations on one method:

<!-- snippet: attributes-multiple-operations -->
<a id='snippet-attributes-multiple-operations'></a>
```cs
[Factory]
public partial class UpsertSetting : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Insert, Update]  // Both operations point to same method
    internal Task Upsert(CancellationToken ct) => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L173-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [Execute]

Marks methods for business operations. Typically on static classes for a command pattern. Underscore prefix on method names is removed in the generated delegate name.

**Inherited:** No | **Auth flags:** `Execute | Read`

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

### [FactoryEventHandler\<T\>]

Class-level attribute that marks a class as a **server-side** static handler for factory events of type `T` (where `T : FactoryEventBase`). The source generator finds one matching `static` method by signature and registers it with `FactoryEventHandlerRegistry`. See [Factory Events](factory-events.md) for the full pattern.

**Inherited:** No | **Multiple:** Yes (stack one per event type)

**Method matching rules (static handler only):**
- Must be `static`
- Return type must be `Task`
- First non-`[Service]`/non-`CancellationToken` parameter must be of type `T`
- Any accessibility allowed
- Exactly one match required — `NF0501` if none, `NF0502` if multiple

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderAuditHandler
{
    internal static Task Log(
        OrderCheckoutCompleted evt,
        [Service] IAuditLogService audit,
        CancellationToken ct) =>
        audit.LogAsync("Checkout", evt.OrderId, "Order", $"Total: {evt.Total:C}", ct);
}
```

Runs in the caller's DI scope via `FactoryEventHandlerRegistry`, triggered by `IFactoryEvents.Raise` during a factory method. All handlers for the event type run sequentially, awaited, sharing the caller's `DbContext` and transaction. A throwing handler aborts the chain and propagates to the caller. For fire-and-forget work that should not participate in the caller's transaction, compose a manual `Task.Run` + `IServiceScopeFactory.CreateScope()` pattern inside the factory method (see the [v1.5.0 release notes](release-notes/v1.5.0.md)).

> **Instance-method handlers are not supported.** Declaring a non-`static` matching method inside a `[FactoryEventHandler<T>]` class emits **NF0503 (Warning)** and is silently skipped at runtime. Client-side reception is handled by implementing `IFactoryEventRelay` on your own class and registering it in DI — see [Factory Events — Client-Side Relay](factory-events.md#client-side-relay-consumer-implements-ifactoryeventrelay) and the [`IFactoryEventRelay`](interfaces-reference.md#ifactoryeventrelay) interface reference.

A single class can stack multiple `[FactoryEventHandler<T>]` attributes to handle several event types — the generator matches one `static` method per attribute.

### [FactoryEvent]

Class-level attribute carried by `FactoryEventBase` with `Inherited = true`. Drives runtime discovery of event types by `FactoryEventTypeRegistry` — every descendant of `FactoryEventBase` is automatically discoverable without per-event annotation.

**Inherited:** Yes | **Multiple:** No

```csharp
// FactoryEventBase (in Neatoo.RemoteFactory) — applied once, inherited by every descendant.
[FactoryEvent]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                            DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract record FactoryEventBase;

// Consumer code — no attribute required on the descendant.
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
```

Consumers **do not** apply `[FactoryEvent]` directly — inheriting `FactoryEventBase` is sufficient. The attribute is documented here for completeness; applying it to a type that does not inherit `FactoryEventBase` has no effect.

## Execution Control

### [Remote]

Marks methods as client-to-server entry points. Without `[Remote]`, methods execute locally. See [Client-Server Architecture](client-server-architecture.md) for when to use it.

**Inherited:** Yes

<!-- snippet: attributes-remote -->
<a id='snippet-attributes-remote'></a>
```cs
[Factory]
public partial class EmployeeRemote
{
    [Create]  // No [Remote] - executes locally without network call
    public EmployeeRemote() { }

    [Remote, Fetch]  // [Remote] - crosses client/server boundary via HTTP
    internal Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L98-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [Service]

Marks parameters for dependency injection. Service parameters are resolved from the DI container and never serialized. See [Service Injection](service-injection.md) for constructor vs method injection.

**Inherited:** No

<!-- snippet: attributes-service -->
<a id='snippet-attributes-service'></a>
```cs
[Factory]
public partial class EmployeeWithService
{
    [Remote, Fetch]
    internal Task<bool> Fetch(
        Guid employeeId,                          // Value parameter: serialized to server
        [Service] IEmployeeRepository repository, // [Service]: resolved from DI container
        CancellationToken ct) => Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L111-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization

### [AuthorizeFactory\<T\>]

Applies a custom authorization interface to the factory. The type parameter must be an interface with methods decorated with `[AuthorizeFactory]`. See [Authorization](authorization.md).

**Inherited:** No

<!-- snippet: attributes-authorizefactory-generic -->
<a id='snippet-attributes-authorizefactory-generic'></a>
```cs
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level authorization
public partial class AuthEmployee { }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L123-L127' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [AuthorizeFactory]

Marks methods in authorization interfaces, mapping them to operation flags.

**Inherited:** No

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L129-L138' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Combine flags with bitwise OR:

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L200-L209' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [AspAuthorize]

Applies ASP.NET Core authorization policies to factory methods. Multiple `[AspAuthorize]` attributes require all policies to pass. See [Authorization](authorization.md).

**Inherited:** No | **Multiple:** Yes

**Properties:** `Policy` (string?), `Roles` (string?, comma-delimited), `AuthenticationSchemes` (string?, comma-delimited)

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
    internal Task<bool> FetchWithPolicy(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);

    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]  // Role-based authorization
    internal Task InsertWithRoles([Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L154-L171' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Assembly-Level Attributes

### [assembly: FactoryHintNameLength]

Limits generated file hint name length. Use when hitting Windows path length limits (260 characters).

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L5-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Attribute Inheritance

| Attribute | Inherited | Note |
|-----------|-----------|------|
| `[Factory]` | Yes | Derived classes get their own factory |
| `[SuppressFactory]` | Yes | Blocks factory on derived classes too |
| `[Remote]` | Yes | Derived methods inherit remote execution |
| `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]` | No | Must redeclare on each class |
| `[FactoryEventHandler<T>]` | No | Stack multiple for multiple event types |
| `[Service]` | No | Must apply to each parameter |
| `[AuthorizeFactory<T>]`, `[AuthorizeFactory]`, `[AspAuthorize]` | No | Must redeclare on each class/method |

<!-- snippet: attributes-inheritance -->
<a id='snippet-attributes-inheritance'></a>
```cs
[Factory]   // Inherited: Yes
public partial class BaseWithFactory
{
    [Create]    // Inherited: No
    public BaseWithFactory() { }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    internal Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);
}

public partial class DerivedEntity : BaseWithFactory
{
    // Inherits [Factory] and [Remote] from base
    // Does NOT inherit [Create] - must redeclare
    [Create]
    public DerivedEntity() : base() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L211-L229' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) — All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) — Operation details and patterns
- [Factory Events](factory-events.md) — `[FactoryEventHandler<T>]` mediator + client relay
- [Authorization](authorization.md) — Authorization attribute usage
