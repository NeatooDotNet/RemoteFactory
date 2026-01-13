using Neatoo.RemoteFactory;

namespace RemoteOnlyTests.Domain;

/// <summary>
/// Test child collection demonstrating local Create and server-only Fetch.
/// </summary>
[Factory]
public class TestChildList : List<ITestChild>, ITestChildList
{
    /// <summary>
    /// Local Create - works on both client and server.
    /// </summary>
    [Create]
    public TestChildList()
    {
    }

#if !CLIENT
    /// <summary>
    /// Server-only Fetch - loads children from data.
    /// </summary>
    [Fetch]
    public void Fetch(
        IEnumerable<(Guid id, string name, decimal value)> childData,
        [Service] ITestChildFactory childFactory)
    {
        foreach (var (id, name, value) in childData)
        {
            var child = childFactory.Fetch(id, name, value);
            Add(child);
        }
    }
#endif
}
