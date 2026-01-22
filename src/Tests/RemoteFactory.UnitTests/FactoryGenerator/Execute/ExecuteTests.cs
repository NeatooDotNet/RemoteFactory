using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Execute;

namespace RemoteFactory.UnitTests.FactoryGenerator.Execute;

/// <summary>
/// Unit tests for static [Execute] factory methods.
/// Execute methods generate delegate types that can be resolved from DI.
/// </summary>
/// <remarks>
/// These tests verify that the generated Execute delegate types work correctly
/// in Server mode. Integration tests for client-server round-trips are in IntegrationTests.
/// </remarks>
public class ExecuteTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public ExecuteTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .WithService<IService2, Service2>()
            .WithService<IService3, Service3>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Simple Execute Tests

    [Fact]
    public async Task Execute_Simple_ReturnsTransformedResult()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_Simple.RunOnServer>();

        var result = await del("Hello");

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Execute_Nullable_ReturnsNull()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_Nullable.RunOnServer>();

        var result = await del("Hello");

        Assert.Null(result);
    }

    #endregion

    #region Single Service Tests

    [Fact]
    public async Task Execute_WithService_InjectsService()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_WithService.RunWithService>();

        var result = await del("Hello");

        Assert.Equal("Received: Hello", result);
    }

    #endregion

    #region Multiple Services Tests

    [Fact]
    public async Task Execute_WithTwoServices_InjectsAllServices()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_WithMultipleServices.RunWithTwoServices>();

        var result = await del("Test");

        Assert.Equal("Input: Test, Value: 42", result);
    }

    [Fact]
    public async Task Execute_WithThreeServices_InjectsAllServices()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_WithMultipleServices.RunWithThreeServices>();

        var result = await del("Test");

        Assert.Equal("Input: Test, Value: 42, Name: Service3", result);
    }

    #endregion

    #region Mixed Parameters Tests

    [Fact]
    public async Task Execute_MixedParameters_PassesBusinessParams()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_MixedParameters.ProcessData>();

        var result = await del(5, "TestName");

        Assert.Equal(10, result);
    }

    [Fact]
    public async Task Execute_MixedParametersMultipleServices_PassesAllParams()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_MixedParameters.ProcessDataWithMultipleServices>();

        var result = await del(7, "TestName", true);

        Assert.Equal("Processed: 7, TestName, True, 42", result);
    }

    #endregion

    #region Nullable Return Type Tests

    [Fact]
    public async Task Execute_NullableWithService_ReturnsValue()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_NullableWithService.GetNullableResult>();

        var result = await del(false);

        Assert.Equal("NotNull", result);
    }

    [Fact]
    public async Task Execute_NullableWithService_ReturnsNull()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_NullableWithService.GetNullableResult>();

        var result = await del(true);

        Assert.Null(result);
    }

    #endregion

    #region Service-Only Parameters Tests

    [Fact]
    public async Task Execute_ServiceOnly_SingleService()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_ServiceOnly.GetServiceValue>();

        var result = await del();

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Execute_ServiceOnly_MultipleServices()
    {
        var del = _provider.GetRequiredService<ExecuteTarget_ServiceOnly.GetCombinedServiceValues>();

        var result = await del();

        Assert.Equal("42-Service3", result);
    }

    #endregion
}
