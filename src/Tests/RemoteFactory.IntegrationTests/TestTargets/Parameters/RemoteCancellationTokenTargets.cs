using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Parameters;

/// <summary>
/// Test target for CancellationToken support with remote factory methods.
/// CancellationToken is excluded from serialized parameters and flows through HTTP layer instead.
/// </summary>
[Factory]
public partial class RemoteCancellableTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public bool CancellationWasChecked { get; set; }
    public int? BusinessParam { get; set; }

    [Create]
    public RemoteCancellableTarget() { }

    [Create]
    [Remote]
    public async Task CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    [Remote]
    public async Task FetchAsync(int param, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BusinessParam = param;
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    [Remote]
    public async Task FetchWithServiceAsync(
        int param,
        CancellationToken cancellationToken,
        [Service] IServerOnlyService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        BusinessParam = param;
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
    }
}
