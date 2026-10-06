# IL Trimming — Keeping Server Logic Off the Client

## Recommended Pattern

Use `internal` classes with `[Remote] internal` entry points. When configured, RemoteFactory optimizes the client binary with IL trimming — removing server-only business logic and its dependencies, reducing client library size.

| Element | Recommended Visibility | Why |
|---|---|---|
| Domain class | `internal` with `public` interface | Hides implementation from client assemblies |
| Aggregate root factory methods | `internal` with `[Remote]` | Client entry points — `[Remote]` promotes to `public` on factory interface; method bodies trimmed on client |
| Child entity factory methods | `internal` (no `[Remote]`) | Server-only — removed from client |
| Methods that run locally (e.g. `CanCreate`) | `public` (no `[Remote]`) | Needed on both client and server |

`[Remote]` requires `internal` — `[Remote] public` is a compile-time error (NF0105). With this pattern and trimming configured, the deployed client contains only remote stubs and locally-needed methods — no server-only logic, no server-only dependencies, no IP exposure.

## Prerequisite: Direct `Neatoo.RemoteFactory` Reference in Every Project with Factory Types

Source generators only run where the generator package is referenced **directly**. Every project that declares `[Factory]`, `[FactoryEventHandler<T>]`, `[Execute]`, `[Save]`, or `[AuthorizeFactory<T>]` must have its own `PackageReference`:

```xml
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
```

A transitive reference (through a `ProjectReference` to the domain project, or a `PackageReference` with `PrivateAssets="all"`) silently skips generation for that project. No `FactoryServiceRegistrar` is emitted and no `DtoConstructorRegistry.Register` calls fire at startup.

For factory events on the **client**, the consumer's `IFactoryEventRelay` implementation does not require generator output — only the `FactoryEventBase` descendants must be loaded so the runtime `FactoryEventTypeRegistry` can discover them. Add a direct `Neatoo.RemoteFactory` `PackageReference` to any project declaring `[Factory]`, `[FactoryEventHandler<T>]`, `[Execute]`, `[Save]`, or `[AuthorizeFactory<T>]`.

## Setup

Four configuration aspects across your projects:

### Domain model project

Mark the assembly as trimmable in the domain model `.csproj`:

```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

Without this, the trimmer only trims framework assemblies and the domain model ships intact to the client.

### Client project (Blazor WASM)

Add the feature switch to the client `.csproj`:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
</ItemGroup>
```

Blazor WASM projects already publish with trimming enabled (`PublishTrimmed=true` is the SDK default). The `RuntimeHostConfigurationOption` is all that's needed for the feature switch.

### Isolate server-only dependencies

In the **domain model** `.csproj`, mark server-only references with `PrivateAssets="all"` to prevent them from flowing transitively to the client:

```xml
<!-- Server-only packages -->
<PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />

<!-- Server-only project references -->
<ProjectReference Include="..\Person.Ef\Person.Ef.csproj" PrivateAssets="all" />
```

Without `PrivateAssets="all"`, these packages flow to the client as transitive dependencies. The trimmer then has to deal with assemblies it may not trim cleanly, causing warnings or runtime failures.

### Mark residual assemblies as trimmable

Some assemblies may still reach the client output through indirect dependency paths. Add `<TrimmableAssembly>` entries in the **client** `.csproj` for assemblies that lack `IsTrimmable` but should be trimmed:

```xml
<ItemGroup>
  <TrimmableAssembly Include="Person.Ef" />
  <TrimmableAssembly Include="Neatoo.Generator" />
</ItemGroup>
```

### Summary

| Setting | Where | Purpose |
|---------|-------|---------|
| `IsTrimmable=true` | Domain `.csproj` | Opts the assembly into trimming |
| `RuntimeHostConfigurationOption` | Client `.csproj` | Tells the trimmer that `IsServerRuntime` is `false` |
| `PrivateAssets="all"` | Domain `.csproj` | Prevents server-only dependencies from flowing to client |
| `TrimmableAssembly` | Client `.csproj` | Marks residual assemblies as safe to trim |

