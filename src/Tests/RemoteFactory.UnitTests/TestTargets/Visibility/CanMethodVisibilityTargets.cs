using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestTargets.Visibility;

/// <summary>
/// Authorization class for testing Can method visibility behavior.
/// </summary>
public class VisibilityTestAuth
{
    public static bool ShouldAllow { get; set; } = true;

    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Read)]
    public bool CanAccess()
    {
        return ShouldAllow;
    }
}

/// <summary>
/// Test target with public Create and authorization.
/// CanCreate should have NO IsServerRuntime guard (public method => no guard on Can).
/// </summary>
[Factory]
[AuthorizeFactory<VisibilityTestAuth>]
public partial class PublicMethodWithAuth
{
    public bool CreateCalled { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }
}

/// <summary>
/// Test target with internal Create and authorization.
/// CanCreate should HAVE IsServerRuntime guard (internal method => guard on Can).
/// </summary>
[Factory]
[AuthorizeFactory<VisibilityTestAuth>]
public partial class InternalMethodWithAuth
{
    public bool CreateCalled { get; set; }

    [Create]
    internal void Create()
    {
        CreateCalled = true;
    }
}
