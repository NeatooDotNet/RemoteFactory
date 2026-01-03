# Logging Implementation Plan for Neatoo.RemoteFactory

## Executive Summary

This document provides a comprehensive plan for adding structured logging to the Neatoo.RemoteFactory library. The implementation follows established patterns from popular .NET libraries while addressing the unique challenges of a source generator-powered 3-tier architecture.

---

## 1. Research: Industry Standard Patterns

### 1.1 How Popular .NET Libraries Handle Logging

#### Entity Framework Core
- Uses `Microsoft.Extensions.Logging.Abstractions` exclusively
- Defines event IDs and categories per subsystem (e.g., `CoreEventId`, `RelationalEventId`)
- Uses `LoggerMessage.Define` for high-performance logging
- Provides dedicated logger categories: `Microsoft.EntityFrameworkCore.Database.Command`, etc.
- Logging is opt-in and silent by default
- Consumers configure via `DbContextOptionsBuilder.LogTo()` or standard DI

#### Polly
- Uses `Microsoft.Extensions.Logging.Abstractions`
- Injects `ILogger<T>` through DI
- Falls back to `NullLogger<T>.Instance` when no logger provided
- Logs policy execution details at Debug/Information levels
- Errors logged at Error level with exception details

#### MediatR
- Uses `Microsoft.Extensions.Logging.Abstractions`
- Pipeline behaviors accept optional `ILogger<T>`
- Silent when no logger configured
- Logs request/response timing at Debug level

#### System.Text.Json
- No external logging (performance-critical)
- Throws exceptions on errors
- Relies on caller to log

#### Key Patterns Observed
1. **Abstractions Only**: Libraries depend on `Microsoft.Extensions.Logging.Abstractions`, not concrete implementations
2. **NullLogger Fallback**: When no logger is configured, use `NullLogger<T>.Instance`
3. **Opt-in Logging**: Logging should be silent by default
4. **High-Performance Logging**: Use `LoggerMessage.Define` for hot paths
5. **Event IDs**: Use structured event IDs for filtering and analysis
6. **Logger Categories**: Use meaningful namespaces as categories

### 1.2 Recommendation for Neatoo.RemoteFactory

Follow the EF Core pattern with some MediatR influences:
- Use `Microsoft.Extensions.Logging.Abstractions`
- Define event IDs for each major operation
- Use `LoggerMessage.Define` for performance
- Provide NullLogger fallback for static methods
- Log through DI-injected loggers where possible

Response: Agreed.

---

## 2. Logging Levels and What to Log

### 2.1 Trace (Most Verbose)
**Use For**: Detailed internal flow for deep debugging

| Area | What to Log |
|------|-------------|
| Serialization | Property-by-property serialization details |
| Converters | Converter selection process, cache hits/misses |
| Factory Core | Method entry/exit with parameter counts |
| Reference Resolution | $id and $ref handling |

**Example**:
```
TRACE: NeatooOrdinalConverter<Person>.Write - Writing 5 properties to array
TRACE: NeatooJsonSerializer.Serialize - Using ordinal format for type Person
```

### 2.2 Debug (Verbose)
**Use For**: Information useful during development/testing

| Area | What to Log |
|------|-------------|
| Factory Operations | Factory method invocation with operation type |
| Serialization | Full JSON output (truncated for large objects) |
| Converter Factory | Converter creation path (AOT, provider, reflection) |
| Remote Requests | Request/response correlation, timing |
| Type Resolution | Type lookup results from ServiceAssemblies |

**Example**:
```
DEBUG: PersonFactory.Create called, operation=Create, parameters=2
DEBUG: NeatooOrdinalConverterFactory.CreateConverter - Using registered converter for Person (AOT path)
DEBUG: MakeRemoteDelegateRequest.ForDelegate - Request completed in 45ms
```

### 2.3 Information (Normal Operations)
**Use For**: High-level significant events

| Area | What to Log |
|------|-------------|
| Factory Operations | Successful factory method completions |
| Remote Calls | Remote delegate invocations (without parameter details) |
| Serialization Format | Format being used (Ordinal/Named) |
| Service Registration | Factory service registrations at startup |

