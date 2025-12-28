# RemoteFactory Generator Testing Approach

## Executive Summary

The FactoryGeneratorTests project employs a **behavioral testing approach** rather than snapshot/golden-file testing. This methodology tests the generated code by actually executing it, providing confidence that:

1. The generator produces syntactically correct code (compilation passes)
2. The generated code behaves correctly at runtime
3. The factory pattern integrates properly with dependency injection
4. Remote execution through serialization works correctly

This document explains the philosophy, patterns, infrastructure, and benefits of this approach.

---

## Philosophy: Test What Matters

### Why Not Snapshot Testing?

Snapshot testing (comparing generated code against static text files) has several drawbacks:

| Snapshot Testing Drawbacks | Behavioral Testing Advantages |
|---------------------------|-------------------------------|
| Fails on formatting changes | Immune to whitespace/formatting |
| Doesn't verify compilation | **Compilation is mandatory** for tests to run |
| Doesn't verify runtime behavior | Tests actual execution paths |
| Requires maintaining snapshots | Self-maintaining through code |
| Brittle to refactoring | Robust to implementation changes |
| Tests "what" not "does it work" | Tests "does it work" |

### The Behavioral Testing Principle

```
Generated Code --> Compiles --> Runs --> Assertions Pass
                      ^
                      |
               If this fails, the test project won't build
```

The genius of this approach: **if the generator produces invalid code, the test project won't compile**. This is a fail-fast mechanism that catches problems before any test even runs.

### Two-Phase Verification

**Phase 1: Compile-Time Verification**
- The test project references the generator as an analyzer
- Generated code is emitted to the `Generated/` folder
- Build failures indicate generator bugs producing invalid C#

**Phase 2: Runtime Verification**
- Unit tests exercise the generated factories
- Verify correct behavior across all execution modes
- Test authorization, serialization, dependency injection integration

---

## Test Infrastructure

### Project Configuration

```xml
<!-- FactoryGeneratorTests.csproj -->
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<ItemGroup>
    <!-- Reference generator as analyzer - runs during compilation -->
    <ProjectReference Include="..\..\RemoteFactory.FactoryGenerator\RemoteFactory.FactoryGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <!-- Exclude generated files from compilation (they're compiled by source generator) -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

### Three-Tier Container Simulation

The `ClientServerContainers` class creates three separate DI containers to simulate real-world deployment:

```
+------------------+     +------------------+     +------------------+
|  Client/Remote   |     |     Server       |     |      Local       |
|    Container     |     |    Container     |     |    Container     |
+------------------+     +------------------+     +------------------+
|                  |     |                  |     |                  |
| - Factory proxies|     | - Full factories |     | - Full factories |
| - Remote delegate|     | - Server services|     | - All services   |
| - Serialization  |     | - Auth handlers  |     | - No remoting    |
|                  |     |                  |     |                  |
+--------+---------+     +--------+---------+     +------------------+
         |                        |
         |    JSON Serialization  |
         +------------------------+
            Simulated HTTP Call
```

#### Container Setup

```csharp
internal static class ClientServerContainers
{
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
    {
        // Server: Full factory with server-only services
        serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, Assembly.GetExecutingAssembly());
        serverCollection.AddSingleton<IServerOnlyService, ServerOnly>();

        // Client: Remote factory proxies with serialization delegate
        clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, Assembly.GetExecutingAssembly());
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        // Local: Full factory for in-process testing
        localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, Assembly.GetExecutingAssembly());
    }
}
```

### Remote Call Simulation

The `MakeSerializedServerStandinDelegateRequest` class simulates actual remote calls:

```csharp
internal sealed class MakeSerializedServerStandinDelegateRequest : IMakeRemoteDelegateRequest
{
    public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
    {
        // 1. Serialize request (like HTTP client would)
        var remoteRequest = NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);
        var json = JsonSerializer.Serialize(remoteRequest);

        // 2. Deserialize on "server" (like ASP.NET Core would)
        var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

