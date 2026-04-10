using MudBlazor;
using Neatoo.RemoteFactory;
using Person.DomainModel;

namespace Person.Client;

[FactoryEventHandler<PersonCreatedEvent>]
[FactoryEventHandler<PersonUpdatedEvent>]
[FactoryEventHandler<PersonDeletedEvent>]
#pragma warning disable CA1711 // Intentional: "EventHandler" is the correct domain term
public sealed partial class PersonEventHandler : IDisposable
#pragma warning restore CA1711
{
    private readonly ISnackbar _snackbar;
    private readonly IFactoryEventRelay _relay;

    public PersonEventHandler(ISnackbar snackbar, IFactoryEventRelay relay)
    {
        ArgumentNullException.ThrowIfNull(snackbar);
        ArgumentNullException.ThrowIfNull(relay);
        _snackbar = snackbar;
        _relay = relay;
        _relay.Register(this);
    }

    public Task HandleCreated(PersonCreatedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _snackbar.Add($"Inserted Person [{evt.Id}]", Severity.Success);
        return Task.CompletedTask;
    }

    public Task HandleUpdated(PersonUpdatedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _snackbar.Add($"Updated Person [{evt.Id}]", Severity.Info);
        return Task.CompletedTask;
    }

    public Task HandleDeleted(PersonDeletedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        _snackbar.Add($"Deleted Person [{evt.Id}]", Severity.Warning);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _relay.Unregister(this);
    }
}
