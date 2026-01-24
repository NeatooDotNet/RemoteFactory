# Client/Server Code Separation

## Status: Complete (Core Feature)

**Completed:**
- [x] Generator support for `FactoryMode.RemoteOnly`
- [x] Unit tests for FactoryMode attribute (9 tests) - verifies generator OUTPUT
- [x] OrderEntry example project structure
- [x] Serialization round-trip tests (19 tests in `AggregateSerializationTests.cs`) - uses Full mode
- [x] Client-server pattern tests (12 tests in `ClientServerSeparationTests.cs`) - uses Full mode
- [x] Documentation: conceptual guide, setup guide, attributes reference, example page
- [x] **RemoteOnly Integration Tests** (19 tests in `RemoteOnlyTests.Integration`) - validates RemoteOnly-compiled client calling Full-compiled server through JSON serialization

**Deferred (Future Enhancements):**
- [ ] E2E tests with actual database (OrderEntry + EF Core)
- [x] Validation error serialization tests (12 tests in `ValidationSerializationTests.cs`)
- [x] Migration guide documentation (`docs/concepts/migrating-to-client-server.md`)
- [x] Update generated-code.md reference (added "FactoryMode: Full vs RemoteOnly" section)

---

## RemoteOnly Integration Test Plan

### Problem Statement

Current tests verify:
1. Generator produces correct output for RemoteOnly mode (text-based verification)
2. Serialization works with Full mode on both client and server containers

**Missing:** Runtime verification that a RemoteOnly-compiled client assembly can call a Full-compiled server assembly through the two-container pattern with actual JSON serialization.

### Solution: New Test Project Structure

Create separate test assemblies that mirror real-world client-server separation:

```
src/Tests/
├── RemoteOnlyTests/
│   ├── RemoteOnlyTests.Domain/           # Shared source files
│   │   ├── ITestAggregate.cs
│   │   ├── TestAggregate.cs              # #if CLIENT / #else sections
│   │   ├── ITestChild.cs
│   │   ├── TestChild.cs                  # Local [Create], server [Fetch]
│   │   ├── ITestChildList.cs
│   │   └── TestChildList.cs
│   │
│   ├── RemoteOnlyTests.Client/           # CLIENT constant, RemoteOnly mode
│   │   ├── RemoteOnlyTests.Client.csproj
│   │   ├── AssemblyAttributes.cs         # [assembly: FactoryMode(RemoteOnly)]
│   │   └── Generated/                    # Generated factories (remote stubs only)
│   │
│   ├── RemoteOnlyTests.Server/           # No constant, Full mode (default)
│   │   ├── RemoteOnlyTests.Server.csproj
│   │   ├── ServerServices.cs             # In-memory "database" for testing
│   │   └── Generated/                    # Generated factories (full)
│   │
│   └── RemoteOnlyTests.Integration/      # Integration tests
│       ├── RemoteOnlyTests.Integration.csproj
│       ├── RemoteOnlyContainers.cs       # Two-container setup using BOTH assemblies
│       └── RemoteOnlyIntegrationTests.cs # Actual tests
```

### Project Details

#### 1. RemoteOnlyTests.Domain/ (Shared Source - Not Compiled Directly)

**TestAggregate.cs:**
```csharp
[Factory]
public class TestAggregate : ITestAggregate, IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public ITestChildList Children { get; set; } = null!;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

#if CLIENT
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Client stub - use factory");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Client stub - use factory");
    }

    [Remote, Insert]
    public Task Insert()
    {
        throw new InvalidOperationException("Client stub - use factory");
    }

    [Remote, Update]
    public Task Update()
    {
        throw new InvalidOperationException("Client stub - use factory");
    }
#else
    [Remote, Create]
    public void Create([Service] ITestChildListFactory childListFactory)
    {
        Id = Guid.NewGuid();
        Children = childListFactory.Create();
    }

    [Remote, Fetch]
    public Task Fetch(Guid id, [Service] ITestDataStore dataStore, [Service] ITestChildListFactory childListFactory)
    {
        var data = dataStore.GetAggregate(id);
        Id = data.Id;
        Name = data.Name;
        Children = childListFactory.Fetch(data.Children);
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Insert]
    public Task Insert([Service] ITestDataStore dataStore)
    {
        dataStore.Insert(this);
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update([Service] ITestDataStore dataStore)
    {
        dataStore.Update(this);
        return Task.CompletedTask;
    }
#endif
}
```

