using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Write;

namespace RemoteFactory.UnitTests.FactoryGenerator.Write;

/// <summary>
/// Unit tests for [Insert]/[Update]/[Delete] factory methods without [Remote] attribute.
/// These tests verify Save method generation and execution in Server mode with strongly-typed calls.
/// </summary>
/// <remarks>
/// When write methods return void or bool (not Task), the generated Save methods are synchronous.
/// When write methods return Task or Task&lt;bool&gt;, the generated Save methods are async.
/// </remarks>
public class LocalWriteTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public LocalWriteTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Insert/Update/Delete Void Tests (Synchronous Save)

    [Fact]
    public void Save_Insert_Void_CallsInsertMethod()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_NoParamsFactory>();
        var entity = new WriteTarget_Void_NoParams { IsNew = true };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public void Save_Update_Void_CallsUpdateMethod()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_NoParamsFactory>();
        var entity = new WriteTarget_Void_NoParams { IsNew = false, IsDeleted = false };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.True(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public void Save_Delete_Void_CallsDeleteMethod()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_NoParamsFactory>();
        var entity = new WriteTarget_Void_NoParams { IsDeleted = true };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region Insert/Update/Delete Bool Tests (Synchronous Save)

    [Fact]
    public void Save_Insert_BoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Bool_NoParamsFactory>();
        var entity = new WriteTarget_Bool_NoParams { IsNew = true };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public void Save_Insert_BoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Bool_NoParamsFactory>();
        var entity = new WriteTarget_Bool_NoParams { IsNew = true };

        var result = factory.SaveBoolFalse(entity);

        // When bool returns false, Save returns null
        Assert.Null(result);
    }

    [Fact]
    public void Save_Update_BoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Bool_NoParamsFactory>();
        var entity = new WriteTarget_Bool_NoParams { IsNew = false, IsDeleted = false };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public void Save_Delete_BoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Bool_NoParamsFactory>();
        var entity = new WriteTarget_Bool_NoParams { IsDeleted = true };

        var result = factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region Insert/Update/Delete Task Tests (Async Save)

    [Fact]
    public async Task Save_Insert_Task_CompletesSuccessfully()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Task_NoParamsFactory>();
        var entity = new WriteTarget_Task_NoParams { IsNew = true };

        var result = await factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task Save_Update_Task_CompletesSuccessfully()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Task_NoParamsFactory>();
        var entity = new WriteTarget_Task_NoParams { IsNew = false, IsDeleted = false };

        var result = await factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public async Task Save_Delete_Task_CompletesSuccessfully()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Task_NoParamsFactory>();
        var entity = new WriteTarget_Task_NoParams { IsDeleted = true };

        var result = await factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region Insert/Update/Delete Task<bool> Tests (Async Save)

    [Fact]
    public async Task Save_Insert_TaskBoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_TaskBool_NoParamsFactory>();
        var entity = new WriteTarget_TaskBool_NoParams { IsNew = true };

        var result = await factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task Save_Insert_TaskBoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_TaskBool_NoParamsFactory>();
        var entity = new WriteTarget_TaskBool_NoParams { IsNew = true };

        var result = await factory.SaveTaskBoolFalse(entity);

        // When Task<bool> returns false, Save returns null
        Assert.Null(result);
    }

    #endregion

    #region Service Injection Tests (Synchronous Save)

    [Fact]
    public void Save_Insert_ServiceParam_InjectsService()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_ServiceParamFactory>();
        var entity = new WriteTarget_Void_ServiceParam { IsNew = true };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Save_Update_ServiceParam_InjectsService()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_ServiceParamFactory>();
        var entity = new WriteTarget_Void_ServiceParam { IsNew = false, IsDeleted = false };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Save_Delete_ServiceParam_InjectsService()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_ServiceParamFactory>();
        var entity = new WriteTarget_Void_ServiceParam { IsDeleted = true };

        var result = factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Parameter Tests (Synchronous Save)

    [Fact]
    public void Save_Insert_IntParam_PassesParameter()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_IntParamFactory>();
        var entity = new WriteTarget_Void_IntParam { IsNew = true };

        var result = factory.Save(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedValue);
    }

    [Fact]
    public void Save_Update_IntParam_PassesParameter()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_IntParamFactory>();
        var entity = new WriteTarget_Void_IntParam { IsNew = false, IsDeleted = false };

        var result = factory.Save(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedValue);
    }

    [Fact]
    public void Save_Delete_IntParam_PassesParameter()
    {
        var factory = _provider.GetRequiredService<IWriteTarget_Void_IntParamFactory>();
        var entity = new WriteTarget_Void_IntParam { IsDeleted = true };

        var result = factory.Save(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedValue);
    }

    #endregion
}
