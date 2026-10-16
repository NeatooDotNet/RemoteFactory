# Anti-Patterns

Each mistake is a short block labelled WRONG, in the example domain's names. The fix is a sentence; the compiled right shape is on the page it names.

## 1. A repository as an interface factory

```csharp
// WRONG - the client gets a repository
[Factory]
public interface IOrderRepository
{
    Task<OrderRow?> GetAsync(int id);
}
```

**Fix:** a repository is a `[Service]` parameter on a server operation (`Order`'s `Fetch`, [class-factory.md](class-factory.md)). What the client calls is a domain service named for what it does (`IProductCatalog`, [interface-factory.md](interface-factory.md)).

## 2. `[Remote, Create]` with nothing to do on the server

```csharp
// WRONG - a round trip for new; the service is never used
[Remote, Create]
internal void Create(string customerName, [Service] IOrderDbContext db)
{
    CustomerName = customerName;
}
```

**Fix:** a root `[Create]` is local and `public` (`Order.Create`, [class-factory.md](class-factory.md)). It is `[Remote, Create]` only when building the object needs something only the server has.

## 3. A `[Create]` that looks for an existing row

```csharp
// WRONG - Create claims "new"; this one may load
[Remote, Create]
internal async Task Create(string customerName, [Service] IOrderDbContext db)
{
    var draft = await db.FindDraftAsync(customerName);
    ...
}
```

**Fix:** load-or-create is an `[Execute]` that calls Fetch, then Create (`OpenDraft`, [class-factory.md](class-factory.md#class-level-execute)). A loaded object built with `Create` claims to be new, and its next save inserts it again.

## 4. A parent that writes its children's rows

```csharp
// WRONG - the order maps its lines
foreach (var line in Lines)
    row.Lines.Add(new OrderLineRow { ProductName = line.ProductName, Quantity = line.Quantity });
```

**Fix:** the root hands the row's child collection to the list factory's `Save`; the list hands each child its row, and the child maps itself ([class-factory.md](class-factory.md#the-list-cascade)).

## 5. `[Remote]` on a child's operation

```csharp
// WRONG - the call is already on the server
[Remote, Insert]
internal void Insert(OrderLineRow row) { ... }
```

**Fix:** `internal`, no `[Remote]` (`OrderLine`, [service-injection.md](service-injection.md#child-entities-already-on-the-server)). It costs no extra round trip - once on the server, `[Remote]` does nothing different - but it promotes the operation onto the client's factory interface and gives the client a way in around the root. A child's `Create` is the one `public` child operation.

## 6. `[Remote]` on a `public` method

```csharp
// WRONG - NF0105
[Remote, Fetch]
public async Task<bool> Fetch(int id, [Service] IOrderDbContext db) { ... }
```

**Fix:** `[Remote] internal`. The generator promotes it to `public` on the factory interface, and the body can be trimmed from the client. A static method is exempt; `[Remote]` alone decides where it runs.

## 7. Operation attributes on an interface factory's methods

```csharp
// WRONG - NF0106
[Factory]
public interface IProductCatalog
{
    [Fetch]
    Task<ProductListing?> FindBySkuAsync(string sku);
}
```

**Fix:** no attributes. `[Factory]` on the interface makes every method a remote entry point.

## 8. `[Factory]` on the interface factory's implementation

```csharp
// WRONG - CS0101/CS0111: both generate ProductCatalogFactory
[Factory]
public class ProductCatalog : IProductCatalog { ... }
```

**Fix:** only the interface carries `[Factory]`; the implementation is a plain class registered on the server.

## 9. A `public` static command method

```csharp
// WRONG - conflicts with the generated delegate
[Remote, Execute]
public static Task<SubmitOutcome> SubmitOrder(int orderId, [Service] IOrderFactory orderFactory) { ... }
```

**Fix:** `private static _SubmitOrder`. The generator makes the public delegate `OrderCommands.SubmitOrder` from it ([static-factory.md](static-factory.md)).

## 10. A result invented for a command that has none

```csharp
// WRONG - the bool is always true
[Remote, Execute]
private static async Task<bool> _CancelOrder(int orderId, [Service] IOrderFactory orderFactory)
{
    ...
    return true;
}
```

**Fix:** return `Task`. The await confirms the server ran it and rethrows a server exception. `void` is NF0102.

## 11. A class-level `[Execute]` that returns another type

```csharp
// WRONG - fails in generated code, no diagnostic yet
[Remote, Execute]
public static Task<Invoice> GenerateInvoice(int orderId, [Service] IInvoiceService invoices) { ... }
```

**Fix:** a class-level `[Execute]` returns `Task<T>` of its own class's service type (`Task<IOrder>`); a command that returns something else is a static factory command. An analyzer is pending.

## 12. A private setter

```csharp
// WRONG - silently left out of serialization
public int Id { get; private set; }
```

**Fix:** a public setter on every property that must cross the wire. A `private` or `protected` setter is dropped with no diagnostic; an analyzer is pending.

## 13. A missing `partial`

```csharp
// WRONG - the class factory falls back to named serialization
[Factory]
internal class Order : IOrder { ... }
```

**Fix:** `partial`. A class factory without it still gets a factory but no ordinal serializer, with no diagnostic; a static `[Factory]` class without it is NF0101.

## 14. A method-injected service kept in a field

```csharp
// WRONG - the field is null once the object reaches the client
private IOrderDbContext _db = null!;
```

**Fix:** use a `[Service]` parameter inside its method. A service the object needs on both tiers is a constructor parameter ([service-injection.md](service-injection.md)).

## 15. Default values on factory method parameters

```csharp
// WRONG - the default is dropped from the generated factory
[Create]
public void Create(string customerName = "") { ... }
```

**Fix:** no defaults. The generated method requires every argument; a caller who omits one gets a compile error.

## 16. Avoiding `internal` services for fear of CS0051

CS0051 occurs only when an `internal` type appears in a `public` method of a `public` class. On an `internal` class, C# caps its `public` methods at `internal`, so a child's `internal` factory interface can be a `[Service]` parameter there. Most aggregate classes are `internal`, so the fear rarely applies.

## Summary

| Mistake | What goes wrong | Fix |
|---|---|---|
| A repository as an interface factory | The client gets the persistence layer | `[Service]` on a server operation; a domain service for the client |
| `[Remote, Create]` with nothing for the server | A round trip for `new` | A local `public` `[Create]` |
| A `[Create]` that loads | A loaded object claims to be new | An `[Execute]` that calls Fetch, then Create |
| A parent writing its children's rows | The child's own operations are bypassed | The list factory's `Save` |
| `[Remote]` on a child operation | A client way in around the root | `internal`, no `[Remote]` |
| `[Remote] public` | NF0105 | `[Remote] internal` |
| Attributes on interface methods | NF0106 | None |
| `[Factory]` on the implementation | CS0101/CS0111 | Only on the interface |
| A `public` static command | Conflicts with the delegate | `private static _Name` |
| An always-`true` result | Tells the caller nothing | `Task` |
| A class `[Execute]` returning another type | Fails in generated code | A static command |
| A `private` or `protected` setter | Dropped from serialization | A public setter |
| No `partial` | Named serialization, or NF0101 | `partial` |
| A method service in a field | Null on the client | Use it in the method, or inject in the constructor |
| Parameter defaults | Dropped | Pass every argument |
| CS0051 fear | `internal` avoided for nothing | `internal` classes take `internal` services |
