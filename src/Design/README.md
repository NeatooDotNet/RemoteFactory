# Design Source of Truth

This directory contains the **authoritative design reference** for RemoteFactory. These projects are specifically designed to help understand the RemoteFactory API through working code with extensive comments.

## Purpose

Design decisions are easily lost or contradicted when spread across documentation, samples, and implementation. These projects solve that by being:

1. **The single source of truth** - Updated first, everything else flows from it
2. **Heavily commented** - Not just "what" but "why" and "what we didn't do"
3. **Fully functional** - Compiles and tests pass, ensuring accuracy
4. **AI-optimized** - Structured for Claude Code to understand the API

## Project Structure

```
src/Design/
├── Design.sln                    # Solution file
├── Design.Domain/                # Domain model demonstrating all patterns
│   ├── FactoryPatterns/          # All three factory patterns side-by-side
│   │   └── AllPatterns.cs        # Class, Interface, Static factories
│   ├── Aggregates/               # DDD aggregate root example
│   │   └── Order.cs              # Full lifecycle with IFactorySaveMeta
│   ├── Entities/                 # Child entity example
│   │   └── OrderLine.cs          # Child and root (R-G): no [Remote] child operation
│   ├── ReadModels/               # Fetch-only read model (OrderSummary)
│   ├── Contexts/                 # A context a screen binds to (OrderCheckout)
│   └── ValueObjects/             # Value object examples
│       ├── Money.cs              # Plain records whose constructors throw (R-F)
│       └── TaxRate.cs            # A value object whose construction needs the server
├── Design.Tests/                 # Comprehensive test suite
│   ├── TestInfrastructure/       # Client/server container simulation
│   └── FactoryTests/             # Tests for each pattern
├── Design.Server/                # ASP.NET Core server
│   └── Program.cs                # AddNeatooAspNetCore() setup
└── Design.Client.Blazor/         # Blazor WASM client
    └── Program.cs                # AddNeatooRemoteFactory() setup
```

## The Three Factory Patterns

### 1. Class Factory
Apply `[Factory]` to a class to generate a factory interface for entity lifecycle.

<!-- snippet: order-class-declaration -->
<a id='snippet-order-class-declaration'></a>
```cs
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L80-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-class-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: operations-fetch-instance -->
<a id='snippet-operations-fetch-instance'></a>
```cs
/// <summary>
/// Loads an existing Order and its lines; false when there is none.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(
    int id,
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(id);
    if (row == null)
        return false;

    LoadFrom(row, lineListFactory);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L251-L268' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generated**: `IOrderFactory` with `Create()`, `Fetch()` and `Save()`. The class is `internal`, the client works through `IOrder`, and a `[Remote]` method is `internal` (NF0105).

### 2. Interface Factory
Apply `[Factory]` to an interface to create a remote proxy.

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

**Generated**: Proxy that serializes calls to server where implementation runs.

### 3. Static Factory
Apply `[Factory]` to a static class for commands and events.

<!-- snippet: ordercommands-declaration -->
<a id='snippet-ordercommands-declaration'></a>
```cs
[Factory]
public static partial class OrderCommands
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L47-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-ordercommands-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: operations-execute -->
<a id='snippet-operations-execute'></a>
```cs
[Remote, Execute]
private static async Task<SubmitOutcome> _SubmitOrder(
    int orderId,
    [Service] IOrderFactory orderFactory,
    CancellationToken cancellationToken)
{
    var order = await orderFactory.Fetch(orderId, cancellationToken);
    if (order == null)
        return SubmitOutcome.NotFound;
    if (!order.CanSubmit)
        return SubmitOutcome.NotSubmittable;

    order.Submit();
    await orderFactory.Save(order, cancellationToken);
    return SubmitOutcome.Submitted;
}
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L60-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generated**: the `OrderCommands.SubmitOrder` delegate type, registered in DI.

`[Execute]` obeys `[Remote]` like every other operation: `_SendNotification` crosses to the server; a bare `[Execute]` (`_ScoreText`) runs on whichever tier resolves it, with that tier's services.

## Key Design Decisions

### [Remote] Marks Client Entry Points
- Only client entry points need `[Remote]`, typically aggregate root operations
- A child's Fetch, Insert and Update are reached from the root's operations, already on the server, so they carry no `[Remote]`; a child's Create is `public` and local
- Once on server, execution stays there

### Service Injection
- **Constructor injection** (`[Service]` on constructor): Available on both client and server
- **Method injection** (`[Service]` on method parameters): resolved on the tier that runs the method, the server for `[Remote]` and `internal` methods (the common case)

### IFactorySaveMeta Controls Routing
`IFactorySaveMeta` declares `IsNew` and `IsDeleted` as getters; `Order` implements them with public setters (R-M):

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L121-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- `IsDeleted` and `IsNew` → nothing to do (never stored)
- `IsDeleted` → Delete
- `IsNew` → Insert
- Otherwise → Update

### Serialization Requirements
- Properties need **public setters**; the generated ordinal converter writes only properties it can set
- Value objects are records, deserialized through their constructors
- Shared and back references between `[Factory]` objects are preserved in both formats; in Ordinal, a back reference needs the type it points at to have only plain setters

## Running the Projects

### Run Tests
```bash
cd src/Design
dotnet test Design.sln
```

### Run the Application
```bash
cd src/Design/Design.Server
dotnet run
```
The server hosts the Blazor WASM client. A single `dotnet run` starts both the API and the client application at `http://localhost:5085`. The RemoteFactory endpoint is at `/api/neatoo`.

## Comment Patterns

The code uses specific comment patterns to document decisions:

| Pattern | Purpose |
|---------|---------|
| `DESIGN DECISION:` | Explains why something is done this way |
| `DID NOT DO THIS:` | Documents rejected alternatives and why |
| `GENERATOR BEHAVIOR:` | Explains what the source generator creates |
| `COMMON MISTAKE:` | Warns about frequent errors |

## Maintenance

When making design changes to RemoteFactory:

1. **Update Design.Domain first** - Add/modify the pattern demonstration
2. **Add tests** - Verify the behavior in Design.Tests
3. **Update comments** - Document the decision and alternatives
4. **Then update main codebase** - Implementation follows design

This ensures design decisions are captured before they're implemented.
