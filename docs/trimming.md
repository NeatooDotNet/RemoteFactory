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

Not all factory methods get guards. The generator uses the developer's `public` vs `internal` declaration to decide:

| Method Declaration | Guard? | Trimming Behavior |
|---|---|---|
| `[Remote] public` | Yes | Method body trimmed. Client routes to server via delegate fork. |
| `public` (no `[Remote]`) | **No** | Method body **survives** trimming. Runs locally on both client and server. |
| `internal` (no `[Remote]`) | Yes | Method body trimmed. Server-only. |

`public` non-`[Remote]` methods like `Create(string name)` or `CanCreate()` have no guard because they are designed to run on the client. Marking child entity factory methods as `internal` makes them trimmable — the trimmer eliminates their method bodies, `[Service]` dependencies, and transitive references from the published output.

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

Add two settings to your **Blazor WASM client project** (or any client `.csproj` that publishes with trimming):

```xml
<PropertyGroup>
  <TrimMode>full</TrimMode>
</PropertyGroup>

<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
</ItemGroup>
```

That's it. No changes to your domain code, no assembly splitting, no conditional compilation.

Blazor WASM projects already publish with trimming enabled (`PublishTrimmed=true` is the SDK default). `TrimMode=full` upgrades from the default `partial` mode — which only trims framework assemblies — to trimming all assemblies including your domain model. The `RuntimeHostConfigurationOption` tells the trimmer to treat `IsServerRuntime` as `false`, enabling dead code elimination of server-only code paths.

### What Each Setting Does

| Setting | Purpose |
|---------|---------|
| `TrimMode=full` | Trims all assemblies, not just framework ones (default is `partial`) |
| `RuntimeHostConfigurationOption` | Tells the trimmer to treat `IsServerRuntime` as `false` at compile time |

The `Trim="true"` on the `RuntimeHostConfigurationOption` is critical — without it, the switch is just a runtime value and the trimmer can't use it for dead code elimination.

### Requirements

- **.NET 9 or later** — `[FeatureSwitchDefinition]` was introduced in .NET 9
- **`dotnet publish`** — Trimming only runs during publish, not during `dotnet build` or `dotnet run`

## Trimming vs RemoteOnly

RemoteFactory offers two complementary approaches for keeping server-only code off the client:

| | IL Trimming | RemoteOnly Mode |
|---|---|---|
| **When it acts** | Publish time (IL trimmer) | Compile time (source generator) |
| **What it removes** | Server-only code paths and transitive dependencies | Local method implementations entirely |
| **Configuration** | MSBuild properties in client `.csproj` | `[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]` |
| **Domain code changes** | None | None |
| **Project structure** | Single shared domain assembly | Works with single or split assemblies |
| **Debugging** | Full code available during development; trimmed only on publish | Stubs only — can't debug local execution on client |

**Which should you use?**

```
Do you need server-only code completely absent from client assemblies?
├── At publish time only (development builds keep everything)
│   └── IL Trimming — simplest setup, no code changes
├── At compile time (client assembly never contains server code)
│   └── RemoteOnly — strictest separation
└── Both
    └── They compose — RemoteOnly skips generation, trimming removes leftovers
```

For most Blazor WASM apps, IL trimming alone is sufficient. RemoteOnly is useful when you need the separation guarantee at compile time — for example, if security policy requires that server logic never exists in client binaries, even during development.

See [Factory Modes](factory-modes.md) for RemoteOnly configuration.

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

## Limitations

- **Development builds are not trimmed.** `dotnet run` and `dotnet build` include all code. Trimming only applies to `dotnet publish` with `PublishTrimmed=true`. This is by design — you get full IntelliSense and debugging during development.
- **Trimming warnings.** Your domain code or its dependencies may produce trimming warnings (e.g., reflection usage). These are standard .NET trimming concerns, not RemoteFactory-specific. See [Microsoft's trimming documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) for guidance.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md) — Understanding the `[Remote]` boundary that trimming leverages
- [Factory Modes](factory-modes.md) — RemoteOnly as a compile-time alternative
- [Getting Started](getting-started.md) — Initial project setup
