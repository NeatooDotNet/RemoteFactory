namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Scoped implementation of ICorrelationContext.
/// Stores correlation ID directly on the instance (no AsyncLocal).
/// </summary>
internal sealed class CorrelationContextImpl : ICorrelationContext
{
    /// <summary>
    /// The HTTP header name used to propagate correlation IDs between client and server.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Generates a new short correlation ID (12 characters from a GUID).
    /// </summary>
    public static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }
}
