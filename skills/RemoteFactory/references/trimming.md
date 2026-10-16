# IL Trimming - Keeping Server Code Off the Client

A `[Remote]` method takes server-only services - the database context, a repository - so the persistence code sits beside the domain object it loads and saves. When the domain assembly ships to a Blazor WebAssembly client, that code ships with it unless it is trimmed: server-only packages bloat the download, the trimmer partly strips packages like Entity Framework Core and they fail at runtime, and the persistence code is readable in the published IL.

RemoteFactory's generator wraps server-only paths in `if (NeatooRuntime.IsServerRuntime)` guards. A client published with that switch set to `false` lets the trimmer treat the guarded branches as dead code and remove them: the method bodies, the server-only types and their transitive dependencies. The guards are in the generated code; the domain code does not change.

## What is guarded

| Declared | Guard | On a trimmed client |
|---|---|---|
| `[Remote] internal` | Yes | Body removed; the call goes to the server. Promoted to `public` on the factory interface |
| `internal`, no `[Remote]` | Yes | Body removed; server-only |
| `public`, no `[Remote]` | No | Body kept; runs on the tier that calls it |
| `[Remote] public static` class-level `[Execute]` | Yes | Body removed |
| `public static` class-level `[Execute]`, no `[Remote]` | No | Body kept; runs where the factory resolves |
| `internal static` class-level `[Execute]`, no `[Remote]` | Yes | Body removed; server-only |

`[Remote] public` on an instance method is NF0105; a static method is exempt, because `[Remote]` alone decides its guard. A `public` local method such as a root `Create` or a `CanCreate` has no guard, because it is meant to run on the client. A child's `internal` operations are trimmable for the same reason a root's `[Remote] internal` ones are.

By shape, measured against a publish-trimmed assembly in RemoteFactory's trimming gate:

| Shape | Server bodies removed |
|---|---|
| Static factory `[Remote, Execute]` | Yes |
| Static factory bare `[Execute]` | No, by design: the body runs on the client |
| `[FactoryEventHandler<T>]` | Yes |
| Class factory operations, sync and `async` | Yes |
| Class-level `[Remote, Execute]` | Yes |
| Class-level bare `[Execute]` | `public static` no, by design; `internal static` yes |
| Interface factory | Not established. No leak has been observed, but the implementation is reached through interfaces, so a client-side check reads "absent" whether or not the body survives |

The registrar attribute that keeps factory registration alive under trimming preserves every method on the type it names, so the generator points it at a single-method forwarding holder rather than at the class or the factory. A guarded `async` operation is emitted as a non-async wrapper that carries the guard and forwards to a private async core, because the trimmer does not remove a guard's dead branch from inside an `async` state machine. The guard therefore throws synchronously from the factory entry point, not as a faulted `Task`.

## Prerequisite: a direct package reference

Source generators run only where their package is referenced directly. Every project that declares `[Factory]`, `[FactoryEventHandler<T>]` or `[AuthorizeFactory<T>]` types needs its own `PackageReference` to `Neatoo.RemoteFactory`. A transitive reference - through a `ProjectReference` to the domain project, or a package reference with `PrivateAssets="all"` - silently skips generation: no registrar, no preservation calls.

## Setup

**1. Mark the domain assembly trimmable** in its project file:

```xml
<PropertyGroup>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

**2. Set the feature switch** in the client project file:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                   Value="false"
                                   Trim="true" />
</ItemGroup>
```

`Trim="true"` is what lets the trimmer fold the switch; without it the switch is a runtime value only.

**3. Keep server-only dependencies off the client** in the domain project file:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
<ProjectReference Include="..\MyApp.Persistence\MyApp.Persistence.csproj" PrivateAssets="all" />
```

**4. Mark residual assemblies trimmable** in the client project file, for assemblies that still reach the client output without `IsTrimmable`:

```xml
<ItemGroup>
  <TrimmableAssembly Include="MyApp.Persistence" />
