using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Client-side integration hook for receiving factory events relayed from the server.
/// Consumers implement this interface to bridge events to their own event aggregator
/// (MediatR, plain aggregator, UI message bus, etc.) and own any threading / SyncContext
/// marshaling inside <see cref="Relay"/>.
///
/// RemoteFactory guarantees <see cref="Relay"/> is invoked fire-and-forget strictly
/// <b>after</b> the factory method returns to its caller and the caller's continuation
/// has resumed. Exceptions thrown by <see cref="Relay"/> are isolated — they are caught
/// and logged, and never propagate to the factory caller.
///
/// One <c>[Remote]</c> factory call produces exactly one <see cref="Relay"/> invocation
/// (the batch may be empty). When deserialization of a relayed event fails, the batch
/// is aborted and <see cref="Relay"/> is not invoked for that call.
/// </summary>
public interface IFactoryEventRelay
{
    /// <summary>
    /// Receive the batch of events captured during the preceding <c>[Remote]</c> factory call.
    /// Invoked fire-and-forget after the caller's continuation has resumed.
    /// </summary>
    /// <param name="events">Fully deserialized events in the order they were raised on the server.</param>
    Task Relay(IReadOnlyList<FactoryEventBase> events);
}
