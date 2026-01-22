using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Read;

namespace RemoteFactory.UnitTests.FactoryGenerator.Read;

/// <summary>
/// Unit tests for [Fetch] factory methods without [Remote] attribute.
/// These tests verify factory behavior in Server mode with strongly-typed method calls.
/// </summary>
public class LocalFetchTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public LocalFetchTests()
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
    public void Fetch_Void_NoParams_CallsMethod()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Void_NoParamsFactory>();

        var result = factory.Fetch();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    [Fact]
    public void Fetch_Void_IntParam_CallsMethodWithParameter()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Void_IntParamFactory>();

        var result = factory.Fetch(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedId);
    }

    [Fact]
    public void Fetch_Void_ServiceParam_InjectsService()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Void_ServiceParamFactory>();

        var result = factory.Fetch();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Fetch_Void_MixedParams_PassesParameterAndInjectsService()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Void_MixedParamsFactory>();

        var result = factory.Fetch(99);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(99, result.ReceivedId);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Bool Return Tests

    [Fact]
    public void Fetch_BoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Bool_NoParamsFactory>();

        var result = factory.FetchBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    [Fact]
    public void Fetch_BoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Bool_NoParamsFactory>();

        var result = factory.FetchBoolFalse();

        // When bool returns false, factory returns null
        Assert.Null(result);
    }

    #endregion

    #region Task Return Tests

    [Fact]
    public async Task Fetch_Task_NoParams_CompletesSuccessfully()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Task_NoParamsFactory>();

        var result = await factory.FetchTask();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    #endregion

    #region Task<bool> Return Tests

    [Fact]
    public async Task Fetch_TaskBoolTrue_ReturnsResult()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_TaskBool_NoParamsFactory>();

        var result = await factory.FetchTaskBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    [Fact]
    public async Task Fetch_TaskBoolFalse_ReturnsNull()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_TaskBool_NoParamsFactory>();

        var result = await factory.FetchTaskBoolFalse();

        // When Task<bool> returns false, factory returns null
        Assert.Null(result);
    }

    #endregion

    #region Constructor Fetch Tests

    [Fact]
    public void Fetch_Constructor_IntParam_CreatesViaConstructorWithParameter()
    {
        var factory = _provider.GetRequiredService<IFetchTarget_Constructor_IntParamFactory>();

        var result = factory.Fetch(123);

        Assert.NotNull(result);
        Assert.True(result.ConstructorCalled);
        Assert.Equal(123, result.ReceivedId);
    }

    #endregion
}