**Example**:
```
INFO: Remote factory call completed: PersonFactory.FetchById (remote)
INFO: NeatooRemoteFactory services registered with Ordinal serialization format
INFO: Registered 12 factory services from assembly MyApp.Domain
```

### 2.4 Warning (Potential Issues)
**Use For**: Recoverable issues that might indicate problems

| Area | What to Log |
|------|-------------|
| Serialization | Fallback to reflection when AOT converter not available |
| Type Resolution | Type not found in registered assemblies (before throwing) |
| Converter Cache | Fallback options being used |
| Authorization | Authorization checks that return false (not exceptions) |

**Example**:
```
WARN: NeatooOrdinalConverterFactory - No registered converter for LegacyType, falling back to reflection
WARN: ServiceAssemblies.FindType - Type 'MyApp.OldType' not found in 3 registered assemblies
```

### 2.5 Error (Failures)
**Use For**: Operation failures that need attention

| Area | What to Log |
|------|-------------|
| Serialization | Deserialization failures with type and JSON excerpt |
| Remote Calls | HTTP failures with status code and error |
| Factory Operations | Exceptions during factory method execution |
| Delegate Resolution | Missing delegate type |
| Authorization | Authorization failures (before exception) |

**Example**:
```
ERROR: NeatooJsonSerializer.Deserialize - Failed to deserialize type Person: "Expected StartArray, got StartObject"
ERROR: MakeRemoteDelegateRequestHttpCall - HTTP 500: Internal Server Error for delegate PersonFactory+FetchDelegate
```

---

## 3. DI vs Non-DI Scenarios

### 3.1 Components That Can Use DI (ILogger<T> Injection)

| Class | DI Approach |
|-------|-------------|
| `NeatooJsonSerializer` | Inject `ILogger<NeatooJsonSerializer>` |
| `MakeRemoteDelegateRequest` | Inject `ILogger<MakeRemoteDelegateRequest>` |
| `MakeLocalSerializedDelegateRequest` | Inject `ILogger<MakeLocalSerializedDelegateRequest>` |
| `FactoryCore<T>` | Inject `ILogger<FactoryCore<T>>` |
| `NeatooOrdinalConverterFactory` (instance) | Inject via constructor (requires DI registration) |
| Generated Factories | Source generator emits logger parameter |

### 3.2 Static Components (Special Handling Required)

| Component | Challenge | Solution |
|-----------|-----------|----------|
| `NeatooOrdinalConverterFactory.RegisterConverter<T>()` | Static method, no DI | Use ambient logger or skip logging |
| `LocalServer.HandlePortalRequest()` | Static factory returning delegate | Pass logger through closure |
| `MakeRemoteDelegateRequestHttpCallImplementation.Create()` | Static factory | Accept optional logger parameter |
| Source Generator Output | Compile-time generation | Emit logger injection in generated code |

### 3.3 Proposed Hybrid Approach

#### Option A: Ambient Logger (Recommended for Static Methods)
Create an internal logger holder that can be set during startup:

```csharp
// Internal/NeatooLogging.cs
namespace Neatoo.RemoteFactory.Internal;

internal static class NeatooLogging
{
    private static ILoggerFactory? _loggerFactory;

    internal static void SetLoggerFactory(ILoggerFactory? factory)
    {
        _loggerFactory = factory;
    }

    internal static ILogger<T> GetLogger<T>()
    {
        return _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    internal static ILogger GetLogger(string categoryName)
    {
        return _loggerFactory?.CreateLogger(categoryName) ?? NullLoggerFactory.Instance.CreateLogger(categoryName);
    }
}
```

#### Option B: Optional Logger Parameters (Clean but Verbose)
Add optional logger parameters to static factory methods:

```csharp
public static MakeRemoteDelegateRequestHttpCall Create(
    HttpClient httpClient,
    ILogger<MakeRemoteDelegateRequestHttpCall>? logger = null)
{
    logger ??= NullLogger<MakeRemoteDelegateRequestHttpCall>.Instance;
    // ...
}
```

**Recommendation**: Use Option A for internal static methods, Option B for public APIs.

Response: Agreed

---

## 4. Implementation Approaches

### 4.1 High-Performance Logging with LoggerMessage.Define

Create a dedicated class for log message definitions:

