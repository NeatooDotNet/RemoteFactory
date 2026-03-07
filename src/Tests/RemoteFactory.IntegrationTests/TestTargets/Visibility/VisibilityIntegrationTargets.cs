using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Visibility;

/// <summary>
/// Test target with public non-[Remote] Create method.
/// Should work on both client and server without a server trip.
/// </summary>
[Factory]
public partial class PublicLocalCreateTarget
{
    public bool CreateCalled { get; set; }
    public string ReceivedName { get; set; } = string.Empty;

    [Create]
    public void Create(string name)
    {
        CreateCalled = true;
        ReceivedName = name;
    }
}

/// <summary>
/// Test target with internal Create method.
/// Should work on the server (IsServerRuntime guard passes)
/// but would fail on client (if someone tried to call LocalCreate directly).
/// </summary>
[Factory]
public partial class InternalCreateTarget
{
    public bool CreateCalled { get; set; }
    public string ReceivedName { get; set; } = string.Empty;

    [Create]
    internal void Create(string name)
    {
        CreateCalled = true;
        ReceivedName = name;
    }
}

/// <summary>
/// Authorization class for testing Can method visibility in integration tests.
/// </summary>
public class IntegrationVisibilityAuth
{
    public static bool ShouldAllow { get; set; } = true;

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanCreate()
    {
        return ShouldAllow;
    }
}

/// <summary>
/// Test target with public Create and authorization.
/// CanCreate should work on client without server trip.
/// </summary>
[Factory]
[AuthorizeFactory<IntegrationVisibilityAuth>]
public partial class PublicCreateWithAuthTarget
{
    public bool CreateCalled { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }
}
