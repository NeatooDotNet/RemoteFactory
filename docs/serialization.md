# Serialization

When the server deserializes an incoming domain object, it can't just call `new Employee()` — that would create a bare instance with no constructor-injected services. RemoteFactory's serialization resolves the instance from the DI container, so constructor injection works across the wire. The deserialized object arrives fully wired, just as if the server had created it locally.

This is the fundamental reason RemoteFactory manages its own serialization rather than using System.Text.Json directly. Standard System.Text.Json uses `Activator.CreateInstance()` — a bare constructor call with no DI awareness. RemoteFactory replaces that with `ServiceProvider.GetRequiredService()`, so your domain objects get their constructor-injected services on deserialization just as they would on creation.

Beyond DI resolution, RemoteFactory's custom serialization handles two things System.Text.Json can't:

- **Interface properties** — System.Text.Json can't serialize interface-typed properties. RemoteFactory embeds a `$type` discriminator so the correct concrete type is resolved on deserialization.
- **Shared instance identity** — When the same object is referenced by two properties (e.g., a parent-child bidirectional reference), System.Text.Json duplicates it. RemoteFactory tracks object identity and serializes shared references as `$ref` pointers, preserving the graph structure.

For everything else — primitives, collections, enums, records — RemoteFactory falls back to standard System.Text.Json.

## LazyLoad\<T\> Properties

`LazyLoad<T>` properties on `[Factory]` classes serialize with full support in both formats. The serialized state includes `Value` and `IsLoaded` — the loader delegate is not serialized (delegates reference server-side services and are reconstructed via the constructor-initialization pattern on deserialization).

**Named format:**
```json
{"value": "loaded data", "isLoaded": true}
```
or for an unloaded property:
```json
{"value": null, "isLoaded": false}
```

**Ordinal format:** Each `LazyLoad<T>` property occupies two consecutive array slots — one for the Value and one for `IsLoaded`. For example, a class with `string Name` and `LazyLoad<string> Reviews`:
```
["ProductName", "review text", true]
                 ^-- Value       ^-- IsLoaded
```

An unloaded property serializes as `[null, false]` in the two slots.

The `PropertyNames` and `PropertyTypes` arrays reflect the two-slot encoding: a property named `Reviews` produces entries `"Reviews"` and `"Reviews__IsLoaded"` in `PropertyNames`, and `typeof(string)` and `typeof(bool)` in `PropertyTypes`.

BCL `Lazy<T>` is not supported — use `LazyLoad<T>` instead. See [LazyLoad](lazyload.md) for the full usage guide.

## Serialization Formats

RemoteFactory supports two JSON formats. The choice is straightforward:

| Format | JSON Shape | Tradeoff |
|--------|-----------|----------|
| **Ordinal** (default) | `[true, 42, "John", "Doe"]` | Compact (~40-50% smaller), harder to debug |
| **Named** | `{"Active": true, "Age": 42, ...}` | Human-readable, larger payloads |

Ordinal serializes properties as a JSON array in alphabetical order — no property names, just values. Named is standard JSON with property names.

Configure the format during DI registration. Both client and server must use the same format:

