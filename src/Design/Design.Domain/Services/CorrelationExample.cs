// =============================================================================
// DESIGN SOURCE OF TRUTH: Correlation Context Usage
// =============================================================================
//
// This file demonstrates how to use CorrelationContext for tracing operations
// across the client/server boundary. Correlation IDs help you track a single
// logical operation through logs, events, and debugging.
//
// =============================================================================

using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace Design.Domain.Services;

/// <summary>
/// Demonstrates correlation context usage in factory operations.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Ambient correlation context with AsyncLocal
///
/// RemoteFactory automatically propagates correlation IDs across the wire:
/// 1. Client generates or inherits a correlation ID
/// 2. ID is sent in X-Correlation-Id header
/// 3. Server extracts and sets CorrelationContext.CorrelationId
/// 4. All server-side code can access the same correlation ID
///
/// Use cases:
/// - Distributed tracing across client/server
/// - Correlating logs from a single user operation
/// - Debugging issues that span multiple services
/// - Audit logging with operation context
///
/// DID NOT DO THIS: Pass correlation ID as a parameter
///
/// Reasons:
/// 1. Would pollute every method signature
/// 2. Easy to forget or pass incorrectly
/// 3. Ambient context works across async boundaries
/// 4. Matches how HttpContext, Transaction, etc. work
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
    // CorrelationContext.CorrelationId contains the current correlation ID.
    // It's automatically set by RemoteFactory when:
    // - A remote request arrives (from X-Correlation-Id header)
    // - EnsureCorrelationId() is called (generates new if missing)
    //
    // COMMON MISTAKE: Assuming CorrelationId is always set
    //
    // In local/logical mode without HTTP, correlation ID may be null.
    // Use EnsureCorrelationId() or null-check before using.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates an order and captures the correlation ID for audit purposes.
    /// </summary>
    [Remote, Create]
    public void Create(string customerName, [Service] IAuditLogger auditLogger)
    {
        CustomerName = customerName;

        // Capture the correlation ID from the current context
        // This ID traces back to the original client request
        CreatedByCorrelationId = CorrelationContext.CorrelationId;

        // Log with correlation context for distributed tracing
        auditLogger.Log(
            $"Order created for {customerName}",
            CorrelationContext.CorrelationId);
    }

    /// <summary>
    /// Fetches an order with correlation-aware logging.
    /// </summary>
    [Remote, Fetch]
    public void Fetch(int id, [Service] IAuditLogger auditLogger)
    {
        Id = id;
        CustomerName = $"Customer_{id}";
        CreatedByCorrelationId = "original-correlation-id";

        // EnsureCorrelationId() creates one if missing (e.g., local mode)
        var correlationId = CorrelationContext.EnsureCorrelationId();

        auditLogger.Log(
            $"Order {id} fetched",
            correlationId);
    }
}

/// <summary>
/// Demonstrates correlation context in static factory methods.
/// </summary>
/// <remarks>
/// Static factory methods can also access CorrelationContext.
/// This is useful for Execute and Event operations that need
/// to log or trace their execution.
/// </remarks>
[Factory]
public static partial class CorrelatedOperations
{
    // -------------------------------------------------------------------------
    // Using BeginScope for Sub-Operations
    //
    // Use CorrelationContext.BeginScope() when you need to:
    // 1. Set a specific correlation ID for a block of code
    // 2. Restore the previous ID when done (disposable pattern)
    // 3. Run sub-operations with their own correlation context
    //
    // This is rarely needed - RemoteFactory handles most cases automatically.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes an operation with explicit correlation scope.
    /// </summary>
    [Remote, Execute]
    private static Task<string> _ProcessWithCorrelation(
        string data,
        [Service] IAuditLogger auditLogger)
    {
        // Current correlation ID (set by RemoteFactory from HTTP header)
        var currentId = CorrelationContext.CorrelationId;

        auditLogger.Log($"Processing data: {data}", currentId);

        // For sub-operations that need isolated correlation:
        using (CorrelationContext.BeginScope("sub-operation-123"))
        {
            // Inside this scope, CorrelationContext.CorrelationId = "sub-operation-123"
            auditLogger.Log("Sub-operation started", CorrelationContext.CorrelationId);
        }
        // Previous correlation ID is restored here

        return Task.FromResult($"Processed: {data}");
    }

    /// <summary>
    /// Event operation with correlation logging.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Events run in an isolated scope, but correlation
    /// context is preserved. This lets you trace events back to the
    /// operation that triggered them.
    /// </remarks>
    [Remote, Event]
    private static Task _OnOrderProcessed(
        int orderId,
        [Service] IAuditLogger auditLogger)
    {
        // Correlation ID traces this event back to the original request
        auditLogger.Log(
            $"Order {orderId} processed event",
            CorrelationContext.CorrelationId);

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