The `Trim="true"` attribute on the `RuntimeHostConfigurationOption` is critical — without it, the switch is a runtime value only and the trimmer cannot eliminate server code.

### Complete example

**Domain Model (`Person.DomainModel.csproj`):**
```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\Person.Ef\Person.Ef.csproj" PrivateAssets="all" />
</ItemGroup>
```

**Client (`Person.Client.csproj`):**
```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
  <TrimmableAssembly Include="Person.Ef" />
  <TrimmableAssembly Include="Neatoo.Generator" />
</ItemGroup>
```

**Server (`Person.Server.csproj`):**
```xml
<!-- No trimming configuration needed — server runs everything -->
```

### Requirements

- **.NET 9 or later** — `[FeatureSwitchDefinition]` was introduced in .NET 9
- **`dotnet publish`** — Trimming runs during publish, not during `dotnet build` or `dotnet run`

## What Gets Trimmed, By Factory Shape

The guarantee is not uniform across factory shapes. What follows is measured against a published trimmed assembly, not inferred from the mechanism.

| Shape | `[Remote]`/handler bodies removed? |
|---|---|
| Static factory, `[Remote, Execute]` | Yes, from v1.7.0 |
| Static factory, bare `[Execute]` | **No, by design** — no `[Remote]`, no guard; the body ships to the client and runs there (v1.9.0) |
| `[FactoryEventHandler<T>]` | Yes, from v1.7.0 |
| Class factory | Yes, from v1.7.0 — synchronous operations were always removed; `async` ones needed the same release |
| Class-level `[Remote, Execute]` | Yes, from v1.7.0 — emitted `async` always, so it needed the same fix |
| Class-level bare `[Execute]` | `public static`: **No, by design** — runs where the factory resolves. `internal static`: Yes — server-only, guarded like any `internal` method (v1.9.0) |
| Interface factory | **Not established.** No leak has been observed, but the leg reaches its implementation through interfaces, so a client-side test reads "absent" whether or not the body survives. Treat it as unverified rather than proven. |

### Why the fix was needed

The generator emits `[assembly: NeatooFactoryRegistrar(typeof(X))]` so factory registration survives trimming. That attribute carries `[DynamicallyAccessedMembers]`, which preserves **every method on the type it names, method bodies included**.

Two distinct problems followed from that, both fixed in v1.7.0:

1. **Static factories and `[FactoryEventHandler<T>]` classes had no generated type to name.** The generator re-opens *your* partial class to host the registrar, so the attribute named your class and preservation covered your `[Remote]` bodies. They shipped to the browser.
2. **Class factories named `{X}Factory`, which was generated but not small.** It hosts every `Local*` method, so naming it preserved all of them, bodies included. Naming a generated type was never the point — naming a type with exactly *one* method is.

Both are fixed by emitting a single-method forwarding holder for the attribute to point at.

`async` class-factory operations needed a second fix on top. Inside an `async` method the compiler lowers the `IsServerRuntime` guard into the state machine's `MoveNext`, within the builder's own protected region; the trimmer folds the switch there but does not remove the unreachable remainder. So guarded `async` operations are now emitted as a non-async wrapper carrying the guard, forwarding to a private async core.

No action needed on your side; it is automatic. If you are on an earlier version and ship `[Execute]` commands, event handlers, or `async` factory operations to a Blazor WASM client, upgrade — those bodies are in your published output today.

One behaviour change to be aware of: the server-only guard now throws **synchronously** from the factory entry point rather than surfacing as a faulted `Task`. Code that awaited the call and caught the exception still works; code that called without awaiting and inspected the returned `Task` will now see the throw at the call site.

### `[Execute]` obeys `[Remote]`

