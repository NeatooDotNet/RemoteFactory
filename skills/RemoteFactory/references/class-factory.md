# Class Factory Pattern

Use for domain entities and aggregate roots that have lifecycle operations (Create, Fetch, Save).

## Complete Example

<!-- snippet: order-class-declaration -->
<a id='snippet-order-class-declaration'></a>
```cs
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L80-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-class-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: readme-domain-model -->
<a id='snippet-readme-domain-model'></a>
```cs
/// <summary>
/// Creates a new Order. Local: runs on the tier that calls it.
/// </summary>
[Create]
public void Create(
    string customerName,
    [Service] IOrderLineListFactory lineListFactory)
{
    CustomerName = customerName;
    Lines = lineListFactory.Create();
    Status = OrderStatus.Draft;
    IsNew = true;
}

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

/// <summary>
/// Inserts a new Order and its lines.
/// </summary>
[Remote, Insert]
internal async Task Insert(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = new OrderRow();
    MapTo(row);
    db.Add(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
    Id = row.Id;
}

/// <summary>
/// Updates an existing Order and its lines.
/// </summary>
[Remote, Update]
internal async Task Update(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    MapTo(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
}

/// <summary>
/// Deletes an Order. Its line rows go with its row.
/// </summary>
[Remote, Delete]
internal async Task Delete([Service] IOrderDbContext db)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    db.Remove(row);
    await db.SaveChangesAsync();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L234-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**: `IOrderFactory` with `Create()`, `Fetch()` and `Save()` methods.

---

## IFactorySaveMeta for Save Routing

Implement `IFactorySaveMeta` to enable automatic Save() routing:

```csharp
public partial class Employee : IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }
}
```

The factory's `Save()` method examines these properties:

| IsNew | IsDeleted | Operation Called |
|-------|-----------|------------------|
| true | false | Insert |
| false | false | Update |
| false | true | Delete |
| true | true | No operation |

### Serialization Gotcha: Private Setters Need `[JsonInclude]`

Save routing happens server-side, so `IsNew` and `IsDeleted` must round-trip through JSON when the client sends the entity to the server. Public setters work by default. If you prefer private setters to prevent external callers from flipping lifecycle state:

```csharp
using System.Text.Json.Serialization;

[JsonInclude]
public bool IsNew { get; private set; } = true;

[JsonInclude]
public bool IsDeleted { get; private set; }

public void MarkDeleted() => IsDeleted = true;
```

Without `[JsonInclude]`, `System.Text.Json` skips the private setter on deserialization — the server deserializes `IsNew = true` from its default initializer and **every Save routes to Insert**, and Delete silently no-ops. This is one of those rules that every RemoteFactory user with `IFactorySaveMeta` + private setters will eventually trip over. Under `PublishTrimmed=true` you also need `[DynamicDependency]` on the `[Create]` constructor — see [trimming.md](trimming.md#ifactorysavemeta-preservation).

---

## Lifecycle Hooks

Implement lifecycle interfaces for cross-cutting concerns:

<!-- snippet: operations-lifecycle-onstart -->
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
public Task FactoryStartAsync(FactoryOperation factoryOperation)
{
    if (factoryOperation is FactoryOperation.Insert or FactoryOperation.Update)
    {
        LastSavedAt = DateTimeOffset.UtcNow;
    }
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L110-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
{
    // Example: Reset IsNew after successful Insert
    if (factoryOperation == FactoryOperation.Insert)
    {
        IsNew = false;
    }
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L185-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available hooks:**
- `IFactoryOnStart` / `IFactoryOnStartAsync` - Before any operation
- `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - After successful operation
- `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - On operation failure

---

## Execute Methods on Class Factories

Use `[Execute]` on `static` methods within a class factory to co-locate orchestration logic with the aggregate it operates on. `[Execute]` obeys `[Remote]`: with it the client crosses to the server; without it the method runs on whichever tier resolves the factory, with that tier's services. Use `public static` for a method that runs where the factory resolves, and `internal static` without `[Remote]` for a server-only one — guarded like any `internal` method, and carried on the factory interface with the `internal` modifier.

### When to Use

- The operation creates or returns an instance of the containing type
- The logic is tightly coupled to the aggregate (calls its factory methods, uses internal helpers)
- You want the operation on the same factory interface as Create/Fetch/Save

If the operation returns a different type, use a static factory `[Execute]` instead (see `references/static-factory.md`).

### Method Signature

<!-- snippet: operations-class-execute -->
<a id='snippet-operations-class-execute'></a>
```cs
[Remote, Execute]
public static async Task<IOrder> OpenDraft(
    string customerName,
    [Service] IOrderFactory factory)
{
    return await factory.FetchDraft(customerName)
        ?? factory.Create(customerName);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L362-L371' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-class-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key differences from static factory `[Execute]`:
- Method is **`static`** — `public static` to run where the factory resolves, `internal static` for server-only (no underscore prefix, no `private`)
- Must return the **containing type** (or its matching interface)
- Generates a **factory interface method**, not a delegate type

### Generated Code

When the class implements a matching `I{ClassName}` interface, all generated factory methods return the interface type. Classes without a matching interface return the concrete type instead.

```csharp
public interface ISkillConsultationFactory
{
    Task<ISkillConsultation> CreateAcute(long patientId, CancellationToken ct = default);
    Task<ISkillConsultation?> FetchActive(long patientId, CancellationToken ct = default);
    Task<ISkillConsultation> StartForPatient(long patientId, CancellationToken ct = default);
}
```

### Caller Usage

```csharp
// Inject the factory -- same as any other factory method
var factory = serviceProvider.GetRequiredService<IConsultationFactory>();
var consultation = await factory.StartForPatient(patientId);
```

---

## Internal Visibility for Child Entities

Child entity factory methods should be `internal` to signal that they are server-only. This enables IL trimming of the method bodies and affects the generated factory interface.

### Factory Interface Visibility Rules

The generator determines factory interface visibility from **method** accessibility, not class accessibility:

| Method Visibility in Class | Generated Factory Interface | Which Methods on Interface? | Guards? |
|---|---|---|---|
| All methods `internal` (no `[Remote]`) | `internal` | All (internal interface) | All get `IsServerRuntime` guard |
| All methods `public` or `[Remote] internal` | `public` | All | `[Remote]`: guarded; `public`: no guard |
| Mix of `public`/`[Remote] internal` and plain `internal` | `public` | **All methods** — plain `internal` methods get `internal` modifier; `[Remote] internal` promoted to `public` | `internal` and `[Remote]`: guarded; `public`: no guard |

**[Remote] promotes to public.** `[Remote] internal` methods are treated as `public` for interface visibility — they appear as `public` members on the factory interface because clients call them. Plain `internal` methods (without `[Remote]`) appear with the `internal` access modifier. All-internal interfaces (where every method is internal and none have `[Remote]`) use an `internal` interface, making them invisible to the client container.

### Class Accessibility vs Method Accessibility

The generator checks the **method's** `DeclaredAccessibility`, not the class's effective accessibility. A `public` method on an `internal` class is treated as `public` by the generator — no `IsServerRuntime` guard, included in the public factory interface.

This means class accessibility and method accessibility serve **different purposes** in RemoteFactory:

| Accessibility | What It Controls |
|---|---|
| **Class** `internal` | Hides the concrete type from external assemblies. Use with a `public` interface (`IOrder`) so the factory returns the interface type. |
| **Method** `internal` (no `[Remote]`) | Tells the generator: emit `IsServerRuntime` guard, add `internal` modifier on public factory interface, make trimmable. |
| **Method** `internal` with `[Remote]` | Tells the generator: emit `IsServerRuntime` guard, promote to `public` on factory interface, make trimmable. Client calls via factory. |
| **Method** `public` | Tells the generator: include on factory interface, no guard. Cannot have `[Remote]` (NF0105 error). |

A `public` method on an `internal` class does **not** behave like an `internal` method for code generation. The method is still unguarded, untrimmable, and included on the public factory interface — even though C# caps its effective accessibility to `internal`.

### Example: All Internal Methods

```csharp
// Internal class with public interface — recommended pattern for child entities
public interface IOrderLine
{
    int Id { get; set; }
    string ProductName { get; set; }
}

[Factory]
internal partial class OrderLine : IOrderLine
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";

    [Create]
    internal void Create(string productName, decimal price, int qty) { }

    [Fetch]
    internal void Fetch(int id, string productName, decimal price, int qty) { }
}
// Generated: internal interface IOrderLineFactory — client can't inject or see it
// Both methods get IsServerRuntime guards — trimmable on client
```

### Example: Mixed Visibility (Entity Duality)

```csharp
[Factory]
internal partial class Department : IDepartment
{
    // [Remote] internal: aggregate root entry point — promoted to public on factory interface, gets guard
    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IDeptRepo repo, CancellationToken ct) { ... }

    // Plain internal: child context — on public interface with `internal` modifier, gets IsServerRuntime guard
    [Fetch]
    internal void FetchAsChild(Guid id, string name) { ... }
}
// Generated:
// public interface IDepartmentFactory
// {
//     Task<IDepartment?> Fetch(...);            // public — promoted by [Remote]
//     internal IDepartment FetchAsChild(...);    // internal modifier — same-assembly only
// }
```

For aggregate roots, use `[Remote] internal` for operations that cross to the server (the generator promotes these to `public` on the factory interface). For child entities called only from server-side aggregate operations, use `internal` without `[Remote]`.

**CS0051 constraint:** CS0051 occurs when an `internal` type appears in a `public` method on a **`public`** class. When the consuming class is also `internal`, there is no CS0051 — C# caps the effective accessibility of `public` methods to the containing type's level, so `internal` service types are allowed. In practice, this means `internal` factory interfaces (from child entities with all-internal methods) can freely be injected as `[Service]` parameters into other `internal` classes. The constraint only applies when injecting into a **`public`** class. If you hit CS0051, either make the consuming class `internal` or keep the child entity methods `public` — the feature is opt-in and all `public` methods work identically to before.

---

## Key Rules

1. **Classes must be `partial`** - Generator adds serialization code
2. **Properties need public setters** - Required for deserialization
3. **[Remote] requires `internal`** - on instance methods, `[Remote] public` is a compile-time error (NF0105); `[Remote] internal` is promoted to `public` on the factory interface. Static methods are exempt: `[Remote]` alone decides their placement
4. **Business logic belongs in the entity** - Not in the factory
5. **Execute methods must be `static`** - `public static` to run where the factory resolves, `internal static` for server-only; no underscore prefix (unlike static factory Execute). `[Remote]` decides whether the client crosses
6. **Execute must return the containing type** (or concrete type if no matching interface) - Keeps the factory interface cohesive
7. **Use `internal` for child entity factory methods** - Server-only, trimmable, invisible to client

---

## [SuppressFactory]

The generator handles only types that carry `[Factory]` on their own declaration: a class derived from a `[Factory]` class gets no factory unless it carries `[Factory]` too. `[SuppressFactory]` turns generation off for a class or record that also carries `[Factory]`; on an interface, or on a type without `[Factory]`, it has no effect. `[Factory]` on an `abstract` or generic class is ignored.
