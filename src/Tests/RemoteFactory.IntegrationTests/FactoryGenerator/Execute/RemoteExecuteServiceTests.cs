using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// Integration tests for [Execute] methods with [Service] injection.
/// Tests both local and remote execution paths with service injection.
/// </summary>
/// <remarks>
/// These tests verify:
/// - Single [Service] parameter injection
/// - Multiple [Service] parameters (2-3 services)
/// - Mixed business parameters + [Service] parameters
/// - Async methods (Task&lt;T&gt;) with [Service]
/// - Local and remote execution paths
/// - Services are resolved correctly on server side for remote calls
/// </remarks>
public class RemoteExecuteServiceTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _localScope;

    public RemoteExecuteServiceTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
        _localScope = scopes.local;
    }

    #region Single Service Tests

    [Fact]
    public async Task ExecuteWithSingleService_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteWithSingleService.RunWithService>();

        var result = await del("Hello");

        Assert.Equal("Received: Hello", result);
    }

    [Fact]
    public async Task ExecuteWithSingleService_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteWithSingleService.RunWithServiceRemote>();

        var result = await del("World");

        Assert.Equal("Remote: World", result);
    }

    #endregion

    #region Multiple Services Tests

    [Fact]
    public async Task ExecuteWithTwoServices_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithTwoServices>();

        var result = await del("Test");

        Assert.Equal("Input: Test, Value: 42", result);
    }

    [Fact]
    public async Task ExecuteWithThreeServices_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithThreeServices>();

        var result = await del("Test");

        Assert.Equal("Input: Test, Value: 42, Name: Service3", result);
    }

    [Fact]
    public async Task ExecuteWithTwoServices_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithTwoServicesRemote>();

        var result = await del("Test");

        Assert.Equal("Remote Input: Test, Value: 42", result);
    }

    [Fact]
    public async Task ExecuteWithThreeServices_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithThreeServicesRemote>();

        var result = await del("Test");

        Assert.Equal("Remote Input: Test, Value: 42, Name: Service3", result);
    }

    #endregion

    #region Mixed Parameters Tests

    [Fact]
    public async Task ExecuteWithMixedParameters_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessData>();

        var result = await del(5, "TestName");

        Assert.Equal(10, result);
    }

    [Fact]
    public async Task ExecuteWithMixedParametersAndMultipleServices_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataWithMultipleServices>();

        var result = await del(7, "TestName", true);

        Assert.Equal("Processed: 7, TestName, True, 42", result);
    }

    [Fact]
    public async Task ExecuteWithMixedParameters_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataRemote>();

        var result = await del(5, "TestName");

        Assert.Equal(15, result);
    }

    [Fact]
    public async Task ExecuteWithMixedParametersAndMultipleServices_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataWithMultipleServicesRemote>();

        var result = await del(7, "TestName", true);

        Assert.Equal("Remote Processed: 7, TestName, True, 42", result);
    }

    #endregion

    #region Nullable Return Type Tests

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsValue_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResult>();

        var result = await del(false);

        Assert.Equal("NotNull", result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsNull_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResult>();

        var result = await del(true);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsValue_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResultRemote>();

        var result = await del(false);

        Assert.Equal("NotNull", result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsNull_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResultRemote>();

        var result = await del(true);

        Assert.Null(result);
    }

    #endregion

    #region Service-Only Parameters Tests

    [Fact]
    public async Task ExecuteServiceOnly_SingleService_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetServiceValue>();

        var result = await del();

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteServiceOnly_MultipleServices_LocalExecution()
    {
        var del = _localScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetCombinedServiceValues>();

        var result = await del();

        Assert.Equal("42-Service3", result);
    }

    [Fact]
    public async Task ExecuteServiceOnly_SingleService_RemoteExecution()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetServiceValueRemote>();

        var result = await del();

        Assert.Equal(42, result);
    }

    #endregion
}