`[Remote]` is what makes an `[Execute]` body trimmable, by deciding whether a guard is emitted at all. With `[Remote]` the local path is guarded by `IsServerRuntime` and the trimmer removes the body. Without it there is no guard and no remote delegate — the body ships to the client and runs there, with the client's services, which is the point of a bare `[Execute]`. On a class factory, `internal static` without `[Remote]` is guarded too (server-only), while `public static` runs wherever the factory resolves. Static methods are exempt from the NF0105 `[Remote] public` check because `[Remote]` alone drives their guard.

Both halves are measured in the trimming gate rather than inferred: a `[Remote, Execute]` body absent from a publish-trimmed client, a bare body present and running there, on both shapes (v1.9.0). Do not add `[Remote]` to an `[Execute]` "for intent" — it changes where the method runs.

## Verifying Results

After publishing, confirm server-only types were removed:

```bash
# Publish with trimming
dotnet publish -c Release

# Search for server-only type names (should return no matches)
grep -aob "YourRepositoryClassName" bin/Release/net9.0/publish/YourApp.dll
```

**Searching for a string literal takes an extra step.** Type and method *names* are UTF-8 in assembly metadata, so `grep -a` finds them. String *literals* from method bodies are UTF-16, so `grep -a` cannot match them — it reports "absent" for text that is sitting in the file. Strip the nulls first:

```bash
tr -d '\000' < bin/Release/net9.0/publish/YourApp.dll | grep -c "SELECT * FROM"
```

Before trusting a clean result, run the same check against the **non-published** build output, where the server-only code definitely still exists. If it reports "absent" there too, your check is broken rather than your code being clean.

If server-only type names still appear:
1. Confirm `TrimMode` is `full` (not `partial` or omitted)
2. Confirm `RuntimeHostConfigurationOption` has `Trim="true"`
3. Inspect the `publish/` output, not the `build/` output

## Authorization Types

The generator automatically emits explicit DI registrations for `[AuthorizeFactory<T>]` types in `FactoryServiceRegistrar`. This creates static references that survive trimming — no additional configuration needed for auth classes.

The concrete type is resolved at compile time using the naming convention (`IPersonModelAuth` -> `PersonModelAuth`).

### RegisterMatchingName

`RegisterMatchingName` uses reflection (`assembly.GetTypes()`) at runtime. The trimmer cannot see these references and may trim types only registered through convention. Factory auth types are handled automatically by the generator. For other convention-registered services, either register them explicitly or use `[DynamicDependency]` to preserve them.

## DTO and Event Record Preservation

When a domain assembly is marked `IsTrimmable=true`, the IL trimmer strips constructor and property metadata from types that are not directly referenced in compiled code. This breaks `System.Text.Json` deserialization because `DefaultJsonTypeInfoResolver` discovers constructors and properties through reflection — reflection that fails once the metadata has been trimmed. The generator preserves the DTOs it can reach from three entry points:

1. **Factory method signatures** — return types and non-service parameters, e.g., the `EmployeeDto` from `Task<EmployeeDto>` on an interface factory method.
2. **`[Factory]` entity members** — DTOs and records carried on an aggregate that never appear in a signature themselves.
3. **Event records** — every concrete `FactoryEventBase` descendant, whether server-raised and relayed to the client (`RemoteResponseDto.RelayedEvents`) or client-raised and sent to the server.

Two primitives do the work:

| Primitive | Emitted when | Behavior |
|-----------|--------------|----------|
| `DtoConstructorRegistry.Register<T>(() => new T())` | `T` has a public parameterless constructor | `[DynamicallyAccessedMembers(All)]` preserves every member; `NeatooJsonTypeInfoResolver.CreateObject` uses the lambda instead of `Activator.CreateInstance` |
| `DtoConstructorRegistry.PreserveType<T>()` | `T` has only parameterized constructors (typical of records) | `[DynamicallyAccessedMembers(All)]` preserves every member; no constructor factory is recorded — STJ flows through the parameterized-ctor pipeline (`RecordBypassConverterFactory`) |

