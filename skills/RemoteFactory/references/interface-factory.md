# Interface Factory

Use for a domain service the client calls: a search, a lookup, a calculation only the server can make. The call returns and is done. The contract is a C# interface with `[Factory]`; the implementation is a plain class on the server; the client resolves the interface and gets a generated proxy that sends each call to the server.

A repository is never an interface factory. A repository is a `[Service]` parameter on a server operation (`Order`'s `Fetch` in [class-factory.md](class-factory.md)); what the client calls is a service named for what it does.

## The contract

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

The methods return a class (`ProductListing`) and a record (`ProductDetails`), plain types with no `[Factory]`, serialized as the call's result.

## The implementation

A plain class, with no `[Factory]` and no attributes on its methods:

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

## Registration

The server registers the implementation. `ProductCatalog` is named for its interface without the leading `I`, in the same namespace, so `RegisterMatchingName` picks it up; it registers transient, and `services.AddTransient<IProductCatalog, ProductCatalog>()` is the explicit form. The client registers nothing: the generated registration gives it the proxy.

<!-- snippet: aspnetcore-service-registration -->
<a id='snippet-aspnetcore-service-registration'></a>
```cs
services.AddScoped<IPhaseAuditService, PhaseAuditService>();
services.AddSingleton<IOrderDbContext, InMemoryOrderDbContext>();
services.AddScoped<IProductReviewService, InMemoryProductReviewService>();
services.AddScoped<IExampleService, ExampleService>();
services.AddSingleton<ITaxTable, InMemoryTaxTable>();
services.AddSingleton<ICustomerPreferencesStore, InMemoryCustomerPreferencesStore>();
services.AddSingleton<ISupportLog, InMemorySupportLog>();
services.AddSingleton<ICheckoutLedger, InMemoryCheckoutLedger>();
services.AddScoped(_ => new TenantTokenService("server-token"));
services.AddScoped<ITenantTokenService>(sp => sp.GetRequiredService<TenantTokenService>());

services.RegisterMatchingName(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L197-L210' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## The call

<!-- snippet: interface-factory-usage -->
<a id='snippet-interface-factory-usage'></a>
```cs
// Get the interface from client - this is the proxy
var catalog = client.GetRequiredService<IProductCatalog>();

// Act - call goes through proxy to server
var result = await catalog.SearchAsync("Widget");
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs#L46-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-interface-factory-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates** `IProductCatalogFactory`, which extends `IProductCatalog`, and its implementation; a delegate type per method, which the server's endpoint invokes; and registrations for each mode. In Remote mode both interfaces resolve to the proxy; in Server mode the delegates call the registered implementation; in Logical mode `IProductCatalog` resolves to the implementation directly.

## Rules

- **No attributes on the interface's methods.** `[Factory]` on the interface makes every method a remote entry point. An operation attribute (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`) on one is compile error NF0106, with or without `[AuthorizeFactory<T>]`. `[Remote]` is not needed either.
- **No `[Factory]` on the implementation.** On both, the build fails with CS0101 and CS0111: the class's own factory would be generated as `IProductCatalogFactory` and `ProductCatalogFactory`, the interface's names.
- **Return `Task` or `Task<T>`.** A `Task` method awaits the server call and reads no result; a server exception rethrows at the await.

## Authorization with `[AuthorizeFactory<T>]`

`[AuthorizeFactory<T>]` on the interface applies an authorization class to its methods. The checks run on the server before the implementation.

**Scopes: `Execute` and `Read`.** An interface has no Create, Fetch or Save, so an auth method scoped `Create`, `Fetch`, `Insert`, `Update` or `Delete` never runs - silently; the generator does not warn. `Execute` and `Read` both run on every interface method.

**Per-method checks match parameters by type.** An auth method whose parameters match an interface method's receives the call's values. Every method of `ICustomerDirectory` takes the customer's `Guid`, so `CanAccessCustomer(Guid)` runs on each. Keep every authorized method on the parameter the auth method takes: one that lacks it fails CS7036 in generated code today ([#123](https://github.com/NeatooDotNet/RemoteFactory/issues/123)).

**Return `bool` or `string?`** (or their `Task` forms). A `string?` refusal reaches the caller as `NotAuthorizedException.Message`, verbatim; a `bool` refusal gets a message naming the operation, the interface and the auth method that refused. Either way `NotAuthorizedException.Context` carries that detail.

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
    /// an exception, which reaches the client at the await.
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

The implementation, `CustomerDirectory`, is a plain class. The generator registers the auth class (`TryAddTransient<ICustomerDirectoryAuth, CustomerDirectoryAuth>`); register the implementation on the server only.

**Generated surface.** For each method, `ICustomerDirectoryFactory` carries the method itself and a `Can{Method}` that runs the same checks without calling the implementation and returns `Authorized` (`CanGetCustomer(Guid customerId, CancellationToken cancellationToken = default)`). A `Can{Method}` takes only the parameters an auth method matches.

**Failure.** A refused call throws `NotAuthorizedException` on the server, and the client throws it again, with the same message and `Context`; over HTTP it travels as a 403. `Can{Method}` returns `HasAccess == false` and does not throw. There is no "null on refusal": the method has no result to carry one.

| | Class factory | Interface factory |
|---|---|---|
| Scopes | `Create`, `Fetch`, `Insert`, `Update`, `Delete`, `Read`, `Write` | `Execute`, `Read` |
| Per-method checks | The operation's scope | Parameter matching by type |
| Generated checks | `CanCreate`, `CanFetch`, `CanSave`, ... | `Can{Method}` per method |
| A refused read | Returns `null` | Throws `NotAuthorizedException` |
| Operation attributes on the contract | Required | NF0106 |

Class-factory authorization is in [advanced-patterns.md](advanced-patterns.md).
