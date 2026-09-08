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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L11-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L20-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L28-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-create' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L43-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L53-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L184-L194' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [Execute]

Marks static methods for business operations — request-response commands on a static `[Factory]` class (the delegate name is the method name with its underscore prefix removed), or orchestration on a class factory (a factory-interface method returning the containing type). Like every operation, **`[Execute]` obeys `[Remote]`**: with it the call is a client-to-server entry point; without it the method runs on whichever tier resolves it, with that tier's services.

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L89-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without `[Remote]`, the same shape runs locally — on the client with the client's services, on the server with the server's:

<!-- snippet: attributes-execute-local -->
<a id='snippet-attributes-execute-local'></a>
```cs
[Factory]
public static partial class TallyCommand
{
    [Execute]  // No [Remote] - runs on whichever tier resolves the delegate, with that tier's services
    private static Task<decimal> _Total(decimal baseSalary, decimal bonus, [Service] ISalaryCalculator calculator)
        => Task.FromResult(calculator.Calculate(baseSalary, bonus));
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L99-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute-local' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Where it runs.** A bare `[Execute]` gets one unguarded local path in every factory mode and no remote delegate or endpoint: it runs on whichever tier resolves it, and its `[Service]` parameters are resolved from that tier's container — so a bare command that takes a server-only repository compiles, then fails on the client at call time with a DI resolution error. `[Remote, Execute]` gives the client a remote delegate and the server a guarded local path. On a class factory, visibility follows the ordinary rule: `public static` runs where the factory resolves; `internal static` without `[Remote]` is server-only, guarded, and carried on the factory interface with the `internal` modifier; `[Remote] internal static` is promoted to `public` on the interface. See [Factory Operations](factory-operations.md#execute-operation) for the two shapes.

**Trimming:** `[Remote]` is what makes an `[Execute]` body trimmable, by deciding whether a guard is emitted at all. With `[Remote]` — or `internal` on a class factory — the local path is guarded by `NeatooRuntime.IsServerRuntime`, so a client published with the feature switch set to `false` drops the method body, its `[Service]` dependencies, and their transitive references. Without it the body ships to the client and runs there; that is the point. Both halves are measured in the trimming gate rather than inferred. See [IL Trimming](trimming.md).

**NF0105 and static methods.** `[Remote] public` is an error on instance methods (NF0105) because on a class factory visibility is the local/server-only axis. Static methods are exempt: a static-factory `[Execute]` is `private static` behind a generated public wrapper, and a class-level `[Execute]` is `public static` by convention — on those shapes `[Remote]` alone drives the guard, and the body trims regardless of visibility.

### [FactoryEventHandler\<T\>]

Class-level attribute that marks a class as a **server-side** static handler for factory events of type `T` (where `T : FactoryEventBase`). The source generator finds one matching `static` method by signature and registers it with `FactoryEventHandlerRegistry`. An optional `DispatchPhase` argument declares *when* the handler runs — `Immediate` (the default, at `Raise`), `AfterFlush` (queued, drained by the factory body's `IFactoryEventPhaseCoordinator.DrainAsync` call), or `AfterCommit` (queued, drained by the framework after the entry call succeeds). See [Factory Events](factory-events.md) for the full pattern and [Dispatch Phases](factory-events.md#dispatch-phases) for the phase contract.

```csharp
[FactoryEventHandler<OrderShipped>]                            // Immediate — the default
[FactoryEventHandler<OrderShipped>(DispatchPhase.AfterFlush)]  // deferred, consumer-drained
[FactoryEventHandler<OrderShipped>(DispatchPhase.AfterCommit)] // deferred, framework-drained
[FactoryEventHandler<OrderShipped>(DispatchPhase.AfterCommit,
                                   Coalesce = true)]           // identical queued dispatches collapse
```

The optional `Coalesce` named argument collapses identical **queued** dispatches (same handler, `Equals`-equal event, same `RaiseOptions`) to one per drain — see [Coalescing](factory-events.md#coalescing-identical-dispatches-opt-in) for the identity contract and its hazards. The flag is inert on anything that isn't queued (`Immediate` emits `NF0505`).

**Inherited:** No | **Multiple:** Yes (stack one per event type)

**Method matching rules (static handler only):**
- Must be `static`
- Return type must be `Task`
- First non-`[Service]`/non-`CancellationToken` parameter must be of type `T`
- Any accessibility allowed
- Exactly one match required — `NF0501` if none, `NF0502` if multiple
- One attribute per event type — a repeated event type on the same class emits `NF0504` (Warning) and only the first declaration registers: its phase and its `Coalesce` flag

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

**Trimming:** handler registrations are wrapped in `NeatooRuntime.IsServerRuntime`, so a client published with the feature switch set to `false` drops the handler bodies and their `[Service]` dependencies. Because those registrations are entirely server-guarded, there is nothing left on a trimmed client to resolve — handler registration cannot be verified from a client-side test, only from server-side or untrimmed ones. See [IL Trimming](trimming.md).

Runs in the caller's DI scope via `FactoryEventHandlerRegistry`, triggered by `IFactoryEvents.Raise` during a factory method. Handlers run sequentially, awaited, at the drain point their phase declares. `Immediate` handlers (the default) run at `Raise`, sharing the caller's `DbContext` and transaction with staged (unflushed) state visible; a throwing `Immediate` handler aborts the chain and propagates to the caller. Deferred handlers (`AfterFlush`, `AfterCommit`) are queued at `Raise` and run at their drain point — in-transaction drains propagate exceptions, the post-completion drain logs and swallows them, and a failed factory call discards queued work entirely. See [Dispatch Phases](factory-events.md#dispatch-phases). For fire-and-forget work that should not participate in the factory operation at all, compose a manual `Task.Run` + `IServiceScopeFactory.CreateScope()` pattern inside the factory method (see the [v1.5.0 release notes](release-notes/v1.5.0.md)).

> **Instance-method handlers are not supported.** Declaring a non-`static` matching method inside a `[FactoryEventHandler<T>]` class emits **NF0503 (Warning)** and is silently skipped at runtime. Client-side reception is handled by implementing `IFactoryEventRelay` on your own class and registering it in DI — see [Factory Events — Client-Side Relay](factory-events.md#client-side-relay-consumer-implements-ifactoryeventrelay) and the [`IFactoryEventRelay`](interfaces-reference.md#ifactoryeventrelay) interface reference.

A single class can stack multiple `[FactoryEventHandler<T>]` attributes to handle **several event types** — the generator matches one `static` method per attribute, and each attribute carries its own phase and `Coalesce` flag. That is the documented basis of stacking, and the generator enforces it: repeating the *same* event type on one class emits `NF0504` (Warning), and only the first declaration's registration — phase and flag — stands.

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

Marks methods as client-to-server entry points. Without `[Remote]`, methods execute locally — on every operation, `[Execute]` included. On class factories `[Remote]` requires `internal` (NF0105) and the generator promotes the member to `public` on the factory interface; static methods are exempt from NF0105 — see [[Execute]](#execute) for why. See [Client-Server Architecture](client-server-architecture.md) for when to use it.

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L109-L120' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L122-L132' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L134-L138' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L140-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L211-L220' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L165-L182' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L222-L240' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) — All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) — Operation details and patterns
- [Factory Events](factory-events.md) — `[FactoryEventHandler<T>]` mediator + client relay
- [Authorization](authorization.md) — Authorization attribute usage