        // 3. Execute using server's container
        var remoteResponseOnServer = await serverProvider
            .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer);

        // 4. Serialize response and return
        json = JsonSerializer.Serialize(remoteResponseOnServer);
        var result = JsonSerializer.Deserialize<RemoteResponseDto>(json);
        return NeatooJsonSerializer.DeserializeRemoteResponse<T>(result!);
    }
}
```

This tests the complete serialization round-trip without needing actual HTTP infrastructure.

### Generic Test Base Class

```csharp
public abstract class FactoryTestBase<TFactory> where TFactory : notnull
{
    protected IServiceScope clientScope;
    protected IServiceScope serverScope;
    protected TFactory factory;

    public FactoryTestBase()
    {
        var scopes = ClientServerContainers.Scopes();
        this.clientScope = scopes.client;
        this.serverScope = scopes.server;
        this.factory = this.clientScope.ServiceProvider.GetRequiredService<TFactory>();
    }
}
```

Benefits:
- Consistent test setup across all test classes
- Automatic factory resolution from client scope
- Access to both client and server scopes for verification

---

## Test Organization

```
FactoryGeneratorTests/
+-- Factory/                      # Core factory operation tests
|   +-- ReadTests.cs              # Create/Fetch operations
|   +-- WriteTests.cs             # Insert/Update/Delete operations
|   +-- RemoteReadTests.cs        # Remote Create/Fetch
|   +-- RemoteWriteTests.cs       # Remote Insert/Update/Delete
|   +-- ReadAuthTests.cs          # Local authorization
|   +-- ReadRemoteAuthTests.cs    # Remote authorization
|   +-- ExecuteTests.cs           # Static Execute methods
|   +-- StaticFactoryMethodTests.cs
|   +-- ConstructorCreateTests.cs
|   +-- NullableParameterTests.cs
|   +-- MixedWriteTests.cs        # Mixed local/remote operations
|   +-- FactoryCoreTests.cs       # FactoryCore extensibility
|   +-- FactoryOnStartCompleteTests.cs  # Lifecycle hooks
|
+-- Mapper/                       # Mapper generator tests
|   +-- MapperTests.cs            # Basic MapTo/MapFrom
|   +-- MapperEnumTests.cs        # Enum type mapping
|   +-- MapperIgnoreAttribute.cs  # [MapperIgnore] attribute
|   +-- MapperNullableBang.cs     # Nullable handling
|   +-- MapperAbstractGenericTests.cs
|   +-- PersonMapperTests.cs
|
+-- InterfaceFactory/             # Interface-based factories
|   +-- InterfaceFactoryTests.cs  # [Factory] on interface
|
+-- Showcase/                     # Complete end-to-end scenarios
|   +-- ShowcaseRead.cs           # Comprehensive read scenarios
|   +-- ShowcaseAuth.cs           # Authorization scenarios
|   +-- ShowcaseAuthRemote.cs     # Remote authorization
|   +-- ShowcaseSave.cs           # Save operation scenarios
|   +-- ShowcasePerformance.cs    # Performance comparison
|
+-- SpecificScenarios/            # Edge cases and bug fixes
|   +-- SaveWNoDeleteIsNotNullableTests.cs
|   +-- SaveNotNullableNoDelete.cs
|   +-- IgnoreWriteMethodReturn.cs
|   +-- HasBaseClassFactoryAttributeTests.cs
|   +-- BugNoCanCreateFetch.cs
|
+-- Shared/                       # Shared test dependencies
|   +-- Service.cs                # IService test dependency
|   +-- ServerOnlyService.cs      # Server-only service
|
+-- Generated/                    # Generator output (auto-created)
    +-- ...Factory.g.cs           # Generated factory code
    +-- ...Mapper.g.cs            # Generated mapper code
