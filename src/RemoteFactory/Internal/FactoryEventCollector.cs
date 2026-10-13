namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Request-scoped service that captures factory events during server-side execution
/// for relay back to the client in <see cref="RemoteResponseDto"/>.
/// </summary>
/// <remarks>
/// Scoped, but a scope can serve more than one remote call (ASP.NET gives each request
/// its own; an in-process host may not), so <see cref="HandleRemoteDelegateRequest"/>
/// clears it as each call begins: a response carries only the events its own call raised.
/// </remarks>
internal interface IFactoryEventCollector
{
    void Collect(FactoryEventBase factoryEvent);
    IReadOnlyList<FactoryEventBase> GetCollectedEvents();
    void Clear();
}

internal sealed class FactoryEventCollector : IFactoryEventCollector
{
    private readonly List<FactoryEventBase> _events = new();

    public void Collect(FactoryEventBase factoryEvent) => _events.Add(factoryEvent);
    public IReadOnlyList<FactoryEventBase> GetCollectedEvents() => _events;
    public void Clear() => _events.Clear();
}
