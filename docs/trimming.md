# IL Trimming

RemoteFactory lets you write `[Remote]` methods that take server-only services like `DbContext` and `IEmployeeRepository` — your business logic lives on the domain object, right where it belongs. But when that domain assembly ships to the browser in Blazor WASM, three problems emerge:

1. **Runtime failures.** Blazor WASM publishes with trimming by default. The trimmer sees Entity Framework Core referenced in your assembly, partially strips its internals, and EF crashes at runtime — even though the client never calls those code paths.

2. **Intellectual property exposure.** Your `[Remote]` method bodies — SQL queries, business rules, data transformations — ship to the browser in decompilable IL. Anyone with a disassembler can read your server-side logic.

3. **Bundle bloat.** Server-only packages and their transitive dependencies inflate the download size, even though the client never executes them.

The traditional workaround is splitting your domain into separate client and server assemblies. That works, but it adds project complexity and fights the single-assembly model that makes RemoteFactory productive.

RemoteFactory solves all three problems with **feature switch guards**. The source generator wraps server-only code paths in `if (NeatooRuntime.IsServerRuntime)` checks. When you configure your Blazor WASM project to set this switch to `false`, the IL trimmer treats the guarded branches as dead code and removes them entirely — method bodies, server-only types, and their transitive dependencies all disappear from the published output. EF Core, repository implementations, helper classes — gone cleanly, no partial trimming, no runtime crashes.

## How It Works

RemoteFactory's source generator emits `if (NeatooRuntime.IsServerRuntime)` guards around server-only code. Which methods get guards depends on method visibility:

### Class Factories — Conditional Guards

Not all factory methods get guards. The generator uses the developer's `public` vs `internal` declaration and the presence of `[Remote]` to decide:

| Method Declaration | Guard? | Trimming Behavior |
|---|---|---|
| `[Remote] internal` | Yes | Method body trimmed. Client routes to server via delegate fork. Promoted to `public` on factory interface. |
| `public` (no `[Remote]`) | **No** | Method body **survives** trimming. Runs locally on both client and server. |
| `internal` (no `[Remote]`) | Yes | Method body trimmed. Server-only. |

`[Remote]` requires `internal` — `[Remote] public` is a compile-time error (NF0105). The `[Remote] internal` pattern enables IL trimming: the trimmer eliminates the method bodies, `[Service]` dependencies, and transitive references from the published output, while the generated factory interface exposes the method as `public` for clients to call.

`public` non-`[Remote]` methods like `Create(string name)` or `CanCreate()` have no guard because they are designed to run on the client. Marking child entity factory methods as `internal` (without `[Remote]`) also makes them trimmable.

### Static and Interface Factories

- **Static factories** — `[Execute]` delegate registrations are guarded. The trimmer removes the registration lambdas and their captured dependencies.
- **Interface factories** — Local method bodies throw `InvalidOperationException` when `IsServerRuntime` is `false`, making the server-only code path unreachable to the trimmer.

The key insight: the guards are in RemoteFactory's **generated** code, not in your application code. You don't need to modify your domain model at all.

### The Feature Switch

`NeatooRuntime.IsServerRuntime` uses .NET's `[FeatureSwitchDefinition]` attribute. At runtime, it reads from `AppContext` and defaults to `true` (server behavior). But when you set it via `RuntimeHostConfigurationOption` with `Trim="true"`, the IL trimmer treats it as a compile-time constant and folds it into the binary. All code behind the `false` branch is eliminated.

```
Published without trimming          Published with trimming
─────────────────────────           ──────────────────────
Domain assembly                     Domain assembly
├── Employee                        ├── Employee
│   ├── Validate()                  │   ├── Validate()
│   ├── DataPortal_Fetch()          │   └── (factory stubs only)
│   ├── DataPortal_Insert()         │
│   └── DataPortal_Update()         │
├── EmployeeRepository              ├── (removed)
├── EmployeeDbContext                ├── (removed)
└── EF Core references              └── (removed)
```

## Prerequisite: Direct `Neatoo.RemoteFactory` Reference in Every Project with Factory Types

Roslyn source generators only run in a project when the generator package (or analyzer reference) is resolved **directly**, not transitively. If a project declares any of the following — `[Factory]`, `[FactoryEventHandler<T>]`, `[Execute]`, `[Save]`, `[AuthorizeFactory<T>]` — it **must** have its own `PackageReference` to `Neatoo.RemoteFactory`:

