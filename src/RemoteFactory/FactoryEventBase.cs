namespace Neatoo.RemoteFactory;

/// <summary>
/// Base record for event objects used with <see cref="IFactoryEvents.Raise{T}"/>.
/// Inherit from this record to define event types that can be published through the mediator.
/// Records are recommended for events (immutable, structural equality).
/// </summary>
public abstract record FactoryEventBase;