```csharp
// Internal/Log.cs
namespace Neatoo.RemoteFactory.Internal;

internal static partial class Log
{
    // ===== Serialization (1xxx) =====

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Serializing object of type {TypeName} using {Format} format")]
    public static partial void SerializingObject(
        this ILogger logger,
        string typeName,
        SerializationFormat format);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Deserialized object of type {TypeName} in {ElapsedMs}ms")]
    public static partial void DeserializedObject(
        this ILogger logger,
        string typeName,
        long elapsedMs);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Using reflection fallback for ordinal converter: {TypeName}")]
    public static partial void ReflectionFallback(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Serialization failed for type {TypeName}: {ErrorMessage}")]
    public static partial void SerializationFailed(
        this ILogger logger,
        string typeName,
        string errorMessage,
        Exception? exception);

    // ===== Factory Operations (2xxx) =====

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Factory operation {Operation} started for {TypeName}")]
    public static partial void FactoryOperationStarted(
        this ILogger logger,
        FactoryOperation operation,
        string typeName);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Factory operation {Operation} completed for {TypeName} in {ElapsedMs}ms")]
    public static partial void FactoryOperationCompleted(
        this ILogger logger,
        FactoryOperation operation,
        string typeName,
        long elapsedMs);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Invoking IFactoryOnStart for {TypeName}")]
    public static partial void InvokingFactoryOnStart(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "Invoking IFactoryOnComplete for {TypeName}")]
    public static partial void InvokingFactoryOnComplete(
        this ILogger logger,
        string typeName);

    // ===== Remote Calls (3xxx) =====

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Remote delegate call started: {DelegateType}")]
    public static partial void RemoteCallStarted(
        this ILogger logger,
        string delegateType);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Remote delegate call completed: {DelegateType} in {ElapsedMs}ms")]
    public static partial void RemoteCallCompleted(
        this ILogger logger,
        string delegateType,
        long elapsedMs);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "Remote delegate call failed: {DelegateType}, HTTP {StatusCode}")]
    public static partial void RemoteCallFailed(
        this ILogger logger,
        string delegateType,
        int statusCode,
        Exception? exception);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Debug,
        Message = "Remote request serialized: {ByteCount} bytes")]
    public static partial void RemoteRequestSerialized(
        this ILogger logger,
        int byteCount);

    // ===== Converter Factory (4xxx) =====

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Trace,
        Message = "Converter cache hit for type {TypeName}")]
    public static partial void ConverterCacheHit(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "Creating converter for type {TypeName} via {CreationPath}")]
    public static partial void CreatingConverter(
        this ILogger logger,
        string typeName,
        string creationPath);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Registered converter count: {Count}")]
    public static partial void ConverterRegistrationCount(
        this ILogger logger,
        int count);

    // ===== Authorization (5xxx) =====

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Debug,
        Message = "Authorization check for operation {Operation} on {TypeName}")]
    public static partial void AuthorizationCheck(
        this ILogger logger,
        string operation,
        string typeName);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "Authorization denied for operation {Operation} on {TypeName}")]
    public static partial void AuthorizationDenied(
        this ILogger logger,
        string operation,
        string typeName);

    // ===== Service Registration (6xxx) =====

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Registering Neatoo RemoteFactory services: Mode={Mode}, Format={Format}")]
    public static partial void RegisteringServices(
        this ILogger logger,
        NeatooFactory mode,
        SerializationFormat format);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Debug,
        Message = "Registered factory from assembly {AssemblyName}: {FactoryCount} factories")]
    public static partial void RegisteredFactories(
        this ILogger logger,
        string assemblyName,
        int factoryCount);

    // ===== Server-Side Request Handling (7xxx) =====

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "Handling remote request for delegate {DelegateType}")]
    public static partial void HandlingRemoteRequest(
        this ILogger logger,
        string delegateType);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Debug,
        Message = "Remote request deserialized with {ParameterCount} parameters")]
    public static partial void RemoteRequestDeserialized(
        this ILogger logger,
        int parameterCount);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Error,
        Message = "Delegate type not found: {DelegateType}")]
    public static partial void DelegateTypeNotFound(
        this ILogger logger,
        string delegateType);
}
```

### 4.2 Event ID Ranges