**TestChild.cs:**
```csharp
[Factory]
public class TestChild : ITestChild
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Value { get; set; }

    // Local Create - works on both client and server (no [Remote])
    [Create]
    public TestChild()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    // Server-only Fetch
    [Fetch]
    public void Fetch(Guid id, string name, decimal value)
    {
        Id = id;
        Name = name;
        Value = value;
    }
#endif
}
```

#### 2. RemoteOnlyTests.Client.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Link shared source files -->
  <ItemGroup>
    <Compile Include="..\RemoteOnlyTests.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\RemoteFactory\RemoteFactory.csproj" />
    <ProjectReference Include="..\..\..\..\Generator\Generator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**AssemblyAttributes.cs:**
```csharp
using Neatoo.RemoteFactory;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

#### 3. RemoteOnlyTests.Server.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Link shared source files -->
  <ItemGroup>
    <Compile Include="..\RemoteOnlyTests.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\RemoteFactory\RemoteFactory.csproj" />
    <ProjectReference Include="..\..\..\..\Generator\Generator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**ServerServices.cs:**
```csharp
// In-memory data store for testing (no database required)
public interface ITestDataStore
{
    AggregateData GetAggregate(Guid id);
    void Insert(ITestAggregate aggregate);
    void Update(ITestAggregate aggregate);
}

public class TestDataStore : ITestDataStore
{
    private readonly Dictionary<Guid, AggregateData> _data = new();

    public AggregateData GetAggregate(Guid id)
    {
        if (_data.TryGetValue(id, out var data)) return data;
        // Return test data for any ID
        return new AggregateData
        {
            Id = id,
            Name = $"Test-{id}",
            Children = new[] { (Guid.NewGuid(), "Child1", 10m), (Guid.NewGuid(), "Child2", 20m) }
        };
    }

    public void Insert(ITestAggregate aggregate) => _data[aggregate.Id] = ToData(aggregate);
    public void Update(ITestAggregate aggregate) => _data[aggregate.Id] = ToData(aggregate);
    // ...
}
```

#### 4. RemoteOnlyTests.Integration.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference BOTH client and server assemblies -->
    <ProjectReference Include="..\RemoteOnlyTests.Client\RemoteOnlyTests.Client.csproj" />
    <ProjectReference Include="..\RemoteOnlyTests.Server\RemoteOnlyTests.Server.csproj" />
    <ProjectReference Include="..\..\..\..\RemoteFactory\RemoteFactory.csproj" />
  </ItemGroup>
</Project>
```

**RemoteOnlyContainers.cs:**
```csharp
/// <summary>
/// Sets up two DI containers:
/// - Client: Uses RemoteOnlyTests.Client assembly (RemoteOnly factories)
/// - Server: Uses RemoteOnlyTests.Server assembly (Full factories)
///
/// The client factory makes "remote" calls that serialize through JSON
/// and execute on the server container.
/// </summary>
public static class RemoteOnlyContainers
{
    public static (IServiceScope client, IServiceScope server) Scopes()
    {
        var clientCollection = new ServiceCollection();
        var serverCollection = new ServiceCollection();

        // Register CLIENT factories (RemoteOnly mode)
        clientCollection.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(RemoteOnlyTests.Client.TestAggregateFactory).Assembly);  // CLIENT assembly

        // Register SERVER factories (Full mode)
        serverCollection.AddNeatooRemoteFactory(
            NeatooFactory.Server,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(RemoteOnlyTests.Server.TestAggregateFactory).Assembly);  // SERVER assembly

        // Server-only services
        serverCollection.AddSingleton<ITestDataStore, TestDataStore>();

        // Wire client to call server via serialization
        clientCollection.AddScoped<ServerServiceProvider>();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        var clientProvider = clientCollection.BuildServiceProvider();
        var serverProvider = serverCollection.BuildServiceProvider();

        var clientScope = clientProvider.CreateScope();
        var serverScope = serverProvider.CreateScope();

        // Connect client to server
        clientScope.GetRequiredService<ServerServiceProvider>().serverProvider = serverScope.ServiceProvider;

        return (clientScope, serverScope);
    }
}
```