```

---

## Testing Patterns

### Pattern 1: Self-Verifying Domain Objects

Domain objects include properties that track which factory methods were called:

```csharp
[Factory]
public class ReadObject
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }

    [Create]
    public void CreateVoid()
    {
        this.CreateCalled = true;  // Self-tracking
    }

    [Fetch]
    public void FetchVoid()
    {
        this.FetchCalled = true;  // Self-tracking
    }
}
```

Test verification:

```csharp
[Fact]
public void Test_Create_SetsFlag()
{
    var obj = factory.CreateVoid();
    Assert.True(obj.CreateCalled);  // Verify method was called
}
```

### Pattern 2: Exhaustive Method Iteration via Reflection

For testing all generated factory method variations:

```csharp
[Fact]
public async Task TestAllReadMethods()
{
    var factory = scope.GetRequiredService<IReadObjectFactory>();

    // Get all Create and Fetch methods
    var methods = factory.GetType().GetMethods()
        .Where(m => m.Name.Contains("Create") || m.Name.Contains("Fetch"))
        .ToList();

    foreach (var method in methods)
    {
        object? result;
        var methodName = method.Name;

        // Invoke with appropriate parameters
        if (method.GetParameters().Any())
            result = method.Invoke(factory, new object[] { 1 });
        else
            result = method.Invoke(factory, null);

        // Handle async methods
        if (result is Task<ReadObject?> task)
        {
            Assert.Contains("Task", methodName);
            var obj = await task;
            if (!methodName.Contains("False"))
                Assert.NotNull(obj);
        }
        // Handle sync methods
        else if (result is ReadObject obj)
        {
            Assert.NotNull(obj);
            Assert.True(obj.CreateCalled || obj.FetchCalled);
        }
    }
}
```

Benefits:
- Automatically tests all method overloads
- Method naming convention drives expected behavior
- Single test covers many generated methods

### Pattern 3: Multi-Container Execution

Test the same factory across different execution modes:

```csharp
[Fact]
public Task ReadFactoryTest_Client()
{
    var factory = clientScope.ServiceProvider.GetRequiredService<IReadObjectFactory>();
    return ReadFactory(factory);  // Exercises remote path
}

[Fact]
public Task ReadFactoryTest_Local()
{
    var factory = localScope.ServiceProvider.GetRequiredService<IReadObjectFactory>();
    return ReadFactory(factory);  // Exercises local path
}

private async Task ReadFactory(IReadObjectFactory factory)
{
    // Common test logic for both scenarios
    var result = await factory.CreateTask();
    Assert.NotNull(result);
}
```

### Pattern 4: Theory-Based Parameterized Tests

Using xUnit theories for comprehensive coverage:

```csharp
public static IEnumerable<object[]> RemoteWriteFactoryTest_Client()
{
    var scopes = ClientServerContainers.Scopes();
    var factory = scopes.client.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();

    foreach (var method in factory.GetType().GetMethods().Where(m => m.Name.StartsWith("Save")))
    {
        yield return new object[] { method, factory };
    }
}