### What the DTO walk reaches

From each entry point the generator walks public instance members — properties with a getter **and public fields**, because `NeatooJsonSerializer` runs with `IncludeFields = true` — recursively, with cycle detection, and emits a `Register` or `PreserveType` call for each DTO it finds. Every member type is unwrapped to what the serializer constructs:

- `Task<T>` on a return type, and nullable `T?` at any level.
- Arrays and every generic collection, recursively — `List<List<T>>` and `Dictionary<string, List<T>>` both reach `T`.
- Both the key and the value of every generic dictionary — `Dictionary`, `IDictionary`, `IReadOnlyDictionary`, and the sorted, concurrent, immutable, and custom generic dictionaries.
- A generic collection of your own, such as `PagedList<T> : List<T>`, **and** its element. The serializer constructs both.

An entity's public fields are walked as well as its properties. In `Named` format, and always for an entity whose constructor takes required parameters, an entity goes through the reflection serializer and its public fields cross the wire; only the default ordinal path carries properties alone. Dictionary entries and public fields are reached from v1.10.1; earlier versions missed them.

### What the DTO walk does not reach

A DTO reachable **only** through one of these shapes needs explicit preservation:

| Shape | Why the walk stops |
|-------|--------------------|
| A member typed as an interface or abstract class, when the concrete type is a plain DTO rather than a `[Factory]` type | The `$type` discriminator picks the concrete type at runtime |
| A tuple element, such as `(LocationDto Location, int Rank)` | Tuples are `System` types |
| A non-generic collection subclass, such as `class Locations : List<LocationDto>` | The subclass is preserved; its element type is not reached |
| A value typed `object` | There is no static type to follow |
| A DTO that reaches the client only through your own HTTP or JSON code | It never flows through a factory, an entity, or an event |

Preserve such a type in the client's `LinkerConfig.xml` with `<type fullname="YourApp.Domain.LocationDto" preserve="all" />`, or in client DI setup with the same call the generator would have emitted:

```csharp
using Neatoo.RemoteFactory.Internal;

DtoConstructorRegistry.Register<LocationDto>(() => new LocationDto()); // public parameterless constructor
DtoConstructorRegistry.PreserveType<PriceBreakdown>();               // positional record
```

**Upgrading across v1.7.0.** Before v1.7.0, async `[Remote]` bodies shipped to trimmed clients, so a `new T()` inside one rooted `T`'s constructor by accident. v1.7.0 removed those bodies, and the accidental root went with them. A DTO constructed only inside an async `[Remote]` body, and not reached by the walk, can work on v1.6.x and fail after upgrading, reported as `DeserializeNoConstructor` in Blazor WebAssembly. Check such types against the table above before publishing an upgraded client.

### Factory event preservation

Factory event records inherit `FactoryEventBase`. Two mechanisms make them work under trimming:

- `[FactoryEvent]` on the base (inherited at runtime) — used by the runtime `FactoryEventTypeRegistry` to discover descendants via attribute scan
- The source generator discovers every concrete, accessible `FactoryEventBase` descendant declared in a compilation and emits a per-assembly event-preservation registrar that preserves the event's constructors/properties and its nested property graph. (The `[DynamicallyAccessedMembers]` annotation on the base does NOT do this — DAM does not flow to derived types under ILLink.)

Net effect: if a record inherits `FactoryEventBase`, its constructors and properties survive `PublishTrimmed=true` automatically. No per-event annotation, no `[FactoryEventHandler<T>]` declaration, and no manual `DtoConstructorRegistry` call is required for the event type itself.

```csharp
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
// Constructors and properties preserved by the generated event-preservation registrar.
```

`IFactoryEvents.Raise<T>` and `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` carry `[DynamicallyAccessedMembers(All)]` on their generic parameter as belt-and-suspenders coverage for concrete call-sites.

### Nested DTO types reachable through event properties

Automatically preserved. The event-preservation registrar walks each event's members with the same DTO walk used for signatures and entities, so a nested record or DTO needs no action:

