using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Execute;

/// <summary>
/// Basic static [Execute] method returning a non-null string.
/// </summary>
[Factory]
public static partial class ExecuteTarget_Simple
{
    [Execute]
    private static Task<string> _RunOnServer(string message)
    {
        return Task.FromResult(message.ToLower());
    }
}

/// <summary>
/// Static [Execute] method returning a nullable string.
/// </summary>
[Factory]
public static partial class ExecuteTarget_Nullable
{
    [Execute]
    private static Task<string?> _RunOnServer(string message)
    {
        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Static [Execute] method with [Service] parameter.
/// </summary>
[Factory]
public static partial class ExecuteTarget_WithService
{
    [Execute]
    private static Task<string> _RunWithService(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult($"Received: {input}");
    }
}

/// <summary>
/// Static [Execute] method with multiple [Service] parameters.
/// </summary>
[Factory]
public static partial class ExecuteTarget_WithMultipleServices
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
}

/// <summary>
/// Static [Execute] method with mixed business parameters and [Service] parameters.
/// </summary>
[Factory]
public static partial class ExecuteTarget_MixedParameters
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
}

/// <summary>
/// Static [Execute] method with nullable return type and [Service] parameter.
/// </summary>
[Factory]
public static partial class ExecuteTarget_NullableWithService
{
    [Execute]
    private static Task<string?> _GetNullableResult(bool returnNull, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(returnNull ? null : "NotNull");
    }
}

/// <summary>
/// Static [Execute] method with service-only parameters (no business parameters).
/// </summary>
[Factory]
public static partial class ExecuteTarget_ServiceOnly
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
}
