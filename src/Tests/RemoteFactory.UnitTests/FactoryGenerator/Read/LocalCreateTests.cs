using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Read;

namespace RemoteFactory.UnitTests.FactoryGenerator.Read;

/// <summary>
/// Unit tests for [Create] factory methods without [Remote] attribute.
/// These tests verify factory behavior in Server mode with strongly-typed method calls.
/// </summary>
public class LocalCreateTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public LocalCreateTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Void Return Tests

    [Fact]
    public void Create_Void_NoParams_CallsMethod()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Void_NoParamsFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public void Create_Void_IntParam_CallsMethodWithParameter()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Void_IntParamFactory>();

        var result = factory.Create(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedValue);
    }

    [Fact]
    public void Create_Void_ServiceParam_InjectsService()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Void_ServiceParamFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Create_Void_MixedParams_PassesParameterAndInjectsService()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Void_MixedParamsFactory>();

        var result = factory.Create(99);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(99, result.ReceivedValue);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Bool Return Tests

    [Fact]
    public void Create_BoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Bool_NoParamsFactory>();

        var result = factory.CreateBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public void Create_BoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Bool_NoParamsFactory>();

        var result = factory.CreateBoolFalse();

        // When bool returns false, factory returns null
        Assert.Null(result);
    }

    #endregion

    #region Task Return Tests

    [Fact]
    public async Task Create_Task_NoParams_CompletesSuccessfully()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Task_NoParamsFactory>();

        var result = await factory.CreateTask();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion

    #region Task<bool> Return Tests

    [Fact]
    public async Task Create_TaskBoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_TaskBool_NoParamsFactory>();

        var result = await factory.CreateTaskBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public async Task Create_TaskBoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_TaskBool_NoParamsFactory>();

        var result = await factory.CreateTaskBoolFalse();

        // When Task<bool> returns false, factory returns null
        Assert.Null(result);
    }

    #endregion

    #region Constructor Create Tests

    [Fact]
    public void Create_Constructor_NoParams_CreatesViaConstructor()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Constructor_NoParamsFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.ConstructorCalled);
    }

    [Fact]
    public void Create_Constructor_IntParam_CreatesViaConstructorWithParameter()
    {
        var factory = _provider.GetRequiredService<ICreateTarget_Constructor_IntParamFactory>();

        var result = factory.Create(123);

        Assert.NotNull(result);
        Assert.True(result.ConstructorCalled);
        Assert.Equal(123, result.ReceivedValue);
    }

    #endregion
}
