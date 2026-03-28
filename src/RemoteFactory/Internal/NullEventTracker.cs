namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// No-op IEventTracker for Remote-mode clients where events
/// serialize to the server. Has zero constructor dependencies,
/// eliminating the ILogger requirement that causes DI validation
/// failures on Blazor WASM clients without logging configured.
/// </summary>
internal sealed class NullEventTracker : IEventTracker
{
    /// <inheritdoc />
    public int PendingCount => 0;

    /// <inheritdoc />
    public void Track(Task eventTask) { }

    /// <inheritdoc />
    public Task WaitAllAsync(CancellationToken ct = default) => Task.CompletedTask;
}