<!-- snippet: serialization-config -->
<a id='snippet-serialization-config'></a>
```cs
// Configure serialization format: Ordinal (compact) or Named (readable).
public static void Configure(IServiceCollection services)
{
    var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
    services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L11-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A practical pattern: use Named in development for readable payloads, Ordinal in production for performance:

<!-- snippet: serialization-debug-named -->
<a id='snippet-serialization-debug-named'></a>
```cs
// Switch to Named format in development for readable JSON debugging.
public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
{
    var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;
    services.AddNeatooAspNetCore(new NeatooSerializationOptions { Format = format }, typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L35-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Ordinal Versioning

With Ordinal format, adding or renaming properties changes the alphabetical order, which shifts array indices. This requires a coordinated client/server rebuild:

<!-- snippet: serialization-ordinal-versioning -->
<a id='snippet-serialization-ordinal-versioning'></a>
```cs
// Properties serialized alphabetically: Active[0], Age[1], Email[2], FirstName[3]...
// Adding "Department" inserts at [2], shifting everything after.
public bool Active { get; set; } = true;
public int Age { get; set; }
public string Email { get; set; } = "";
public string FirstName { get; set; } = "";
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L13-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Named format is more tolerant of property changes since each value is identified by name, not position.

## Interface Properties

System.Text.Json can't serialize a property typed as `IContactInfo` — it doesn't know the concrete type. RemoteFactory embeds a `$type` discriminator:

<!-- snippet: serialization-interface -->
<a id='snippet-serialization-interface'></a>
```cs
// Interface property serializes with $type: "EmailContact" or "PhoneContact"
public IContactInfo? PrimaryContact { get; set; }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L89-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization, so `IContactInfo` resolves to `EmailContact` or `PhoneContact` as appropriate. This also powers polymorphic collections — a `List<EmployeeBase>` can contain mixed concrete types, each tagged with `$type`.

## Reference Preservation

When two properties reference the same object instance — common in parent-child relationships — RemoteFactory preserves that identity instead of duplicating the object:

<!-- snippet: serialization-references -->
<a id='snippet-serialization-references'></a>
```cs
// Bidirectional reference - serializes as {"$ref": "1"} to preserve identity.
public void AddMember(string name) => Members.Add(new TeamMember(name, this));
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L58-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-references' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The first occurrence gets a `$id`, and subsequent references use `$ref` pointers:

```json
{
  "$id": "1",
  "Name": "Team Alpha",
  "Members": [
    {
      "$id": "2",
      "Name": "Alice",
      "Team": { "$ref": "1" }
    }
  ]
}
```

This handles circular references and ensures the deserialized graph has the same object identity as the original.

### Reference Handling by Type Category

RemoteFactory uses a two-path strategy so that reference tracking works for mutable types while records (DDD value objects) serialize cleanly:

| Type Category | Examples | Reference Handling | Rationale |
|---------------|----------|--------------------|-----------|
| **Neatoo types** (custom converters) | Entities, lists with `[Factory]` | `$id`/`$ref` via `NeatooReferenceResolver.Current` | Converter-level, unchanged from v0.22.0 |
| **Mutable reference types** (STJ built-in) | `Dictionary<K,V>`, `List<T>`, plain classes with default constructors | `$id`/`$ref` via `NeatooPreserveReferenceHandler` on `JsonSerializerOptions` | STJ's built-in converters participate in reference tracking through the same resolver |
| **Types with parameterized constructors** | Records, immutable classes | No `$id`/`$ref` -- `RecordBypassConverterFactory` claims them | STJ cannot deserialize reference metadata on parameterized-constructor types; records are value objects where duplication is semantically correct |

All three paths share the same `NeatooReferenceResolver` instance per serialization operation, so cross-type reference identity is maintained. For example, the same `Dictionary` instance referenced by both a Neatoo entity property and a plain class property will be serialized once (with `$id`) and referenced (with `$ref`) in both locations.

A mutable type nested inside a record's constructor parameters is serialized as an independent copy. This is correct DDD behavior -- records are value objects, and their internal state is logically independent even if the same instance was shared at runtime. See [Appendix: Record Reference Handling](appendix/record-reference-handling.md) for the full rationale.

Do not mix Neatoo domain types with plain records in the same return type. A record containing an `IValidateBase` property creates a serialization mismatch -- the record bypasses reference handling entirely (including its subtree), but the embedded Neatoo type's converter expects the resolver to be tracking references. Use either pure Neatoo types or pure records/DTOs.

## Debugging

Enable verbose logging to trace serialization issues:

<!-- snippet: serialization-logging -->
<a id='snippet-serialization-logging'></a>
```cs
// Enable verbose logging for serialization debugging.
public static void ConfigureWithLogging(IServiceCollection services)
{
    services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
    services.AddNeatooAspNetCore(typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L23-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler. Switching to Named format in development (shown above) makes payloads human-readable.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md) — How serialization fits in the remote call lifecycle
- [Factory Modes](factory-modes.md) — When serialization occurs (Remote mode only)
- [Service Injection](service-injection.md) — Constructor injection that survives serialization
