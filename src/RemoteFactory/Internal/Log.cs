using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// High-performance log message definitions using LoggerMessage source generator.
/// Organized by event ID ranges for different subsystems.
/// </summary>
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

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Serialized object of type {TypeName} in {ElapsedMs}ms")]
    public static partial void SerializedObject(
        this ILogger logger,
        string typeName,
        long elapsedMs);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Deserializing remote request for delegate {DelegateType}")]
    public static partial void DeserializingRemoteRequest(
        this ILogger logger,
        string delegateType);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Error,
        Message = "Deserialization failed for type {TypeName}: {ErrorMessage}")]
    public static partial void DeserializationFailed(
        this ILogger logger,
        string typeName,
        string errorMessage,
        Exception? exception);

    // ===== Factory Operations (2xxx) =====

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

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] Factory operation {Operation} failed for {TypeName}: {ErrorMessage}")]
    public static partial void FactoryOperationFailed(
        this ILogger logger,
        string? correlationId,
        FactoryOperation operation,
        string typeName,
        string errorMessage,
        Exception? exception);

    // ===== Remote Calls (3xxx) =====

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

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Remote request serialized: {ByteCount} bytes")]
    public static partial void RemoteRequestSerialized(
        this ILogger logger,
        string correlationId,
        int byteCount);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Remote response received: {ByteCount} bytes")]
    public static partial void RemoteResponseReceived(
        this ILogger logger,
        string correlationId,
        int byteCount);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] Remote call error: {DelegateType}, {ErrorMessage}")]
    public static partial void RemoteCallError(
        this ILogger logger,
        string correlationId,
        string delegateType,
        string errorMessage,
        Exception? exception);

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Remote delegate call cancelled: {DelegateType}")]
    public static partial void RemoteCallCancelled(
        this ILogger logger,
        string correlationId,
        string delegateType);

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

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Debug,
        Message = "Registered ordinal converter for type {TypeName}")]
    public static partial void ConverterRegistered(
        this ILogger logger,
        string typeName);

    // ===== Authorization (5xxx) =====

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Authorization check for operation {Operation} on {TypeName}")]
    public static partial void AuthorizationCheck(
        this ILogger logger,
        string? correlationId,
        string operation,
        string typeName);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] Authorization denied for operation {Operation} on {TypeName}")]
    public static partial void AuthorizationDenied(
        this ILogger logger,
        string? correlationId,
        string operation,
        string typeName);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Authorization granted for operation {Operation} on {TypeName}")]
    public static partial void AuthorizationGranted(
        this ILogger logger,
        string? correlationId,
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

    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Debug,
        Message = "Configured logging for Neatoo RemoteFactory")]
    public static partial void LoggingConfigured(
        this ILogger logger);

    // ===== Server-Side Request Handling (7xxx) =====

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Handling remote request for delegate {DelegateType}")]
    public static partial void HandlingRemoteRequest(
        this ILogger logger,
        string correlationId,
        string delegateType);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Remote request deserialized with {ParameterCount} parameters")]
    public static partial void RemoteRequestDeserialized(
        this ILogger logger,
        string correlationId,
        int parameterCount);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] Delegate type not found: {DelegateType}")]
    public static partial void DelegateTypeNotFound(
        this ILogger logger,
        string correlationId,
        string delegateType);

    [LoggerMessage(
        EventId = 7004,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Remote request completed for delegate {DelegateType} in {ElapsedMs}ms")]
    public static partial void RemoteRequestCompleted(
        this ILogger logger,
        string correlationId,
        string delegateType,
        long elapsedMs);

    [LoggerMessage(
        EventId = 7005,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] Remote request failed for delegate {DelegateType}: {ErrorMessage}")]
    public static partial void RemoteRequestFailed(
        this ILogger logger,
        string correlationId,
        string delegateType,
        string errorMessage,
        Exception? exception);

    [LoggerMessage(
        EventId = 7006,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] Authorization forbidden for delegate {DelegateType}")]
    public static partial void RemoteRequestForbidden(
        this ILogger logger,
        string correlationId,
        string delegateType);

    [LoggerMessage(
        EventId = 7007,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Remote request cancelled for delegate {DelegateType}")]
    public static partial void RemoteRequestCancelled(
        this ILogger logger,
        string correlationId,
        string delegateType);

    // ===== Factory Lifecycle (8xxx) =====

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Debug,
        Message = "Invoking IFactoryOnCancelled for {TypeName}")]
    public static partial void InvokingFactoryOnCancelled(
        this ILogger logger,
        string typeName);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Factory operation {Operation} cancelled for {TypeName}")]
    public static partial void FactoryOperationCancelled(
        this ILogger logger,
        string? correlationId,
        FactoryOperation operation,
        string typeName);

    // ===== Event Tracking (9xxx) =====

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Waiting for {PendingCount} pending event(s) to complete")]
    public static partial void WaitingForPendingEvents(
        this ILogger logger,
        int pendingCount);

    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Warning,
        Message = "Wait for pending events was cancelled with {PendingCount} event(s) still pending")]
    public static partial void PendingEventsCancelled(
        this ILogger logger,
        int pendingCount);

    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Warning,
        Message = "Some event tasks failed during shutdown")]
    public static partial void PendingEventsShutdownFailed(
        this ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 9004,
        Level = LogLevel.Error,
        Message = "Event handler failed")]
    public static partial void EventHandlerFailed(
        this ILogger logger,
        Exception? exception);

    [LoggerMessage(
        EventId = 9005,
        Level = LogLevel.Debug,
        Message = "No pending events to wait for during shutdown")]
    public static partial void NoPendingEventsAtShutdown(
        this ILogger logger);

    [LoggerMessage(
        EventId = 9006,
        Level = LogLevel.Information,
        Message = "Waiting for {PendingCount} pending event(s) to complete during shutdown")]
    public static partial void WaitingForPendingEventsAtShutdown(
        this ILogger logger,
        int pendingCount);

    [LoggerMessage(
        EventId = 9007,
        Level = LogLevel.Information,
        Message = "All pending events completed successfully")]
    public static partial void AllPendingEventsCompleted(
        this ILogger logger);

    [LoggerMessage(
        EventId = 9008,
        Level = LogLevel.Warning,
        Message = "Shutdown timeout reached with {PendingCount} event(s) still pending")]
    public static partial void ShutdownTimeoutReached(
        this ILogger logger,
        int pendingCount);

    [LoggerMessage(
        EventId = 9009,
        Level = LogLevel.Error,
        Message = "Error waiting for pending events during shutdown")]
    public static partial void ShutdownWaitError(
        this ILogger logger,
        Exception? exception);
}
