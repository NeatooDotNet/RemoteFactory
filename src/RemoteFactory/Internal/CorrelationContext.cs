namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Ambient context for correlation ID propagation.
/// Enables tracing a single logical operation across client and server.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// The HTTP header name used to propagate correlation IDs.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Gets or sets the current correlation ID.
    /// Returns null if no correlation ID has been set.
    /// </summary>
    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// Ensures a correlation ID exists, creating one if needed.
    /// </summary>
    /// <returns>The current or newly created correlation ID.</returns>
    public static string EnsureCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId.Value))
        {
            _correlationId.Value = GenerateCorrelationId();
        }
        return _correlationId.Value!;
    }

    /// <summary>
    /// Executes code within a specific correlation ID scope.
    /// The previous correlation ID is restored when the scope is disposed.
    /// </summary>
    /// <param name="correlationId">The correlation ID to use. If null, a new one is generated.</param>
    /// <returns>A disposable scope that restores the previous correlation ID.</returns>
    public static IDisposable BeginScope(string? correlationId)
    {
        var previous = _correlationId.Value;
        _correlationId.Value = correlationId ?? GenerateCorrelationId();
        return new CorrelationScope(previous);
    }

    /// <summary>
    /// Clears the current correlation ID.
    /// </summary>
    public static void Clear()
    {
        _correlationId.Value = null;
    }

    /// <summary>
    /// Generates a new short correlation ID.
    /// </summary>
    /// <returns>A 12-character correlation ID.</returns>
    private static string GenerateCorrelationId()
    {
        // Use first 12 chars of a GUID for a short but unique ID
        return Guid.NewGuid().ToString("N")[..12];
    }

    private sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previous;
        private bool _disposed;

        public CorrelationScope(string? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _correlationId.Value = _previous;
                _disposed = true;
            }
        }
    }
}
