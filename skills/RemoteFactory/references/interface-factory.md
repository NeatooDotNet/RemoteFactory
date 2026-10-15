# Interface Factory Pattern

Use for remote services where the client calls through a generated proxy. The implementation lives only on the server.

## Complete Example

<!-- snippet: interface-factory-contract -->
<a id='snippet-interface-factory-contract'></a>
```cs
[Factory]
public interface IProductCatalog
{
    /// <summary>
    /// Products whose name contains the text. Executed on the server, called from the client.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: The proxy serializes this call to the server,
    /// where the actual ProductCatalog implementation runs.
    /// No attributes needed - the [Factory] on the interface is sufficient.
    /// </remarks>
    Task<IReadOnlyList<ProductListing>> SearchAsync(string text);

    /// <summary>
    /// One product by SKU, or null when there is none.
    /// </summary>
    Task<ProductListing?> FindBySkuAsync(string sku);

    /// <summary>
    /// A product's details, returned as a record with a primary constructor.
    /// </summary>
    /// <remarks>
    /// Demonstrates that Interface Factory methods can return plain records
    /// (no [Factory] attribute). The record is serialized without $id/$ref
    /// metadata, avoiding the System.Text.Json parameterized constructor limitation.
    /// ProductListing (a class) and ProductDetails (a record) take the two
    /// serialization paths a returned type can take.
    /// </remarks>
    Task<ProductDetails?> GetDetailsAsync(string sku);
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs#L228-L259' title='Snippet source file'>snippet source</a> | <a href='#snippet-interface-factory-contract' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: interface-factory-implementation -->
<a id='snippet-interface-factory-implementation'></a>
```cs
public class ProductCatalog : IProductCatalog
{
    // In real code, this would query a product store
    private static readonly ProductDetails[] Products =
    [
        new("WA-100", "Widget A", 10.00m, "The standard widget."),
        new("WB-200", "Widget B", 25.00m, "The heavy-duty widget."),
        new("WC-300", "Widget C", 7.00m, "The compact widget."),
    ];

