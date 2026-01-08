---
title: Log Events Reference
parent: Reference
nav_order: 5
---

# Log Events Reference

Complete reference for all log events emitted by Neatoo RemoteFactory.

## Serialization Events (1xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 1001 | Debug | Serializing object of type {TypeName} using {Format} format | Serialization started |
| 1002 | Debug | Deserialized object of type {TypeName} in {ElapsedMs}ms | Deserialization completed |
| 1003 | Warning | Using reflection fallback for ordinal converter: {TypeName} | No generated converter found |
| 1004 | Error | Serialization failed for type {TypeName}: {ErrorMessage} | Serialization error |
| 1005 | Debug | Serialized object of type {TypeName} in {ElapsedMs}ms | Serialization completed |
| 1006 | Debug | Deserializing remote request for delegate {DelegateType} | Request deserialization started |
| 1007 | Error | Deserialization failed for type {TypeName}: {ErrorMessage} | Deserialization error |

## Factory Operation Events (2xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 2001 | Information | [{CorrelationId}] Factory operation {Operation} started for {TypeName} | Operation started |
| 2002 | Information | [{CorrelationId}] Factory operation {Operation} completed for {TypeName} in {ElapsedMs}ms | Operation completed |
| 2003 | Debug | Invoking IFactoryOnStart for {TypeName} | FactoryStart hook called |
| 2004 | Debug | Invoking IFactoryOnComplete for {TypeName} | FactoryComplete hook called |
| 2005 | Error | [{CorrelationId}] Factory operation {Operation} failed for {TypeName}: {ErrorMessage} | Operation failed |

## Remote Call Events (3xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 3001 | Information | [{CorrelationId}] Remote delegate call started: {DelegateType} | Client call started |
| 3002 | Information | [{CorrelationId}] Remote delegate call completed: {DelegateType} in {ElapsedMs}ms | Client call completed |
| 3003 | Error | [{CorrelationId}] Remote delegate call failed: {DelegateType}, HTTP {StatusCode} | HTTP error response |
| 3004 | Debug | [{CorrelationId}] Remote request serialized: {ByteCount} bytes | Request size |
| 3005 | Debug | [{CorrelationId}] Remote response received: {ByteCount} bytes | Response size |
| 3006 | Error | [{CorrelationId}] Remote call error: {DelegateType}, {ErrorMessage} | Client-side error |

## Converter Factory Events (4xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 4001 | Trace | Converter cache hit for type {TypeName} | Cached converter used |
| 4002 | Debug | Creating converter for type {TypeName} via {CreationPath} | New converter created |
| 4003 | Debug | Registered converter count: {Count} | Converter registration stats |
| 4004 | Debug | Registered ordinal converter for type {TypeName} | Converter registration |

## Authorization Events (5xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 5001 | Debug | [{CorrelationId}] Authorization check for operation {Operation} on {TypeName} | Auth check started |
| 5002 | Warning | [{CorrelationId}] Authorization denied for operation {Operation} on {TypeName} | Auth denied |
| 5003 | Debug | [{CorrelationId}] Authorization granted for operation {Operation} on {TypeName} | Auth granted |

## Service Registration Events (6xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 6001 | Information | Registering Neatoo RemoteFactory services: Mode={Mode}, Format={Format} | Service registration |
| 6002 | Debug | Registered factory from assembly {AssemblyName}: {FactoryCount} factories | Factory count |
| 6003 | Debug | Configured logging for Neatoo RemoteFactory | Logging configured |

## Server Request Events (7xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 7001 | Information | [{CorrelationId}] Handling remote request for delegate {DelegateType} | Server handling started |
| 7002 | Debug | [{CorrelationId}] Remote request deserialized with {ParameterCount} parameters | Request parsed |
| 7003 | Error | [{CorrelationId}] Delegate type not found: {DelegateType} | Unknown delegate |
| 7004 | Information | [{CorrelationId}] Remote request completed for delegate {DelegateType} in {ElapsedMs}ms | Server handling completed |
| 7005 | Error | [{CorrelationId}] Remote request failed for delegate {DelegateType}: {ErrorMessage} | Server-side error |
| 7006 | Warning | [{CorrelationId}] Authorization forbidden for delegate {DelegateType} | Server auth denied |
| 7007 | Information | [{CorrelationId}] Remote request cancelled for delegate {DelegateType} | Request cancelled |

## Factory Lifecycle Events (8xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 8001 | Debug | Invoking IFactoryOnCancelled for {TypeName} | Cancellation hook invoked |
| 8002 | Information | [{CorrelationId}] Factory operation {Operation} cancelled for {TypeName} | Operation cancelled |

## Event Tracker Events (9xxx)

| Event ID | Level | Message Template | Description |
|----------|-------|------------------|-------------|
| 9001 | Information | Waiting for {PendingCount} pending event(s) to complete | Wait started |
| 9002 | Warning | Wait for pending events was cancelled with {PendingCount} event(s) still pending | Wait cancelled |
| 9003 | Warning | Some event tasks failed during shutdown | Shutdown failure |
| 9004 | Error | Event handler failed | Event error |
| 9005 | Debug | No pending events to wait for during shutdown | Shutdown clean |
| 9006 | Information | Waiting for {PendingCount} pending event(s) to complete during shutdown | Shutdown wait |
| 9007 | Information | All pending events completed successfully | Shutdown complete |
| 9008 | Warning | Shutdown timeout reached with {PendingCount} event(s) still pending | Shutdown timeout |
| 9009 | Error | Error waiting for pending events during shutdown | Shutdown error |

## Filtering Examples

### Show Only Errors

```json
{
  "Logging": {
    "LogLevel": {
      "Neatoo.RemoteFactory": "Error"
    }
  }
}
```

### Debug Remote Calls

```json
{
  "Logging": {
    "LogLevel": {
      "Neatoo.RemoteFactory.Remote": "Debug",
      "Neatoo.RemoteFactory.Server": "Debug"
    }
  }
}
```

### Trace Serialization

```json
{
  "Logging": {
    "LogLevel": {
      "Neatoo.RemoteFactory.Serialization": "Trace"
    }
  }
}
```

## Programmatic Filtering

```csharp
builder.Logging.AddFilter("Neatoo.RemoteFactory", LogLevel.Information);
builder.Logging.AddFilter("Neatoo.RemoteFactory.Serialization", LogLevel.Warning);
```
