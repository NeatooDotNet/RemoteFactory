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

- **Static factories** — Delegate and event registrations are guarded. The trimmer removes the registration lambdas and their captured dependencies.
- **Interface factories** — Local method bodies throw `InvalidOperationException` when `IsServerRuntime` is `false`, making the server-only code path unreachable to the trimmer.

### Event Registrations

Both class factory and static factory `[Event]` registrations are wrapped in `if (NeatooRuntime.IsServerRuntime)` guards. The local event infrastructure (scope isolation, `Task.Run`, `IHostApplicationLifetime`, `IEventTracker`) only runs on the server. On client assemblies with `IsServerRuntime=false`, the trimmer eliminates these registrations entirely. Remote-mode clients use remote event stubs that serialize to the server instead.

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

The source generator discovers plain DTO return types from factory method signatures at compile time and emits `DtoConstructorRegistry.Register<T>(() => new T())` calls. The `[DynamicallyAccessedMembers(All)]` annotation on the generic parameter tells the trimmer to preserve the entire type — constructors, properties, and all metadata that `System.Text.Json` needs for deserialization.

This covers all factory patterns:

- **Interface Factory methods** — e.g., `Task<EmployeeDto>` or `Task<IReadOnlyList<EmployeeDto>>` return types
- **Class Factory `[Execute]` methods** — DTO return types are discovered and preserved
- **Static Factory `[Execute]` methods** — same treatment

The generator unwraps `Task<T>`, nullable `T?`, and collection types (like `IReadOnlyList<T>`) to find the DTO type inside.

### What Qualifies as a DTO

Not every return type needs this treatment. The generator preserves a return type when it:

- Has a public parameterless constructor
- Is **not** a `[Factory]`-annotated type (those are already preserved via DI registration)
- Is **not** a record with only parameterized constructors (handled separately by `RecordBypassConverterFactory`)
- Is **not** a primitive, string, or framework type

### What You Need to Know

If you return a plain DTO class through any factory method, it is automatically trimming-safe. You do not need to take any action.

**Nested DTOs are automatically discovered.** The generator recursively walks public instance properties (including inherited properties) of each discovered DTO type to find nested DTOs that also need registration. Collection properties (`List<T>`, `IReadOnlyList<T>`, arrays) and nullable properties (`T?`) are unwrapped to find the inner type. The same eligibility criteria apply to nested DTOs as to direct return types. Cycle detection prevents infinite recursion from circular references.

For example, if a factory method returns `ParentDto` which has a `List<ChildDto> Children` property, both `ParentDto` and `ChildDto` are automatically registered — no additional action is needed.

If you have a DTO that is **not** returned by any factory method and **not** reachable as a property of a discovered DTO, you need to preserve it yourself. See [Microsoft's documentation on preserving dependencies](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#dynamicdependency).

## Limitations

- **Development builds are not trimmed.** `dotnet run` and `dotnet build` include all code. Trimming only applies to `dotnet publish` with `PublishTrimmed=true`. This is by design — you get full IntelliSense and debugging during development.
- **Trimming warnings.** Your domain code or its dependencies may produce trimming warnings (e.g., reflection usage). These are standard .NET trimming concerns, not RemoteFactory-specific. See [Microsoft's trimming documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) for guidance.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md) — Understanding the `[Remote]` boundary that trimming leverages
- [Factory Modes](factory-modes.md) — Runtime modes (Server, Remote, Logical)
- [Getting Started](getting-started.md) — Initial project setup
