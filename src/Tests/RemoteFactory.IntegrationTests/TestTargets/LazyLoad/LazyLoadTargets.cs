using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.LazyLoad;

/// <summary>
/// Test target for client-server round-trip of a [Factory] class with a loaded LazyLoad&lt;T&gt; property.
/// The [Remote, Fetch] method creates the object on the server with a loaded LazyLoad,
/// which is then serialized back to the client.
/// </summary>
[Factory]
public partial class LazyLoadRoundTrip_Loaded
{
    public string Name { get; set; } = "";
    public LazyLoad<string> Lines { get; set; } = new LazyLoad<string>();

    [Fetch]
    [Remote]
    internal void Fetch()
    {
        Name = "ServerData";
        Lines = new LazyLoad<string>("loaded-from-server");
    }
}

/// <summary>
/// Test target for client-server round-trip of a [Factory] class with an unloaded LazyLoad&lt;T&gt; property.
/// The [Remote, Fetch] method creates the object on the server with an unloaded LazyLoad
/// (parameterless constructor), which is then serialized back to the client.
/// </summary>
[Factory]
public partial class LazyLoadRoundTrip_Unloaded
{
    public string Name { get; set; } = "";
    public LazyLoad<string> Lines { get; set; } = new LazyLoad<string>();

    [Fetch]
    [Remote]
    internal void Fetch()
    {
        Name = "ServerData";
        // Lines stays as unloaded (parameterless LazyLoad)
    }
}
