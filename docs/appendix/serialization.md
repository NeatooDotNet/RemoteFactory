# Appendix: Serialization Internals

This appendix explains **why** RemoteFactory manages its own serialization and **how** the type resolution pipeline works under the hood. For user-facing serialization documentation (formats, configuration, debugging), see [Serialization](../serialization.md).

## The Three Problems System.Text.Json Can't Solve

RemoteFactory's custom serialization exists because System.Text.Json makes three assumptions that break in a 3-tier DDD application:

### 1. Interface-Typed Properties Lose Their Concrete Type

When System.Text.Json encounters an interface-typed property, it has no way to round-trip it:

```csharp
public IContactInfo PrimaryContact { get; set; }
```

On **serialization**, STJ serializes only the properties declared on `IContactInfo`, silently dropping any properties that exist on the concrete type (`EmailContact`, `PhoneContact`). On **deserialization**, STJ fails entirely — it can't construct an interface.

STJ's built-in solution (`[JsonPolymorphic]` + `[JsonDerivedType]`) requires you to annotate every interface with every possible concrete type at compile time. In a DDD application where the domain model defines the interfaces and concrete types evolve independently, this is impractical.

RemoteFactory solves this by wrapping interface-typed values with a `$type` discriminator:

```json
{
  "$type": "MyApp.Domain.EmailContact",
  "$value": { "Email": "alice@example.com" }
}
```

The concrete type's `FullName` (namespace-qualified, **not** assembly-qualified) is embedded in the JSON, and the receiving side resolves it back to a `Type` object. This works transparently for interface properties, abstract base classes, and polymorphic collections (`List<IContactInfo>` containing mixed concrete types).

### 2. Deserialized Objects Need DI Services

System.Text.Json creates instances using `Activator.CreateInstance()` — a bare constructor call with no DI awareness. In a DDD application, domain objects have constructor-injected services:

```csharp
[Factory]
public partial class Employee
{
    private readonly IValidationService _validator;

    public Employee([Service] IValidationService validator)
    {
        _validator = validator;
    }
}
```

If STJ deserializes this, `_validator` is null. The domain object is broken.

RemoteFactory's deserialization resolves instances from the DI container (`ServiceProvider.GetRequiredService()`), so constructor-injected services are wired up on deserialization exactly as they would be on creation.

### 3. Shared Object Identity Is Lost

When two properties reference the same object instance (common in aggregate root / child entity relationships), STJ duplicates it — creating two independent copies. RemoteFactory preserves identity using `$id` / `$ref` pointers, maintaining the object graph structure across the wire.

## Type Resolution Pipeline

The critical question in RemoteFactory's serialization is: **given a type name string from the JSON, how do you get back a `Type` object?**

### Why `Type.FullName`, Not `AssemblyQualifiedName`

RemoteFactory uses `Type.FullName` (e.g., `MyApp.Domain.Employee`) — **not** `Type.AssemblyQualifiedName` (e.g., `MyApp.Domain.Employee, MyApp.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`).

This is a deliberate design decision for the 3-tier scenario. In a Blazor WASM application:

- The **client** loads assemblies in the browser's WebAssembly runtime
- The **server** loads the same assemblies in the ASP.NET Core process

Both sides reference the same domain assembly (`MyApp.Domain.dll`), but the runtime environments are completely different. Using assembly-qualified names would couple serialization to a specific assembly version and loading context, breaking the client-server round-trip if anything differs between the two environments.

`Type.FullName` is assembly-agnostic — it identifies a type by namespace and class name only. As long as both sides have a type with the same namespace and class name (which they do, since they reference the same domain project), the lookup succeeds.

### The `ServiceAssemblies` Type Cache

Type resolution is handled by `ServiceAssemblies` (`src/RemoteFactory/Internal/ServiceAssemblies.cs`), which maintains a dictionary mapping `Type.FullName` strings to `Type` objects.

**Registration** — During DI setup, the application passes its domain assemblies to `AddNeatooRemoteFactory()`:

```csharp
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Employee).Assembly);
```

`ServiceAssemblies` iterates every type in those assemblies and indexes them by `FullName`:

```
"MyApp.Domain.Employee"       → typeof(Employee)
"MyApp.Domain.EmailContact"   → typeof(EmailContact)
"MyApp.Domain.PhoneContact"   → typeof(PhoneContact)
...
```

**Resolution** — When deserializing, the type name from the JSON is looked up in this cache. The lookup is a direct dictionary hit — no assembly scanning, no `Type.GetType()` reflection, no runtime assembly loading.

### Where Type Names Are Written and Read

Three serialization paths embed type names in the JSON:

| Path | Writes | Reads | Purpose |
|------|--------|-------|---------|
| `NeatooJsonSerializer.ToRemoteDelegateRequest()` | `delegateType.FullName` into `RemoteRequestDto.DelegateFullName` | `ServiceAssemblies.FindType()` | Identifies which factory delegate to invoke on the server |
| `NeatooJsonSerializer.ToObjectJson()` | `targetType.FullName` into `ObjectJson.TypeFullName` | `ServiceAssemblies.FindType()` | Carries the concrete type of serialized parameters |
| `NeatooInterfaceJsonTypeConverter<T>.Write()` | `value.GetType().FullName` into `$type` JSON property | `ServiceAssemblies.FindType()` | Carries the concrete type of interface-typed properties |

All three use the same resolution mechanism — the `ServiceAssemblies` type cache keyed by `FullName`.

### How Interface Serialization Works

When System.Text.Json encounters an interface-typed property during serialization, RemoteFactory's converter pipeline intercepts it:

1. **`NeatooInterfaceJsonConverterFactory.CanConvert()`** — Checks if the type is an interface (or abstract class) and exists in the registered assemblies. If so, it claims the type.

2. **`NeatooInterfaceJsonTypeConverter<T>.Write()`** — Wraps the value with `$type` (the concrete type's `FullName`) and `$value` (the serialized concrete object). The concrete object is serialized using its actual type, so all properties are included — not just the interface's declared properties.

3. **`NeatooInterfaceJsonTypeConverter<T>.Read()`** — Reads the `$type` string, resolves it through `ServiceAssemblies.FindType()` to get the concrete `Type`, then delegates to `JsonSerializer.Deserialize()` with that concrete type.

This applies equally to single properties and collection elements. A `List<IContactInfo>` serializes as an array where each element is wrapped with its own `$type`/`$value`, allowing mixed concrete types in the same collection.

### The Delegate Type: Routing, Not Serialization

The `DelegateFullName` field in `RemoteRequestDto` serves a different purpose from `$type` — it's a **routing key**, not a serialization discriminator.

When the client calls a `[Remote]` factory method, the generated code sends a request containing the delegate type's `FullName`. The server uses this to look up the corresponding delegate in the DI container:

```csharp
// Server-side: resolve the delegate type, then resolve the delegate from DI
var delegateType = serviceAssemblies.FindType(request.DelegateFullName);
var method = (Delegate)serviceProvider.GetRequiredService(delegateType);
```

The delegate type identifies *which operation to run*. The `ObjectJson` parameters identify *what data to pass*. Both use `FullName` for type identification, but they serve fundamentally different roles.