[Theory]
[MemberData(nameof(RemoteWriteFactoryTest_Client))]
[MemberData(nameof(RemoteWriteFactoryTest_Local))]
public async Task RemoteWrite(MethodInfo method, IRemoteWriteObjectFactory factory)
{
    // Test each Save method variant
}
```

### Pattern 5: Authorization Testing

Comprehensive authorization flow testing:

```csharp
public class ReadAuth
{
    public int CanReadCalled { get; set; }

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanReadBool()
    {
        CanReadCalled++;  // Track call count
        return true;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanReadBoolFalse(int? p)
    {
        CanReadCalled++;
        return p != 10;  // Conditionally deny
    }
}

[Fact]
public async Task ReadAuthBoolFailTest()
{
    var factory = scope.GetRequiredService<IReadAuthObjectFactory>();
    var auth = scope.GetRequiredService<ReadAuth>();

    // Test with parameter that triggers denial
    var result = await factory.Fetch(10);

    Assert.Null(result);  // Denied
    Assert.True(auth.CanReadCalled > 0);  // Auth was checked
}
```

### Pattern 6: Service Injection Verification

Verifying dependency injection works in generated code:

```csharp
[Factory]
public class ReadObject
{
    [Create]
    public void CreateWithService([Service] IService service)
    {
        Assert.NotNull(service);  // Fails if DI doesn't work
        this.CreateCalled = true;
    }
}
```

### Pattern 7: Compilation-Only Tests

Some tests verify code generates and compiles without needing runtime behavior:

```csharp
[Fact]
public void DerivedClass_ShouldHaveFactoryGeneratedCode()
{
    // Just ensure it compiles - type exists
    DerivedClassFactory? factory = null;
    Assert.Null(factory);  // Placeholder assertion
}
```

---

## What Gets Tested

### Factory Generator Coverage

| Feature | Test File(s) |
|---------|-------------|
| Create/Fetch operations | `ReadTests.cs`, `ShowcaseRead.cs` |
| Insert/Update/Delete | `WriteTests.cs`, `ShowcaseSave.cs` |
| Remote operations | `RemoteReadTests.cs`, `RemoteWriteTests.cs` |
| Authorization (local) | `ReadAuthTests.cs`, `ShowcaseAuth.cs` |
| Authorization (remote) | `ReadRemoteAuthTests.cs`, `ShowcaseAuthRemote.cs` |
| Static factory methods | `StaticFactoryMethodTests.cs` |
| Static Execute delegates | `ExecuteTests.cs` |
| Constructor factories | `ConstructorCreateTests.cs` |
| Nullable parameters | `NullableParameterTests.cs` |
| Mixed local/remote | `MixedWriteTests.cs` |
| IFactorySaveMeta | `WriteTests.cs`, `ShowcaseSave.cs` |
| FactoryCore hooks | `FactoryCoreTests.cs` |
| Lifecycle interfaces | `FactoryOnStartCompleteTests.cs` |
| Interface factories | `InterfaceFactoryTests.cs` |
| Return type handling | `IgnoreWriteMethodReturn.cs` |
| Inheritance chains | `HasBaseClassFactoryAttributeTests.cs` |
| Non-nullable Save | `SaveWNoDeleteIsNotNullableTests.cs` |

### Mapper Generator Coverage

| Feature | Test File(s) |
|---------|-------------|
| Basic MapTo/MapFrom | `MapperTests.cs` |
| Enum mapping | `MapperEnumTests.cs` |
| MapperIgnore attribute | `MapperIgnoreAttribute.cs` |
| Nullable handling | `MapperNullableBang.cs` |
| Abstract generic classes | `MapperAbstractGenericTests.cs` |

### Execution Modes Tested

- **Local Mode**: Direct in-process execution
- **Remote Mode**: Client-to-server with serialization
- **Server Mode**: Server-side handling of remote requests

---

## Benefits Summary

### 1. Compilation as First-Class Test
Invalid generated code = build failure = immediate feedback

### 2. Runtime Behavior Verification
Tests actual execution, not just text output

### 3. Serialization Testing
Full round-trip JSON serialization tested automatically

### 4. DI Integration Testing
Generated code properly resolves dependencies

### 5. Authorization Flow Testing
Complete auth pipeline verified across all scenarios

### 6. Maintainability
No snapshot files to maintain or update

### 7. Refactoring Safety
Implementation changes don't break tests if behavior is preserved

### 8. Real-World Simulation
Three-container setup mirrors actual deployment topology

---

## Adding New Tests

### For New Factory Features

1. Create a domain class with `[Factory]` attribute
2. Add factory methods with appropriate attributes
3. Include self-verification properties if needed
4. Create test class, optionally extending `FactoryTestBase<TFactory>`
5. Write tests that exercise generated factory methods
6. Run tests - if generator has bugs, build will fail first

### Example: Testing a New Feature

```csharp
// Domain object with new feature
[Factory]
public class NewFeatureObject
{
    public bool NewFeatureCalled { get; set; }

    [NewFeatureAttribute]
    public void DoNewThing()
    {
        NewFeatureCalled = true;
    }
}

// Test class
public class NewFeatureTests : FactoryTestBase<INewFeatureObjectFactory>
{
    [Fact]
    public void NewFeature_ShouldWork()
    {
        var result = factory.DoNewThing();
        Assert.True(result.NewFeatureCalled);
    }
}
```

---

## Conclusion

This behavioral testing approach provides strong guarantees about generator correctness while remaining maintainable and expressive. By testing what the generated code **does** rather than what it **looks like**, the test suite is robust against formatting changes while catching real behavioral bugs.

The three-container setup and serialization simulation ensure that the complete factory pipeline works correctly, from client request through remote execution to server-side handling - all without requiring actual HTTP infrastructure.