```xml
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
```

Relying on a transitive flow (e.g. a `ProjectReference` to a domain project that references `Neatoo.RemoteFactory`, or a `PackageReference` with `PrivateAssets="all"`) will silently skip code generation for that project. The symptoms are specific and easy to misdiagnose:

- `FactoryServiceRegistrar` is never emitted for types in the project — nothing gets registered into DI, nothing gets registered into `DtoConstructorRegistry`, and server-side `[FactoryEventHandler<T>]` handlers declared in that project are never registered with `FactoryEventHandlerRegistry`.
- Factories appear to work in one project and fail in another that depends on the first.
- Server-raised events still reach the wire, but server-side handlers declared in the affected project never fire.

Client-side event reception is unaffected by this issue — the consumer-implemented `IFactoryEventRelay` is discovered by plain DI, not by the generator.

This applies to **every** project that declares factory types, including Blazor WASM client projects that host client-side relay handlers. The only projects that may rely on a transitive reference are those that purely *consume* factories (inject the interface, call methods) without declaring any factory types themselves.

## Configuration

### Step 1: Mark your domain assembly as trimmable

In your **domain model project** `.csproj`:

```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

This tells the trimmer your assembly is safe to trim. Without it, the trimmer only trims framework assemblies and your domain model ships to the client intact — with all server-only code and dependencies.

The library author declares trimmability once, rather than every consuming client project needing to add `<TrimmableAssembly>` entries.

### Step 2: Configure the client project

In your **Blazor WASM client project** `.csproj`:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
</ItemGroup>
```

Blazor WASM projects already publish with trimming enabled (`PublishTrimmed=true` is the SDK default). The `RuntimeHostConfigurationOption` tells the trimmer to treat `IsServerRuntime` as `false`, enabling dead code elimination of server-only code paths.

### Step 3: Isolate server-only dependencies with `PrivateAssets="all"`

Your domain model often references server-only packages (like EF Core) and server-only projects (like a data access layer). Without intervention, these flow as transitive dependencies to the client project — the trimmer then has to deal with assemblies it may not be able to trim cleanly, causing warnings or runtime failures.

Mark server-only references with `PrivateAssets="all"` in your **domain model project** `.csproj` to prevent transitive flow:

```xml
<!-- Server-only packages -->
<PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />

<!-- Server-only project references -->
<ProjectReference Include="..\Person.Ef\Person.Ef.csproj" PrivateAssets="all" />
```

`PrivateAssets="all"` means these dependencies are available at compile time (so your domain code can reference EF Core types and repository implementations) but they are **not** forwarded to projects that reference your domain model. The server project references both the domain model and the data access layer directly, so it gets everything it needs.

### Step 4: Mark residual assemblies as trimmable in the client

Some assemblies may still end up in the client output through indirect paths — for example, if the build resolves them as transitive dependencies despite `PrivateAssets="all"` on the primary path. These assemblies are not marked `IsTrimmable` themselves, so the trimmer leaves them intact by default.

Add `<TrimmableAssembly>` entries in your **client project** `.csproj` to tell the trimmer it is safe to trim them:

```xml
<ItemGroup>
  <TrimmableAssembly Include="Person.Ef" />
  <TrimmableAssembly Include="Neatoo.Generator" />
</ItemGroup>
```

- **`Person.Ef`** — The data access layer. Even with `PrivateAssets="all"` on the domain model's project reference, the build may resolve it through other dependency paths. Marking it trimmable ensures it gets stripped from the published client.
- **`Neatoo.Generator`** — The RemoteFactory source generator assembly. It runs at compile time only, but its output assembly may appear in the client output. Marking it trimmable removes it from the published binary.

### What Each Setting Does

| Setting | Where | Purpose |
|---------|-------|---------|
| `IsTrimmable=true` | Domain `.csproj` | Opts the assembly into trimming |
| `RuntimeHostConfigurationOption` | Client `.csproj` | Tells the trimmer to treat `IsServerRuntime` as `false` at compile time |
| `PrivateAssets="all"` | Domain `.csproj` | Prevents server-only dependencies from flowing transitively to client |
| `TrimmableAssembly` | Client `.csproj` | Marks residual assemblies as safe to trim even though they lack `IsTrimmable` |

