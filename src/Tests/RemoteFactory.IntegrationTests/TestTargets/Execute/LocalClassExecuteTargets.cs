using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Class-level [Execute] WITHOUT [Remote]: local-only factory methods (EXRM-002).
/// </summary>
/// <remarks>
/// A bare [Execute] on a class factory obeys [Remote] like every other operation. The generated
/// Local method is unguarded and resolves its [Service] parameters from the factory's own
/// container, so it runs wherever the factory is resolved. On the client that is client DI:
/// <see cref="IService"/> resolves there (RegisterMatchingName maps it), <see cref="IServerOnlyService"/>
/// does not (its implementation is named ServerOnly on purpose), which is the negative control.
/// <c>RunRemote</c> is the [Remote] control that must still cross the wire. Mirrors
/// <see cref="ClassExecMulti"/>: no matching interface, result carried in a property.
/// </remarks>
[Factory]
public partial class ClassExecLocal
{
    public string Result { get; set; } = string.Empty;

    public ClassExecLocal() { }

    /// <summary>Bare [Execute] taking a service the client container can resolve.</summary>
    [Execute]
    public static Task<ClassExecLocal> RunLocal(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(new ClassExecLocal { Result = $"Local: {input}" });
    }

    /// <summary>Bare [Execute] taking a server-only service: on the client this fails at call time.</summary>
    [Execute]
    public static Task<ClassExecLocal> RunLocalServerOnly(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult(new ClassExecLocal { Result = $"LocalServerOnly: {service.ServerOnlyValue}: {input}" });
    }

    /// <summary>Bare [Execute] with no services: resolvable and callable in every factory mode.</summary>
    [Execute]
    public static Task<ClassExecLocal> RunLocalNoServices(string input)
    {
        return Task.FromResult(new ClassExecLocal { Result = $"NoServices: {input}" });
    }

    /// <summary>[Remote] control: same shape, must still execute on the server.</summary>
    [Remote, Execute]
    public static Task<ClassExecLocal> RunRemote(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult(new ClassExecLocal { Result = $"Remote: {service.ServerOnlyValue}: {input}" });
    }
}