| Range | Category |
|-------|----------|
| 1000-1999 | Serialization |
| 2000-2999 | Factory Operations |
| 3000-3999 | Remote Calls (Client) |
| 4000-4999 | Converter Factory |
| 5000-5999 | Authorization |
| 6000-6999 | Service Registration |
| 7000-7999 | Server-Side Request Handling |

### 4.3 Logger Categories (Namespaces)

| Category | Description |
|----------|-------------|
| `Neatoo.RemoteFactory` | General library logs |
| `Neatoo.RemoteFactory.Serialization` | Serialization operations |
| `Neatoo.RemoteFactory.Factory` | Factory operations |
| `Neatoo.RemoteFactory.Remote` | Remote call operations |
| `Neatoo.RemoteFactory.Authorization` | Authorization checks |

---

## 5. Consumer Experience

### 5.1 Enabling Logging

#### Via appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Neatoo.RemoteFactory": "Debug",
      "Neatoo.RemoteFactory.Serialization": "Information"
    }
  }
}
```

#### Via Code (Startup)
```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
});
```

### 5.2 Configuring Logger Factory for Static Methods

Add extension method to service registration:

```csharp
// In AddRemoteFactoryServices.cs
public static IServiceCollection AddNeatooRemoteFactory(
    this IServiceCollection services,
    NeatooFactory remoteLocal,
    NeatooSerializationOptions serializationOptions,
    params Assembly[] assemblies)
{
    // ... existing code ...

    // Configure logging support for static methods
    services.AddSingleton<IStartupFilter, NeatooLoggingStartupFilter>();

    return services;
}

internal class NeatooLoggingStartupFilter : IStartupFilter
{
    private readonly ILoggerFactory _loggerFactory;

    public NeatooLoggingStartupFilter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        NeatooLogging.SetLoggerFactory(_loggerFactory);
        return next;
    }
}
```

**Note**: For non-ASP.NET Core apps, provide an explicit configuration method:

```csharp
// Public API for non-web apps
public static void ConfigureLogging(ILoggerFactory loggerFactory)
{
    NeatooLogging.SetLoggerFactory(loggerFactory);
}
```

### 5.3 Logging Is Opt-In

- By default, no logs are emitted
- All loggers fall back to `NullLogger<T>.Instance`
- No performance impact when logging is disabled
- No exceptions if logging infrastructure is missing

Response: Agreed.

---

## 6. Testing Benefits

### 6.1 Diagnosing Test Issues

With logging enabled, tests can capture:
1. **Serialization Flow**: See exactly how objects are serialized/deserialized
2. **Converter Selection**: Understand which converter path is used
3. **Remote Call Timing**: Identify slow remote operations
4. **Factory Lifecycle**: Track IFactoryOnStart/IFactoryOnComplete invocations

### 6.2 Test-Specific Logging Helper

Create a helper for capturing logs in tests:

```csharp
// Tests/TestLoggerProvider.cs
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();

    public IReadOnlyCollection<LogEntry> Logs => _logs.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, _logs);
    }

    public void Clear() => _logs.Clear();

    public void Dispose() { }

    public IEnumerable<LogEntry> GetLogs(LogLevel minLevel = LogLevel.Trace)
    {
        return _logs.Where(l => l.Level >= minLevel);
    }

    public IEnumerable<LogEntry> GetLogsByCategory(string categoryPrefix)
    {
        return _logs.Where(l => l.Category.StartsWith(categoryPrefix));
    }
}

public record LogEntry(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception);

internal sealed class TestLogger : ILogger
{
    private readonly string _category;
    private readonly ConcurrentQueue<LogEntry> _logs;

    public TestLogger(string category, ConcurrentQueue<LogEntry> logs)
    {
        _category = category;
        _logs = logs;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logs.Enqueue(new LogEntry(_category, logLevel, eventId, formatter(state, exception), exception));
    }
}
```

### 6.3 Using in Tests

```csharp
public class SerializationLoggingTests
{
    private readonly TestLoggerProvider _loggerProvider;
    private readonly IServiceProvider _serviceProvider;

