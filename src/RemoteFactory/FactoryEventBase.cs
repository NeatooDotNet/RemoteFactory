using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Base record for event objects used with <see cref="IFactoryEvents.Raise{T}"/>.
/// Inherit from this record to define event types that can be published through the mediator.
/// Records are recommended for events (immutable, structural equality).
///
/// <see cref="FactoryEventAttribute"/> is inherited at runtime, making every descendant
/// discoverable by <see cref="FactoryEventTypeRegistry"/>. The
/// <see cref="DynamicallyAccessedMembersAttribute"/> here does NOT preserve descendants'
/// members under IL trimming (DAM does not flow to derived types in ILLink) — descendant
/// preservation comes from the generator-emitted per-assembly event-preservation registrar,
/// which emits DtoConstructorRegistry calls for every concrete, accessible descendant and
/// its nested property graph.
/// </summary>
[FactoryEvent]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract record FactoryEventBase;
