using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestTargets.Visibility;

/// <summary>
/// Test target with all internal factory methods.
/// Generated interface should be internal; all Local* methods should have IsServerRuntime guards.
/// </summary>
[Factory]
public partial class AllInternalTarget
{
    public bool CreateCalled { get; set; }
    public int ReceivedId { get; set; }

    [Create]
    internal void Create()
    {
        CreateCalled = true;
    }

    [Fetch]
    internal void Fetch(int id)
    {
        ReceivedId = id;
    }
}

/// <summary>
/// Test target with all public non-[Remote] factory methods.
/// Generated interface should be public; Local* methods should have NO IsServerRuntime guards.
/// This verifies backward compatibility -- identical behavior to pre-internal-visibility.
/// </summary>
[Factory]
public partial class AllPublicNonRemoteTarget
{
    public bool CreateCalled { get; set; }
    public int ReceivedId { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }

    [Fetch]
    public void Fetch(int id)
    {
        ReceivedId = id;
    }
}

/// <summary>
/// Test target with mixed visibility: one public Create, one internal Fetch.
/// Generated interface should be public but should only contain Create (not Fetch).
/// LocalCreate should have NO guard; LocalFetch should HAVE guard.
/// </summary>
[Factory]
public partial class MixedVisibilityTarget
{
    public bool CreateCalled { get; set; }
    public int ReceivedId { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }

    [Fetch]
    internal void Fetch(int id)
    {
        ReceivedId = id;
    }
}

// --- Internal class with matched public interface targets ---

/// <summary>
/// Public interface for InternalClassTarget (naming convention: strip I to match class name).
/// </summary>
public interface IInternalClassTarget
{
    bool CreateCalled { get; }
    int ReceivedId { get; }
}

/// <summary>
/// Internal class with all-internal factory methods and a matching public interface.
/// Generated factory interface should be internal; return types should use IInternalClassTarget.
/// All Local* methods should have IsServerRuntime guards.
/// </summary>
[Factory]
internal partial class InternalClassTarget : IInternalClassTarget
{
    public bool CreateCalled { get; set; }
    public int ReceivedId { get; set; }

    [Create]
    internal void Create()
    {
        CreateCalled = true;
    }

    [Fetch]
    internal void Fetch(int id)
    {
        ReceivedId = id;
    }
}

/// <summary>
/// Public interface for InternalClassPublicMethods.
/// </summary>
public interface IInternalClassPublicMethods
{
    bool CreateCalled { get; }
}

/// <summary>
/// Internal class with all-public factory methods and a matching public interface (aggregate root pattern).
/// Generated factory interface should be public; return types should use IInternalClassPublicMethods.
/// Local* methods should have NO IsServerRuntime guards.
/// </summary>
[Factory]
internal partial class InternalClassPublicMethods : IInternalClassPublicMethods
{
    public bool CreateCalled { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }
}

/// <summary>
/// Public interface for InternalClassMixed.
/// </summary>
public interface IInternalClassMixed
{
    bool CreateCalled { get; }
    int ReceivedId { get; }
}

/// <summary>
/// Internal class with mixed visibility and a matching public interface.
/// Generated factory interface should be public (has public methods),
/// uses IInternalClassMixed in return types, and only includes Create (not Fetch).
/// LocalCreate should have NO guard; LocalFetch should HAVE guard.
/// </summary>
[Factory]
internal partial class InternalClassMixed : IInternalClassMixed
{
    public bool CreateCalled { get; set; }
    public int ReceivedId { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }

    [Fetch]
    internal void Fetch(int id)
    {
        ReceivedId = id;
    }
}
