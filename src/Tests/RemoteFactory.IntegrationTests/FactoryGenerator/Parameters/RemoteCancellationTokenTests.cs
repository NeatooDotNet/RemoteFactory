using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Parameters;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Parameters;

/// <summary>
/// Integration tests for CancellationToken support with remote factory methods.
/// CancellationToken is excluded from serialized parameters and flows through the HTTP layer instead.
/// These tests verify the full client-server serialization round-trip works correctly.
/// </summary>
public class RemoteCancellationTokenTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IRemoteCancellableTargetFactory _factory;

    public RemoteCancellationTokenTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _serverScope = server;
        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IRemoteCancellableTargetFactory>();
    }

    [Fact]
    public async Task CreateAsync_Remote_WithNonCancelledToken_Completes()
    {
        using var cts = new CancellationTokenSource();

        // Goes through remote serialization path
        var result = await _factory.CreateAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task CreateAsync_Remote_WithAlreadyCancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // CancellationToken flows through HTTP layer
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _factory.CreateAsync(cts.Token));
    }

    [Fact]
    public async Task FetchAsync_Remote_WithBusinessParamAndCancellationToken_Works()
    {
        const int expectedParam = 42;
        using var cts = new CancellationTokenSource();

        // Business param is serialized, CancellationToken flows through HTTP
        var result = await _factory.FetchAsync(expectedParam, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(expectedParam, result.BusinessParam);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchAsync_Remote_WithCancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _factory.FetchAsync(123, cts.Token));
    }

    [Fact]
    public async Task FetchWithServiceAsync_Remote_Works()
    {
        const int expectedParam = 99;
        using var cts = new CancellationTokenSource();

        // Service is injected on server side, business param serialized
        var result = await _factory.FetchWithServiceAsync(expectedParam, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(expectedParam, result.BusinessParam);
        Assert.True(result.CancellationWasChecked);
    }
}
