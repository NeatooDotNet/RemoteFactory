# IL Trimming for Blazor WASM

## The Problem

RemoteFactory's single-assembly model means `[Remote]` methods can take server-only services like `DbContext` and `IEmployeeRepository` directly. When that assembly ships to the browser in Blazor WASM, three problems emerge:

1. **Runtime failures.** Blazor WASM publishes with trimming by default. The trimmer sees Entity Framework referenced, partially strips its internals, and EF crashes at runtime — even though the client never calls those code paths.

2. **IP exposure.** `[Remote]` method bodies — SQL queries, business rules, data transformations — ship in decompilable IL. Anyone with a disassembler can read the server-side logic.

3. **Bundle bloat.** Server-only packages and their transitive dependencies inflate download size unnecessarily.

The traditional workaround is splitting into separate client and server assemblies. IL trimming eliminates that need.

## How RemoteFactory Solves It

RemoteFactory's source generator wraps server-only code paths in `if (NeatooRuntime.IsServerRuntime)` guards. When a Blazor WASM project sets this switch to `false`, the IL trimmer treats the guarded branches as dead code and removes them — along with their transitive dependencies.

No changes to domain code required. The guards are in the **generated** factory code, not in application code.

### What Gets Guarded

Guards are conditional based on method visibility:

- **Class factories** — `[Remote]` and `internal` method bodies are wrapped in `if (NeatooRuntime.IsServerRuntime)` checks. `public` non-`[Remote]` methods (like `Create` or `CanCreate`) have **no guard** and survive trimming — they run locally on both client and server.
- **Static factories** — Delegate and event registrations guarded; trimmer removes registration lambdas and captured dependencies
- **Interface factories** — Local method bodies throw `InvalidOperationException` when `IsServerRuntime` is `false`

| Method Declaration | Guard? | Trimming Behavior |
|---|---|---|
| `[Remote] public` | Yes | Trimmed on client. Client uses delegate fork to route to server. |
| `public` (no `[Remote]`) | No | Survives trimming. Runs locally on client and server. |
| `internal` (no `[Remote]`) | Yes | Trimmed on client. Server-only. |

Mark child entity factory methods as `internal` to make them trimmable. The trimmer eliminates their method bodies, `[Service]` dependencies, and transitive references.

### The Feature Switch

`NeatooRuntime.IsServerRuntime` uses .NET's `[FeatureSwitchDefinition]` attribute. At runtime it defaults to `true` (server behavior). When set via `RuntimeHostConfigurationOption` with `Trim="true"`, the IL trimmer constant-folds it into the binary and eliminates all code behind the `false` branch.

## Configuration

Add these settings to the **Blazor WASM client project** `.csproj`:

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

Blazor WASM projects already publish with trimming enabled (`PublishTrimmed=true` is the SDK default). These two settings are all that's needed.

| Setting | Purpose |
|---------|---------|
| `TrimMode=full` | Trim all assemblies, not just framework ones (default is `partial`) |
| `RuntimeHostConfigurationOption` | Tell the trimmer to treat `IsServerRuntime` as `false` at compile time |

The `Trim="true"` attribute on the `RuntimeHostConfigurationOption` is critical — without it, the switch is just a runtime value and the trimmer cannot use it for dead code elimination.

### Requirements

- **.NET 9 or later** — `[FeatureSwitchDefinition]` was introduced in .NET 9
- **`dotnet publish`** — Trimming only runs during publish, not during `dotnet build` or `dotnet run`

## Trimming vs RemoteOnly

Both keep server-only code off the client. They are complementary.

| | IL Trimming | RemoteOnly Mode |
|---|---|---|
| **When it acts** | Publish time (IL trimmer) | Compile time (source generator) |
| **What it removes** | Server-only code paths and transitive dependencies | Local method implementations entirely |
| **Configuration** | MSBuild properties in client `.csproj` | `[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]` |
| **Domain code changes** | None | None |
| **Project structure** | Single shared domain assembly | Works with single or split assemblies |

**Recommendation:** Start with IL trimming — simplest setup, no project restructuring. Add RemoteOnly later if compile-time separation is required (e.g., security policy demands server logic never exists in client binaries, even during development).

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

## Limitations

- **Development builds are not trimmed.** `dotnet run` and `dotnet build` include all code. Trimming applies only to `dotnet publish`.
- **Trimming warnings.** Domain code or dependencies using reflection may produce standard .NET trimming warnings. These are not RemoteFactory-specific.
