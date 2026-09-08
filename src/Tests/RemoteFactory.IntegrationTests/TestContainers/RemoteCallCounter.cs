namespace RemoteFactory.IntegrationTests.TestContainers;

/// <summary>
/// Per-scope count of requests that crossed the simulated client/server boundary.
/// Registered scoped in every container by <see cref="ClientServerContainers"/>; only the client
/// container's <c>MakeSerializedServerStandinDelegateRequest</c> increments it, so a test can
/// prove that a call did (or did not) go over the wire from the scope it ran in.
/// </summary>
/// <remarks>
/// Added by EXRM-001. Before it, a client-scope test that resolved a delegate and asserted its
/// result could pass whether the delegate ran remotely or locally, which is exactly the vacuity
/// that appeared once <c>[Execute]</c> stopped being forced remote.
/// </remarks>
public sealed class RemoteCallCounter
{
    private int _count;

    /// <summary>Number of remote requests made from this scope.</summary>
    public int Count => Volatile.Read(ref _count);

    internal void Increment() => Interlocked.Increment(ref _count);
}
