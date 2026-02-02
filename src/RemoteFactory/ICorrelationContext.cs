namespace Neatoo.RemoteFactory;

/// <summary>
/// Provides access to the current correlation ID for distributed tracing.
/// Correlation IDs flow from client to server via the X-Correlation-Id HTTP header.
/// </summary>
/// <remarks>
/// <para>
/// This service is registered as scoped, meaning each HTTP request gets its own instance.
/// For fire-and-forget events, the correlation ID is explicitly captured and propagated
/// to the event's scope by generated code.
/// </para>
/// <para>
/// Inject this interface to access the correlation ID in factory methods, event handlers,
/// or any other server-side code that needs tracing context.
/// </para>
/// </remarks>
public interface ICorrelationContext
{
    /// <summary>
    /// Gets or sets the current correlation ID.
    /// Returns null if no correlation ID has been set for this scope.
    /// </summary>
    string? CorrelationId { get; set; }
}
