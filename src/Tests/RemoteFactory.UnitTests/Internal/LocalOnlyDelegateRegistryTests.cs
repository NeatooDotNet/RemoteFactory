using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Unit tests for <see cref="LocalOnlyDelegateRegistry"/> (EXRM-001): the process-global set of
/// static-factory [Execute] delegate types declared local-only by generated registrars, consulted
/// by the server's delegate handler to refuse crafted remote requests.
/// </summary>
public class LocalOnlyDelegateRegistryTests
{
    private delegate Task<int> ProbeLocalOnly(int value);
    private delegate Task<int> ProbeNeverRegistered(int value);

    [Fact]
    public void Register_ThenIsLocalOnly_ReturnsTrue()
    {
        LocalOnlyDelegateRegistry.Register(typeof(ProbeLocalOnly));

        Assert.True(LocalOnlyDelegateRegistry.IsLocalOnly(typeof(ProbeLocalOnly)));
    }

    [Fact]
    public void Register_IsIdempotent()
    {
        LocalOnlyDelegateRegistry.Register(typeof(ProbeLocalOnly));
        LocalOnlyDelegateRegistry.Register(typeof(ProbeLocalOnly));

        Assert.True(LocalOnlyDelegateRegistry.IsLocalOnly(typeof(ProbeLocalOnly)));
    }

    [Fact]
    public void IsLocalOnly_UnregisteredType_ReturnsFalse()
    {
        Assert.False(LocalOnlyDelegateRegistry.IsLocalOnly(typeof(ProbeNeverRegistered)));
    }

    [Fact]
    public void Register_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => LocalOnlyDelegateRegistry.Register(null!));
    }
}