### Test Cases

**RemoteOnlyIntegrationTests.cs:**

```csharp
public class RemoteOnlyIntegrationTests
{
    // === Verify Client Assembly is Actually RemoteOnly ===

    [Fact]
    public void ClientAssembly_HasNoLocalCreateMethod()
    {
        // Verify the client factory doesn't have LocalCreate (only RemoteCreate)
        var clientFactoryType = typeof(RemoteOnlyTests.Client.TestAggregateFactory);
        Assert.Null(clientFactoryType.GetMethod("LocalCreate"));
        Assert.NotNull(clientFactoryType.GetMethod("RemoteCreate"));
    }

    [Fact]
    public void ClientAssembly_HasOnlyRemoteConstructor()
    {
        // Verify client factory has only one constructor (remote)
        var clientFactoryType = typeof(RemoteOnlyTests.Client.TestAggregateFactory);
        var constructors = clientFactoryType.GetConstructors();
        Assert.Single(constructors);
        Assert.Contains(constructors[0].GetParameters(), p => p.ParameterType == typeof(IMakeRemoteDelegateRequest));
    }

    [Fact]
    public void ServerAssembly_HasBothConstructors()
    {
        // Verify server factory has both constructors
        var serverFactoryType = typeof(RemoteOnlyTests.Server.TestAggregateFactory);
        var constructors = serverFactoryType.GetConstructors();
        Assert.Equal(2, constructors.Length);
    }

    // === Create Operations ===

    [Fact]
    public async Task Create_ClientToServer_InitializesAggregate()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();

        var aggregate = await factory.Create();

        Assert.NotNull(aggregate);
        Assert.NotEqual(Guid.Empty, aggregate.Id);
        Assert.NotNull(aggregate.Children);
        Assert.True(aggregate.IsNew);
    }

    [Fact]
    public async Task Create_ChildFactoryOnServer_InitializesChildList()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();

        var aggregate = await factory.Create();

        // Child list was created by server-side ITestChildListFactory
        Assert.NotNull(aggregate.Children);
        Assert.IsAssignableFrom<ITestChildList>(aggregate.Children);
    }

    // === Fetch Operations ===

    [Fact]
    public async Task Fetch_ClientToServer_LoadsAggregateWithChildren()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();
        var testId = Guid.NewGuid();

        var aggregate = await factory.Fetch(testId);

        Assert.NotNull(aggregate);
        Assert.Equal(testId, aggregate.Id);
        Assert.NotNull(aggregate.Children);
        Assert.True(aggregate.Children.Count > 0);
        Assert.False(aggregate.IsNew);
    }

    [Fact]
    public async Task Fetch_ChildPropertiesPreserved_ThroughSerialization()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();

        var aggregate = await factory.Fetch(Guid.NewGuid());

        Assert.All(aggregate.Children, child =>
        {
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
        });
    }

    // === Save Operations ===

    [Fact]
    public async Task Save_NewAggregate_InsertsCalled()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();
        var aggregate = await factory.Create();
        aggregate.Name = "Test";

        var saved = await factory.Save(aggregate);

        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_ExistingAggregate_UpdateCalled()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        aggregate.Name = "Updated";

        var saved = await factory.Save(aggregate);

        Assert.NotNull(saved);
        Assert.Equal("Updated", saved.Name);
    }

    // === Local Create for Child Entities ===

    [Fact]
    public void LocalCreate_Child_WorksOnClient()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestChildFactory>();

        var child = factory.Create();

        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
    }

    // === Child Modification Round-Trip ===

    [Fact]
    public async Task AddChild_OnClient_PreservedOnServer()
    {
        var (client, server) = RemoteOnlyContainers.Scopes();
        var aggregateFactory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestAggregateFactory>();
        var childFactory = client.ServiceProvider.GetRequiredService<RemoteOnlyTests.Client.ITestChildFactory>();

        var aggregate = await aggregateFactory.Create();
        var newChild = childFactory.Create();
        newChild.Name = "AddedOnClient";
        newChild.Value = 100m;
        aggregate.Children.Add(newChild);

        var saved = await aggregateFactory.Save(aggregate);

        Assert.Contains(saved.Children, c => c.Name == "AddedOnClient");
    }
}
```