    public SerializationLoggingTests()
    {
        _loggerProvider = new TestLoggerProvider();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddProvider(_loggerProvider);
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddNeatooRemoteFactory(NeatooFactory.Server, Assembly.GetExecutingAssembly());
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Serialize_LogsConverterPath()
    {
        var serializer = _serviceProvider.GetRequiredService<INeatooJsonSerializer>();
        var person = new Person { Name = "Test" };

        serializer.Serialize(person);

        var converterLogs = _loggerProvider.GetLogsByCategory("Neatoo.RemoteFactory.Serialization");
        Assert.Contains(converterLogs, l => l.Message.Contains("ordinal format"));
    }
}
```

---

## 7. Phased Implementation Approach

### Phase 1: Infrastructure (Week 1)
**Goal**: Establish logging foundation without changing existing behavior

1. Add `Microsoft.Extensions.Logging.Abstractions` package reference
2. Create `Internal/Log.cs` with `LoggerMessage.Define` definitions
3. Create `Internal/NeatooLogging.cs` for ambient logger support
4. Create `Internal/NeatooLoggerCategories.cs` with category constants
5. Create `Internal/CorrelationContext.cs` for request correlation
6. Add logging infrastructure to DI registration (without using it yet)

**Files to Create**:
- `src/RemoteFactory/Internal/Log.cs`
- `src/RemoteFactory/Internal/NeatooLogging.cs`
- `src/RemoteFactory/Internal/NeatooLoggerCategories.cs`
- `src/RemoteFactory/Internal/CorrelationContext.cs`

**Files to Modify**:
- `src/RemoteFactory/RemoteFactory.csproj` (add package reference)

### Phase 2: Core Serialization Logging (Week 2)
**Goal**: Add logging to serialization path

1. Add `ILogger<NeatooJsonSerializer>` to `NeatooJsonSerializer`
2. Log serialization/deserialization operations
3. Add logging to `NeatooOrdinalConverterFactory`
4. Add logging to `NeatooOrdinalConverter<T>`

**Files to Modify**:
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`
- `src/RemoteFactory/AddRemoteFactoryServices.cs`

### Phase 3: Factory Core Logging (Week 3)
**Goal**: Add logging to factory operations

1. Add `ILogger<FactoryCore<T>>` to `FactoryCore<T>`
2. Log factory operation lifecycle (Start/Complete callbacks)
3. Log operation timing

**Files to Modify**:
- `src/RemoteFactory/Internal/FactoryCore.cs`
- `src/RemoteFactory/AddRemoteFactoryServices.cs`

### Phase 4: Remote Call Logging (Week 4)
**Goal**: Add logging to client-server communication

1. Add logging to `MakeRemoteDelegateRequest`
2. Add logging to `MakeRemoteDelegateRequestHttpCallImplementation`
3. Add logging to `MakeLocalSerializedDelegateRequest`
4. Add logging to `HandleRemoteDelegateRequest` (server-side)

**Files to Modify**:
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs`
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequestHttpCall.cs`
- `src/RemoteFactory/Internal/MakeLocalSerializedDelegateRequest.cs`
- `src/RemoteFactory/HandleRemoteDelegateRequest.cs`

### Phase 5: ASP.NET Core Integration (Week 5)
**Goal**: Add logging to ASP.NET Core components

1. Add logging to `WebApplicationExtensions.UseNeatoo()`
2. Add request/response logging middleware option
3. Add logging to `AspAuthorize`

**Files to Modify**:
- `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs`
- `src/RemoteFactory.AspNetCore/AspAuthorize.cs`
- `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs`

### Phase 6: Source Generator Updates (Week 6)
**Goal**: Emit logging calls in generated code

1. Update source generator to emit `ILogger<TFactory>` injection
2. Emit logging calls for factory method invocations
3. Emit logging for authorization checks

**Files to Modify**:
- `src/Generator/*.cs` (relevant generator files)

### Phase 7: Testing Infrastructure (Week 7)
**Goal**: Add test logging helpers

1. Create `TestLoggerProvider`
2. Update `ClientServerContainers` to support logging
3. Add logging verification tests

**Files to Create**:
- `src/Tests/FactoryGeneratorTests/TestLoggerProvider.cs`

**Files to Modify**:
- `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs`

### Phase 8: Documentation (Week 8)
**Goal**: Document logging for consumers

1. Add logging section to docs
2. Document logger categories and event IDs
3. Add troubleshooting guide

**Files to Create**:
- `docs/concepts/logging.md`
- `docs/reference/log-events.md`

---

## 8. Backward Compatibility Considerations

### 8.1 No Breaking Changes
- All logging is optional and disabled by default
- No new required constructor parameters
- Existing code continues to work unchanged

### 8.2 Optional Logger Parameters
For DI-injected classes, loggers are added as optional parameters:

```csharp
// Before
public NeatooJsonSerializer(
    IEnumerable<NeatooJsonConverterFactory> converterFactories,
    IServiceAssemblies serviceAssemblies,
    NeatooJsonTypeInfoResolver typeInfoResolver,
    NeatooSerializationOptions serializationOptions)

// After
public NeatooJsonSerializer(
    IEnumerable<NeatooJsonConverterFactory> converterFactories,
    IServiceAssemblies serviceAssemblies,
    NeatooJsonTypeInfoResolver typeInfoResolver,
    NeatooSerializationOptions serializationOptions,
    ILogger<NeatooJsonSerializer>? logger = null) // Optional, defaults to NullLogger
```

### 8.3 Version Considerations
- Package dependency on `Microsoft.Extensions.Logging.Abstractions` is version-compatible
- Already in `Directory.Packages.props` as version 9.0.2
- Compatible with all target frameworks (net8.0, net9.0, net10.0)

---

## 9. Files Summary

### New Files
| File | Purpose |
|------|---------|
| `src/RemoteFactory/Internal/Log.cs` | LoggerMessage.Define definitions |
| `src/RemoteFactory/Internal/NeatooLogging.cs` | Ambient logger support |
| `src/RemoteFactory/Internal/NeatooLoggerCategories.cs` | Logger category constants |
| `src/RemoteFactory/Internal/CorrelationContext.cs` | CorrelationId propagation |
| `src/Tests/FactoryGeneratorTests/TestLoggerProvider.cs` | Test logging helper |
| `docs/concepts/logging.md` | Consumer documentation |
| `docs/reference/log-events.md` | Event ID reference |

### Modified Files
| File | Changes |
|------|---------|
| `RemoteFactory.csproj` | Add logging package reference |
| `AddRemoteFactoryServices.cs` | Register logging, add startup filter |
| `NeatooJsonSerializer.cs` | Add logger injection and log calls |
| `NeatooOrdinalConverterFactory.cs` | Add logging for converter creation |
| `FactoryCore.cs` | Add logging for factory operations |
| `MakeRemoteDelegateRequest.cs` | Add logging for remote calls |
| `MakeRemoteDelegateRequestHttpCall.cs` | Add logging for HTTP calls |
| `MakeLocalSerializedDelegateRequest.cs` | Add logging for local serialized calls |
| `HandleRemoteDelegateRequest.cs` | Add logging for server-side handling |
| `WebApplicationExtensions.cs` | Add request logging |
| `AspAuthorize.cs` | Add authorization logging |
| `ClientServerContainers.cs` | Add test logging support |

---

## 10. Example Implementation: NeatooJsonSerializer

Here is a complete example of how logging would be added to `NeatooJsonSerializer`:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace Neatoo.RemoteFactory.Internal;

public interface INeatooJsonSerializer
{
    SerializationFormat Format { get; }
    string? Serialize(object? target);
    string? Serialize(object? target, Type targetType);
    T? Deserialize<T>(string json);
    object? Deserialize(string json, Type type);
    RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, params object?[]? parameters);
    RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, object saveTarget, params object?[]? parameters);
    RemoteRequest DeserializeRemoteDelegateRequest(RemoteRequestDto remoteDelegateRequest);
    T? DeserializeRemoteResponse<T>(RemoteResponseDto remoteResponse);
}

