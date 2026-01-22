using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Static [Execute] with single [Service] - both local and remote variants.
/// </summary>
[Factory]
public static partial class ExecuteWithSingleService
{
    [Execute]
    private static Task<string> _RunWithService(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult($"Received: {input}");
    }

    [Execute]
    [Remote]
    private static Task<string> _RunWithServiceRemote(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult($"Remote: {input}");
    }
}

/// <summary>
/// Static [Execute] with multiple [Service] parameters - both local and remote variants.
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
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        return Task.FromResult($"Input: {input}, Value: {service2.GetValue()}");
    }

    [Execute]
    private static Task<string> _RunWithThreeServices(
        string input,
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        if (service3 == null)
            throw new InvalidOperationException("Service3 was not injected");
        return Task.FromResult($"Input: {input}, Value: {service2.GetValue()}, Name: {service3.GetName()}");
    }

    [Execute]
    [Remote]
    private static Task<string> _RunWithTwoServicesRemote(
        string input,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
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
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        if (service3 == null)
            throw new InvalidOperationException("Service3 was not injected");
        return Task.FromResult($"Remote Input: {input}, Value: {service2.GetValue()}, Name: {service3.GetName()}");
    }
}

/// <summary>
/// Static [Execute] with mixed business parameters and [Service] parameters.
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
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        if (name != "TestName")
            throw new InvalidOperationException($"Expected 'TestName', got '{name}'");
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
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        if (name != "TestName")
            throw new InvalidOperationException($"Expected 'TestName', got '{name}'");
        if (!flag)
            throw new InvalidOperationException("Expected flag to be true");
        return Task.FromResult($"Processed: {id}, {name}, {flag}, {service2.GetValue()}");
    }

    [Execute]
    [Remote]
    private static Task<int> _ProcessDataRemote(
        int id,
        string name,
        [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        if (name != "TestName")
            throw new InvalidOperationException($"Expected 'TestName', got '{name}'");
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
        if (service1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        if (name != "TestName")
            throw new InvalidOperationException($"Expected 'TestName', got '{name}'");
        if (!flag)
            throw new InvalidOperationException("Expected flag to be true");
        return Task.FromResult($"Remote Processed: {id}, {name}, {flag}, {service2.GetValue()}");
    }
}

/// <summary>
/// Static [Execute] with nullable return types and [Service] parameters.
/// </summary>
[Factory]
public static partial class ExecuteNullableWithService
{
    [Execute]
    private static Task<string?> _GetNullableResult(bool returnNull, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(returnNull ? null : "NotNull");
    }

    [Execute]
    [Remote]
    private static Task<string?> _GetNullableResultRemote(bool returnNull, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(returnNull ? null : "NotNull");
    }
}

/// <summary>
/// Static [Execute] with service-only parameters (no business parameters).
/// </summary>
[Factory]
public static partial class ExecuteServiceOnly
{
    [Execute]
    private static Task<int> _GetServiceValue([Service] IService2 service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(service.GetValue());
    }

    [Execute]
    private static Task<string> _GetCombinedServiceValues(
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        if (service2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        if (service3 == null)
            throw new InvalidOperationException("Service3 was not injected");
        return Task.FromResult($"{service2.GetValue()}-{service3.GetName()}");
    }

    [Execute]
    [Remote]
    private static Task<int> _GetServiceValueRemote([Service] IService2 service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(service.GetValue());
    }
}
