using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Default <see cref="IFactoryEventRelay"/> registered in Remote mode when the consumer
/// does not supply their own implementation. Drops every relayed batch.
///
/// To surface the "silent drop" footgun, the no-op logs once per process on its first
/// invocation: if a consumer expected factory events on the client and forgot to
/// register a relay, a single warning points at the right knob.
/// </summary>
internal sealed class NoOpFactoryEventRelay : IFactoryEventRelay
{
    private readonly ILogger _logger;
    private int _loggedFirstCall;

    public NoOpFactoryEventRelay(ILogger<NoOpFactoryEventRelay>? logger = null)
    {
        _logger = (ILogger?)logger ?? NullLogger.Instance;
    }

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        if (events is { Count: > 0 }
            && Interlocked.Exchange(ref _loggedFirstCall, 1) == 0)
        {
            _logger.NoOpFactoryEventRelayFirstEvent(events.Count);
        }
        return Task.CompletedTask;
    }
}
