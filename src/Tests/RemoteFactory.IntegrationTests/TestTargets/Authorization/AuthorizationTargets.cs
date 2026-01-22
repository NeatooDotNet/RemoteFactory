using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Authorization;

/// <summary>
/// Authorization class that can be configured to allow or deny access.
/// </summary>
public class EnforcementTestAuth
{
    public static bool ShouldAllow { get; set; } = true;

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute | AuthorizeFactoryOperation.Read)]
    public bool CanExecute()
    {
        return ShouldAllow;
    }
}

/// <summary>
/// Class-based factory target with authorization.
/// </summary>
[Factory]
[AuthorizeFactory<EnforcementTestAuth>]
public class ClassBasedAuthTarget
{
    public string Value { get; }

    [Fetch]
    public ClassBasedAuthTarget()
    {
        Value = "Fetched";
    }
}

/// <summary>
/// Interface-based factory target with authorization.
/// </summary>
[Factory]
[AuthorizeFactory<EnforcementTestAuth>]
public interface IInterfaceBasedAuthTarget
{
    Task<string> GetValue();
}

/// <summary>
/// Named to match IInterfaceBasedAuthTarget for RegisterMatchingName pattern.
/// </summary>
public class InterfaceBasedAuthTarget : IInterfaceBasedAuthTarget
{
    public Task<string> GetValue()
    {
        return Task.FromResult("FromInterface");
    }
}
