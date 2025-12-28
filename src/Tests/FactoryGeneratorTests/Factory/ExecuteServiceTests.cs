using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for [Execute] methods with [Service] injection.
/// This addresses GAP-005 from the test plan: Execute with Service Parameters - completely untested.
///
/// Tests verify:
/// - Single [Service] parameter injection
/// - Multiple [Service] parameters (2-3 services)
/// - Mixed business parameters + [Service] parameters
/// - Async methods (Task<T>) with [Service]
/// - Local and remote execution paths
/// - Services are resolved correctly on server side for remote calls
/// </summary>

#region Service Interfaces for Testing

/// <summary>
/// Second service interface for testing multiple service injection.
/// </summary>
public interface IService2
{
    int GetValue();
}

/// <summary>
/// Third service interface for testing multiple service injection.
/// </summary>
public interface IService3
{
    string GetName();
}

/// <summary>
/// Implementation of IService2.
/// </summary>
public class Service2 : IService2
{
    public int GetValue() => 42;
}

/// <summary>
/// Implementation of IService3.
/// </summary>
public class Service3 : IService3
{
    public string GetName() => "Service3";
}

#endregion

#region Static Execute Classes

/// <summary>
/// Static partial class testing [Execute] with single [Service] parameter.
/// </summary>
[Factory]
public static partial class ExecuteWithSingleService
{
    [Execute]
    private static Task<string> _RunWithService(string input, [Service] IService service)
    {
        Assert.NotNull(service);
        return Task.FromResult($"Received: {input}");
    }

    [Execute]
    [Remote]
    private static Task<string> _RunWithServiceRemote(string input, [Service] IService service)
    {
        Assert.NotNull(service);
        return Task.FromResult($"Remote: {input}");
    }
}

/// <summary>
/// Static partial class testing [Execute] with multiple [Service] parameters.
/// </summary>
[Factory]
public static partial class ExecuteWithMultipleServices
{
    [Execute]
    private static Task<string> _RunWithTwoServices(
        string input,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        return Task.FromResult($"Input: {input}, Value: {service2.GetValue()}");
    }

    [Execute]
    private static Task<string> _RunWithThreeServices(
        string input,
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        return Task.FromResult($"Input: {input}, Value: {service2.GetValue()}, Name: {service3.GetName()}");
    }

    [Execute]
    [Remote]
    private static Task<string> _RunWithTwoServicesRemote(
        string input,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        return Task.FromResult($"Remote Input: {input}, Value: {service2.GetValue()}");
    }

    [Execute]
    [Remote]
    private static Task<string> _RunWithThreeServicesRemote(
        string input,
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        return Task.FromResult($"Remote Input: {input}, Value: {service2.GetValue()}, Name: {service3.GetName()}");
    }
}

/// <summary>
/// Static partial class testing [Execute] with mixed business parameters and [Service] parameters.
/// Parameter ordering is preserved with business params first, then services.
/// </summary>
[Factory]
public static partial class ExecuteWithMixedParameters
{
    [Execute]
    private static Task<int> _ProcessData(
        int id,
        string name,
        [Service] IService service)
    {
        Assert.NotNull(service);
        Assert.Equal("TestName", name);
        return Task.FromResult(id * 2);
    }

    [Execute]
    private static Task<string> _ProcessDataWithMultipleServices(
        int id,
        string name,
        bool flag,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal("TestName", name);
        Assert.True(flag);
        return Task.FromResult($"Processed: {id}, {name}, {flag}, {service2.GetValue()}");
    }

    [Execute]
    [Remote]
    private static Task<int> _ProcessDataRemote(
        int id,
        string name,
        [Service] IService service)
    {
        Assert.NotNull(service);
        Assert.Equal("TestName", name);
        return Task.FromResult(id * 3);
    }

    [Execute]
    [Remote]
    private static Task<string> _ProcessDataWithMultipleServicesRemote(
        int id,
        string name,
        bool flag,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal("TestName", name);
        Assert.True(flag);
        return Task.FromResult($"Remote Processed: {id}, {name}, {flag}, {service2.GetValue()}");
    }
}

/// <summary>
/// Static partial class testing [Execute] with nullable return types and [Service] parameters.
/// </summary>
[Factory]
public static partial class ExecuteNullableWithService
{
    [Execute]
    private static Task<string?> _GetNullableResult(bool returnNull, [Service] IService service)
    {
        Assert.NotNull(service);
        return Task.FromResult(returnNull ? null : "NotNull");
    }