public class NeatooJsonSerializer : INeatooJsonSerializer
{
    private readonly IServiceAssemblies serviceAssemblies;
    private readonly NeatooSerializationOptions serializationOptions;
    private readonly ILogger<NeatooJsonSerializer> logger;

    JsonSerializerOptions Options { get; }
    private NeatooReferenceHandler ReferenceHandler { get; } = new NeatooReferenceHandler();

    public SerializationFormat Format => serializationOptions.Format;

    public NeatooJsonSerializer(
        IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories,
        IServiceAssemblies serviceAssemblies,
        NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver)
        : this(neatooJsonConverterFactories, serviceAssemblies, neatooDefaultJsonTypeInfoResolver,
               new NeatooSerializationOptions(), null)
    {
    }

    public NeatooJsonSerializer(
        IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories,
        IServiceAssemblies serviceAssemblies,
        NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver,
        NeatooSerializationOptions serializationOptions,
        ILogger<NeatooJsonSerializer>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(neatooJsonConverterFactories);
        ArgumentNullException.ThrowIfNull(serializationOptions);

        this.serializationOptions = serializationOptions;
        this.serviceAssemblies = serviceAssemblies;
        this.logger = logger ?? NullLogger<NeatooJsonSerializer>.Instance;

        // Log format being used
        this.logger.LogDebug("Initializing NeatooJsonSerializer with {Format} format", serializationOptions.Format);

        this.Options = new JsonSerializerOptions
        {
            ReferenceHandler = this.ReferenceHandler,
            TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
            WriteIndented = serializationOptions.Format == SerializationFormat.Named,
            IncludeFields = true
        };

        if (serializationOptions.Format == SerializationFormat.Ordinal)
        {
            this.Options.Converters.Add(new NeatooOrdinalConverterFactory(serializationOptions));
        }

        foreach (var factory in neatooJsonConverterFactories)
        {
            this.Options.Converters.Add(factory);
        }
    }

