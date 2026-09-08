using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Execute;

/// <summary>
/// Authorization class for class-level [Execute] targets.
/// </summary>
public class ClassExecAuth
{
    public static bool ShouldAllow { get; set; } = true;

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecute() => ShouldAllow;
}

/// <summary>
/// [Execute] on a class factory under [AuthorizeFactory] (EXRM-002).
/// </summary>
/// <remarks>
/// The shape that exposed the nullability bug: an authorized Execute's public factory method
/// returns <c>Authorized&lt;T&gt;.Result</c>, which is default when the check denies, so the
/// method must be declared <c>Task&lt;T?&gt;</c>. Before the fix it was declared
/// <c>Task&lt;T&gt;</c> and the generated file failed CS8603 under TreatWarningsAsErrors —
/// which is why no target in the repo had ever combined class Execute with authorization.
/// </remarks>
[Factory]
[AuthorizeFactory<ClassExecAuth>]
public partial class ClassExecWithAuth
{
    public string Name { get; set; } = string.Empty;

    public ClassExecWithAuth() { }

    [Execute]
    public static Task<ClassExecWithAuth> Run(string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        return Task.FromResult(new ClassExecWithAuth { Name = $"Executed: {input}" });
    }
}