    [Execute]
    [Remote]
    private static Task<string?> _GetNullableResultRemote(bool returnNull, [Service] IService service)
    {
        Assert.NotNull(service);
        return Task.FromResult(returnNull ? null : "NotNull");
    }
}

/// <summary>
/// Static partial class testing [Execute] with service-only parameters (no business parameters).
/// </summary>
[Factory]
public static partial class ExecuteServiceOnly
{
    [Execute]
    private static Task<int> _GetServiceValue([Service] IService2 service)
    {
        Assert.NotNull(service);
        return Task.FromResult(service.GetValue());
    }

    [Execute]
    private static Task<string> _GetCombinedServiceValues(
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        return Task.FromResult($"{service2.GetValue()}-{service3.GetName()}");
    }

    [Execute]
    [Remote]
    private static Task<int> _GetServiceValueRemote([Service] IService2 service)
    {
        Assert.NotNull(service);
        return Task.FromResult(service.GetValue());
    }
}

#endregion

#region Test Class

public class ExecuteServiceTests
{
    private readonly IServiceScope clientScope;
    private readonly IServiceScope localScope;

    public ExecuteServiceTests()
    {
        var scopes = ClientServerContainers.Scopes();
        this.clientScope = scopes.client;
        this.localScope = scopes.local;
    }

    #region Single Service Tests

    [Fact]
    public async Task ExecuteWithSingleService_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteWithSingleService.RunWithService>();
        var result = await del("Hello");
        Assert.Equal("Received: Hello", result);
    }

    [Fact]
    public async Task ExecuteWithSingleService_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteWithSingleService.RunWithServiceRemote>();
        var result = await del("World");
        Assert.Equal("Remote: World", result);
    }

    #endregion

    #region Multiple Services Tests

    [Fact]
    public async Task ExecuteWithTwoServices_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithTwoServices>();
        var result = await del("Test");
        Assert.Equal("Input: Test, Value: 42", result);
    }

    [Fact]
    public async Task ExecuteWithThreeServices_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithThreeServices>();
        var result = await del("Test");
        Assert.Equal("Input: Test, Value: 42, Name: Service3", result);
    }

    [Fact]
    public async Task ExecuteWithTwoServices_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithTwoServicesRemote>();
        var result = await del("Test");
        Assert.Equal("Remote Input: Test, Value: 42", result);
    }

    [Fact]
    public async Task ExecuteWithThreeServices_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteWithMultipleServices.RunWithThreeServicesRemote>();
        var result = await del("Test");
        Assert.Equal("Remote Input: Test, Value: 42, Name: Service3", result);
    }

    #endregion

    #region Mixed Parameters Tests

    [Fact]
    public async Task ExecuteWithMixedParameters_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessData>();
        var result = await del(5, "TestName");
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task ExecuteWithMixedParametersAndMultipleServices_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataWithMultipleServices>();
        var result = await del(7, "TestName", true);
        Assert.Equal("Processed: 7, TestName, True, 42", result);
    }

    [Fact]
    public async Task ExecuteWithMixedParameters_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataRemote>();
        var result = await del(5, "TestName");
        Assert.Equal(15, result);
    }

    [Fact]
    public async Task ExecuteWithMixedParametersAndMultipleServices_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteWithMixedParameters.ProcessDataWithMultipleServicesRemote>();
        var result = await del(7, "TestName", true);
        Assert.Equal("Remote Processed: 7, TestName, True, 42", result);
    }

    #endregion

    #region Nullable Return Type Tests

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsValue_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResult>();
        var result = await del(false);
        Assert.Equal("NotNull", result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsNull_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResult>();
        var result = await del(true);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsValue_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResultRemote>();
        var result = await del(false);
        Assert.Equal("NotNull", result);
    }

    [Fact]
    public async Task ExecuteNullableWithService_ReturnsNull_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteNullableWithService.GetNullableResultRemote>();
        var result = await del(true);
        Assert.Null(result);
    }

    #endregion

    #region Service-Only Parameters Tests

    [Fact]
    public async Task ExecuteServiceOnly_SingleService_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetServiceValue>();
        var result = await del();
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteServiceOnly_MultipleServices_LocalExecution()
    {
        var del = this.localScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetCombinedServiceValues>();
        var result = await del();
        Assert.Equal("42-Service3", result);
    }

    [Fact]
    public async Task ExecuteServiceOnly_SingleService_RemoteExecution()
    {
        var del = this.clientScope.ServiceProvider.GetRequiredService<ExecuteServiceOnly.GetServiceValueRemote>();
        var result = await del();
        Assert.Equal(42, result);
    }

    #endregion
}

#endregion
