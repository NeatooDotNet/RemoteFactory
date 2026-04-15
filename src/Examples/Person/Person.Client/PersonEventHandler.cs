using MudBlazor;
using Neatoo.RemoteFactory;
using Person.DomainModel;

namespace Person.Client;

/// <summary>
/// IFactoryEventRelay implementation that fans factory events out to MudBlazor snackbar
/// notifications. Register as a singleton against <see cref="IFactoryEventRelay"/>;
/// RemoteFactory invokes <see cref="Relay"/> fire-and-forget after each [Remote] factory
/// call on the client.
/// </summary>
#pragma warning disable CA1711 // Intentional: "EventHandler" is the correct domain term
public sealed class PersonEventHandler : IFactoryEventRelay
#pragma warning restore CA1711
{
    private readonly ISnackbar _snackbar;

    public PersonEventHandler(ISnackbar snackbar)
    {
        ArgumentNullException.ThrowIfNull(snackbar);
        _snackbar = snackbar;
    }

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var evt in events)
        {
            switch (evt)
            {
                case PersonCreatedEvent created:
                    _snackbar.Add($"Inserted Person [{created.Id}]", Severity.Success);
                    break;
                case PersonUpdatedEvent updated:
                    _snackbar.Add($"Updated Person [{updated.Id}]", Severity.Info);
                    break;
                case PersonDeletedEvent deleted:
                    _snackbar.Add($"Deleted Person [{deleted.Id}]", Severity.Warning);
                    break;
            }
        }
        return Task.CompletedTask;
    }
}
