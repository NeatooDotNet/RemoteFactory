using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Base record for event objects used with <see cref="IFactoryEvents.Raise{T}"/>.
/// Inherit from this record to define event types that can be published through the mediator.
/// Records are recommended for events (immutable, structural equality).
///
/// <see cref="FactoryEventAttribute"/> and <see cref="DynamicallyAccessedMembersAttribute"/>
/// are applied here with <c>Inherited = true</c>, so every descendant is automatically
/// discoverable by <see cref="FactoryEventTypeRegistry"/> and its constructors and properties
/// are preserved through IL trimming.
/// </summary>
[FactoryEvent]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract record FactoryEventBase;
