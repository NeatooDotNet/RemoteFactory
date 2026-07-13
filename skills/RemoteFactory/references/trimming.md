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

## Verifying Results

After publishing, confirm server-only types were removed:

```bash
# Publish with trimming
dotnet publish -c Release

# Search for server-only type names (should return no matches)
grep -aob "YourRepositoryClassName" bin/Release/net9.0/publish/YourApp.dll
```

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

When a domain assembly is marked `IsTrimmable=true`, the IL trimmer strips constructor and property metadata from types that are not directly referenced in compiled code. This breaks `System.Text.Json` deserialization because `DefaultJsonTypeInfoResolver` discovers constructors and properties through reflection — reflection that fails once the metadata has been trimmed. Two categories of types cross the client/server boundary via JSON and therefore need preservation:

1. **Plain DTOs returned by factory methods** — e.g., the `EmployeeDto` from `Task<EmployeeDto>` on an interface factory method.
2. **Event records raised via `IFactoryEvents.Raise<T>()`** — both server-raised events relayed to the client (`RemoteResponseDto.RelayedEvents`) and client-raised events sent to the server.

Both are handled automatically by the generator. Two primitives do the work:

| Primitive | Emitted when | Behavior |
|-----------|--------------|----------|
| `DtoConstructorRegistry.Register<T>(() => new T())` | `T` has a public parameterless constructor | `[DynamicallyAccessedMembers(All)]` preserves every member; `NeatooJsonTypeInfoResolver.CreateObject` uses the lambda instead of `Activator.CreateInstance` |
| `DtoConstructorRegistry.PreserveType<T>()` | `T` has only parameterized constructors (typical of records) | `[DynamicallyAccessedMembers(All)]` preserves every member; no constructor factory is recorded — STJ flows through the parameterized-ctor pipeline (`RecordBypassConverterFactory`) |

### DTO return-type discovery

The generator walks factory method return types, unwrapping `Task<T>`, nullable `T?`, arrays, and single-argument collection types (`IReadOnlyList<T>`, `List<T>`, `IEnumerable<T>`), and emits a `Register` or `PreserveType` call for each discovered DTO. Nested reference-type properties on each discovered DTO are walked recursively, with cycle detection.

### Factory event preservation

Factory event records inherit `FactoryEventBase`. Two mechanisms make them work under trimming:

- `[FactoryEvent]` on the base (inherited at runtime) — used by the runtime `FactoryEventTypeRegistry` to discover descendants via attribute scan
- The source generator discovers every concrete, accessible `FactoryEventBase` descendant declared in a compilation and emits a per-assembly event-preservation registrar that preserves the event's constructors/properties and its nested property graph. (The `[DynamicallyAccessedMembers]` annotation on the base does NOT do this — DAM does not flow to derived types under ILLink.)

Net effect: if a record inherits `FactoryEventBase`, its constructors and properties survive `PublishTrimmed=true` automatically. No per-event annotation, no `[FactoryEventHandler<T>]` declaration, and no manual `DtoConstructorRegistry` call is required for the event type itself.

```csharp
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
// Constructors and properties preserved automatically via base class annotation.
```

`IFactoryEvents.Raise<T>` and `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` carry `[DynamicallyAccessedMembers(All)]` on their generic parameter as belt-and-suspenders coverage for concrete call-sites.

### Nested DTO types reachable through event properties

The base-class annotation covers the event's own ctors and properties — it does not recursively annotate property *types*. If an event property is itself a complex record or DTO, it needs its own preservation:

```csharp
public record PriceBreakdown(decimal Base, decimal Tax);    // NOT automatically preserved
public record OrderPriced(int OrderId, PriceBreakdown Breakdown) : FactoryEventBase;
//                                       ^^^^^^^^^^^^^^^
//                                       Reachable through Breakdown property — needs preservation
```

Three ways to preserve `PriceBreakdown`:

- Return it from any factory method (the factory-return walker preserves it via `Register` or `PreserveType`)
- Use it as a parameter type on a factory method (same walker)
- Call `DtoConstructorRegistry.PreserveType<PriceBreakdown>()` (or `Register<PriceBreakdown>(() => new PriceBreakdown(...))` if it has a parameterless ctor) in DI setup

In practice, types reachable through event properties are usually also returned from or passed to factory methods, so this rarely needs explicit handling.

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

If you return a plain DTO from a factory method, carry a DTO as a `[Factory]` entity property, or declare a `FactoryEventBase` descendant in a project with a direct `Neatoo.RemoteFactory` `PackageReference`, the type and its constructors and properties are automatically trimming-safe. Nested property types reachable from events are walked and preserved automatically too. One boundary: private/protected/file-scoped nested event records cannot be preserved (the generated registrar cannot reference them) — declare wire-crossing events as top-level or internal/public nested types.

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