```csharp
public record PriceBreakdown(decimal Base, decimal Tax);   // preserved through the Breakdown property
public record OrderPriced(int OrderId, PriceBreakdown Breakdown) : FactoryEventBase;
```

The only exceptions are the shapes listed in [What the DTO walk does not reach](#what-the-dto-walk-does-not-reach). (An earlier version of this section said nested event types needed manual preservation. That was true before the generator began walking event graphs, and is no longer.)

### User code that forwards `Raise<T>` through a generic wrapper

If your code re-exposes `Raise` through a generic wrapper, the compiler will flag the wrapper with `IL2091` because the wrapper's `T` does not carry `[DynamicallyAccessedMembers(All)]`:

```csharp
// Produces IL2091 under trimming — the wrapper's T is not annotated
public Task RelayAnyEvent<T>(T evt) where T : FactoryEventBase =>
    _factoryEvents.Raise(evt);
```

Resolve by matching the annotation on your own parameter, or by closing the generic at the wrapper:

```csharp
// Option 1 — propagate the annotation
public Task RelayAnyEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T evt)
    where T : FactoryEventBase =>
    _factoryEvents.Raise(evt);

// Option 2 — close the generic at the boundary
public Task RelayCheckoutCompleted(OrderCheckoutCompleted evt) =>
    _factoryEvents.Raise(evt);
```

Direct calls with a concrete type (`_factoryEvents.Raise(new OrderCheckoutCompleted(...))`) are unaffected.

### What you need to know

If you return a plain DTO from a factory method, carry a DTO as a `[Factory]` entity property, or declare a `FactoryEventBase` descendant in a project with a direct `Neatoo.RemoteFactory` `PackageReference`, the type and its constructors and properties are automatically trimming-safe. Nested types reachable from any of these are walked and preserved automatically too, except through the shapes in [What the DTO walk does not reach](#what-the-dto-walk-does-not-reach). One more boundary: private/protected/file-scoped nested event records cannot be preserved (the generated registrar cannot reference them) — declare wire-crossing events as top-level or internal/public nested types.

## IFactorySaveMeta Preservation

Entities implementing `IFactorySaveMeta` must round-trip `IsNew` and `IsDeleted` across the client/server boundary. Save routing happens server-side, so if these properties drop out of the outbound JSON payload, every Save routes to Insert (the server sees the property-initializer default `IsNew = true`) and Delete silently no-ops.

**Public setters just work:**

```csharp
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```

**Private setters need two annotations under trim:**

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

[Factory]
internal class Employee : IEmployee
{
    [DynamicDependency(nameof(IsNew))]
    [DynamicDependency(nameof(IsDeleted))]
    [Create]
    public Employee() { }

    [JsonInclude]
    public bool IsNew { get; private set; } = true;

    [JsonInclude]
    public bool IsDeleted { get; private set; }

    public void MarkDeleted() => IsDeleted = true;
}
```

Why both annotations:

- `[JsonInclude]` tells `System.Text.Json` to use the non-public setter (needed on the deserializing side).
- `[DynamicDependency]` on the `[Create]` constructor prevents the IL trimmer's **visibility analysis** from narrowing the property getter from `public` to `private`. Without it, the trimmer sees no concrete-type callsite reading the getter (all reads go through `IFactorySaveMeta` interface dispatch) and silently downgrades visibility. STJ then skips the property outbound. `[DynamicallyAccessedMembers]` on the class preserves reflection metadata but does NOT prevent visibility narrowing.

**Verify with ilspycmd:**

```bash
ilspycmd <Client>/obj/Release/net10.0/linked/<Domain>.dll -t Full.Name.Employee | grep -B1 "IsNew\|IsDeleted"
```

Expect `public bool IsDeleted` / `public bool IsNew`. If you see `private`, the trimmer narrowed them — `[DynamicDependency]` is missing or the name didn't resolve.
