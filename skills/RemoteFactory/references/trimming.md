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
