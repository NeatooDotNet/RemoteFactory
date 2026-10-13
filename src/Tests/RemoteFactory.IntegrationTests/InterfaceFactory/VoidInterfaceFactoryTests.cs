using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;
using RemoteFactory.IntegrationTests.TestTargets.InterfaceFactory;

namespace RemoteFactory.IntegrationTests.InterfaceFactory;

/// <summary>
/// Interface-factory methods returning non-generic Task, run end to end (#122).
/// </summary>
/// <remarks>
/// The client resolves the generated proxy; each call crosses the serializing stand-in,
/// the server's implementation runs with the server's services, and the client awaits
/// completion with no result to read.
/// </remarks>
public class VoidInterfaceFactoryTests
{
    [Fact]
    public async Task VoidTask_FromClient_RunsOnServer()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IVoidTaskService>();

        await service.Record("iface-crossed");

        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
        Assert.Contains("iface-crossed", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }

    [Fact]
    public async Task VoidTask_AsyncBody_CompletesBeforeTheCallReturns()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IVoidTaskService>();

        await service.RecordAsync("iface-awaited", CancellationToken.None);

        Assert.Contains("iface-awaited", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }

    [Fact]
    public async Task VoidTask_ServerException_ReachesTheClient()
    {
        var (_, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IVoidTaskService>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Fail("iface-refused"));
        Assert.Equal("iface-refused", ex.Message);
    }

    [Fact]
    public async Task VoidTask_BesideTaskOfT_StillReturnsItsResult()
    {
        var (_, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IVoidTaskService>();

        await service.Record("iface-counted");
        var count = await service.RecordedCount();

        Assert.True(count >= 1);
    }

    [Fact]
    public async Task AuthorizedVoidTask_Allowed_RunsOnServer()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IAuthorizedVoidTaskService>();
        var id = Guid.NewGuid();

        await service.Close(id);

        Assert.Contains($"closed:{id}", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }

    [Fact]
    public async Task AuthorizedVoidTask_Denied_ThrowsAndDoesNotRun()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var service = client.ServiceProvider.GetRequiredService<IAuthorizedVoidTaskService>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => service.Close(VoidTaskAuth.DeniedId));

        Assert.DoesNotContain($"closed:{VoidTaskAuth.DeniedId}", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }
}
