namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Request-scoped service that captures factory events during server-side execution
/// for relay back to the client in <see cref="RemoteResponseDto"/>.
/// </summary>
internal interface IFactoryEventCollector
{
    void Collect(FactoryEventBase factoryEvent);
    IReadOnlyList<FactoryEventBase> GetCollectedEvents();
}

internal sealed class FactoryEventCollector : IFactoryEventCollector
{
    private readonly List<FactoryEventBase> _events = new();

    public void Collect(FactoryEventBase factoryEvent) => _events.Add(factoryEvent);
    public IReadOnlyList<FactoryEventBase> GetCollectedEvents() => _events;
}