    public Task<IReadOnlyList<ProductListing>> SearchAsync(string text)
    {
        IReadOnlyList<ProductListing> found = Products
            .Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Select(ToListing)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<ProductListing?> FindBySkuAsync(string sku)
    {
        var product = Products.FirstOrDefault(p => p.Sku == sku);
        return Task.FromResult(product == null ? null : ToListing(product));
    }

    public Task<ProductDetails?> GetDetailsAsync(string sku)
    {
        return Task.FromResult(Products.FirstOrDefault(p => p.Sku == sku));
    }

    private static ProductListing ToListing(ProductDetails product) =>
        new() { Sku = product.Sku, Name = product.Name, UnitPrice = product.UnitPrice };
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs#L291-L325' title='Snippet source file'>snippet source</a> | <a href='#snippet-interface-factory-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**: Proxy implementation that serializes calls to server.

---

## Critical Rules

### Interface methods do NOT need operation attributes

```csharp
// WRONG - emits NF0106 (factory-operation attribute on interface factory method)
[Factory]
public interface IMyRepository
{
    [Fetch]  // DON'T DO THIS — NF0106 error
    Task<Item> GetByIdAsync(int id);
}

// RIGHT - no attributes needed
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);
}
```

The interface IS the remote boundary. Every method crosses it automatically. Enforced by **NF0106** — factory-operation attributes (`[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]`) on any `[Factory]` interface method are a compile-time error. Applies with or without `[AuthorizeFactory<T>]` on the interface.

### Server implementation does NOT have [Factory]

```csharp
// WRONG - causes duplicate registration
[Factory]
public class EmployeeRepository : IEmployeeRepository { }

// RIGHT - no [Factory] on implementation
public class EmployeeRepository : IEmployeeRepository { }
```

The interface already has `[Factory]`; the implementation is just a service.

---

## Server Registration

Register the implementation in DI:

```csharp
// Program.cs
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```

Or use convention-based registration:
```csharp
builder.Services.RegisterMatchingName<IEmployeeRepository>();  // Auto-finds EmployeeRepository
```

---

## When to Use Interface Factory

- **Remote services without entity identity** - Query services, report generators
- **Clean separation** - Interface defines contract, server provides implementation
- **Multiple implementations** - Can swap implementations via DI
- **Third-party integrations** - Wrap external APIs behind interface

---

## Authorization with `[AuthorizeFactory<T>]`

Interface factories support `[AuthorizeFactory<T>]` for server-side authorization of remote calls — but the scope model is different from class factories.

### Scope model — use `Execute` / `Read`, not CRUD

Interface factories have no Create/Fetch/Insert/Update/Delete operations (every interface method is just a remote call), so CRUD scopes silently never match. Use:

- **`AuthorizeFactoryOperation.Execute`** — fires on every interface method call. The broad-scope default for interface factories.
- **`AuthorizeFactoryOperation.Read`** — also fires on every interface method call. Use for "read-like" semantics on the auth side (identical runtime effect to `Execute` on an interface factory).

Using `Create` / `Fetch` / `Insert` / `Update` / `Delete` scopes on an interface-factory auth class is a silent no-op — the auth method is never invoked. The generator does not warn about this today.

### Per-method authorization via parameter matching

Fine-grained per-method auth comes from **parameter matching by type**, not per-operation scopes. An auth method declaring parameters typed to match an interface-method's parameters receives the forwarded values from the call site. Auth methods can return `bool`, `Task<bool>`, `string?`, or `Task<string?>`. String returns surface denial messages in `NotAuthorizedException.Message`, verbatim; a `bool` return carries no reason, so the framework supplies one naming the operation, the interface, and which auth method refused — `Authorization denied for operation GetItem on IRepository (IRepositoryAuth.CanAccessItem); no reason was supplied.` Either way `NotAuthorizedException.Context` exposes that same detail as a property for structured logging. Prefer `string?` whenever the caller benefits from knowing why.

### Complete example

**Auth class:**

<!-- snippet: skill-interface-factory-auth-authclass -->
<a id='snippet-skill-interface-factory-auth-authclass'></a>
```cs
public interface ICustomerDirectoryAuth
{
    /// <summary>
    /// Parameterless Execute auth — fires on every interface method call.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Execute scope on interface factories
    ///
    /// AuthorizeFactoryOperation.Execute is the broad scope for interface
    /// factories — the auth method fires on every interface method unless
    /// parameter matching restricts it. With no parameters, this method
    /// runs on ALL four methods of ICustomerDirectory.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool HasAccess();

    /// <summary>
    /// Parameterized Execute auth — receives the customer Guid from every interface method.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Parameter matching by type
    ///
    /// This auth method's Guid parameter matches by TYPE against the interface
    /// method's parameters. The generator forwards the Guid from each
    /// interface method into this auth method — enabling per-entity
    /// authorization (deny access to specific customers) without per-method
    /// scoping.
    ///
    /// All interface methods on ICustomerDirectory take the customer's Guid,
    /// so this auth method fires on every call.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanAccessCustomer(Guid customerId);

    /// <summary>
    /// String-returning Read auth — returns empty string on authorize, message on deny.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: String return types surface denial messages
    ///
    /// Auth methods can return bool, Task&lt;bool&gt;, string?, or Task&lt;string?&gt;.
    /// For string variants:
    /// - null or empty string = authorized
    /// - non-empty string     = denied with that string as the denial message
    ///
    /// NotAuthorizedException.Message contains the denial string, surfacing
    /// domain-specific "why" to the caller.
    ///
    /// Read scope on an interface factory behaves identically to Execute:
    /// both apply uniformly across all interface methods. Prefer Execute
    /// for interface factories for clarity (Read implies a class-factory
    /// Fetch-like semantic that doesn't exist here).
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    string? CheckReadAccess();
}
```
<sup><a href='/src/Design/Design.Domain/Services/CustomerDirectory.cs#L121-L178' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-auth-authclass' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Interface factory (bare contract — no method attributes):**

<!-- snippet: skill-interface-factory-auth-factory -->
<a id='snippet-skill-interface-factory-auth-factory'></a>
```cs
[Factory]
[AuthorizeFactory<ICustomerDirectoryAuth>]
public interface ICustomerDirectory
{
    /// <summary>One customer, or null. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<CustomerProfile?> GetCustomer(Guid customerId);

    /// <summary>Closes a customer's account. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    /// <remarks>
    /// Returns Task: the command has no result to report. A missing customer is
    /// an exception (VisionAlignment R-I), which reaches the client at the await.
    ///
    /// GENERATOR BEHAVIOR: A Task-returning interface method generates a Task
    /// delegate and factory method; the client proxy awaits the server call and
    /// reads no result (#122, v1.10.4).
    /// </remarks>
    Task CloseAccount(Guid customerId);

    /// <summary>Renames a customer. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<CustomerProfile> RenameCustomer(Guid customerId, string name);

    /// <summary>A customer's contacts. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<IReadOnlyList<CustomerContact>> ListContacts(Guid customerId);
}
```
<sup><a href='/src/Design/Design.Domain/Services/CustomerDirectory.cs#L260-L287' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-auth-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server-side impl (plain class — no `[Factory]`, no op attributes):**

The implementation, `CustomerDirectory`, is a plain class: no `[Factory]` and no attributes on its methods.

The auth class is auto-registered by the generator via `services.TryAddTransient<ICustomerDirectoryAuth, CustomerDirectoryAuth>()` in the generated `FactoryServiceRegistrar` — no manual registration needed. Register the impl on the server only (not the client).

### Generated surface

For each interface method, the generator emits on `IAuthorizedRepositoryFactory`:

- **`{Method}(params)`** — normal forwarding method. `Local{Method}` on the server invokes all applicable auth methods (parameterless + parameter-matched by signature) and throws `NotAuthorizedException` on any denial before calling the impl.
- **`Can{Method}(matching-params, CancellationToken = default)`** — non-throwing helper returning `Authorized`. Runs the same scoped+parameter-matched auth checks. Useful for UI disable-states.

Example: `IAuthorizedRepository.GetItem(Guid id)` generates `CanGetItem(Guid id, CancellationToken ct = default)`.

`Can{Method}` parameters include only those matched by auth methods — e.g., `UpdateItem(Guid id, string name)` → `CanUpdateItem(Guid id)` (the `string name` is dropped because no auth method takes a string parameter).

### Failure behavior

- **Method path:** any denied auth check throws `NotAuthorizedException`. For string-returning auth methods, the denial message appears in `Exception.Message`.
- **`Can{Method}` path:** returns `Authorized` with `HasAccess=false` (no throw).

Unlike class-factory auth, interface factories have no "null on Read failure" behavior — denied calls always throw. There is no `TrySave` equivalent for interface factories (no Save semantics).

### Contrast with class factory auth

| Aspect | Class Factory | Interface Factory |
|--------|--------------|-------------------|
| Auth scopes used | `Create`/`Fetch`/`Insert`/`Update`/`Delete`/`Read`/`Write` | `Execute`/`Read` only |
| Per-method scoping | Via operation attribute on factory method | Via parameter matching by type |
| Generated `Can*` methods | `CanCreate`/`CanFetch`/`CanSave`/`CanDelete` | `Can{Method}` per interface method |
| Failure on Read | Returns `null` | Throws `NotAuthorizedException` |
| `TrySave` available | Yes (with `IFactorySaveMeta`) | No |
| Op attributes on contract | `[Create]`/etc. on class factory methods | **Forbidden** (NF0106) |

See `references/advanced-patterns.md` for class-factory auth (CRUD scopes, `CanSave` aggregation, target parameters).