### Implementation Steps

- [ ] **Step 1: Create folder structure**
  - [ ] `src/Tests/RemoteOnlyTests/RemoteOnlyTests.Domain/`
  - [ ] `src/Tests/RemoteOnlyTests/RemoteOnlyTests.Client/`
  - [ ] `src/Tests/RemoteOnlyTests/RemoteOnlyTests.Server/`
  - [ ] `src/Tests/RemoteOnlyTests/RemoteOnlyTests.Integration/`

- [ ] **Step 2: Create shared domain files**
  - [ ] `ITestAggregate.cs`, `ITestChild.cs`, `ITestChildList.cs` (interfaces)
  - [ ] `TestAggregate.cs` with `#if CLIENT` sections
  - [ ] `TestChild.cs` with local `[Create]` and server-only `[Fetch]`
  - [ ] `TestChildList.cs` with server-only `[Fetch]`

- [ ] **Step 3: Create client project**
  - [ ] `RemoteOnlyTests.Client.csproj` with `CLIENT` constant and linked files
  - [ ] `AssemblyAttributes.cs` with `[assembly: FactoryMode(RemoteOnly)]`
  - [ ] Verify it builds and generates only remote stubs

- [ ] **Step 4: Create server project**
  - [ ] `RemoteOnlyTests.Server.csproj` with linked files (no CLIENT constant)
  - [ ] `ServerServices.cs` with `ITestDataStore` in-memory implementation
  - [ ] Verify it builds and generates full factories

- [ ] **Step 5: Create integration test project**
  - [ ] `RemoteOnlyTests.Integration.csproj` referencing both client and server
  - [ ] `RemoteOnlyContainers.cs` with two-container setup
  - [ ] `RemoteOnlyIntegrationTests.cs` with test cases

- [ ] **Step 6: Add to solution and CI**
  - [ ] Add all projects to `Neatoo.RemoteFactory.sln`
  - [ ] Verify all tests pass on all target frameworks
  - [ ] Update this todo with completion status

### Key Differences from Existing Tests

| Aspect | Existing Tests | New RemoteOnly Tests |
|--------|----------------|----------------------|
| Client Factory Mode | Full | RemoteOnly |
| Server Factory Mode | Full | Full |
| LocalCreate generated? | Yes (both) | No (client), Yes (server) |
| Delegate registrations | Yes (both) | No (client), Yes (server) |
| Two separate assemblies | No (same code) | Yes (different generated code) |
| Verifies RemoteOnly feature | No | Yes |

---

## Goal

Enable minimal client assemblies that contain:
- Entity properties (for serialization/binding)
- Validation rules (for real-time feedback)
- Remote factory stubs (HTTP calls to server)

**Exclude from client:**
- Factory method implementations (Fetch/Insert/Update/Delete/Execute bodies)
- Database types and infrastructure
- Child factories

**Benefits:**
- Smaller client assembly size
- No business logic to reverse engineer
- No persistence code or schema hints on client

## Architecture

### Project Structure