The `Trim="true"` on the `RuntimeHostConfigurationOption` is critical — without it, the switch is just a runtime value and the trimmer can't use it for dead code elimination.

### Complete Example

Here are all three project files showing the full trimming configuration, based on the [Person example](https://github.com/NeatooDotNet/RemoteFactory/tree/main/src/Examples/Person):

**Domain Model (`Person.DomainModel.csproj`):**
```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>

<ItemGroup>
  <!-- Server-only packages: PrivateAssets="all" prevents transitive flow to client -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <!-- Server-only project references: same treatment -->
  <ProjectReference Include="..\Person.Ef\Person.Ef.csproj" PrivateAssets="all" />
</ItemGroup>
```

**Client (`Person.Client.csproj`):**
```xml
<ItemGroup>
  <!-- Tell trimmer IsServerRuntime is false at compile time -->
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
  <!-- Mark additional assemblies as trimmable -->
  <TrimmableAssembly Include="Person.Ef" />
  <TrimmableAssembly Include="Neatoo.Generator" />
</ItemGroup>
```

**Server (`Person.Server.csproj`):**
```xml
<!-- No trimming configuration needed — server runs everything.
     The server references both the domain model and data access layer directly. -->
<ItemGroup>
  <ProjectReference Include="..\Person.DomainModel\Person.DomainModel.csproj" />
  <ProjectReference Include="..\Person.Ef\Person.Ef.csproj" />
  <ProjectReference Include="..\Person.Client\Person.Client.csproj" />
</ItemGroup>
```

### Requirements

- **.NET 9 or later** — `[FeatureSwitchDefinition]` was introduced in .NET 9
- **`dotnet publish`** — Trimming only runs during publish, not during `dotnet build` or `dotnet run`

## Verifying Trimming Results

After publishing, you can verify that server-only types were removed:

```bash
# Publish with trimming
dotnet publish -c Release

# Search for server-only type names in the output assembly
# (should return no matches)
grep -aob "YourRepositoryClassName" bin/Release/net9.0/publish/YourApp.dll

# Or use ILSpy for detailed inspection
ilspycmd bin/Release/net9.0/publish/YourApp.dll
```

If server-only type names still appear in the output, check that:
1. `TrimMode` is set to `full` (not `partial` or omitted)
2. The `RuntimeHostConfigurationOption` has `Trim="true"`
3. You're inspecting the `publish/` output, not the `build/` output

## Factory Type Preservation

All factory types — class, static, and interface — are automatically preserved from trimming. The source generator emits `[assembly: NeatooFactoryRegistrar(typeof(X))]` for every factory, creating a static reference that the IL trimmer follows. The `NeatooFactoryRegistrarAttribute` carries `[DynamicallyAccessedMembers]` annotations that instruct the trimmer to preserve all methods on the referenced type, including the internal `FactoryServiceRegistrar` method used for DI registration.

At startup, `AddNeatooRemoteFactory()` and `AddNeatooAspNetCore()` discover factory types by enumerating these assembly attributes rather than scanning all types via reflection. This means factory registration is fully trimming-safe — no factory types are lost during IL trimming, regardless of whether they are class factories, static factories, or interface factories.

You do not need to take any action to preserve your factory types. This is handled automatically by the generator.

## Authorization Types and Trimming

RemoteFactory's generator automatically emits explicit DI registrations for `[AuthorizeFactory<T>]` types in the generated `FactoryServiceRegistrar`. This creates static references that the IL trimmer preserves — your auth classes survive trimming without any additional configuration.

For example, if your factory uses `[AuthorizeFactory<IPersonModelAuth>]`, the generator emits `services.TryAddTransient<IPersonModelAuth, PersonModelAuth>()` in the generated registration code. The concrete type is discovered by the generator at compile time using the naming convention (`IPersonModelAuth` → `PersonModelAuth`).

### RegisterMatchingName and Trimming

`RegisterMatchingName` uses `assembly.GetTypes()` reflection to discover services at runtime. The IL trimmer cannot see these runtime-only references and may trim types that are only registered through this convention.

**Factory auth types are handled automatically** by the generator (see above). For other services registered via `RegisterMatchingName`, you have two options if they get trimmed:

1. **Explicit registration** — Register the service directly in your DI setup instead of relying on convention discovery.
2. **`[DynamicDependency]`** — Apply this attribute to preserve specific types from trimming. See [Microsoft's documentation on preserving dependencies](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#dynamicdependency).

## DTO Return Type Preservation

Plain DTO classes returned by factory methods are automatically preserved from trimming. When your domain assembly has `IsTrimmable=true`, the IL trimmer strips constructor and property metadata from types that aren't directly referenced in compiled code. This breaks `System.Text.Json` deserialization — `DefaultJsonTypeInfoResolver` uses reflection to discover constructors and properties, and that reflection fails when the metadata has been trimmed away.

Normal Blazor WASM apps don't hit this because their assemblies aren't trimmed — `TrimMode=partial` only trims assemblies explicitly marked `IsTrimmable=true`. RemoteFactory intentionally marks domain assemblies as trimmable to remove server-only business logic from the client. DTOs returned through factory methods cross the client-server boundary via JSON serialization, so they must survive trimming intact.

### How RemoteFactory Handles It

The source generator discovers DTO types in factory method signatures at compile time — return types **and** non-service parameters — and emits one of two preservation calls per discovered type, chosen by constructor shape:

- **Public parameterless constructor** → `DtoConstructorRegistry.Register<T>(() => new T())`. The registered lambda replaces reflection-based construction at deserialization time.
- **Only parameterized public constructors** (positional records) → `DtoConstructorRegistry.PreserveType<T>()`. No constructor lambda is registered; deserialization flows through `RecordBypassConverterFactory`, and the call exists purely to root the type for the trimmer.

Both calls carry `[DynamicallyAccessedMembers(All)]` on the generic parameter, which tells the trimmer to preserve the entire type — constructors, properties, and all metadata that `System.Text.Json` needs for deserialization.

This covers all factory patterns:

- **Interface Factory methods** — e.g., `Task<EmployeeDto>` or `Task<IReadOnlyList<EmployeeDto>>` return types
- **Class Factory `[Execute]` methods** — DTO return types are discovered and preserved
- **Static Factory `[Execute]` methods** — same treatment

The generator unwraps `Task<T>`, nullable `T?`, and collection types (like `IReadOnlyList<T>`) to find the DTO type inside.

### What Qualifies as a DTO

Not every signature type needs this treatment. The generator preserves a discovered type when it:

- Has at least one public constructor (parameterless → `Register<T>`; parameterized-only, e.g. positional records → `PreserveType<T>`)
- Is **not** a `[Factory]`-annotated type (those are already preserved via DI registration)
- Is **not** a primitive, string, or framework type
- Is **not** abstract or an interface

### What You Need to Know

If you return or accept a plain DTO class **or a positional record** through any factory method, it is automatically trimming-safe. You do not need to take any action.

**Nested DTOs are automatically discovered.** The generator recursively walks public instance properties (including inherited properties) of each discovered DTO type — classes and records alike — to find nested DTOs that also need preservation. Collection properties (`List<T>`, `IReadOnlyList<T>`, arrays) and nullable properties (`T?`) are unwrapped to find the inner type. The same eligibility criteria and bucket rule apply to nested DTOs as to direct signature types. Cycle detection prevents infinite recursion from circular references.

**DTOs carried as `[Factory]` entity properties are automatically discovered.** Every class carrying `[Factory]` directly also walks its own public property graph (inherited properties included) during generation and emits preservation for reachable DTOs in its own `FactoryServiceRegistrar` — so a DTO or record that only rides on an aggregate (never appearing in a factory method signature itself) is still trimming-safe. The entity itself is never treated as a DTO (entities are preserved via DI registration), and entity-typed properties are not walked by the *parent* — each `[Factory]` class's own registrar covers its own graph.

For example, if a factory method returns `ParentDto` which has a `List<ChildDto> Children` property, both `ParentDto` and `ChildDto` are automatically registered — no additional action is needed. Likewise, if an `[Execute]`-opened aggregate carries a `Banner` record property, the record is preserved through the aggregate's own registrar.

If you have a DTO that is **not** in any factory method signature, **not** reachable as a property of a discovered DTO, and **not** reachable through a `[Factory]` entity's public property graph, you need to preserve it yourself. See [Microsoft's documentation on preserving dependencies](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#dynamicdependency). (One deliberate boundary: a class that merely *implements* a `[Factory]` interface — an interface-factory service implementation — gets no property walk; those are stateless services, not serialized state.)

## Factory Event Type Preservation

Event records raised via `IFactoryEvents.Raise<T>()` and handled by `[FactoryEventHandler<T>]` classes (server) or `IFactoryEventRelay` (client) cross the client/server boundary via JSON — both when the server relays captured events back to the client in `RemoteResponseDto.RelayedEvents` and when a client raises an event that a server handler processes. Like DTO return types, event records must survive IL trimming intact.

### How RemoteFactory Handles It

The source generator discovers every concrete `FactoryEventBase` descendant declared in a compilation and emits a per-assembly event-preservation registrar — a generated static class registered via the same assembly-level `[NeatooFactoryRegistrar]` mechanism as factory registrars, invoked automatically by `AddNeatooRemoteFactory`. The registrar emits `DtoConstructorRegistry.PreserveType<T>()` for each event record (and `Register<T>` / `PreserveType<T>` for the DTOs reachable through each event's property graph, bucketed by constructor shape like every other discovered DTO).

Declaring the event is all it takes:

```csharp
public record OrderShippedEvent(int OrderId, ShippingAddress Address) : FactoryEventBase;
// → generated registrar preserves OrderShippedEvent AND ShippingAddress
```

The `[FactoryEvent]` annotation on `FactoryEventBase` (inherited at runtime) remains what makes descendants discoverable by the `FactoryEventTypeRegistry` assembly scan. Its `[DynamicallyAccessedMembers]` annotation, however, does **not** preserve descendants' members under trimming — `DynamicallyAccessedMembers` does not flow from a base type to derived types in ILLink, which is exactly why the generator emission exists. (An earlier version of this page claimed the annotation alone made every descendant trimming-safe; a publish-trimmed repro proved that wrong for event records whose only reference is a generic subscription call site.)

`IFactoryEvents.Raise<T>` retains `[DynamicallyAccessedMembers(All)]` on its generic parameter for producer-side call-site preservation.

One boundary: the generated registrar is a separate file, so `private`/`protected`/file-scoped nested event records cannot be preserved this way and are skipped. Declare wire-crossing events as top-level (or `internal`/`public` nested) types.

### What You Need to Know

Any accessible record inheriting `FactoryEventBase` is automatically trimming-safe — no manual `DtoConstructorRegistry` calls, no `[FactoryEventHandler<T>]` required, no consumer-side annotation, nothing to configure. End-to-end verification lives in `src/Tests/RemoteFactory.TrimmingTests/EventSubscribeOnlySmokeTest.cs` — a publish-trimmed check whose event record's only static reference is a generic `Subscribe<TEvent>` call site (the hardest shape: nothing else roots the type's members), plus the `EventRelaySmokeTest` round-trip.

### Nested Reference Types in Event Records

Automatically preserved. The generator walks each discovered event's public property graph with the same bucketed walk used for factory-signature and entity-property DTOs — nested records land in the `PreserveType` bucket, parameterless DTOs in the `Register` bucket, collections and nullables are unwrapped, and cycles are detected. No manual `DtoConstructorRegistry` calls are needed for types reachable from an event record's properties.

### User Code That Forwards `Raise<T>` Through a Generic Passthrough

`IFactoryEvents.Raise<T>` and its implementations now carry `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]` on the `T` parameter. This is the mechanism that preserves `T` from the call site at every concrete `Raise<MyEvent>(...)` invocation.

If your own code re-exposes `Raise` through a generic wrapper method, the compiler will now flag the wrapper with `IL2091` because the wrapper's `T` does not carry the same annotation:

```csharp
// In your own code — now produces IL2091 under trimming
public Task RelayAnyEvent<T>(T evt) where T : FactoryEventBase =>
    _factoryEvents.Raise(evt);
```

Resolve by matching the annotation on your own parameter, or by passing a concrete event type instead:

```csharp
// Option 1 — propagate the annotation
public Task RelayAnyEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T evt)
    where T : FactoryEventBase =>
    _factoryEvents.Raise(evt);

// Option 2 — close the generic at the boundary (no warning)
public Task RelayCheckoutCompleted(OrderCheckoutCompleted evt) =>
    _factoryEvents.Raise(evt);
```

The warning fires only when user code forwards a generic `T` into `Raise<T>`. Direct calls with a concrete type (`_factoryEvents.Raise(new OrderCheckoutCompleted(...))`) are unaffected.

## IFactorySaveMeta Preservation

When an entity implements `IFactorySaveMeta`, its `IsNew` and `IsDeleted` properties must round-trip across the client/server boundary as JSON. Save routing happens server-side — the generated `LocalSave` reads `target.IsNew` and `target.IsDeleted` on the server-side deserialized instance. If either property drops out of the wire payload, the server deserializes them as default values (`IsNew = true` from the property initializer, `IsDeleted = false`), and **every Save routes to Insert regardless of the client's actual state**. Delete silently becomes a no-op.

### The Trimming Interaction

A common domain design uses private setters to prevent external callers from flipping lifecycle state:

```csharp
public bool IsNew { get; private set; } = true;
public bool IsDeleted { get; private set; }
```

`[JsonInclude]` (covered in [Save Operation — Serializing IsNew and IsDeleted](save-operation.md#serializing-isnew-and-isdeleted-across-the-remote-boundary)) handles the serialization side, but under `PublishTrimmed=true` the IL trimmer performs **visibility analysis** independently: when no concrete-type callsite reads the getter (all reads go through the `IFactorySaveMeta` / entity interface dispatch), the trimmer narrows the property itself from `public` to `private` in the emitted assembly. `System.Text.Json`'s default reflection-based resolver then skips the property entirely outbound, and you're back to the Insert-only routing bug — but only in published Release builds.

**`[DynamicallyAccessedMembers]` on the class is not enough** — that annotation preserves reflection metadata but does not prevent visibility narrowing.

### The Fix: `[DynamicDependency]` on the Constructor

Annotate the `[Create]` constructor with `[DynamicDependency]` pointing at the two properties by name. This roots them against trimmer optimization and preserves their public getter visibility:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

[Factory]
internal class Employee : IEmployee
{
    [DynamicDependency(nameof(IsNew))]
    [DynamicDependency(nameof(IsDeleted))]
    [Create]
    public Employee() { /* ... */ }

    [JsonInclude]
    public bool IsNew { get; private set; } = true;

    [JsonInclude]
    public bool IsDeleted { get; private set; }

    public void MarkDeleted() => this.IsDeleted = true;
}
```

`[DynamicDependency]` requires either:

- A named target (as above), or
- `DynamicallyAccessedMemberTypes.PublicProperties` applied to `typeof(Employee)` to cover all public properties.

The named form is narrower and the recommended default for this specific pattern.

### Verifying the Preservation

After publishing, decompile the trimmed client-side DLL and confirm the properties remain public:

```bash
ilspycmd <YourClient>/obj/Release/net10.0/linked/<YourDomain>.dll -t Your.Namespace.Employee | grep -B1 "IsNew\|IsDeleted"
```

Expected output:

```
[JsonInclude]
public bool IsDeleted

[JsonInclude]
public bool IsNew
```

If the decompiled output shows `private bool IsDeleted` / `private bool IsNew`, the trimmer has narrowed visibility — `[DynamicDependency]` is missing or the name doesn't resolve.

### If You Use Public Setters

Fully-public `public bool IsNew { get; set; }` and `public bool IsDeleted { get; set; }` avoid both the `[JsonInclude]` and the `[DynamicDependency]` requirement. The tradeoff is giving up the "only the framework and the entity itself can set these" encapsulation. Either design is viable — pick based on how strict you want the domain contract to be.

## Limitations

- **Development builds are not trimmed.** `dotnet run` and `dotnet build` include all code. Trimming only applies to `dotnet publish` with `PublishTrimmed=true`. This is by design — you get full IntelliSense and debugging during development.
- **Trimming warnings.** Your domain code or its dependencies may produce trimming warnings (e.g., reflection usage). These are standard .NET trimming concerns, not RemoteFactory-specific. See [Microsoft's trimming documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) for guidance.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md) — Understanding the `[Remote]` boundary that trimming leverages
- [Factory Modes](factory-modes.md) — Runtime modes (Server, Remote, Logical)
- [Getting Started](getting-started.md) — Initial project setup