</ItemGroup>
```

**`TrimMode` needs no setting.** Blazor WebAssembly publishes with `TrimMode=partial`, which trims only the assemblies marked `IsTrimmable`; step 1 is what puts the domain assembly in that set. RemoteFactory's trimming gate publishes at `partial` with `IsTrimmable=true`. `TrimMode=full` also works, because it trims every assembly.

Requirements: .NET 9 or later (`[FeatureSwitchDefinition]`), and `dotnet publish` - `dotnet build` and `dotnet run` do not trim.

## Verifying

```bash
dotnet publish -c Release
grep -aob "OrderPersistence" bin/Release/net9.0/publish/wwwroot/_framework/MyApp.Domain.wasm
```

Type and method names are UTF-8 in metadata, so `grep -a` finds them. String literals from method bodies are UTF-16; strip the nulls before searching for one (`tr -d '\000' < file | grep -c "SELECT"`). Before trusting a clean result, run the same check on the untrimmed build output, where the code definitely exists: if that also reports "absent", the check is broken.

If server-only names remain: confirm the domain assembly has `IsTrimmable=true`, confirm the switch has `Trim="true"`, and inspect the publish output, not the build output.

## Authorization types and `RegisterMatchingName`

The generator emits explicit registrations for `[AuthorizeFactory<T>]` classes, resolved by name at compile time (`IOrderAuth` → `OrderAuth`), so they survive trimming. `RegisterMatchingName` scans the assembly with reflection at runtime, so the trimmer cannot see what it registers; a service registered only by convention may be trimmed. Register such services explicitly on the client.

## DTO and event preservation

A trimmable domain assembly loses the constructor and property metadata of types nothing references directly, and `System.Text.Json` needs that metadata to deserialize them. The generator preserves what it reaches from three entry points: factory method signatures (return types and non-service parameters), the members of `[Factory]` classes, and every concrete, accessible `FactoryEventBase` descendant.

From each it walks public instance properties and public fields, recursively with cycle detection, unwrapping `Task<T>`, nullables, arrays, every generic collection (including your own, such as `PagedList<T> : List<T>`, and its element), and both the key and the value of every generic dictionary. For each type it finds it emits `DtoConstructorRegistry.Register<T>(...)` (a public parameterless constructor) or `DtoConstructorRegistry.PreserveType<T>()` (parameterized constructors only, as a positional record has). An event's nested records need nothing:

<!-- snippet: trimming-event-nested-record -->
<a id='snippet-trimming-event-nested-record'></a>
```cs
public record ShippingAddress(string Street, string City, string PostalCode);

/// <summary>
/// Event object raised when an order ships. Demonstrates an event record with
/// a nested record property — the generator automatically emits preservation
/// for both <c>OrderShippedEvent</c> and <c>ShippingAddress</c> so the event
/// round-trips correctly in IL-trimmed builds.
/// </summary>
public record OrderShippedEvent(Guid OrderId, ShippingAddress Address) : FactoryEventBase;
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs#L164-L174' title='Snippet source file'>snippet source</a> | <a href='#snippet-trimming-event-nested-record' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**What the walk does not reach,** and so needs explicit preservation:

| Shape | Why |
|---|---|
| A member typed as an interface or abstract class whose concrete type is a plain DTO | The `$type` discriminator picks the type at runtime |
| A tuple element | Tuples are `System` types |
| A non-generic collection subclass (`class Locations : List<LocationDto>`) | Its element type is not reached |
| A value typed `object` | No static type to follow |
| A DTO that reaches the client only through your own HTTP or JSON code | It never flows through a factory, an entity or an event |

Preserve such a type with `<type fullname="MyApp.Domain.LocationDto" preserve="all" />` in the client's linker configuration, or in client setup with the call the generator would have emitted: `DtoConstructorRegistry.Register<LocationDto>(() => new LocationDto())` or `DtoConstructorRegistry.PreserveType<PriceBreakdown>()` (namespace `Neatoo.RemoteFactory.Internal`). A private, protected or file-scoped nested event record cannot be preserved; declare events that cross the wire at top level or as `internal`/`public` nested types.

**A generic wrapper around `Raise<T>`.** `Raise<T>` carries `[DynamicallyAccessedMembers(All)]` on `T`. A wrapper that forwards its own `T` gets IL2091:

```csharp
// WRONG - IL2091: the wrapper's T lacks Raise<T>'s annotation
public Task RelayAnyEvent<T>(T evt) where T : FactoryEventBase =>
    _factoryEvents.Raise(evt);
```

Put the same annotation on the wrapper's `T`, or close the generic at the boundary with a concrete event type. A direct call with a concrete type is unaffected.

## `IFactorySaveMeta` on a trimmed client

`Save` routes on the server from `IsNew` and `IsDeleted`; if either drops out of the payload, every save routes to Insert and Delete does nothing. They are public `get; set;`:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L121-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Nothing else is needed. A `partial`, non-nested class whose constructors take no services gets a generated ordinal converter, whose reads and writes keep the accessors. Any other class factory - a constructor that takes services, no `partial`, nested - gets `DtoConstructorRegistry.PreserveProperties<T>()` from its registrar, which keeps its public properties and fields and nothing else, so its `[Remote]` bodies still trim. A private setter does not work: the ordinal format skips a property whose setter the converter cannot call, trimmed or not.

To check, decompile the trimmed client's domain assembly (`ilspycmd <Client>/obj/Release/net10.0/linked/<Domain>.dll -t <Type>`) and confirm both properties keep a getter and a setter.
