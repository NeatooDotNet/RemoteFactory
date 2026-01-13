using Neatoo.RemoteFactory;

namespace RemoteOnlyTests.Domain;

/// <summary>
/// Test child entity demonstrating local Create pattern.
/// Local [Create] works on both client and server.
/// Server-only [Fetch] loads from data.
/// </summary>
[Factory]
public class TestChild : ITestChild
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Value { get; set; }

    /// <summary>
    /// Local Create - works on both client and server (no [Remote]).
    /// </summary>
    [Create]
    public TestChild()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    /// <summary>
    /// Server-only Fetch - loads child from data.
    /// </summary>
    [Fetch]
    public void Fetch(Guid id, string name, decimal value)
    {
        Id = id;
        Name = name;
        Value = value;
    }
#endif
}