```
/Domain/
  Order.cs                         # Single file with #if CLIENT / #else sections
  IOrder.cs                        # Interface (shared)

/Domain.Client/
  Domain.Client.csproj             # Defines CLIENT symbol
  [assembly: FactoryMode(FactoryMode.RemoteOnly)]
  # Links to Domain/*.cs files
  # Generator produces: remote stubs for factory interface

/Domain.Server/
  Domain.Server.csproj             # Defines SERVER symbol (or no symbol)
  # Links to Domain/*.cs files
  # Generator produces: full factory wiring
```

### What Goes Where

| Component | Always | Client (#if CLIENT) | Server (#else) |
|-----------|--------|---------------------|----------------|
| Entity properties | ✓ | | |
| Validation rules | ✓ | | |
| `[Create]` (simple entities) | ✓ | | |
| `[Remote]` method placeholders | | ✓ (throw) | |
| `[Remote]` method implementations | | | ✓ |
| `[Service]` parameters | | | ✓ |

## Key Design Decisions

### 1. Single File with Conditional Compilation

Use `#if CLIENT` / `#else` to include different code for client vs server in the same source file:

```csharp
[Factory]
internal partial class Order : IOrder
{
    // Shared - compiled by both client and server
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public IOrderLineList Lines { get; set; }
    // ... properties and validation

#if CLIENT
    // Client placeholder - generator sees method, produces factory interface
    // Throws if called directly (should always go through factory)
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Remote method - call through IOrderFactory");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Remote method - call through IOrderFactory");
    }

    [Remote, Insert]
    public Task Insert()
    {
        throw new InvalidOperationException("Remote method - call through IOrderFactory");
    }
#else
    // Server implementation with [Service] parameters
    [Remote, Create]
    public void Create(
        [Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        Lines = lineListFactory.Create();
    }

    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IOrderEntryContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == id);
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Lines = lineListFactory.Fetch(entity.Lines);
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IOrderEntryContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = new OrderEntity { Id = Id, CustomerName = CustomerName };
        lineListFactory.Insert(Lines, entity.Lines);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
    }
#endif
}
```

**Why this works:**
1. Generator sees `[Remote, Fetch]` on both client and server - generates `IOrderFactory.Fetch()`
2. Client factory implementation makes HTTP call (never calls the placeholder method)
3. Server factory implementation calls the real method with DI-injected services
4. If someone mistakenly calls `order.Fetch()` directly on client - clear error message
5. No generator changes needed for partial method support

### 2. Child Factories Only on Server

Child factories don't exist on client. Children are:
- Created by parent's `[Create]` on server
- Fetched as part of aggregate by parent's `[Fetch]` on server
- Serialized to client as part of aggregate

### 3. Client-Side Create for Simple Entities

Entities without children can have local `[Create]` in shared code (outside `#if`):

```csharp
// Shared - runs on both client and server
[Create]
public OrderLine()
{
    Id = Guid.NewGuid();
}
```

Aggregates with children need `[Remote, Create]` with server-only implementation:

```csharp
#if CLIENT
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Remote method - call through factory");
    }
#else
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        Lines = lineListFactory.Create();
    }
#endif
```

### 4. Rules on Both Client and Server

Validation rules run on both sides:
- **Client**: Real-time validation feedback as user types
- **Server**: Validation before persistence (can't trust client)

Rules are defined in the shared section (outside `#if`).

## Generator Changes Required

### New Assembly Attribute

```csharp
[assembly: FactoryMode(FactoryMode.RemoteOnly)]

public enum FactoryMode
{
    Full,       // Default - generate everything
    RemoteOnly  // Only generate remote stubs for factory interface
}
```

### Generator Logic Changes

1. **In RemoteOnly mode:**
   - Generate factory interface from `[Remote]` methods it finds
   - Generate factory implementation with remote HTTP stubs
   - Do NOT call the entity methods (they throw)

2. **In Full mode (server):**
   - Generate factory interface from `[Remote]` methods
   - Generate factory implementation that calls entity methods with DI-injected `[Service]` params

### Project Configuration

**Domain.Client.csproj:**
```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
</PropertyGroup>

<ItemGroup>
  <Compile Include="..\Domain\*.cs" Link="%(Filename)%(Extension)" />
</ItemGroup>
```

**Domain.Server.csproj:**
```xml
<ItemGroup>
  <Compile Include="..\Domain\*.cs" Link="%(Filename)%(Extension)" />
</ItemGroup>
```

## Tasks

### Generator Implementation
- [x] Add `FactoryMode` enum and `FactoryModeAttribute`
- [x] Update generator to check for assembly-level `FactoryMode`
- [x] In RemoteOnly mode, generate factory with remote HTTP stubs only
- [x] Unit tests added: `src/Tests/FactoryGeneratorTests/FactoryMode/FactoryModeTests.cs`

### OrderEntry Example Project
- [x] Create example project structure
  ```
  /Examples/OrderEntry/
  ├── OrderEntry.Domain/              # Shared source files
  ├── OrderEntry.Domain.Client/       # CLIENT constant, links Domain/
  ├── OrderEntry.Domain.Server/       # Links Domain/, refs EF
  ├── OrderEntry.Ef/                  # DbContext, entities
  ├── OrderEntry.Server/              # ASP.NET Core API
  └── OrderEntry.BlazorClient/        # Blazor WASM app
  ```
- [x] Create `OrderEntry.Domain/` shared source files
  - [x] `IOrder.cs` - aggregate root interface
  - [x] `Order.cs` - with `#if CLIENT` / `#else` sections
  - [x] `IOrderLine.cs` - child entity interface
  - [x] `OrderLine.cs` - simple entity with local `[Create]` and server-only `[Fetch]`
  - [x] `IOrderLineList.cs` - child collection interface
  - [x] `OrderLineList.cs` - child collection
- [x] Create `OrderEntry.Ef/` project
  - [x] `OrderEntryContext.cs` - DbContext with `IOrderEntryContext` interface
  - [x] `OrderEntity.cs` - EF entity
  - [x] `OrderLineEntity.cs` - EF child entity
  - [ ] Migrations
- [x] Create `OrderEntry.Domain.Client/` project
  - [x] Define `CLIENT` constant
  - [x] Add `[assembly: FactoryMode(RemoteOnly)]`
  - [x] Link to `OrderEntry.Domain/*.cs` files
  - [x] Reference only RemoteFactory (no EF dependency)
- [x] Create `OrderEntry.Domain.Server/` project
  - [x] Link to `OrderEntry.Domain/*.cs` files
  - [x] Reference `OrderEntry.Ef` for `IOrderEntryContext`
- [x] Create `OrderEntry.Server/` ASP.NET Core project
  - [x] Reference `OrderEntry.Domain.Server`
  - [x] Reference `OrderEntry.Ef`
  - [x] Configure RemoteFactory endpoints
  - [x] Register DbContext and services
- [x] Create `OrderEntry.BlazorClient/` Blazor WASM project
  - [x] Reference `OrderEntry.Domain.Client`
  - [x] Simple UI: create order, add lines (save/fetch need server running)
  - [x] Verify NO EF types in compiled assembly
- [ ] Verify end-to-end flow
  - [ ] Client creates order → HTTP → server creates with child factory
  - [ ] Client fetches order → HTTP → server loads from EF with children
  - [ ] Client saves order → HTTP → server persists to EF
  - [ ] Validation runs on both client and server

### Unit Tests for Generator (FactoryGeneratorTests)
Tests for `FactoryMode` attribute handling in the generator:
- [x] Test: `FactoryModeAttribute` detected at assembly level
- [x] Test: `FactoryMode.Full` (default) generates full factory with local methods
- [x] Test: `FactoryMode.RemoteOnly` generates factory with remote stubs only
- [x] Test: RemoteOnly mode does NOT generate local method calls
- [x] Test: RemoteOnly mode generates correct delegate types for remote calls
- [x] Test: Factory interface identical in both modes (same method signatures)
- [x] Test: `[Service]` parameters stripped from factory interface in both modes
- [x] Test: Non-`[Remote]` methods (like local `[Create]`) still work in RemoteOnly mode
- [ ] Test: Diagnostic emitted if `[Remote]` method missing in RemoteOnly mode (optional)

### Serialization Round-Trip Tests (Two DI Container Pattern)
Tests using `ClientServerContainers.Scopes()` to validate full remote operation flow:
File: `src/Tests/FactoryGeneratorTests/Factory/AggregateSerializationTests.cs`
- [x] Test: Create aggregate on server, serialize to client, deserialize maintains state
- [x] Test: Fetch aggregate with children, round-trip preserves parent-child relationships
- [x] Test: Client modifies entity, sends to server, server persists correctly
- [x] Test: Insert new aggregate with children, round-trip through containers
- [x] Test: Update existing aggregate, modified properties serialize correctly
- [x] Test: Delete aggregate, round-trip handles IsDeleted flag
- [ ] Test: Validation errors serialize from server to client
- [x] Test: Child collection modifications (add/remove/update) round-trip correctly
- [x] Test: Factory interface calls work identically from client and server containers

### Integration Tests (OrderEntry Example)
End-to-end tests for the OrderEntry example:
File: `src/Tests/FactoryGeneratorTests/ClientServerSeparation/ClientServerSeparationTests.cs`
- [x] Test: Local `[Create]` for simple child entity (works on client, server, local)
- [x] Test: Client assembly has no EF type references (reflection check)
- [x] Test: Calling placeholder method directly throws `InvalidOperationException`
- [x] Test: Factory interface doesn't expose [Service] parameters
- [x] Test: Remote factory routes to server correctly

The following tests require running OrderEntry with actual database (E2E tests):
- [ ] Test: Aggregate with children (Order → OrderLines)
- [ ] Test: `[Remote, Create]` for aggregate (needs child factory)
- [ ] Test: Fetch loads aggregate with children from EF
- [ ] Test: Insert persists new aggregate with children to EF
- [ ] Test: Update persists modified aggregate to EF
- [ ] Test: Delete removes aggregate from EF
- [ ] Test: Validation rules execute on both client and server

### Documentation
Update `docs/` with client-server separation guidance:

#### Conceptual Guide (`docs/concepts/client-server-separation.md`)
- [x] When and why to use client-server separation
- [x] Benefits: smaller client, no business logic exposure, no EF on client
- [x] Trade-offs: more project complexity, conditional compilation
- [x] Comparison with single-project approach (current Person example)
- [x] Decision tree: when to use which approach

#### Setup Guide (`docs/getting-started/client-server-setup.md`)
- [x] Step-by-step project structure creation
- [x] Configuring `CLIENT` constant in csproj
- [x] Linking source files between projects
- [x] Setting up `[assembly: FactoryMode(RemoteOnly)]`
- [x] Configuring server project with EF references
- [x] Wiring up ASP.NET Core and Blazor projects

#### Reference Updates
- [x] Update `docs/reference/attributes.md` with `FactoryModeAttribute`
- [x] Add OrderEntry example to `docs/examples/client-server-separation.md`

## Open Questions

1. **Linked files vs shared project (.shproj)?**
   - Linked files: explicit, works everywhere
   - .shproj: cleaner but older technology
   - Recommendation: linked files for simplicity

2. **How to handle Execute commands?**
   - Same pattern: placeholder in `#if CLIENT`, implementation in `#else`
   - Static partial classes work the same way

3. **NuGet distribution?**
   - Client NuGet: includes client-compiled assembly
   - Server NuGet: includes server-compiled assembly
   - Both reference same source files, different compilation

## Deferred Documentation Tasks

#### Migration Guide (`docs/concepts/migrating-to-client-server.md`)
- [ ] Migrating existing single-project domain to client-server split
- [ ] Identifying which methods need `#if CLIENT` / `#else`
- [ ] Moving EF references to server-only sections
- [ ] Testing migration: verifying client has no EF types
- [ ] Common pitfalls and how to avoid them

#### Additional Reference Updates
- [ ] Update `docs/reference/generated-code.md` with RemoteOnly mode output