    public string? Serialize(object? target)
    {
        if (target == null)
        {
            return null;
        }

        var typeName = target.GetType().Name;
        logger.SerializingObject(typeName, this.Format);

        var sw = Stopwatch.StartNew();
        try
        {
            using var rr = new NeatooReferenceResolver();
            this.ReferenceHandler.ReferenceResolver.Value = rr;

            var result = JsonSerializer.Serialize(target, this.Options);

            sw.Stop();
            logger.DeserializedObject(typeName, sw.ElapsedMilliseconds);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                var truncated = result.Length > 500 ? result[..500] + "..." : result;
                logger.LogTrace("Serialized {TypeName}: {Json}", typeName, truncated);
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.SerializationFailed(typeName, ex.Message, ex);
            throw;
        }
    }

    // ... rest of the implementation with similar logging patterns
}
```

---

## 11. Success Criteria

1. **Zero Performance Impact**: Logging disabled by default, NullLogger pattern ensures no overhead
2. **Complete Coverage**: All major operations logged at appropriate levels
3. **Useful for Debugging**: Logs provide actionable information for troubleshooting
4. **Test Integration**: Test suite can capture and verify logs
5. **Documentation**: Clear documentation for consumers on how to enable and use logging
6. **Backward Compatible**: No breaking changes to existing API

---

## 12. Open Questions

1. **Scope of Trace Logging**: Should we log every property during serialization at Trace level, or is that too verbose?

Response: No, too Verbose

2. **HTTP Content Logging**: Should we log request/response bodies at Debug level? Security/size concerns?

Response: No, for now. I think there are external tools they can use.

3. **Source Generator Logging**: Should generated factories log all operations or just errors?

Response: All operations

4. **Structured Logging Properties**: What additional structured properties would be useful beyond what's shown?

Response: Add CorrelationId for request tracing across client/server.

---

## 14. CorrelationId Implementation

### 14.1 Overview

CorrelationId enables tracing a single logical operation across client and server. When a client makes a remote call, a unique ID is generated and flows through:
- Client-side serialization
- HTTP request (as header)
- Server-side deserialization
- Server-side factory execution
- Response back to client

### 14.2 Infrastructure

```csharp
// Internal/CorrelationContext.cs
namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Ambient context for correlation ID propagation.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// Gets or sets the current correlation ID.
    /// </summary>
    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// Ensures a correlation ID exists, creating one if needed.
    /// </summary>
    public static string EnsureCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId.Value))
        {
            _correlationId.Value = Guid.NewGuid().ToString("N")[..12]; // Short ID
        }
        return _correlationId.Value!;
    }

    /// <summary>
    /// Executes an action with a specific correlation ID.
    /// </summary>
    public static IDisposable BeginScope(string? correlationId)
    {
        var previous = _correlationId.Value;
        _correlationId.Value = correlationId ?? Guid.NewGuid().ToString("N")[..12];
        return new CorrelationScope(previous);
    }

    private sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previous;
        public CorrelationScope(string? previous) => _previous = previous;
        public void Dispose() => _correlationId.Value = _previous;
    }
}
```

### 14.3 HTTP Header Propagation

```csharp
// Client-side: MakeRemoteDelegateRequestHttpCallImplementation
public async Task<RemoteResponseDto> ForDelegateAsync(RemoteRequestDto request)
{
    var correlationId = CorrelationContext.EnsureCorrelationId();

    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
    httpRequest.Headers.Add("X-Correlation-Id", correlationId);
    // ... rest of implementation
}

