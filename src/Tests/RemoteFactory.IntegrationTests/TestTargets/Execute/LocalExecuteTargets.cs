using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Static [Execute] WITHOUT [Remote]: local-only delegates (EXRM-001).
/// </summary>
/// <remarks>
/// A bare [Execute] obeys [Remote] like every other operation. The generator emits one unguarded
/// local registration in every factory mode and no remote registration, so the delegate runs
/// wherever it is called and resolves its [Service] parameters from that container. On the
/// client container that is client DI: <see cref="IService"/> resolves there (RegisterMatchingName
/// maps it), <see cref="IServerOnlyService"/> does not (its implementation is named ServerOnly on
/// purpose), which is the negative control. <c>RunRemote</c> is the [Remote] control that must
/// still cross the wire.
/// </remarks>
[Factory]
public static partial class LocalExecuteTarget
{
    /// <summary>Bare [Execute] taking a service the client container can resolve.</summary>
    [Execute]
    private static Task<string> _RunLocal(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult($"Local: {input}");
    }

    /// <summary>Bare [Execute] taking a server-only service: on the client this fails at call time.</summary>
    [Execute]
    private static Task<string> _RunLocalServerOnly(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult($"LocalServerOnly: {service.ServerOnlyValue}: {input}");
    }

    /// <summary>Bare [Execute] with no services: resolvable and callable in every factory mode.</summary>
    [Execute]
    private static Task<int> _RunLocalNoServices(int value)
    {
        return Task.FromResult(value * 2);
    }

    /// <summary>[Remote] control: same shape, must still execute on the server.</summary>
    [Remote]
    [Execute]
    private static Task<string> _RunRemote(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult($"Remote: {service.ServerOnlyValue}: {input}");
    }
}
