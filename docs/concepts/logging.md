---
title: Logging
parent: Concepts
nav_order: 6
---

# Logging

Neatoo RemoteFactory provides comprehensive structured logging using `Microsoft.Extensions.Logging`. All log messages include correlation IDs for distributed tracing across client/server boundaries.

## Configuration

### ASP.NET Core Server

Logging is automatically configured when you call `UseNeatoo()`:

```csharp
var app = builder.Build();
app.UseNeatoo();  // Configures logging and correlation ID middleware
```

The middleware:
- Extracts correlation IDs from incoming `X-Correlation-Id` headers
- Generates a new correlation ID if none is provided
- Propagates the correlation ID in response headers
- Configures the ambient `NeatooLogging` for static method logging

### Non-ASP.NET Core Applications

For console applications or other non-ASP.NET Core scenarios:

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

NeatooLogging.ConfigureLogging(loggerFactory);
```

### appsettings.json Configuration

Filter Neatoo logs by category:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Neatoo.RemoteFactory": "Information",
      "Neatoo.RemoteFactory.Serialization": "Warning",
      "Neatoo.RemoteFactory.Factory": "Information",
      "Neatoo.RemoteFactory.Remote": "Debug",
      "Neatoo.RemoteFactory.Server": "Information",
      "Neatoo.RemoteFactory.Authorization": "Debug"
    }
  }
}
```

## Log Categories

| Category | Description |
|----------|-------------|
| `Neatoo.RemoteFactory` | General library logs |
| `Neatoo.RemoteFactory.Serialization` | JSON serialization operations |
| `Neatoo.RemoteFactory.Factory` | Factory operations (Create, Fetch, Insert, Update, Delete) |
| `Neatoo.RemoteFactory.Remote` | Client-side remote call operations |
| `Neatoo.RemoteFactory.Server` | Server-side request handling |
| `Neatoo.RemoteFactory.Authorization` | Authorization check results |

## Correlation IDs

Every log message includes a correlation ID for distributed tracing:

```
[abc123def456] Factory operation Fetch started for Employee
[abc123def456] Remote delegate call started: FetchEmployee
[abc123def456] Handling remote request for delegate FetchEmployee
[abc123def456] Remote request completed for delegate FetchEmployee in 45ms
[abc123def456] Factory operation Fetch completed for Employee in 52ms
```

### Client-Side Propagation

Correlation IDs are automatically:
1. Generated on the client if not already set
2. Added to HTTP request headers (`X-Correlation-Id`)
3. Extracted from response headers

### Setting Correlation ID Manually

```csharp
using Neatoo.RemoteFactory.Internal;

// Set a correlation ID for the current async context
using (CorrelationContext.BeginScope("my-custom-id"))
{
    var result = await employeeFactory.Fetch(id);
    // All operations within this scope share "my-custom-id"
}

// Or set it directly
CorrelationContext.CorrelationId = "my-correlation-id";
```

## Event IDs

Log events are organized by subsystem:

| Range | Subsystem |
|-------|-----------|
| 1xxx | Serialization |
| 2xxx | Factory Operations |
| 3xxx | Remote Calls (Client) |
| 4xxx | Converter Factory |
| 5xxx | Authorization |
| 6xxx | Service Registration |
| 7xxx | Server Request Handling |

See [Log Events Reference](../reference/log-events.md) for the complete list.

## Performance Considerations

- All log messages use `LoggerMessage.Define` for zero-allocation logging
- Correlation IDs are short (12 characters) to minimize overhead
- Log level checks prevent expensive string formatting when disabled

## Testing with Logging

Use `ClientServerContainers.ScopesWithLogging()` to capture logs in unit tests:

```csharp
[Fact]
public async Task Fetch_LogsOperation()
{
    var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

    var factory = client.GetRequiredService<IEmployeeFactory>();
    await factory.Fetch(1);

    // Assert logging occurred
    loggerProvider.AssertLoggedEventId(2001); // FactoryOperationStarted
    loggerProvider.AssertLoggedEventId(2002); // FactoryOperationCompleted
    loggerProvider.AssertLoggedContains("Employee");
}
```
