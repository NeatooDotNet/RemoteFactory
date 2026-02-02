// =============================================================================
// DESIGN SOURCE OF TRUTH: Correlation Context Usage
// =============================================================================
//
// This file demonstrates how to use ICorrelationContext for tracing operations
// across the client/server boundary. Correlation IDs help you track a single
// logical operation through logs, events, and debugging.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Services;

/// <summary>
/// Demonstrates correlation context usage in factory operations.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Scoped ICorrelationContext with explicit DI
///
/// RemoteFactory automatically propagates correlation IDs across the wire:
/// 1. Client generates a correlation ID (if not set)
/// 2. ID is sent in X-Correlation-Id header
/// 3. Server middleware extracts and sets ICorrelationContext.CorrelationId
/// 4. Inject ICorrelationContext via [Service] to access the correlation ID
///
/// Use cases:
/// - Distributed tracing across client/server
/// - Correlating logs from a single user operation
/// - Debugging issues that span multiple services
/// - Audit logging with operation context
///
/// For events: The generator captures the correlation ID before Task.Run
/// and restores it in the event's new DI scope, so events inherit the
/// correlation ID from the triggering operation.
/// </remarks>
[Factory]
public partial class AuditedOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CreatedByCorrelationId { get; set; }

    // -------------------------------------------------------------------------
    // Accessing Correlation Context
    //
    // Inject ICorrelationContext via [Service] parameter to access the
    // correlation ID. The middleware sets it from the X-Correlation-Id header
    // or generates a new one if missing.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates an order and captures the correlation ID for audit purposes.
    /// </summary>
    [Remote, Create]
    public void Create(
        string customerName,
        [Service] ICorrelationContext correlationContext,
        [Service] IAuditLogger auditLogger)
    {
        CustomerName = customerName;

        // Capture the correlation ID from the injected context
        // This ID traces back to the original client request
        CreatedByCorrelationId = correlationContext.CorrelationId;

        // Log with correlation context for distributed tracing
        auditLogger.Log(
            $"Order created for {customerName}",
            correlationContext.CorrelationId);
    }

    /// <summary>
    /// Fetches an order with correlation-aware logging.
    /// </summary>
    [Remote, Fetch]
    public void Fetch(
        int id,
        [Service] ICorrelationContext correlationContext,
        [Service] IAuditLogger auditLogger)
    {
        Id = id;
        CustomerName = $"Customer_{id}";
        CreatedByCorrelationId = "original-correlation-id";

        // Middleware ensures correlation ID is always set for HTTP requests
        auditLogger.Log(
            $"Order {id} fetched",
            correlationContext.CorrelationId);
    }
}

/// <summary>
/// Demonstrates correlation context in static factory methods.
/// </summary>
/// <remarks>
/// Static factory methods can also inject ICorrelationContext.
/// This is useful for Execute and Event operations that need
/// to log or trace their execution.
/// </remarks>
[Factory]
public static partial class CorrelatedOperations
{
    /// <summary>
    /// Executes an operation with correlation context.
    /// </summary>
    [Remote, Execute]
    private static Task<string> _ProcessWithCorrelation(
        string data,
        [Service] ICorrelationContext correlationContext,
        [Service] IAuditLogger auditLogger)
    {
        // Correlation ID is set by middleware from HTTP header
        auditLogger.Log($"Processing data: {data}", correlationContext.CorrelationId);

        return Task.FromResult($"Processed: {data}");
    }

    /// <summary>
    /// Event operation with correlation logging.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Events run in an isolated DI scope, but the
    /// generator captures the correlation ID before Task.Run and sets it
    /// on the event scope's ICorrelationContext. This lets you trace events
    /// back to the operation that triggered them.
    /// </remarks>
    [Remote, Event]
    private static Task _OnOrderProcessed(
        int orderId,
        [Service] ICorrelationContext correlationContext,
        [Service] IAuditLogger auditLogger)
    {
        // Correlation ID traces this event back to the original request
        auditLogger.Log(
            $"Order {orderId} processed event",
            correlationContext.CorrelationId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple audit logger interface for demonstration.
/// </summary>
public interface IAuditLogger
{
    void Log(string message, string? correlationId);
}

/// <summary>
/// Console implementation for testing.
/// </summary>
public class ConsoleAuditLogger : IAuditLogger
{
    public void Log(string message, string? correlationId)
    {
        Console.WriteLine($"[{correlationId ?? "no-correlation"}] {message}");
    }
}
