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

That's it. No changes to your domain code, no assembly splitting, no conditional compilation.

Blazor WASM projects already publish with trimming enabled (`PublishTrimmed=true` is the SDK default). The `RuntimeHostConfigurationOption` tells the trimmer to treat `IsServerRuntime` as `false`, enabling dead code elimination of server-only code paths.

### What Each Setting Does

| Setting | Where | Purpose |
|---------|-------|---------|
| `IsTrimmable=true` | Domain `.csproj` | Opts the assembly into trimming |
| `RuntimeHostConfigurationOption` | Client `.csproj` | Tells the trimmer to treat `IsServerRuntime` as `false` at compile time |

The `Trim="true"` on the `RuntimeHostConfigurationOption` is critical — without it, the switch is just a runtime value and the trimmer can't use it for dead code elimination.

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

## Authorization Types and Trimming

RemoteFactory's generator automatically emits explicit DI registrations for `[AuthorizeFactory<T>]` types in the generated `FactoryServiceRegistrar`. This creates static references that the IL trimmer preserves — your auth classes survive trimming without any additional configuration.

For example, if your factory uses `[AuthorizeFactory<IPersonModelAuth>]`, the generator emits `services.TryAddTransient<IPersonModelAuth, PersonModelAuth>()` in the generated registration code. The concrete type is discovered by the generator at compile time using the naming convention (`IPersonModelAuth` → `PersonModelAuth`).

### RegisterMatchingName and Trimming

`RegisterMatchingName` uses `assembly.GetTypes()` reflection to discover services at runtime. The IL trimmer cannot see these runtime-only references and may trim types that are only registered through this convention.

**Factory auth types are handled automatically** by the generator (see above). For other services registered via `RegisterMatchingName`, you have two options if they get trimmed:

1. **Explicit registration** — Register the service directly in your DI setup instead of relying on convention discovery.
2. **`[DynamicDependency]`** — Apply this attribute to preserve specific types from trimming. See [Microsoft's documentation on preserving dependencies](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#dynamicdependency).

## Limitations

- **Development builds are not trimmed.** `dotnet run` and `dotnet build` include all code. Trimming only applies to `dotnet publish` with `PublishTrimmed=true`. This is by design — you get full IntelliSense and debugging during development.
- **Trimming warnings.** Your domain code or its dependencies may produce trimming warnings (e.g., reflection usage). These are standard .NET trimming concerns, not RemoteFactory-specific. See [Microsoft's trimming documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) for guidance.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md) — Understanding the `[Remote]` boundary that trimming leverages
- [Factory Modes](factory-modes.md) — Runtime modes (Server, Remote, Logical)
- [Getting Started](getting-started.md) — Initial project setup
