using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Write;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Write;

/// <summary>
/// Integration tests for write operations with mixed return types.
/// Tests Insert/Update/Delete routing based on IsNew/IsDeleted flags.
/// Tests both local and remote ([Remote] attribute) execution paths.
/// </summary>
/// <remarks>
/// These tests use explicit strongly-typed calls instead of reflection-based enumeration.
/// Each Save method signature gets its own explicit test.
/// </remarks>
public class MixedWriteRoundTripTests
{
    private readonly IServiceScope _clientScope;

    public MixedWriteRoundTripTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
    }

    // ============================================================================
    // INSERT Tests - Void returns
    // ============================================================================

    [Fact]
    public void SaveVoid_Insert_CallsInsertVoid()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public void SaveVoidParam_Insert_PassesParameter()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveVoidParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    [Fact]
    public void SaveVoidService_Insert_InjectsService()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveVoidService(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceInjected);
    }

    [Fact]
    public void SaveVoidParamService_Insert_PassesParamAndInjectsService()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveVoidParamService(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(99, result.ReceivedParam);
        Assert.True(result.ServiceInjected);
    }

    // ============================================================================
    // INSERT Tests - Bool returns
    // ============================================================================

    [Fact]
    public void SaveBoolTrue_Insert_ReturnsResult()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public void SaveBoolFalse_Insert_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = factory.SaveBoolFalse(entity);

        // When bool returns false, Save returns null
        Assert.Null(result);
    }

    // ============================================================================
    // INSERT Tests - Task returns
    // ============================================================================

    [Fact]
    public async Task SaveTask_Insert_CompletesSuccessfully()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = await factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    // ============================================================================
    // INSERT Tests - Task<bool> returns
    // ============================================================================

    [Fact]
    public async Task SaveTaskBoolTrue_Insert_ReturnsResult()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = await factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task SaveTaskBoolFalse_Insert_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = true };

        var result = await factory.SaveTaskBoolFalse(entity);

        // When Task<bool> returns false, Save returns null
        Assert.Null(result);
    }

    // ============================================================================
    // UPDATE Tests - Void returns
    // ============================================================================

    [Fact]
    public void SaveVoid_Update_CallsUpdateVoid()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public void SaveVoidParam_Update_PassesParameter()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = factory.SaveVoidParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    // ============================================================================
    // UPDATE Tests - Bool returns
    // ============================================================================

    [Fact]
    public void SaveBoolTrue_Update_ReturnsResult()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public void SaveBoolFalse_Update_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = factory.SaveBoolFalse(entity);

        Assert.Null(result);
    }

    // ============================================================================
    // UPDATE Tests - Remote with [Remote] attribute
    // ============================================================================

    [Fact]
    public async Task SaveBoolTrueRemote_Update_ExecutesRemotely()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = await factory.SaveBoolTrueRemote(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceInjected);
    }

    [Fact]
    public async Task SaveBoolTrueRemoteParam_Update_ExecutesRemotely()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsNew = false, IsDeleted = false };

        var result = await factory.SaveBoolTrueRemoteParam(entity, 77);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(77, result.ReceivedParam);
        Assert.True(result.ServiceInjected);
    }

    // ============================================================================
    // DELETE Tests - Void returns
    // ============================================================================

    [Fact]
    public void SaveVoid_Delete_CallsDeleteVoid()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    [Fact]
    public void SaveVoidParam_Delete_PassesParameter()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = factory.SaveVoidParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    // ============================================================================
    // DELETE Tests - Bool returns
    // ============================================================================

    [Fact]
    public void SaveBoolTrue_Delete_ReturnsResult()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    [Fact]
    public void SaveBoolFalse_Delete_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = factory.SaveBoolFalse(entity);

        Assert.Null(result);
    }

    // ============================================================================
    // DELETE Tests - Task returns
    // ============================================================================

    [Fact]
    public async Task SaveTask_Delete_CompletesSuccessfully()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    // ============================================================================
    // DELETE Tests - Task<bool> returns
    // ============================================================================

    [Fact]
    public async Task SaveTaskBoolTrue_Delete_ReturnsResult()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    [Fact]
    public async Task SaveTaskBoolFalse_Delete_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveTaskBoolFalse(entity);

        Assert.Null(result);
    }

    // ============================================================================
    // DELETE Tests - Remote with [Remote] attribute
    // ============================================================================

    [Fact]
    public async Task SaveBoolTrueRemote_Delete_ExecutesRemotely()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveBoolTrueRemote(entity, 55);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(55, result.ReceivedParam);
    }

    [Fact]
    public async Task SaveVoidRemote_Delete_InjectsService()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveVoidRemote(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceInjected);
    }

    [Fact]
    public async Task SaveBoolFalseRemote_Delete_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveBoolFalseRemote(entity, 33);

        // Remote bool returning false should return null
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveTaskRemote_Delete_ExecutesRemotely()
    {
        var factory = _clientScope.GetRequiredService<IMixedWriteTargetFactory>();
        var entity = new MixedWriteTarget { IsDeleted = true };

        var result = await factory.SaveTaskRemote(entity, 88);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(88, result.ReceivedParam);
        Assert.True(result.ServiceInjected);
    }
}
