# Interface Factory

An interface factory is a domain service the client calls and the server runs. You declare the contract as a C# interface with `[Factory]` and implement it on the server. RemoteFactory generates a proxy that serializes each call to the server. Clients inject the interface from DI and call it as usual. The code comes from RemoteFactory's compiled example set, `src/Design`.

Use it for a call that returns and is done: a search, a lookup, a calculation only the server can make. When the client holds an object and binds to it, use a [Class Factory](factory-operations.md). For a stateless command, use a [Static Factory](factory-operations.md#execute-operation). A repository is never an interface factory: persistence is a `[Service]` injected into server-side operations.

See the [Decision Guide](decision-guide.md#which-factory-pattern) for a comparison of all three patterns.

## Complete Example

### 1. Declare the interface

Put `[Factory]` on the interface. Its methods need no operation attributes: every method is a call to the server. Design's product catalog:

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

The methods return a class (`ProductListing`) and a record (`ProductDetails`). Both are plain types with no `[Factory]`, serialized as the call's result.

### 2. Implement it on the server

The implementation is a plain class. It does **not** get `[Factory]`; only the interface has it.

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

### 3. Register the implementation on the server

The server registers the implementation for the interface. `ProductCatalog` is named for its interface without the leading `I`, in the same namespace, so the server's `RegisterMatchingName` call registers it (see [Service Injection](service-injection.md#registermatchingname)). The explicit form is `services.AddScoped<IProductCatalog, ProductCatalog>()`. The client registers nothing: the generated registration gives it the proxy.

### 4. Call it from the client

Resolve the interface and call it. The proxy sends the call to the server:

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

## What the Generator Produces

For `IProductCatalog` the generator emits:
1. **`IProductCatalogFactory`**, which extends `IProductCatalog`, and **`ProductCatalogFactory`**, which implements it. On the client each method serializes its arguments, posts them to `/api/neatoo`, and deserializes the result.
2. **A delegate type per method**, which the server's endpoint invokes.
3. **Registrations for each mode:**
   - **Remote:** `IProductCatalog` and `IProductCatalogFactory` resolve to the proxy.
   - **Server:** the delegates call the `IProductCatalog` the application registered. The generator registers no implementation; step 3 does.
   - **Logical:** nothing in between. `IProductCatalog` resolves to the implementation.

The client injects `IProductCatalog` and never names the proxy.

## No Attributes on Interface Methods

`[Factory]` on the interface makes every method a remote entry point. An operation attribute on an interface method is compile error **NF0106**:

```csharp
// WRONG - NF0106: factory-operation attribute on interface factory method
[Factory]
public interface IProductCatalog
{
    [Fetch]
    Task<ProductListing?> FindBySkuAsync(string sku);
}
```

`[Remote]` is not needed either: every method of an interface factory is already remote.

## No [Factory] on the Implementation

Only the interface gets `[Factory]`. On the implementation too, the build fails with CS0101 and CS0111: the class's own factory is generated as `IProductCatalogFactory` and `ProductCatalogFactory`, the same names as the interface's.

```csharp
// WRONG - CS0101/CS0111: both declarations generate ProductCatalogFactory
[Factory]
public class ProductCatalog : IProductCatalog { ... }
```

## Authorization

`[AuthorizeFactory<T>]` on the interface applies an authorization class to its methods. The checks run on the server before the implementation, and the generated `I{Name}Factory` gains a `Can*` method for each method (`ICustomerDirectoryFactory.CanGetCustomer`). A method has no result to carry a refusal, so a refusal is a `NotAuthorizedException`, thrown on the server and thrown again on the client. Design's customer directory carries one. See [Authorization — Authorization Failures](authorization.md#authorization-failures).

## Differences from Class Factory

| Aspect | Class Factory | Interface Factory |
|--------|---------------|-------------------|
| `[Factory]` goes on | The class (`internal partial class Order`) | The interface (`public interface IProductCatalog`) |
| Operation attributes | Required (`[Create]`, `[Fetch]`, `[Insert]`, ...), `[Remote]` on entry points | None: every method is remote |
| Object state | Serialized across the boundary | None: arguments and result only |
| `IFactorySaveMeta` | Supported (Insert/Update/Delete routing) | Not applicable |
| `partial` | Expected on the class. Without it the factory is still generated, but the class is written in the Named shape, never the ordinal one ([Serialization](serialization.md)) | Not used |
| Implementation | The `[Factory]` class is the implementation | A separate class, with no `[Factory]` |
| Generated output | A factory that creates, loads and saves instances | A proxy that forwards calls to the server |

## When to Use an Interface Factory

- **Queries that return and are done:** a search, a lookup, a report, returning records or plain classes.
- **Server-side computation:** a pricing or scheduling service the client asks for an answer.
- **Wrappers for external services:** a third-party API the server calls on the client's behalf.

When the client holds an object, edits it and saves it, use a [Class Factory](factory-operations.md). When it binds to a read-only object, use a `[Fetch]`-only class factory.

## Next Steps

- [Decision Guide](decision-guide.md#which-factory-pattern): choosing between factory patterns
- [Factory Operations](factory-operations.md): class factory operations
- [Client-Server Architecture](client-server-architecture.md): how the proxy fits in
- [ASP.NET Core Integration](aspnetcore-integration.md): server setup for the `/api/neatoo` endpoint
