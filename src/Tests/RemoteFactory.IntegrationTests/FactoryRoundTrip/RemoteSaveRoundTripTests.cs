using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.FactoryRoundTrip;

namespace RemoteFactory.IntegrationTests.FactoryRoundTrip;

/// <summary>
/// Integration tests for Save methods (Insert/Update/Delete) with [Remote] attribute.
/// These tests verify full client-server serialization round-trips.
/// </summary>
public class RemoteSaveRoundTripTests
{
    [Fact]
    public async Task RemoteSave_Insert_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteSaveTarget_SimpleFactory>();
        var entity = new RemoteSaveTarget_Simple { IsNew = true };

        var result = await clientFactory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public async Task RemoteSave_Update_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteSaveTarget_SimpleFactory>();
        var entity = new RemoteSaveTarget_Simple { IsNew = false, IsDeleted = false };

        var result = await clientFactory.Save(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.True(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public async Task RemoteSave_Delete_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteSaveTarget_SimpleFactory>();
        var entity = new RemoteSaveTarget_Simple { IsNew = false, IsDeleted = true };

        var result = await clientFactory.Save(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.True(result.DeleteCalled);
    }

    [Fact]
    public async Task RemoteSave_Insert_Comparison_ClientVsServer()
    {
        var scopes = ClientServerContainers.Scopes();

        // Client (remote mode) - goes through serialization
        var clientFactory = scopes.client.GetRequiredService<IRemoteSaveTarget_SimpleFactory>();
        var clientEntity = new RemoteSaveTarget_Simple { IsNew = true };
        var clientResult = await clientFactory.Save(clientEntity);

        // Server (direct mode) - no serialization
        var serverFactory = scopes.server.GetRequiredService<IRemoteSaveTarget_SimpleFactory>();
        var serverEntity = new RemoteSaveTarget_Simple { IsNew = true };
        var serverResult = await serverFactory.Save(serverEntity);

        // Both should produce same results
        Assert.NotNull(clientResult);
        Assert.NotNull(serverResult);
        Assert.True(clientResult.InsertCalled);
        Assert.True(serverResult.InsertCalled);
    }
}
