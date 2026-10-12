using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// Static [Execute] returning non-generic Task, run end to end (#113).
/// </summary>
/// <remarks>
/// Before #113 a <c>Task</c>-returning [Execute] generated a <c>Task&lt;Task&gt;</c> delegate
/// and did not compile, so these targets could not exist. The remote cases go through the
/// serializing client stand-in: the request crosses as JSON, the server method runs with the
/// server's services, and the client awaits completion with no result to read.
/// </remarks>
public class VoidExecuteTests
{
    [Fact]
    public async Task RemoteVoidExecute_RunsOnServer()
    {
        var (server, client, _) = ClientServerContainers.Scopes();

        var recordRemote = client.ServiceProvider.GetRequiredService<VoidExecuteCommands.RecordRemote>();
        await recordRemote("crossed");

        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
        Assert.Contains("crossed", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }

    [Fact]
    public async Task RemoteVoidExecute_AsyncBody_CompletesBeforeTheCallReturns()
    {
        var (server, client, _) = ClientServerContainers.Scopes();

        var recordRemoteAsync = client.ServiceProvider.GetRequiredService<VoidExecuteCommands.RecordRemoteAsync>();
        await recordRemoteAsync("awaited");

        Assert.Contains("awaited", server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }

    [Fact]
    public async Task RemoteVoidExecute_ServerException_ReachesTheClient()
    {
        var (_, client, _) = ClientServerContainers.Scopes();

        var failRemote = client.ServiceProvider.GetRequiredService<VoidExecuteCommands.FailRemote>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => failRemote("refused on the server"));
        Assert.Equal("refused on the server", ex.Message);
    }

    [Fact]
    public async Task LocalVoidExecute_RunsOnTheCallingTier()
    {
        var (_, _, local) = ClientServerContainers.Scopes();

        var recordLocal = local.ServiceProvider.GetRequiredService<VoidExecuteCommands.RecordLocal>();
        await recordLocal("local");

        Assert.Equal(0, local.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
        Assert.Contains("local", local.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded);
    }
}