// Server-side: ASP.NET Core middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N")[..12];

    using (CorrelationContext.BeginScope(correlationId))
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await next();
    }
});
```

### 14.4 Updated Log Messages with CorrelationId

Log messages for remote calls and factory operations should include CorrelationId:

```csharp
// In Internal/Log.cs - Updated remote call messages

[LoggerMessage(
    EventId = 3001,
    Level = LogLevel.Information,
    Message = "[{CorrelationId}] Remote delegate call started: {DelegateType}")]
public static partial void RemoteCallStarted(
    this ILogger logger,
    string correlationId,
    string delegateType);

[LoggerMessage(
    EventId = 3002,
    Level = LogLevel.Information,
    Message = "[{CorrelationId}] Remote delegate call completed: {DelegateType} in {ElapsedMs}ms")]
public static partial void RemoteCallCompleted(
    this ILogger logger,
    string correlationId,
    string delegateType,
    long elapsedMs);

[LoggerMessage(
    EventId = 3003,
    Level = LogLevel.Error,
    Message = "[{CorrelationId}] Remote delegate call failed: {DelegateType}, HTTP {StatusCode}")]
public static partial void RemoteCallFailed(
    this ILogger logger,
    string correlationId,
    string delegateType,
    int statusCode,
    Exception? exception);

// Server-side handling
[LoggerMessage(
    EventId = 7001,
    Level = LogLevel.Information,
    Message = "[{CorrelationId}] Handling remote request for delegate {DelegateType}")]
public static partial void HandlingRemoteRequest(
    this ILogger logger,
    string correlationId,
    string delegateType);

// Factory operations (when invoked via remote call)
[LoggerMessage(
    EventId = 2001,
    Level = LogLevel.Information,
    Message = "[{CorrelationId}] Factory operation {Operation} started for {TypeName}")]
public static partial void FactoryOperationStarted(
    this ILogger logger,
    string? correlationId,
    FactoryOperation operation,
    string typeName);

[LoggerMessage(
    EventId = 2002,
    Level = LogLevel.Information,
    Message = "[{CorrelationId}] Factory operation {Operation} completed for {TypeName} in {ElapsedMs}ms")]
public static partial void FactoryOperationCompleted(
    this ILogger logger,
    string? correlationId,
    FactoryOperation operation,
    string typeName,
    long elapsedMs);
```

### 14.5 Usage Pattern

```csharp
// Client code - correlation ID is automatic
var person = await personFactory.FetchById(123);
// Logs: [a1b2c3d4e5f6] Remote delegate call started: PersonFactory+FetchByIdDelegate
// Logs: [a1b2c3d4e5f6] Remote delegate call completed: PersonFactory+FetchByIdDelegate in 45ms

// Server-side logs will show same correlation ID:
// Logs: [a1b2c3d4e5f6] Handling remote request for delegate PersonFactory+FetchByIdDelegate
// Logs: [a1b2c3d4e5f6] Factory operation Fetch started for Person
// Logs: [a1b2c3d4e5f6] Factory operation Fetch completed for Person in 12ms
```

### 14.6 Files to Add/Modify

**New Files**:
- `src/RemoteFactory/Internal/CorrelationContext.cs`

**Modified Files**:
- `src/RemoteFactory/Internal/Log.cs` - Add CorrelationId to relevant messages
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequestHttpCall.cs` - Set header
- `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs` - Add middleware
- `src/RemoteFactory/Internal/FactoryCore.cs` - Read from CorrelationContext

---

## 13. References

- [Microsoft.Extensions.Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [High-Performance Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
- [EF Core Logging](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/)
- [LoggerMessage Source Generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
