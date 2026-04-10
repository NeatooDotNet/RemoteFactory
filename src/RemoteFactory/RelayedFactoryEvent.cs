namespace Neatoo.RemoteFactory;

/// <summary>
/// Transport DTO carrying a single factory event from server to client.
/// Contains the event's full type name and its JSON-serialized payload.
/// </summary>
public class RelayedFactoryEvent
{
    public string TypeFullName { get; set; } = null!;
    public string Json { get; set; } = null!;
}
