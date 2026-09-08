using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Client-side authorization for a bare class-level [Execute] (EXRM-002, AC-4). The auth method
/// carries no [Remote], so the check runs wherever the factory runs — on the client, from client
/// DI. Own flag rather than <c>EnforcementTestAuth</c> so parallel test classes cannot interfere.
/// </summary>
public class LocalExecAuth
{
    public static bool ShouldAllow { get; set; } = true;

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanRun() => ShouldAllow;
}

/// <summary>
/// [Execute] under a client-side [AuthorizeFactory]. <c>Run</c> is bare and stays local, allowed
/// or denied; <c>RunRemote</c> is the [Remote] sibling, whose auth check runs on the server.
/// </summary>
/// <remarks>
/// Both pin the generator fix from EXRM-002: a class-level [Execute] under authorization
/// declares its factory method nullable (<c>Task&lt;T?&gt;</c>), because a denied check returns
/// <c>Authorized&lt;T&gt;.Result</c>, which is default. Before the fix the signature was
/// non-nullable and the generated file failed CS8603 under TreatWarningsAsErrors.
/// </remarks>
[Factory]
[AuthorizeFactory<LocalExecAuth>]
public partial class ClassExecLocalAuth
{
    public string Result { get; set; } = string.Empty;

    public ClassExecLocalAuth() { }

    [Execute]
    public static Task<ClassExecLocalAuth> Run(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(new ClassExecLocalAuth { Result = $"Authorized: {input}" });
    }

    [Remote, Execute]
    public static Task<ClassExecLocalAuth> RunRemote(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult(new ClassExecLocalAuth { Result = $"AuthorizedRemote: {service.ServerOnlyValue}: {input}" });
    }
}

/// <summary>
/// [Remote] authorization for a bare [Execute]: the auth method forces the call to the server by
/// the same rule as Create/Fetch. The implementation is registered on the server only (see the
/// test), mirroring <c>ShowcaseAuthRemoteTests</c>.
/// </summary>
public interface IExecAuthRemote
{
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanRun();
}

internal class ExecAuthServerOnly : IExecAuthRemote
{
    public ExecAuthServerOnly([Service] IServerOnlyService service)
    {
        if (service == null)
            throw new InvalidOperationException("Server-only service was not injected");
    }

    public bool CanRun() => true;
}

/// <summary>Bare [Execute] whose [Remote] auth method forces the wire.</summary>
[Factory]
[AuthorizeFactory<IExecAuthRemote>]
public partial class ClassExecRemoteAuth
{
    public string Result { get; set; } = string.Empty;

    public ClassExecRemoteAuth() { }

    [Execute]
    public static Task<ClassExecRemoteAuth> Run(string input)
    {
        return Task.FromResult(new ClassExecRemoteAuth { Result = $"RemoteAuth: {input}" });
    }
}

/// <summary>
/// Bare [Execute] under [AspAuthorize]: forced to the server, where <c>IAspAuthorize</c> is
/// consulted before the method runs. The server-only [Service] doubles as proof it ran there.
/// </summary>
[Factory]
public partial class ClassExecAspAuth
{
    public string Result { get; set; } = string.Empty;

    public ClassExecAspAuth() { }

    [Execute]
    [AspAuthorize("ExecutePolicy")]
    public static Task<ClassExecAspAuth> Run(string input, [Service] IServerOnlyService service)
    {
        return Task.FromResult(new ClassExecAspAuth { Result = $"Asp: {service.ServerOnlyValue}: {input}" });
    }
}
