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

A transitive reference (through a `ProjectReference` to the domain project, or a `PackageReference` with `PrivateAssets="all"`) silently skips generation for that project. No `FactoryServiceRegistrar` is emitted, no `DtoConstructorRegistry.Register` calls fire at startup, and for Factory Events no `FactoryEventRelayRegistry.RegisterHandlerType` calls are emitted — so client-side `[FactoryEventHandler<T>]` handlers never get dispatched.

This includes the Blazor WASM client project when it hosts client-side relay handlers. Only pure consumers — projects that inject factory interfaces and call them but declare no factory types — may rely on a transitive reference.

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

### Factory event-type discovery

Every `[FactoryEventHandler<T>]` attribute causes the generator to emit `DtoConstructorRegistry.PreserveType<T>()` in the handler class's `FactoryServiceRegistrar`. Nested reference-type properties on `T` are walked recursively using the same rules as the factory-return walker. Preservation is emitted **unconditionally** — not wrapped in an `IsServerRuntime` guard — because both client and server need the metadata.

`IFactoryEvents.Raise<T>` and `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` both carry `[DynamicallyAccessedMembers(All)]` on their generic parameter, so concrete call-sites also preserve `T` — belt-and-suspenders coverage for events raised in assemblies that do not declare a matching handler.

### Known gap: `Dictionary<K, V>` value types are not walked

The property walker unwraps single-argument generic collections only. `Dictionary<TKey, TValue>` (and any other two-argument generic) is not unwrapped, so the value type is not preserved automatically:

```csharp
public record CacheWarmed(Dictionary<string, Payload> Items) : FactoryEventBase;
//                                                ^^^^^^^
//                                                NOT automatically preserved
```

Workaround — preserve the value type explicitly:

- Declare a `[FactoryEventHandler<Payload>]` somewhere in the project, or
- Return `Payload` from any factory method, or
- Call `DtoConstructorRegistry.PreserveType<Payload>()` (or `Register<Payload>(() => new Payload())` if it has a parameterless ctor) manually in DI setup before any deserialization.

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

If you return a plain DTO from a factory method, or declare a `[FactoryEventHandler<T>]` in a project with a direct `Neatoo.RemoteFactory` `PackageReference`, the type and its nested records are automatically trimming-safe. No manual `DtoConstructorRegistry` calls are needed except in the `Dictionary<K, V>` case above.
