using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestTargets.LazyLoad;

/// <summary>
/// Test target for ordinal serialization of a [Factory] class with a LazyLoad&lt;T&gt; property.
/// Validates two-slot encoding: the LazyLoad property occupies two ordinal array slots
/// (Value + IsLoaded) alongside a regular property.
/// </summary>
[Factory]
public partial class LazyLoadOrdinalTarget
{
    public string Name { get; set; } = "";
    public LazyLoad<string> Lines { get; set; } = new LazyLoad<string>();

    [Create]
    public static LazyLoadOrdinalTarget Create(string name, LazyLoad<string> lines)
        => new LazyLoadOrdinalTarget { Name = name, Lines = lines };
}
